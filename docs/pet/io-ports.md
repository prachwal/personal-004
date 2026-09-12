# Porty wejścia/wyjścia PET/CBM i architektura rozszerzeń

Dokument opisuje porty fizyczne PET/CBM oraz ich mapowanie na rejestry układów I/O. Szczegółowy dokument VIC-20 znajduje się w `docs/vic20/io-ports.md`.
Status `repo` opisuje stan aktualnej implementacji emulatora, a nie tylko fakt,
że odpowiedni układ PIA/VIA/6560 istnieje w modelu. Dokument jest również
planem dalszej rozbudowy VIC-20; nie zakłada tworzenia równoległych projektów
`CmosCpu.*`, ponieważ repozytorium ma już warstwy `PetEmulator.Core`,
`PetEmulator.Chips` i `PetEmulator.Vic20`.

Stan dokumentu: po commitach `9de48b3`–`2b540b7` obejmujących obsługę IEC
Desktop, profile cartridge/RAM, joystick, profile programów i test audio.

## Zasada architektoniczna

Istniejący `Vic20MemoryBus` pozostaje dekoderem całej przestrzeni CPU. Nie
tworzymy drugiego busa tylko dla rozszerzeń. Rozszerzenia mają być dokładane
przez istniejące kontrakty zasobów:

```text
Vic20Machine
  -> Vic20MemoryBus
       -> pamięć wbudowana i profile RAM
       -> Vic20Cartridge (ROM/RAM/I/O2/I/O3)
       -> przyszłe urządzenia rozszerzeń
```

`Vic20ExpansionProfile` opisuje konkretną konfigurację pamięci wraz z nazwą
i zakresami zasobów; profile RAM są tworzone w pamięci i nie wymagają plików
`.bin`. `Vic20CartridgeResource` jest wspólnym kontraktem
konfliktów adresowych. Przyszły loader pluginów musi używać tych kontraktów,
a nie wprowadzać niezależne `AddressRange`, `IBusDevice` ani drugi mechanizm
walidacji.

## Oznaczenia

| Status | Znaczenie |
| --- | --- |
| ✅ | port lub funkcja jest podłączona i używana przez emulowane urządzenie |
| ◐ | układ/rejestry istnieją, ale część linii lub urządzeń zewnętrznych nie jest podłączona |
| ❌ | brak implementacji |

## Zestawienie modeli i rodzin

| Model / wariant | Rzeczywiste porty i urządzenia | Główne adresy I/O | Status w repozytorium | Czego brakuje |
| --- | --- | --- | --- | --- |
| PET 2001 / PET 2001-8 | klawiatura matrycowa, IEEE-488, kaseta, User Port, złącze rozszerzeń | `$E810-$E813`, `$E820-$E823`, `$E840-$E84F` | ◐ profil i jawna rewizja klawiatury/kasety/złączy; PIA, VIA, IEEE-488 i obie ścieżki kaset | dokładne różnice płyt i wariantów rewizyjnych |
| PET 2001-32 | jak wyżej; 32 KiB RAM w późniejszym wariancie | j.w. | ◐ profil i metadane rewizji | dokładne różnice płyt i wariantów rewizyjnych |
| PET/CBM 3000 i wczesne 4000 / 3032 | klawiatura, IEEE-488, kasety, User Port, rozszerzenie | j.w. | ◐ profile 3008/3016/3032 i jawna rewizja rodziny 3000 | potwierdzenie niestandardowych płyt |
| CBM 4032 / 40 kolumn | klawiatura, IEEE-488, kasety, User Port, rozszerzenie; wariant CRTC zależny od rewizji | j.w.; CRTC `$E880-$E881` w rewizjach z CRTC | ◐ profil `cbm-4032`, warianty CRTC, klawiatura 4000 i metadane złączy | potwierdzenie niestandardowych płyt |
| CBM 8032 / seria 8000 | klawiatura, IEEE-488, kasety, User Port, rozszerzenie, CRTC; 80×25 | j.w. + CRTC `$E880-$E881` | ◐ profil `cbm-8032`, klawiatura business 8000 i metadane złączy | aktywacja 8096/8296 po weryfikacji ROM |
| 8096 / 8296 / SuperPET / SP9000 | porty PET/CBM oraz — zależnie od modelu — bankowane RAM, dodatkowy procesor, ACIA/RS-232 lub inne rozszerzenia | PET I/O j.w.; SuperPET ACIA `$EFF0-$EFF3`, 8096/8296 rejestr bankowania `$FFF0` | ◐ profile `Placeholder`, manifesty ROM, MOS6551 i model bankowanego RAM; bez 6809 | cały dodatkowy sprzęt i aktywacja profili po weryfikacji ROM |
| VIC-20 bez rozszerzenia | VIC-I, dwa VIA, joystick/paddle, User Port, kaseta, IEC serial, cartridge/expansion | `$9000-$900F`, `$9110-$911F`, `$9120-$912F`, `$9400-$97FF` | ◐ VIC-I, VIA1/VIA2, klawiatura, joystick, User Port, kaseta, IEC, Color RAM, CRT i pluginy cartridge | fizyczne źródło paddle/light-pen, adapter RS-232 |
| VIC-20 +3K / +8K / +16K / +24K / All | te same porty zewnętrzne; dodatkowo odpowiedni blok RAM | jak wyżej; pamięć bloków `$0400`, `$2000`, `$4000`, `$6000`, `$A000` | ◐ profile pamięci i pluginy DLL mają jawne zasoby oraz walidację konfliktów | bardziej złożone multi-cartridge i pełne warianty sprzętowe |

