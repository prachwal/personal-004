# Porty wejścia/wyjścia PET/CBM i VIC-20 oraz architektura rozszerzeń

Dokument porównuje porty fizyczne oraz ich mapowanie na rejestry układów I/O.
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
| PET 2001 / PET 2001-8 | klawiatura matrycowa, IEEE-488, kaseta, User Port, złącze rozszerzeń | `$E810-$E813`, `$E820-$E823`, `$E840-$E84F` | ◐ klawiatura, PIA, VIA, IEEE-488 i kaseta #1 | kaseta #2, semantyka User Port, magistrala rozszerzeń |
| PET 2001-32 | jak wyżej; 32 KiB RAM w późniejszym wariancie | j.w. | ◐ profil `pet-2001-32`, klawiatura, IEEE-488, kaseta #1 | jak wyżej; brak bankowania/rozszerzeń PET |
| PET/CBM 3000 i wczesne 4000 / 3032 | klawiatura, IEEE-488, dwie kasety, User Port, rozszerzenie | j.w. | ◐ najbliżej profilu 40-kolumnowego | druga kaseta, User Port i rozszerzenie |
| CBM 4032 / 40 kolumn | klawiatura, IEEE-488, kasety, User Port, rozszerzenie; wariant CRTC zależny od rewizji | j.w.; CRTC `$E880-$E881` w rewizjach z CRTC | ◐ profil `cbm-4032`, 40×25, CRTC, klawiatura, IEEE-488, kaseta #1 | druga kaseta, User Port, rozszerzenie; profil upraszcza różnice rewizji |
| CBM 8032 / seria 8000 | klawiatura, IEEE-488, kasety, User Port, rozszerzenie, CRTC; 80×25 | j.w. + CRTC `$E880-$E881` | ◐ profil `cbm-8032`, 80×25, CRTC, klawiatura, IEEE-488, kaseta #1 | druga kaseta, User Port, rozszerzenie 8096/8296 |
| 8096 / 8296 / SuperPET / SP9000 | porty PET/CBM oraz — zależnie od modelu — bankowane RAM, dodatkowy procesor, ACIA/RS-232 lub inne rozszerzenia | PET I/O j.w.; sterowanie pamięcią rozszerzoną m.in. `$FFF0` w 8096/8296 | ❌ brak osobnych profili | cały dodatkowy sprzęt i bankowanie |
| VIC-20 bez rozszerzenia | VIC-I, dwa VIA, joystick/paddle, User Port, kaseta, IEC serial, cartridge/expansion | `$9000-$900F`, `$9110-$911F`, `$9120-$912F`, `$9400-$97FF` | ◐ VIC-I, VIA1/VIA2, klawiatura, joystick, User Port, kaseta, IEC, Color RAM, CRT i pluginy cartridge | fizyczne źródło paddle/light-pen, adapter RS-232 |
| VIC-20 +3K / +8K / +16K / +24K / All | te same porty zewnętrzne; dodatkowo odpowiedni blok RAM | jak wyżej; pamięć bloków `$0400`, `$2000`, `$4000`, `$6000`, `$A000` | ◐ profile pamięci i pluginy DLL mają jawne zasoby oraz walidację konfliktów | bardziej złożone multi-cartridge i pełne warianty sprzętowe |

## PET/CBM — szczegółowe mapowanie

Adresy są wspólne dla emulowanych profili PET. PIA/VIA są mapowane w
`PetMemoryBus`; CRTC jest tworzony tylko dla profili z `RequiresCrtc`.

