# Plan integracji Amstrad CPC6128

Status: M1-M5 oraz pierwszy pionowy zakres M7 zaimplementowane; M0 fixture ROM,
M6 realny firmware, M8 snapshot/debug i M9 pełna regresja pozostają do wykonania.

## Źródła wcześniejszych implementacji

### `personal-002` — źródło referencyjne

Lokalizacja: `/home/prachwal/source/emulators/personal-002`.

Implementacja powstała etapami w commitach `545d286`–`db78473`; najważniejsze
punkty historii to dodanie maszyny, bankowania RAM, FDC, kasety, PPI/AY,
snapshotów i kontraktów portów. Jest to jedyna wcześniejsza wersja, która ma
spójną, samodzielną kompozycję CPC6128 oraz rozbudowany zestaw testów.

### `personal-003` — port niekompatybilny architektonicznie

Lokalizacja: `/home/prachwal/source/emulators/personal-003`.

Implementacja z commitów `3609b2d` i `9380dbc` ma działające podstawy RAM-u,
ROM-u, FDC, PPI, Gate Array i bootu, ale dziedziczy po starym kontrakcie
`ITrs80Machine` i korzysta z `Retro.*`. Nie należy jej przenosić mechanicznie.
Warto wykorzystać z niej tylko testy/obserwacje, których brakuje w `personal-002`.

## Inwentaryzacja `personal-002`

| Plik | Co implementuje | Decyzja dla `personal-004` |
| --- | --- | --- |
| `lib/Machines/Amstrad/Cpc6128/Cpc6128Machine.cs` | Kompozycję Z80, Gate Array, CRTC, AY, klawiatury, kasety, PPI, I8272/FDC, RAM-u 128 KB; reset, krok CPU, przebieg cykli i ramek, dysk, ROM rozszerzeń, debug status oraz snapshoty | Przepisać do `src/PetEmulator.Cpc6128/Cpc6128Machine.cs` na `IMachine` i aktualny `Z80Cpu`; zachować kolejność taktowania urządzeń |
| `Cpc6128MemoryBus.cs` | 64 KB przestrzeni CPU, 128 KB RAM fizycznego, osiem konfiguracji mapowania 16 KB, overlay lower/upper ROM, osobny odczyt video RAM, `Load`/`Flush` i banki | Przenieść jako osobną magistralę; nie upraszczać do tablicy 64 KB z CPC464 |
| `Cpc6128Ports.cs` | Dekodowanie `7Fxx`, `BCxx/BDxx`, `F4xx-F7xx`, `FA7E`, `FB7E/FB7F`, `DFxx`; PPI, AY, klawiatura, kaseta, FDC i wybór ROM-u | Przenieść po dopasowaniu interfejsu `IBus`; zachować `0xFF` dla nieznanych odczytów i brak efektu nieznanych zapisów |
| `Cpc6128PortsSnapshot.cs` | Wersjonowany stan latchy PPI A/B/C i rejestru sterującego | Przenieść razem ze snapshotami |
| `Cpc6128Snapshot.cs` | Stan CPU, pamięci CPU i fizycznej, Gate Array, CRTC, AY, FDC, kasety, PPI i klawiatury oraz zegarów pomocniczych | Przenieść po ustaleniu wspólnego kontraktu snapshotów w aktualnym repo |
| `Cpc6128SnapshotCodec.cs` | JSON/binarna serializacja snapshotu z wersją i walidacją | Przenieść lub dostosować do istniejącego formatu snapshotów; nie mieszać formatów PET/VIC |
| `Cpc6128.csproj` | Zależności starej platformy `Abstraction`, `Cpc464`, `CpcGateArray`, `I8272`, `Z80` | Nie kopiować; zbudować projekt na `PetEmulator.Core`, `PetEmulator.CpuZ80`, `PetEmulator.Chips` i ewentualnych nowych wspólnych chipach |
| `tests/Cpc6128.Tests/Cpc6128MachineTests.cs` | 97 testów resetu, bootu, portów, taśmy, FDC, RAM-u, bankowania, PPI, snapshotów i debug API | Rozbić logicznie na testy maszyny, magistrali, portów, FDC i snapshotów w aktualnym stylu NUnit |