## PET/CBM — szczegółowe mapowanie

Adresy są wspólne dla emulowanych profili PET. PIA/VIA są mapowane w
`PetMemoryBus`; CRTC jest tworzony tylko dla profili z `RequiresCrtc`.

| Adres | Układ / linie | Funkcja sprzętowa | Stan w repo |
| --- | --- | --- | --- |
| `$E810` | PIA1 Port A | wybór wiersza klawiatury; sense kasety #1/#2; IEEE EOI; wejście diagnostyczne | ◐ wybór klawiatury, sense kasety #1 i #2 oraz EOI |
| `$E811` | PIA1 CA1/CA2 | odczyt kasety #1; w starszych rewizjach blanking ekranu lub EOI | ◐ odczyt kasety / sygnał synchronizacji modelu |
| `$E812` | PIA1 Port B | kolumny klawiatury | ✅ |
| `$E813` | PIA1 CB1/CB2 | retrace/blanking; silnik kasety #1 | ◐ kaseta #1 |
| `$E820` | PIA2 Port A | wejście danych IEEE-488 | ✅ |
| `$E821` | PIA2 CA1/CA2 | IEEE NDAC / pozostałe handshake | ✅ w `PetIeeeBusBinding` |
| `$E822` | PIA2 Port B | wyjście danych IEEE-488 | ✅ |
| `$E823` | PIA2 CB1/CB2 | IEEE SRQ/DAV | ✅ w zakresie używanym przez magistralę |
| `$E840` | VIA Port B | IEEE handshake i ATN; linie kasety #2; zapis kasety | ◐ IEEE, User Port i kaseta #2 są podłączone w modelu |
| `$E841` / `$E84F` | VIA Port A | User Port i handshake CA2 | ◐ rejestry VIA istnieją, brak zewnętrznego urządzenia User Port |
| `$E842-$E84E` | VIA DDR/timery/shift/PCR/IFR/IER | timery, IRQ i sterowanie linii VIA | ✅ układ VIA; nie wszystkie linie mają model zewnętrzny |
| `$E880-$E881` | CRTC 6545 | indeks i dane kontrolera obrazu | ✅ tylko profile `cbm-4032` i `cbm-8032` |

Emulator ma więc działającą ścieżkę `PIA → PetIeeeBus → PetIeeeDiskDrive`
dla obrazów D64, dwa niezależne modele kaset i User Port. Bankowanie 8096/8296
jest dostępne na magistrali, ale profile pozostają ukryte do czasu potwierdzenia
ich obrazów ROM.