| Adres | Układ / linie | Funkcja sprzętowa | Stan w repo |
| --- | --- | --- | --- |
| `$E810` | PIA1 Port A | wybór wiersza klawiatury; sense kasety #1/#2; IEEE EOI; wejście diagnostyczne | ◐ wybór klawiatury, sense kasety #1 i EOI; kaseta #2 pozostaje nieaktywna |
| `$E811` | PIA1 CA1/CA2 | odczyt kasety #1; w starszych rewizjach blanking ekranu lub EOI | ◐ odczyt kasety / sygnał synchronizacji modelu |
| `$E812` | PIA1 Port B | kolumny klawiatury | ✅ |
| `$E813` | PIA1 CB1/CB2 | retrace/blanking; silnik kasety #1 | ◐ kaseta #1 |
| `$E820` | PIA2 Port A | wejście danych IEEE-488 | ✅ |
| `$E821` | PIA2 CA1/CA2 | IEEE NDAC / pozostałe handshake | ✅ w `PetIeeeBusBinding` |
| `$E822` | PIA2 Port B | wyjście danych IEEE-488 | ✅ |
| `$E823` | PIA2 CB1/CB2 | IEEE SRQ/DAV | ✅ w zakresie używanym przez magistralę |
| `$E840` | VIA Port B | IEEE handshake i ATN; linie kasety #2; zapis kasety | ◐ IEEE jest podłączone; User Port i kaseta #2 nie |
| `$E841` / `$E84F` | VIA Port A | User Port i handshake CA2 | ◐ rejestry VIA istnieją, brak zewnętrznego urządzenia User Port |
| `$E842-$E84E` | VIA DDR/timery/shift/PCR/IFR/IER | timery, IRQ i sterowanie linii VIA | ✅ układ VIA; nie wszystkie linie mają model zewnętrzny |
| `$E880-$E881` | CRTC 6545 | indeks i dane kontrolera obrazu | ✅ tylko profile `cbm-4032` i `cbm-8032` |

Emulator ma więc działającą ścieżkę `PIA → PetIeeeBus → PetIeeeDiskDrive`
dla obrazów D64 oraz jedną emulowaną kasetę. Nie ma osobnego modelu drugiego
gniazda kasety, User Portu ani rozszerzeń pamięciowych 8096/8296.

## VIC-20 — szczegółowe mapowanie

W VIC-20 nie ma równoległej magistrali IEEE-488. Napęd dyskowy komunikuje się
szeregową magistralą IEC, której linie są rozdzielone pomiędzy oba VIA.

| Adres | Układ / linie | Funkcja sprzętowa | Stan w repo |
| --- | --- | --- | --- |
| `$9000-$900F` | VIC-I 6560/6561 | obraz, kolory, raster, dźwięk, wejścia paddle/light-pen | ◐ rejestry wejść paddle/light-pen są podłączone do API maszyny; brak fizycznego źródła analogowego w Desktop |
| `$9110` | VIA1 Port B | User Port / linie pomocnicze interfejsów szeregowych | ✅ osiem linii User Portu z wejściem, wyjściem i DDRB; brak adaptera RS-232 |
| `$9111` | VIA1 Port A | IEC CLK IN bit 0, DATA IN bit 1, joystick bits 2–5, cassette sense bit 6, IEC ATN OUT bit 7 | ✅ IEC, joystick i cassette sense są scalane na właściwych bitach |
| `$911C-$911F` | VIA1 PCR/IFR/IER/ORA | sterowanie CA2 kasety oraz obsługa VIA | ◐ CA2 kasety jest podłączone; pozostałe linie bez urządzeń zewnętrznych |
| `$9120` | VIA2 Port B | skanowanie klawiatury; cassette WRITE bit 3; prawa joysticka PB7 | ✅ klawiatura, cassette WRITE i joystick PB7 |
| `$9121` | VIA2 Port A | wybór kolumn/wierszy klawiatury | ✅ |
| `$912C` | VIA2 PCR/CA1/CA2/CB1/CB2 | cassette READ; IEC CLK OUT, DATA OUT i SRQ | ◐ cassette READ i IEC CLK/DATA; SRQ bez urządzenia |
| `$9400-$97FF` | Color RAM | pamięć atrybutów koloru | ✅ `MOS2114` |
| `$9800-$9FFF` | I/O2/I/O3 | obszar urządzeń cartridge/rozszerzeń I/O | ✅ konfigurowalne urządzenia rejestrowe cartridge; bez urządzenia otwarta magistrala |
| `$A000-$BFFF` | cartridge / expansion | ROM/RAM cartridge i urządzenia rozszerzeń | ✅ surowy ROM, pojedynczy i bankowany CRT oraz pluginy RAM; zasoby są walidowane przed montowaniem |

W repozytorium podłączenie dysku VIC-20 jest zatem funkcjonalne dla IEC:

| Linia IEC | VIA / bit | Implementacja |
| --- | --- | --- |
| ATN OUT | VIA1 PA7, `$9111` / `$911F` | `Vic20SerialBusBinding` |
| CLK IN | VIA1 PA0, `$9111` / `$911F` | `Vic20SerialBusBinding` |
| DATA IN | VIA1 PA1, `$9111` / `$911F` | `Vic20SerialBusBinding` |
| CLK OUT | VIA2 CA2, `$912C` | `Vic20SerialBusBinding` |
| DATA OUT | VIA2 CB2, `$912C` | `Vic20SerialBusBinding` |

Kaseta VIC-20 korzysta z VIA2 CA1 jako `cassette READ`, VIA1 PA6 jako sense,
VIA1 CA2 jako sterowania silnikiem oraz VIA2 PB3 jako `cassette WRITE`.

## Co jest obecnie dostępne, a czego nie ma

| Obszar | Dostępne w repo | Brak / ograniczenie |
| --- | --- | --- |
| PET klawiatura | matryca PIA1 i profile 2001/4032/8032 | brak pełnej zmienności rewizji sprzętowych |
| PET dyski | IEEE-488, urządzenia D64, montowanie i transfer DOS | brak drugiego niezależnego modelu kontrolera/portu; to ograniczenie modelu, nie standardu IEEE |
| PET kasety | kaseta #1 i jej status | kaseta #2, pełny User Port i zewnętrzne linie VIA |
| PET obraz | profile 40/80 kolumn, CRTC dla profili CRTC | brak 8096/8296 i ich bankowania |
| VIC-20 klawiatura | VIA2 skanowanie matrycy | brak konfliktu z joystickiem; joystick Desktop używa numpada |
| VIC-20 dyski | IEC przez VIA1/VIA2, napędy D64, status w modelu i podgląd Desktop | brak dodatkowych urządzeń pod I/O2/I/O3 poza kontraktem cartridge |
| VIC-20 kaseta | odczyt, zapis logiczny/SAVE-LOAD, motor/sense | brak osobnego zewnętrznego modelu analogowego portu |
| VIC-20 User Port / RS-232 | User Port VIA1 PB0-PB7 z DDRB; odczyt/zapis dostępny przez `Vic20Machine.UserPort` | brak adaptera RS-232 i obsługi protokołu szeregowego |
| VIC-20 pamięć | presety 3K/8K/16K/24K/All oraz odpowiadające pluginy DLL | brak pełnej emulacji wszystkich komercyjnych urządzeń rozszerzeń |

## Źródła i kod repozytorium