### Funkcje potwierdzone testami w `personal-002`

- zimny reset i boot prawdziwego ROM-u CPC6128 do ekranu BASIC `Ready`;
- Z80: wykonanie instrukcji przez pełny pump maszyny i timing urządzeń;
- 128 KB RAM: wszystkie konfiguracje 0–7, cztery okna 16 KB, granice adresów,
  zapis pod lower/upper ROM i niezależny od CPU odczyt video RAM;
- Gate Array: tryby 0/1/2, paleta, border, latch trybu, HSync i IRQ;
- CRTC 6845/6545: rejestry, adres startowy obrazu, HSync/VSYNC i raster;
- PPI/AY/klawiatura: kierunki portów, readback, skan 10x8, operacje PSG;
- kaseta: wejście PPI, scheduler co cztery cykle Z80 i ścieżka debug API;
- I8272/uPD765: status/data ports, komendy, transfer sektorów, motor, DSK;
- AMSDOS: wybór expansion ROM przez `DFxx`;
- snapshoty: CPU, RAM, banki, PPI, klawiatura, kaseta i otwarty transfer FDC;
- status/debug API oraz headless uruchomienie z prawdziwym ROM-em i DSK.

## Inwentaryzacja `personal-003`

| Plik | Co implementuje | Wartość dla planu |
| --- | --- | --- |
| `src/CPC.Machines.CPC6128/Cpc6128Machine.cs` | Kompozycję urządzeń, timing 4 MHz, ramki, kasetę, FDC, bankowanie i wejście klawiatury | Potwierdza kolejność ticków i minimalny cykl maszyny; API wymaga przepisania |
| `Cpc6128MemoryBus.cs` | Te same osiem konfiguracji RAM i overlay ROM | Niezależne potwierdzenie mapowania z `personal-002` |
| `Cpc6128Ports.cs` | Porty PPI, Gate Array, CRTC, FDC, motor i ROM selector | Niezależne potwierdzenie dekodowania; `TraceAccess` i stare `IIoBus` nie są kontraktem docelowym |
| `Cpc6128MachineFactory.cs` | Budowanie maszyny z ROM/tape/disk i wymaganie CPC DSK | Wzorzec dla przyszłej fabryki/loadera, po adaptacji do `MainWindowViewModel` |
| `Cpc6128ConfigFactory.cs` | Opis konfiguracji sprzętu | Wzorzec konfiguracji, jeśli Desktop dostanie wybór profilu |
| `Cpc6128ConfigViewModel.cs` | Prezentacja opisu CPC6128 | Nie kopiować bez aktualnej warstwy konfiguracji Desktop |
| `Cpc6128Registration.cs` | Rejestracja presetów ROM, chargen, DSK i AMSDOS ROM | Przepisać do aktualnego mechanizmu menu/presetów, jeśli będzie potrzebny |
| `tests/.../Cpc6128MachineTests.cs` | 5 krótszych testów: wszystkie banki, AMSDOS ROM, boot, realny DSK i unmapped ports | Dodać jako smoke/regression tests obok pełniejszego zestawu z `personal-002` |

## Docelowa architektura w `personal-004`

Źródłem zachowania jest `personal-002`, ale integracja musi używać lokalnych
kontraktów. CPC6128 powinien być osobnym profilem CPC, a nie warunkiem
rozrzuconym po `Cpc464Machine`.

### Pliki do utworzenia