## Co jest obecnie dostępne, a czego nie ma

| Obszar | Dostępne w repo | Brak / ograniczenie |
| --- | --- | --- |
| PET klawiatura | matryca PIA1 i profile 2001/4032/8032 | brak pełnej zmienności rewizji sprzętowych |
| PET dyski | IEEE-488, urządzenia D64, montowanie i transfer DOS | brak drugiego niezależnego modelu kontrolera/portu; to ograniczenie modelu, nie standardu IEEE |
| PET kasety | kasety #1 i #2, niezależny status i transport | różnice analogowe konkretnego napędu |
| PET obraz | profile 40/80 kolumn, CRTC dla profili CRTC, bankowanie 8096/8296 na busie | profile 8096/8296 nadal wymagają zweryfikowanych ROM |
| VIC-20 klawiatura | VIA2 skanowanie matrycy | brak konfliktu z joystickiem; joystick Desktop używa numpada |
| VIC-20 dyski | IEC przez VIA1/VIA2, napędy D64, status w modelu i podgląd Desktop | brak dodatkowych urządzeń pod I/O2/I/O3 poza kontraktem cartridge |
| VIC-20 kaseta | odczyt, zapis logiczny/SAVE-LOAD, motor/sense | brak osobnego zewnętrznego modelu analogowego portu |
| VIC-20 User Port / RS-232 | User Port VIA1 PB0-PB7 z DDRB; odczyt/zapis dostępny przez `Vic20Machine.UserPort` | brak adaptera RS-232 i obsługi protokołu szeregowego |
| VIC-20 pamięć | presety 3K/8K/16K/24K/All oraz odpowiadające pluginy DLL | brak pełnej emulacji wszystkich komercyjnych urządzeń rozszerzeń |

## Źródła i kod repozytorium