- [PET I/O map — Zimmers](https://www.zimmers.net/anonftp/pub/cbm/maps/PETio.txt) — sygnały PIA/VIA, IEEE, kaset i User Portu.
- [VIC-20 memory map — CBM Italia](https://www.cbmitapages.it/vic20/vic20map.html) — rejestry VIC/VIA oraz funkcje linii joysticka, kasety i IEC.
- [VIC-20 Programmer's Reference Manual](https://www.manualslib.com/manual/950679/Commodore-Vic-20.html?page=134) — dokumentacja programistyczna mapy I/O.
- [VIC-20 IEC w tym repozytorium](vic20-disk.md) — potwierdzone połączenia linii IEC i ich rejestrów.
- [VIC-20 kaseta w tym repozytorium](vic20-tape.md) — mapowanie linii kasety i ograniczenia modelu.
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
- [ ] Zaprojektować `IVic20ExpansionDevice` dla urządzeń niebędących cartridge.
  - [ ] Urządzenie udostępnia listę `Vic20CartridgeResource`.
  - [ ] Odczyt i zapis są opcjonalne zależnie od uprawnień zasobu.
  - [ ] Nie dublować `AddressRange`, `BusAccess` ani walidatora konfliktów.
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
- [ ] Nie ładować pluginów w CPU, ViewModelu ani kodzie-behind.

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

### Etap 4 — joystick i porty zewnętrzne

- [x] Zachować lokalne mapowanie Desktop do `Vic20Joystick`.
- [x] Dodać wirtualny joystick Desktop jako osobny komponent z kierunkami i FIRE.
- [x] Zachować sterowanie klawiaturą numpada `8/2/4/6/0`.
- [ ] Dodać `IJoystickSource` jako źródło stanu, bez zależności od Avalonia.
- [ ] Dodać adapter klawiatura/test/Windows.
- [ ] Dodać osobny transport TCP dla WSL; protokół nie może zależeć od pamięci VIC-20.
- [ ] Dodać test bitowego mapowania active-low i utraty połączenia.
- [ ] Dodać opcjonalny adapter RS-232 User Portu poza ViewModelem.

### Etap 5 — audio VIC-I

- [x] Zachować `MOS6560` jako źródło audio: trzy tony, noise, volume.
- [x] Zachować `IAudioOutput` i uruchamianie backendu w Desktop.
- [x] Dodać testy mapowania rejestrów `$900A-$900E` do parametrów audio.
  - [x] `Vic20AudioTests` — mapowanie przez magistralę i reset rejestrów.
- [x] Poprawić obliczanie częstotliwości z pełnej wartości rejestru VIC, włącznie z bitem enable.
  - [x] `MOS6560Tests` — częstotliwość i liczba zboczy generatora.
- [x] Dodać assemblerowy kartridż testowy z trzema tonami i profilem Desktop.
  - [x] `Vic20AutostartCartridgeTests.SoundTestCartridge_EnablesVicOscillatorAndVolume`.
- [x] Zweryfikować backend PulseAudio testem rzeczywistego odtwarzania.
- [ ] Dodać `NullAudioOutput`/backend testowy, jeżeli aktualny kontrakt nie wystarcza do testów integracyjnych.
- [x] Nie wiązać implementacji audio z cartridge ani pluginem; kartridż testowy korzysta wyłącznie z rejestrów VIC.

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
- [ ] Dopiero potem implementować MegaCart i jawny obiekt `MultiCartridge` dla konkurujących banków/zasobów.

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

### 2. VIC-20 — wejścia i User Port

- [x] Dodać model joysticka VIC-20.
  - [x] Podłączyć kierunki i fire do właściwych bitów VIA1/VIA2.
  - [x] Dodać obsługę joysticka w Desktop przez numpad `8/2/4/6/0` oraz test odczytu linii.
- [x] Dodać wejścia paddle/light-pen.
  - [x] Rozszerzyć model VIC-I o wejścia zewnętrzne `SetPaddlePosition` i `StrobeLightPen`.
  - [x] Zweryfikować odczyt przez rejestry `$9006-$9009`.
- [x] Dodać urządzenie User Portu VIC-20.
  - [x] Udostępnić VIA1 Port B jako osiem linii wejścia/wyjścia z kierunkiem DDRB.
  - [ ] Dodać opcjonalny adapter RS-232 bez wiązania go bezpośrednio z ViewModelem.
  - [x] Dodać test kierunku linii, odczytu wejść i zapisu wyjść.

### 3. VIC-20 — cartridge i I/O2/I/O3

- [x] Wprowadzić model urządzenia cartridge.
  - [x] Rozdzielić cartridge ROM, cartridge RAM i urządzenie I/O.
  - [x] Zachować obecne presety RAM jako osobny, kompatybilny przypadek.
- [x] Podłączyć `$9800-$9FFF` jako konfigurowalny obszar I/O2/I/O3.
  - [x] Zdefiniować odczyt, zapis i zachowanie niezmapowanych adresów.
  - [x] Dodać test ładowania obrazu cartridge oraz test urządzenia rejestrowego.
- [x] Dodać montowanie cartridge w CLI/Desktop po ustabilizowaniu API magistrali.
- [x] Dodać widget cartridge obok stacji dysków.
  - [x] Pokazywać stan `No cartridge` / nazwę zamontowanego obrazu.
  - [x] Udostępnić przycisk wysunięcia bez zmiany kontraktu magistrali.

W tej fazie obraz cartridge może być surowym plikiem binarnym o rozmiarze 1--8192
bajtów, mapowanym domyślnie od `$A000`, albo pojedynczym obrazem `.crt`. CRT-y
wieloukładowe i bankowane nadal wymagają osobnego urządzenia przełączającego banki.

#### Przygotowanie cartridge pamięciowych i MultiCartridge

- [x] Zdefiniować jawny opis zasobów cartridge: nazwa, zakres adresów, typ `ROM`/`RAM`/`I/O` i uprawnienia.
  - [x] Udostępnić zasoby również dla istniejących presetów `3K/8K/16K/24K/All`.
  - [x] Walidować nakładanie zasobów przed połączeniem wielu cartridge.
- [x] Przygotować obrazy cartridge pamięciowych dla każdego presetu.
  - [x] Ustalić manifest obrazu: plik, zakresy zasobów i docelowy preset.
  - [x] Dodać obrazy do `roms/vic20/cartridges/` oraz walidację ich rozmiarów.
- [x] Podłączyć profile VIC-20 do rzeczywistych obrazów RAM cartridge przy starcie maszyny.
- [x] Dodać wielokrotne montowanie cartridge na magistrali.
  - [x] Montować wiele cartridge tylko po pozytywnej walidacji wszystkich zakresów.
  - [x] Zwracać błąd z nazwami obu konkurujących zasobów i adresem konfliktu.
  - [x] Dodać testy konfliktu ROM/RAM, I/O/I/O oraz niezależnych zakresów.
- [ ] Wydzielić publiczny obiekt `MultiCartridge`, jeśli kolejne typy urządzeń będą wymagały
  niezależnego zarządzania grupą cartridge poza `Vic20MemoryBus`.

### 4. VIC-20 — pełniejszy model kasety

- [ ] Oddzielić model linii fizycznych kasety od obecnego dekodowania impulsów.
  - [ ] Zachować istniejące, działające SAVE/LOAD logiczne jako ścieżkę zgodności.
  - [ ] Dodać osobny zapis impulsów z VIA2 PB3 tylko wtedy, gdy testy potwierdzą stabilność.
- [ ] Dodać testy przejść motor, sense, READ i WRITE oraz test round-trip z prawdziwym ROM-em.

### 5. PET — druga kaseta i pełne linie VIA

- [ ] Dodać niezależny model kasety #2.
  - [ ] Podłączyć sense PIA1 PA5 i sterowanie silnikiem VIA Port B bit 4.
  - [ ] Rozdzielić status, transport i obraz taśmy od kasety #1.
  - [ ] Dodać test równoległego podłączenia kasety #1 i #2.
- [ ] Uzupełnić brakujące linie IEEE/User Portu w wiązaniu PET VIA.
  - [ ] Zweryfikować bity DAV, NRFD, NDAC, ATN i zapisu kasety względem mapy PET.
  - [ ] Nie zmieniać działającego transferu D64 bez testu regresji IEEE-488.
- [ ] Dodać model PET User Portu.
  - [ ] Udostępnić Port A VIA oraz handshake CA2 jako zewnętrzny interfejs.
  - [ ] Dodać test kierunku linii i aktywnego handshake.

### 6. PET — rewizje modeli i obraz

- [ ] Rozdzielić profile według rzeczywistych rewizji, jeśli różnice wpływają na I/O.
  - [ ] Określić, które warianty mają CRTC, a które dyskretny układ obrazu.
  - [ ] Dodać osobne manifesty ROM i test startu dla każdego profilu.
- [ ] Zweryfikować różnice klawiatury, kaset i złączy między PET 2001, 3000,
      4000/4032 i 8000/8032.
  - [ ] Nie tworzyć profilu tylko dla różnicy obudowy lub nazwy handlowej.

### 7. PET 8096/8296 i SuperPET

- [ ] Najpierw dodać osobne profile i manifesty ROM, bez zmiany istniejących profili.
- [ ] Zaimplementować bankowanie pamięci oraz rejestr sterujący rozszerzeniem.
  - [ ] Dodać test przełączania banku i ochrony obszarów ROM/I/O.
- [ ] Dopiero potem dodać dodatkowy procesor, ACIA/RS-232 i pozostałe urządzenia
      SuperPET/SP9000.
  - [ ] Każdy dodatkowy układ powinien mieć własną mapę adresów i test boot/diagnostic.
- [ ] Rozszerzyć Desktop/CLI o wybór tych profili po przejściu testów ROM i magistrali.