| Plik | Zakres |
| --- | --- |
| `src/PetEmulator.Cpc6128/PetEmulator.Cpc6128.csproj` | Projekt .NET 10 zależny od Core, CpuZ80, Chips i ewentualnego wydzielonego audio/media |
| `src/PetEmulator.Cpc6128/Cpc6128Machine.cs` | Publiczny `IMachine`, kompozycja sprzętu, reset, `StepInstruction`, `Run`, licznik ramek i dostęp do urządzeń |
| `src/PetEmulator.Cpc6128/Cpc6128MemoryBus.cs` | 128 KB RAM, bankowanie 0–7, ROM overlays i video RAM |
| `src/PetEmulator.Cpc6128/Cpc6128Ports.cs` | Pełne dekodowanie portów CPC6128 i połączenie urządzeń |
| `src/PetEmulator.Cpc6128/CpcDskDiskImage.cs` | Parser/adapter standardowego i Extended DSK, odrębny od D64, JV1, DMK i obrazów PET/VIC |
| `src/PetEmulator.Cpc6128/Cpc6128Snapshot.cs` | Wersjonowany model stanu |
| `src/PetEmulator.Cpc6128/Cpc6128SnapshotCodec.cs` | Serializacja i walidacja stanu |
| `src/PetEmulator.Cpc6128/Cpc6128PortsSnapshot.cs` | Stan PPI/portów |
| `src/PetEmulator.Desktop/ViewModels/Cpc6128MachineViewModel.cs` | Framebuffer, klawiatura, audio AY, datasette i disk capability |
| `src/PetEmulator.Desktop/Views/Cpc6128MachineView.axaml` | Widok CPC6128 z ekranem, statusem, datasette i stacją dysków |
| `src/PetEmulator.Cli/Cpc6128DebuggerSession.cs` | CLI/debug API: ROM, DSK, CDT, status, krokowanie, tekst i snapshot |
| `tests/PetEmulator.Cpc6128.Tests/Cpc6128MachineTests.cs` | Testy integracyjne maszyny i prawdziwego ROM-u |
| `tests/PetEmulator.Cpc6128.Tests/Cpc6128MemoryBusTests.cs` | Testy wszystkich banków, overlayów i Load/Flush |
| `tests/PetEmulator.Cpc6128.Tests/Cpc6128PortsTests.cs` | Testy dekodowania portów, PPI, AY, kasety, ROM selector i unmapped ports |
| `tests/PetEmulator.Cpc6128.Tests/CpcDskDiskImageTests.cs` | Testy standard/Extended DSK oraz odczytu/zapisu sektorów |
| `tests/PetEmulator.Cpc6128.Tests/Cpc6128SnapshotTests.cs` | Deterministyczne restore CPU/RAM/urządzeń/FDC |

### Pliki istniejące do rozszerzenia

| Plik | Zakres zmiany |
| --- | --- |
| `PetEmulator.slnx` | Dodanie projektu maszyny i testów |
| `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs` | Rejestracja CPC6128 i routing wyłącznie przez `IMachineViewModel`, `IDiskDriveViewModel`, `IDatasetteViewModel` oraz capability tworzenia mediów |
| `src/PetEmulator.Desktop/Views/MainWindow.axaml` | DataTemplate/menu dla CPC6128 |
| `src/PetEmulator.Cli/Program.cs` | Komenda/sesja CPC6128, bez kopiowania ścieżki CPC464 |
| `lib/PetEmulator.Chips/` | Wydzielenie lub import I8272/uPD765 tylko jako niezależny kontrakt; nie używać FD1791/FD1793 jako zamiennika |
| `lib/PetEmulator.Audio/` | Podłączenie AY do istniejącego `IAudioOutput`; brak audio nie dotyczy CPC6128, bo AY jest rzeczywistym źródłem dźwięku |
| `docs/desktop/README.md` | Dodanie CPC6128 do menu i opis sprzętu |
| `docs/desktop/multi-machine.md` | Opis capability oraz różnicy CPC464/CPC6128 |
| `docs/chips/README.md` lub osobny dokument I8272 | Kontrakt kontrolera i testów FDC |
| `docs/README.md` | Indeks tego planu i późniejszej dokumentacji CPC6128 |

## Kolejność implementacji