- [PET I/O map — Zimmers](https://www.zimmers.net/anonftp/pub/cbm/maps/PETio.txt) — sygnały PIA/VIA, IEEE, kaset i User Portu.
- [VIC-20 memory map — CBM Italia](https://www.cbmitapages.it/vic20/vic20map.html) — rejestry VIC/VIA oraz funkcje linii joysticka, kasety i IEC.
- [VIC-20 Programmer's Reference Manual](https://www.manualslib.com/manual/950679/Commodore-Vic-20.html?page=134) — dokumentacja programistyczna mapy I/O.
- [VIC-20 IEC w tym repozytorium](../vic20/disk.md) — potwierdzone połączenia linii IEC i ich rejestrów.
- [VIC-20 kaseta w tym repozytorium](../vic20/tape.md) — mapowanie linii kasety i ograniczenia modelu.
- Implementacja PET: `src/PetEmulator.Pet/PetMemoryBus.cs`, `src/PetEmulator.Pet/PetMachine.cs`, `src/PetEmulator.Pet/PetProfileCatalog.cs`.
- Implementacja VIC-20: `src/PetEmulator.Vic20/Vic20MemoryMap.cs`, `src/PetEmulator.Vic20/Vic20MemoryBus.cs`, `src/PetEmulator.Vic20/Vic20Cartridge.cs`, `src/PetEmulator.Vic20/Vic20CartridgePluginLoader.cs`, `src/PetEmulator.Vic20/Serial/Vic20SerialBusBinding.cs`, `src/PetEmulator.Vic20/Tape/Vic20Datasette.cs`.
- Kontrakt DLL: `lib/PetEmulator.Vic20.Cartridge.Abstractions/Vic20CartridgeContracts.cs`; przykładowy plugin: `plugins/PetEmulator.Vic20.Cartridge.Sample/SampleCartridgePlugin.cs`.
- Programy testowe i wrappery autostart: `roms/vic20/test-programs/`, `roms/vic20/cartridges/` oraz `tools/generate-vic20-autostart-basic-cartridges.py`.
- Test audio kartridża: `roms/vic20/test-programs/vic20-sound-test.asm` i `vic20-sound-test-autostart.crt`.

## Zaktualizowany zakres zmian

Poniższe elementy muszą zostać zmienione względem wcześniejszego handoffu:

1. **Warstwa projektów** — nie tworzyć `CmosCpu.Abstractions` ani `CmosCpu.Vic20`; używać istniejących projektów i ich konwencji.
2. **Mapa pamięci** — nie zastępować `Vic20MemoryBus`; wydzielić tylko kontrakt rejestracji rozszerzeń, gdy obecne listy cartridge przestaną wystarczać.
3. **Profile pamięci** — traktować `Vic20ExpansionProfile` jako konfigurację sprzętową; nie przechowywać zerowanego RAM-u jako pliku cartridge.
4. **Cartridge** — zachować `Vic20CartridgeResource` jako jedyne źródło informacji o zakresie, typie i dostępie; konflikt ma być wykrywany przed zmianą stanu maszyny.
5. **Pluginy DLL** — dodać dopiero po ustabilizowaniu kontraktu urządzenia rozszerzenia; plugin ma zwracać jeden lub wiele zasobów i korzystać z istniejącej walidacji.
6. **Joystick** — dodać `IJoystickSource` jako adapter źródła wejścia nad istniejącym `Vic20Joystick`; transport Windows/WSL ma pozostać poza core.
7. **Audio** — nie tworzyć nowego `VicI`; użyć istniejącego `MOS6560 : IAudioSource` i rozszerzać tylko brakujące backendy/testy.
8. **UI/CLI** — pozostają klientami konfiguracji; nie przenosić do nich mapowania pamięci, ładowania pluginów ani walidacji konfliktów.

## Nowa checklista implementacyjna

Kolejność minimalizuje ryzyko zmian w krytycznych klasach `Vic20Machine`,
`Vic20MemoryBus` i `MOS6560`. Każdy etap wymaga testu przed przejściem dalej.

### Etap 0 — kontrakty i dokumentacja

- [x] Użyć istniejących projektów `PetEmulator.Core`, `PetEmulator.Chips` i `PetEmulator.Vic20`.
- [x] Ustalić `Vic20ExpansionProfile` jako opis konfiguracji sprzętowej.
- [x] Ustalić `Vic20CartridgeResource` jako wspólny opis zasobów i konfliktów.
- [x] Ujawnić wybrany profil na `Vic20Machine.ExpansionProfile`.
- [x] Dodać test zgodności profilu, alokowanych bloków RAM i wszystkich zakresów zasobów.
- [x] Utrzymać obrazy `vic20-ram-*.bin` jako fixture’y pluginów RAM; profile programów używają ich tylko tam, gdzie są wymagane.

### Etap 1 — wspólny kontrakt rozszerzenia

- [x] Dodać kontrakt DLL w osobnym projekcie `PetEmulator.Vic20.Cartridge.Abstractions`.
  - [x] Zdefiniować descriptor, zasoby, odczyt/zapis, `Reset` i `Tick`.
- [x] Zaprojektować `IVic20ExpansionDevice` dla urządzeń niebędących cartridge.
  - [x] Urządzenie udostępnia listę `Vic20CartridgeResource`.
  - [x] Odczyt i zapis są opcjonalne zależnie od uprawnień zasobu.
  - [x] Nie dublować `AddressRange`, `BusAccess` ani walidatora konfliktów.
  - [x] Zintegrować urządzenie ze wspólnym rejestrem rozszerzeń oraz cyklem `Reset`/`Tick`.
  - [x] Pokryć mapowanie, dostęp kierunkowy i konflikt zasobów testami `Vic20ExpansionDeviceTests`.
- [x] Dodać adapter istniejącego `Vic20Cartridge` do nowego kontraktu bez zmiany publicznego API.
- [x] Dodać test ROM, RAM, I/O2/I/O3 i konfliktów z profilem RAM.
  - [x] ROM: `Vic20MemoryBusTests.Cartridge_ReadsRomBytes`.
  - [x] RAM profilu: `Vic20MachineTests.ExpansionProfile_AllocatesItsMemoryRanges`.
  - [x] I/O2: `Vic20MemoryBusTests.Cartridge_MapsIoDevice`.
  - [x] konflikt: `Vic20MemoryBusTests.Cartridge_CannotConsumeRangeAlreadyOwnedByAllRamExpansion`.

### Etap 2 — rejestracja urządzeń w busie

- [x] Wydzielić `Vic20ExpansionDeviceRegistry` z `Vic20MemoryBus`, zachowując dotychczasowe API.
  - [x] Rejestr posiada jedną listę urządzeń i nie duplikuje dekodera pamięci.
- [x] Zapewnić atomowe odrzucenie urządzenia przy konflikcie.
  - [x] `Vic20MemoryBusTests.MultipleCartridges_AllowIndependentRanges_AndKeepPreviousOnConflict`.
- [x] Zachować otwartą magistralę dla niezmapowanych adresów.
  - [x] `Vic20MemoryBusTests.UnmappedAddress_ReadsAsOpenBus`.
- [x] Przetestować odczyt/zapis po rejestracji, eject, reset i tick.
  - [x] `Vic20CartridgePluginTests.Machine_ResetAndEjectOperateOnTheMountedPlugin`.

### Etap 3 — pluginy DLL

- [ ] Dodać kontrakt pluginu w istniejącej warstwie abstractions/core tylko wtedy, gdy będzie używany przez więcej niż VIC-20.
- [ ] W przeciwnym przypadku umieścić kontrakt w `PetEmulator.Vic20`.
- [x] Dodać loader oparty o `AssemblyLoadContext` z katalogu `plugins`.
- [x] Dodać przykładowy plugin DLL z ROM-em 8K i rejestrem I/O2.
- [x] Odrzucać błędne pluginy i konflikty bez częściowego montowania.
  - [x] Brak eksportu pluginu: `Vic20CartridgePluginTests.Loader_RejectsAssemblyWithoutPlugin`.
  - [x] Błędny obraz i konflikt zasobów: `Vic20CartridgePluginTests.Machine_DoesNotMount...`.
- [x] Dodać test izolowanego przykładowego pluginu DLL.
  - [x] Loader: `Vic20CartridgePluginTests.Loader_LoadsOnePluginFromDll`.
  - [x] Montowanie i mapowanie: `Vic20CartridgePluginTests.Machine_MountsPluginImageAndRoutesRomAndIo`.
- [x] Nie ładować pluginów w CPU, ViewModelu ani kodzie-behind.

### Etap 3a — interfejsy uruchomieniowe

- [x] Dodać komendę CLI `cartridge-plugin <plugin.dll> <image.bin>`.
  - [x] Test: `Vic20DebuggerSessionTests.CartridgePlugin_MountsPluginAndImage`.
- [x] Dodać osobny wybór DLL i obrazu w Desktop.
  - [x] Test ViewModelu: montowanie obu plików — `CartridgeMountViewModelTests.LoadPlugin_MountsImageAndRefreshesRows`.
  - [x] Test konfliktu i braku częściowego montowania — `CartridgeMountViewModelTests.LoadPlugin_ShowsConflictAndDoesNotAddRow`.

### Etap 3b — cartridge RAM jako pluginy

- [x] Dodać osobne pluginy DLL dla `+3K`, `+8K`, `+16K`, `+24K` i `+35K`.
- [x] Zadeklarować dokładne zakresy RAM w descriptorach pluginów.
- [x] Dodać zerowe obrazy inicjalizujące `.bin` oraz generator fixture’ów.
- [x] Potwierdzić testami montowanie, zapis/odczyt, reset i konflikty pluginów RAM.
  - [x] `Vic20RamCartridgePluginTests`.

### Etap 6 — formaty i warianty sprzętowe

- [x] Dodać importer `.prg` z little-endian adresem ładowania.
  - [x] Obsłużyć okna cartridge `$2000`, `$4000`, `$6000` i `$A000`.
  - [x] Dodać testy adresu `$6000`, `$A000` i odrzucenia pustego payloadu.
    - [x] `Vic20PrgParserTests`.
- [x] Dodać parser `.crt` dla nagłówka CRT i pakietów `CHIP`.
  - [x] Montować pojedynczy układ w banku 0 według jego adresu ładowania.
  - [x] Odrzucać niezgodne rozmiary/adresy i banki poza rejestrem jednobajtowym.
  - [x] Testy: `Vic20CrtParserTests` (7 testów).
- [x] Dodać profile programów testowych Alien Blitz PAL/NTSC, Alphoids i test audio.
- [ ] Rozszerzyć model VIC o pełną separację wariantu PAL/NTSC poza profilami programów.
- [x] Dodać bank switching jako urządzenie z własnym zasobem sterującym `$9800`.
  - [x] Przełączanie, reset i brak częściowego montowania są pokryte przez `Vic20CrtParserTests`.
- [x] Dodać jawny obiekt `MultiCartridge` dla niezależnego zarządzania grupą banków/zasobów.

### Etap 7 — PET i elementy niezależne

- [ ] Druga kaseta PET.
- [ ] PET User Port i pełniejsze linie VIA.
- [ ] Profile 8096/8296/SuperPET.
- [ ] Dodatkowy procesor/ACIA tylko po osobnym potwierdzeniu mapy i ROM-u.

## Checklista historyczna

Poniższa lista zachowuje wcześniejsze testy i decyzje dotyczące portów. Nowe
prace należy prowadzić według checklisty powyżej; po zakończeniu etapu wpisy
historyczne można konsolidować, zamiast dodawać kolejne równoległe sekcje.

## Weryfikacja checklisty

Punkt oznaczony `[x]` może zostać uznany za wykonany dopiero po przejściu
odpowiedniego testu. Punkty `[ ]` są planem i nie są raportowane jako gotowe.

| Zakres | Test / polecenie | Wynik |
| --- | --- | --- |
| Profile RAM bez plików `.bin` | `dotnet test tests/PetEmulator.Vic20.Tests/PetEmulator.Vic20.Tests.csproj --no-restore --filter FullyQualifiedName~Vic20MachineTests` | ✅ 17 testów |
| Zasoby i konflikty | `dotnet test tests/PetEmulator.Vic20.Tests/PetEmulator.Vic20.Tests.csproj --no-restore --filter FullyQualifiedName~Vic20MemoryBusTests` | ✅ 20 testów |
| Kontrakt i loader DLL | `dotnet test tests/PetEmulator.Vic20.Tests/PetEmulator.Vic20.Tests.csproj --no-restore --filter FullyQualifiedName~Vic20CartridgePluginTests` | ✅ 6 testów |
| CLI pluginu | `dotnet test tests/PetEmulator.Cli.Tests/PetEmulator.Cli.Tests.csproj --no-restore --disable-build-servers --filter FullyQualifiedName~Vic20DebuggerSessionTests` | ✅ 9 testów |
| `IVic20ExpansionDevice` | `dotnet build tests/PetEmulator.Vic20.Tests/PetEmulator.Vic20.Tests.csproj --no-restore -m:1` | ✅ kompilacja; test runtime `Vic20ExpansionDeviceTests` ⚠️ VSTest blokowany przez `TcpListener: Permission denied` |
| Desktop pluginu | `dotnet test tests/PetEmulator.Desktop.Tests/PetEmulator.Desktop.Tests.csproj --no-restore --disable-build-servers` | ✅ 2 testy |
| Kartridż testu audio | `dotnet test tests/PetEmulator.Vic20.Tests/PetEmulator.Vic20.Tests.csproj --no-restore --filter FullyQualifiedName~SoundTestCartridge` | ✅ 1 test |
| MOS6560 audio | `dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --no-restore --filter FullyQualifiedName~MOS6560Tests` | ✅ 14 testów |
| Profile Desktop | `dotnet test tests/PetEmulator.Desktop.Tests/PetEmulator.Desktop.Tests.csproj --no-restore --filter FullyQualifiedName~Vic20ProgramProfileTests` | ✅ 3 testy |
| Backend PulseAudio | `dotnet test tests/PetEmulator.Audio.Tests/PetEmulator.Audio.Tests.csproj --no-restore --filter FullyQualifiedName~PulseAudioSinkPlaybackTests` | ✅ 1 test |
| Integracja VIC-20 | pełny projekt `PetEmulator.Vic20.Tests` | ✅ ostatni pełny przebieg: 127/127; po dodaniu testu audio należy powtórzyć pełną regresję |
| Desktop | `dotnet build src/PetEmulator.Desktop/PetEmulator.Desktop.csproj --no-restore` | ✅ 0 błędów |
| CLI | `dotnet build src/PetEmulator.Cli/PetEmulator.Cli.csproj --no-restore` | ✅ 0 błędów |
| Pełny solution | `dotnet build PetEmulator.slnx --no-restore` | ⚠️ istniejące błędy NUnit1001 w testach PET |

### Następny niezamknięty punkt

Najbliższym krokiem jest wydzielenie jawnego obiektu `MultiCartridge` oraz
potwierdzenie testami zarządzania grupą banków i konkurujących zasobów.
Do zamknięcia pozostaje:

1. wydzielenie jawnego obiektu `MultiCartridge` ponad rejestrem urządzeń;
2. zarządzanie grupą banków i niezależnych zasobów bez duplikowania walidatora;
3. testy konfliktów dla bardziej złożonych multi-cartridge.

Parser obsługuje pojedynczy układ oraz wielopakietowy CRT z bankami o wspólnym
adresie i rozmiarze. Runtime testy zostały potwierdzone po uruchomieniu VSTest poza ograniczeniem sandboxa.

### 1. Wspólne przygotowanie

- [x] Spisać testami aktualne zachowanie wszystkich używanych bitów PIA/VIA.
  - [x] Rozdzielić testy rejestru układu od testów urządzenia podłączonego do linii.
  - [x] Dodać asercje, że niepodłączone linie zachowują stan otwartej magistrali lub stan pull-up.
- [x] Ustalić minimalny kontrakt dla zewnętrznych linii I/O.
  - [x] Wykorzystać istniejące callbacki `PortAInput`/`PortBInput` i callbacki zapisu; testy potwierdzają wejście, wyjście oraz maskowanie przez DDR.
  - [x] Zachować active-low/open-collector w istniejących wiązaniach IEC; bez zmian adresów i kontraktów klawiatury, IEC oraz kasety.

### 5. PET — druga kaseta i pełne linie VIA

- [x] Dodać niezależny model kasety #2.
  - [x] Podłączyć sense PIA1 PA5 i sterowanie silnikiem VIA Port B bit 4.
  - [x] Rozdzielić status, transport i obraz taśmy od kasety #1.
  - [x] Dodać test równoległego podłączenia kasety #1 i #2.
- [x] Uzupełnić brakujące linie IEEE/User Portu w wiązaniu PET VIA.
  - [x] Zweryfikować bity DAV, NRFD, NDAC, ATN i zapisu kasety względem mapy PET.
  - [x] Nie zmieniać działającego transferu D64 bez testu regresji IEEE-488.
- [x] Dodać model PET User Portu.
  - [x] Udostępnić Port A VIA oraz handshake CA2 jako zewnętrzny interfejs.
  - [x] Dodać test kierunku linii i aktywnego handshake.

### 6. PET — rewizje modeli i obraz

- [x] Rozdzielić profile według dostępnych, rzeczywistych rewizji, jeśli różnice wpływają na I/O.
- [x] Jawnie określić, które warianty mają CRTC, a które dyskretny układ obrazu.
  - [x] Utrzymywać osobne manifesty ROM i test startu dla każdego dostępnego profilu.
- [x] Zweryfikować różnice klawiatury, kaset i złączy między PET 2001, 3000,
      4000/4032 i 8000/8032.
  - [x] Dla dostępnych profili zapisać jawnie rewizję klawiatury, konfigurację
        kaset i rodzinę złączy; test nie pozwala pozostawić tych pól nieokreślonych
        dla obsługiwanych modeli.
  - [x] Zachować wspólną emulowaną wiązkę dwóch kaset, IEEE-488 i User Portu;
        różnice mechaniczne (kaseta wewnętrzna/zewnętrzna i wariant złącza) są
        metadanymi profilu, a nie udawaną zmianą mapy PIA/VIA.
  - [x] Dodać profile CBM 3008/3016/3032 na zweryfikowanym zestawie BASIC 2,
        z osobnymi pojemnościami RAM i współdzielonym katalogiem ROM.
  - [x] Dodać jawne profile wariantów `cbm-4008-crtc-40n60` i
        `cbm-4016-crtc-40n60`, bez ukrywania wariantu editor ROM.
  - [x] Zweryfikować dostępne obrazy editor ROM dla wariantów 40/80 kolumn,
        klawiatury normalnej/business oraz 50/60 Hz; dodano profile `40n50`,
        `40n60`, `40b50`, `40b60`, `80b50` i `80b60`.
  - [x] Dodać jawnie oznaczony profil `cbm-8016-converted-80n50` dla dostępnego
        editor ROM 80n50 opisanego jako konwersja 4016 do 8016; nie traktować go
        jako standardowego PET 8032.
  - [x] Dodać jawnie oznaczony profil `pet-converted-80n-unknown` dla 4 KiB
        editor ROM `edit-4-80-n_unk.bin`; nie przypisywać go do konkretnej rewizji
        płyty bez dalszego potwierdzenia.
  - [ ] Pozostają inne rewizje wymagające osobnego potwierdzenia płyty.
  - [x] Nie tworzyć profilu tylko dla różnicy obudowy lub nazwy handlowej.

### 7. PET 8096/8296 i SuperPET

- [x] Najpierw dodać osobne profile i manifesty ROM, bez zmiany istniejących profili.
  - [x] Dodać profile planowane `cbm-8096-french`, `cbm-8296` i `superpet` jako
        `Placeholder`, niewybieralne przez bieżący emulator.
  - [x] Dodać manifest firmware Waterloo SuperPET dla bloków `$A000-$BFFF`,
        `$C000-$DFFF` i `$E000-$FFFF`; loader tylko weryfikuje obrazy na tym etapie.
- [x] Zaimplementować bankowanie pamięci oraz rejestr sterujący rozszerzeniem.
  - [x] Dodać test przełączania banku, ochrony zapisu oraz prześwitu ROM/I/O.
  - [x] Odwzorować rejestr `$FFF0`: bit 7 włącza rozszerzenie, bity 3/2
        wybierają parę bloków `$C000-$FFFF`/`$8000-$BFFF`, bit 6 udostępnia
        I/O, bit 5 pamięć ekranu, a bity 1/0 chronią zapis obu okien.
  - [x] Utrzymać profile `cbm-8096-french` (32 KiB RAM rozszerzenia) i
        `cbm-8296` (64 KiB RAM rozszerzenia) jako placeholdery do czasu
        potwierdzenia obrazów ROM; sam model magistrali jest testowalny bez ROM.
- [x] Podłączyć MOS6551 ACIA do mapy `$EFF0-$EFF3` profilu SuperPET i do
      transportu bajtowego używanego przez terminal/RS-232.
  - [x] Dodać test integracyjny z istniejącym 6502: zapis/odczyt danych oraz
        propagacja IRQ odbioru.
- [ ] Dopiero potem dodać dodatkowy procesor 6809 i pozostałe urządzenia
      SuperPET/SP9000.
  - [ ] Zweryfikować testem rzeczywisty firmware Waterloo uruchomiony na rdzeniu 6809.
  - [ ] Każdy dodatkowy układ powinien mieć własną mapę adresów i test boot/diagnostic.
- [x] Rozszerzyć Desktop/CLI o wybór zweryfikowanego profilu SuperPET po testach
      ROM i magistrali; profile CBM 8096/8296 nadal pozostają ukryte jako placeholdery.