1. **M0 — kontrakty i fixture’y**: skopiować `cpc6128.rom` z `personal-002`
   (`lib/Machines/Amstrad/Cpc6128/roms/cpc6128.rom`) lub `personal-003`
   (`roms/cpc6128/cpc6128.rom`) do `roms/cpc6128/`; zarejestrować ROM 32 KB,
   AMSDOS ROM 16 KB, standard/Extended DSK, rozmiary i SHA-256; ustalić PAL
   50 Hz i port map.
2. **M1 — projekt i maszyna minimalna**: dodać projekt, kompozycję Z80/ROM/64 KB
   widoku CPU i test pełnego cyklu `IMachine`.
3. **M2 — 128 KB RAM**: zaimplementować mapowanie banków i testy tabelaryczne
   konfiguracji 0–7, zanim zostanie podłączony firmware.
4. **M3 — Gate Array/CRTC/video**: rozszerzyć lub wydzielić kod CPC464 tak, aby
   CPC6128 czytał bazowe video RAM niezależnie od banku CPU.
5. **M4 — PPI, AY, klawiatura i kaseta**: zachować wspólne urządzenia CPC464,
   ale sprawdzić ich kontrakty na portach CPC6128; podłączyć `IAudioOutput`.
6. **M5 — I8272 i DSK**: najpierw kontrakt kontrolera, potem adapter formatu i
   transfer sektorów; nie mieszać z istniejącymi FDC Kaypro/TRS-80.
7. **M6 — prawdziwy ROM**: boot do `Ready`, `PRINT 1+1`, przełączanie banków,
   `|CPM`, `CAT` i odczyt DSK przez firmware.
8. **M7 — Desktop/CLI**: dodać ViewModel, widok, menu, audio, wspólny routing
   capability i debug session.
9. **M8 — snapshot/debug**: snapshot otwartego transferu FDC, kasety, banków,
   PPI, AY, CRTC i następnego kroku CPU; potem endpointy status/screen/text/disk.
10. **M9 — regresja i wydanie**: pełne testy CPC6128/CPC464, build solution,
    headless Debug API i osobny commit dla każdego etapu.

## Kryteria zakończenia

- pięć warstw ma test kontraktu: CPU/machine, RAM/banking, video/raster, I/O/audio
  i FDC/media;
- prawdziwy ROM CPC6128 bootuje do `Ready`, a `PRINT 1+1`, `CAT`, `|CPM` i
  odczyt sektora DSK przechodzą przez rzeczywistą ścieżkę firmware;
- wszystkie osiem konfiguracji RAM oraz overlaye ROM są sprawdzone na granicach;
- audio AY jest podłączone przez `IAudioOutput`, a kaseta i dysk są dostępne z
  Desktop przez capability interfaces;
- snapshot przywraca stan CPU, pełne 128 KB RAM, porty, urządzenia i transfer FDC;
- nie deklarować pełnej zgodności CPC6128 na podstawie samego ekranu `Ready`.

## Ryzyka i decyzje

- `personal-002` używa innych namespace’ów i kontraktów CPU/FDC niż aktualny
  projekt; plan zakłada adaptację, nie kopiowanie drzewa źródłowego.
- Aktualne repo ma CPC464, ale nie ma jeszcze I8272/uPD765 ani adaptera CPC DSK;
  to są osobne zadania przed integracją Desktop.
- CRTC to już samodzielny, przetestowany chip `MT6545` (`lib/PetEmulator.Chips/`),
  współdzielony dziś przez PET i CPC464 — reuse dla CPC6128 niskiego ryzyka.
  Tylko `Cpc464GateArray` (tryby/paleta/border) jest CPC464-specific; przed
  reuse trzeba potwierdzić jej snapshoty, timing i odczyt video RAM dla 128 KB.
- Indeks GitNexus `personal-004` jest nieaktualny; przed każdą zmianą kodu należy
  go odświeżyć i wykonać impact analysis dla modyfikowanych symboli.
- Obrazy ROM i dysków wymagają zachowania metadanych pochodzenia, rozmiaru,
  checksumów i ograniczeń redystrybucji.
