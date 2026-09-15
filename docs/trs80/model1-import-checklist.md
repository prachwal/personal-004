# TRS-80 Model I — checklist importu z personal-003

## Context

`personal-004` ma dziś w `src/PetEmulator.Trs80/` wyłącznie parsery formatów dysków
(`Jv1DiskImage`, `DmkDiskImage`, `NewDos80FileSystem`, `LsDos6FileSystem`) — zero maszyny, zero CPU,
zero wideo/klawiatury/kasety.

W `/home/prachwal/source/emulators/personal-003` istnieje kompletna, wielomodelowa implementacja
TRS-80 (Model I/III/4/4 Native/4P) w C#/.NET, ~2 656 linii kodu TRS-80-specific + własny rdzeń Z80
(`Retro.Z80`, ~2 353 linii) + własny FDC (`Retro.Fdc`, ~2 000 linii) + ~5 000 linii testów + 70 plików
dokumentacji. Model I jest najprostszy: `ModelIMachine.cs` ma 114 linii, `ModelIMemory.cs` 102 linie,
zero bankowania pamięci, zero CRTC, zero RTC/NMI.

**Kluczowe odkrycie:** `personal-004` już ma własny, przetestowany odpowiednik większości
infrastruktury, której używa `personal-003`:

| Potrzebne | personal-003 (źródło wzorca) | personal-004 (już istnieje, użyj tego) |
| --- | --- | --- |
| CPU Z80 | `Retro.Z80` (własny rdzeń) | `lib/PetEmulator.CpuZ80/` — `Z80Cpu`, `IBus` (identyczny kontrakt: `ReadMemory/WriteMemory/ReadPort/WritePort/AcknowledgeInterrupt/NotifyInterruptReturn`) |
| FDC WD1771/WD179x | `Retro.Fdc/Wd179xCore.cs` + `Wd1771Controller.cs` | `lib/PetEmulator.Chips/FD1791.cs` (bazowa maszyna stanów WD179x) + `FD1793.cs` (wariant) — już ma testy (`FD1791Tests.cs`, `FD1791ActivityTests.cs`), `ActivityChanged` event, `IFD1791DiskImage`/`IFD1791SidedDiskImage` |
| Wzorzec okablowania FDC | `Wd1771Controller` (per-machine wrapper) | `src/PetEmulator.Kaypro/KayproFdcWiring.cs` — identyczny wzorzec (drive-select latch, WAIT watchdog, `Tick(tStates)`) do skopiowania/dostosowania |
| Format dysku JV1 | `Retro.Devices/Fdc/Jv1DiskImage.cs` | `src/PetEmulator.Trs80/Jv1DiskImage.cs` — **już istnieje**, tylko brakuje adaptera do `IFD1791DiskImage` |
| Tekstowe wideo (znak+font) | `Retro.TextVideo/VideoController.cs` (generyczny) | Wzorzec per-maszyna: `<Machine>CharacterRomProvider` + `Display/<Machine>RasterDisplay` (patrz `Vic20CharacterRomProvider.cs`, `Vic20RasterDisplay.cs`, `KayproCharacterRomProvider.cs`) — **nie importować generycznego VideoController, iść wzorcem repo** |
| Maszyna implementuje | `ITrs80Machine` (własny, per-projekt) | `IMachine` (`lib/PetEmulator.Core/Abstractions/IMachine.cs`) — `Name/IsReady/CycleCount/Processor/Memory/Reset()/StepInstruction()/Run(ulong)` |

Więc **nie importujemy** `Retro.Z80` ani `Retro.Fdc` (personal-004 ma lepsze, przetestowane
odpowiedniki). Importujemy/portujemy tylko to, czego personal-004 faktycznie nie ma: mapę pamięci
Model I, klawiaturę-matrycę, kasetę (kodowanie Level II), font/generator znaków, drukarkę, i samo
okablowanie maszyny — plus adaptery łączące istniejący `Jv1DiskImage` z istniejącym `IFD1791DiskImage`.

## Recommended approach

Rozszerzyć **istniejący** projekt `src/PetEmulator.Trs80/` (nie tworzyć nowego) o maszynę — dodać
`ProjectReference` do `PetEmulator.Core`, `PetEmulator.CpuZ80`, `PetEmulator.Chips` (dziś projekt nie
ma żadnych referencji — to się musi zmienić, tak jak `Vic20`/`Pet`/`Kaypro` łączą format+maszynę w
jednym projekcie). Docelowo: **Model I only** w tym przebiegu (najprostszy, wystarczy do postawienia
szkieletu; Model III/4 to osobna, późniejsza czeklista).

---

## A. Nowe pliki do utworzenia w `src/PetEmulator.Trs80/`

- [ ] `Trs80MemoryMap.cs` — stałe adresowe portowane 1:1 z `personal-003/src/TRS80.Memory/ModelIMemory.cs`:
      `RomEnd=0x3000`, `KeyboardStart/End=0x3800/0x38FF`, `VideoRamStart/End=0x3C00/0x3FFF`,
      `FdcStart/End=0x37E0/0x37EF`, `PrinterStart/End=0x37E8/0x37E8` (sub-range FDC), kaseta = port `0xFF`
      (I/O, nie pamięć).
- [ ] `Trs80MemoryBus.cs` — implementuje `PetEmulator.CpuZ80.Bus.IBus` (jak `KayproBus`) **oraz**
      `PetEmulator.Core.IMemoryBus` (jak `Vic20MemoryBus`/`KayproBus` robią oba). Dispatch adresowy
      wg wzorca `ModelIMemory.Read/Write` (switch/range-check): ROM (read-only) → drukarka (jeśli
      podłączona) → FDC (0x37E0-EF) → klawiatura (0x3800-38FF) → RAM. Porty I/O
      (`ReadPort/WritePort`): `0xFF` → kaseta. Rozważ dodanie `Action<BusAccess>? Observer` dla
      spójności z Vic20/Pet (opcjonalne — Kaypro go nie ma).
- [ ] `Trs80KeyboardMatrix.cs` — port niemal 1:1 `personal-003/src/TRS80.Devices/Keyboard/ModelIKeyboardMatrix.cs`
      (135 linii, self-contained, zero zależności poza `TRS80Key` enum). Algorytm: `Read(address)`
      maskuje niskim bajtem adresu 8 wierszy, OR-uje zaznaczone. `SetKeyDown(key, down)`. Bez
      debounce (celowo — realny sprzęt, patrz komentarz źródłowy).
- [ ] `Trs80Key.cs` — port enuma z `personal-003/src/Retro.Devices/Keyboard/TRS80Key.cs` (klawisze:
      A-Z, D0-D9, Enter/Clear/Break/kursory/Space/Shift/At/nawiasy/etc. — patrz tabela `Position()`
      w `ModelIKeyboardMatrix.cs` dla pełnej listy).
- [ ] `Trs80CassetteEncoding.cs` + `Trs80CassettePlayer.cs` — port `personal-003/src/TRS80.Devices/Cassette/{CassetteEncoding.cs (66 linii), CassettePlayer.cs (298 linii)}`.
      Level II, 500 bps, sync `0xA5`, port `0xFF`. `.CAS` = surowy strumień bajtów (bez nagłówka
      `CASSETTE1A`/CRC16 — **luka na realnym sprzęcie, nie tylko w tym repo**, patrz sekcja C).
      Implementuje `IIoBus`-owy fragment (`ReadPort(0xFF)`/`WritePort(0xFF, ...)`) — sprawdzić dokładny
      kontrakt `IIoBus` w `lib/PetEmulator.CpuZ80/Bus/IIoBus.cs` przed portowaniem.
- [ ] `Trs80CharacterRomProvider.cs` — wzorowany na `Vic20CharacterRomProvider.cs`/`KayproCharacterRomProvider.cs`
      (sprawdzić dokładny kontrakt `ICharacterRomProvider`/wzorzec ładowania w tych dwóch plikach).
      Dekoduje font MCM6670/6673 — logika dekodowania w `personal-003/src/Retro.Devices/CharacterGenerators/Mcm667x.cs`
      (63 linie, portować algorytm bitowy, nie całą klasę 1:1).
- [ ] `Display/Trs80RasterDisplay.cs` — wzorowany na `Display/Vic20RasterDisplay.cs` /
      `Display/PetRasterDisplay.cs`. Model I: 64x16 znaków, 384x192 px, znak 6x12, video RAM
      `0x3C00-0x3FFF`, 262 scanlines, 113 T-state/scanline (stałe z `personal-003`'s
      `TrsVideoProfiles.cs`, 63 linie — port stałych, nie generycznego `VideoController`).
      Glyph-code mapping: port `TrsGlyphCodeMapper.cs` (10 linii, trywialne) jeśli TRS-80 ma
      nietrywialne mapowanie kod-ekranowy→glif (do zweryfikowania przy porcie).
- [ ] `Trs80Printer.cs` — port `personal-003/src/TRS80.Devices/Printer/CentronicsPrinter.cs` (87 linii).
      Model I: pamięć `0x37E8` (nie port). Status/data, bez timingu BUSY/ACK (luka realnego sprzętu
      w personal-003 też — nie pogarszać, ale nie trzeba naprawiać przy imporcie).
- [ ] `Trs80FdcWiring.cs` — **nowy plik, nie port** — analogiczny do `src/PetEmulator.Kaypro/KayproFdcWiring.cs`,
      ale dla Model I: drive-select latch na `0x37E0-0x37E3` (pamięć, nie port — inaczej niż Kaypro),
      rejestry WD1771 na `0x37EC-0x37EF`. Opakowuje `FD1791`/`FD1793` (sprawdzić który wariant bus-mode
      pasuje do WD1771 — patrz `Fd179xDataBusMode` enum, `FD1793.DataBusMode => True`; WD1771 to
      **single-density, prawdopodobnie ten sam `True` bus mode** — zweryfikować przy imporcie, nie
      zakładać).
- [ ] `Trs80DiskImageAdapter.cs` — **nowy plik** — cienki adapter: `Jv1DiskImage` (już istnieje w tym
      repo) → implementuje `IFD1791DiskImage` (`WriteProtected`, `SectorSize`, `TryReadSector`,
      `TryWriteSector`, itd. — patrz `lib/PetEmulator.Chips/IFD1791DiskImage.cs`). `Jv1DiskImage` już
      ma `ReadSector(track,sector)`/`WriteSector(...)` — mapowanie powinno być mechaniczne.
- [ ] `Trs80Machine.cs` — główna klasa, wzorowana na `KayproMachine.cs` (najbliższy szablon: Z80,
      brak RomLoader/Manifest — TRS-80 ROM też prawdopodobnie jednym plikiem, jak Kaypro
      `LoadMonitorRom(ReadOnlySpan<byte>)`, **albo** wzorzec `Vic20RomLoader`/`Vic20RomManifest` jeśli
      wolimy manifest — do decyzji, Kaypro-style jest prostsze i wystarczające dla Model I jednego ROM
      pliku 12KB). Implementuje `IMachine`. Konstruktor buduje: `Trs80KeyboardMatrix`,
      `Trs80FdcWiring?` (tylko gdy dysk podłączony — **ważne**: personal-003 świadomie NIE podłącza
      FDC gdy brak dysku, bo realny ROM busy-polluje DRQ w nieskończoność czekając na napęd NOT READY
      — patrz komentarz w `ModelIMachine.cs` konstruktorze, ten sam trik zastosować tu),
      `Trs80CassettePlayer?` (opcjonalna, jak Kaypro/Vic20 tape), `Trs80Printer`,
      `Trs80MemoryBus`, `Z80Cpu(bus, interruptLines, cycleObserver: ...)`, `Trs80RasterDisplay`.
      `StepInstruction()`: wzorzec Kaypro (`Processor.StepInstruction()`, licz delta cykli, `Bus.Tick(delta)`
      tickujący FDC/kasetę/wideo).
- [ ] `Trs80Machine.csproj` zmiany — dodać `<ProjectReference>` do `../../lib/PetEmulator.Core`,
      `../../lib/PetEmulator.CpuZ80`, `../../lib/PetEmulator.Chips` w istniejącym
      `PetEmulator.Trs80.csproj` (dziś zero referencji).

## B. ROM-y i assety

- [ ] Skopiować/zweryfikować Level II ROM z `personal-003/roms/model1/` (warianty `level1`, `v1.2`,
      `v1.3`, `v1.4`) do `roms/trs80/model1/` w tym repo — sprawdzić, który wariant ma personal-003
      jako domyślny/najlepiej przetestowany (patrz `ModelIMachineFactory.cs`/testy).
- [ ] Font/character generator ROM (MCM6670/6673 dump) — zlokalizować plik źródłowy w personal-003
      (prawdopodobnie obok ROM-ów lub w assets dekodera) i skopiować.
- [ ] Dyski testowe: ten repo **już ma** `roms/trs80/newdos80-sssd-system.jv1` — użyć go jako
      pierwszego smoke-testu bootowania zamiast szukać nowego.
- [ ] Sprawdzić status licencyjny/copyright ROM-ów tak samo jak dla istniejących `roms/vic20`,
      `roms/pet` w tym repo (ten sam standard, nic nowego do ustalania — po prostu być spójnym).

## C. Znane luki do zaimplementowania (NIE tylko import — realne braki nawet w personal-003)

Z `personal-003/TRS-80_COMPLETE_DOCUMENTATION/TRS80/Plans/03_CURRENT_IMPLEMENTATION_STATUS.md`
(źródło prawdy, stan 2026-09-04) — te luki **przyjeżdżają razem z importem**, nie są nowe:

- [ ] **Boot do CLOAD niestabilny** — realny ROM boot Model I nie dochodzi do stabilnego stanu przed
      CLOAD (`CassetteClOadTests` w personal-003 to dokumentuje jako known gap). Do zbadania po
      imporcie: czy ten sam problem występuje w porcie, czy to artefakt konkretnego ROM-u/testu.
- [ ] **WD1771/FD1791 braki**: brak CRC, READ ADDRESS, READ TRACK, STEP, side select — sprawdzić czy
      `personal-004`'s `FD1791` ma te komendy już zaimplementowane (możliwe, że lepiej niż
      personal-003 — do zweryfikowania osobno, nie zakładać że luka się powtarza).
- [ ] **Kaseta**: `.CAS` bez nagłówka `CASSETTE1A`/CRC16 — jeśli chcemy współpracować z plikami `.cas`
      z innych emulatorów (np. z internetu), ten kontener trzeba dopisać.
- [ ] **Klawiatura**: brak debounce (celowe, nie luka), BREAK nie generuje NMI/RESET sprzętowo
      (zgodne z realnym sprzętem — nic do zrobienia).
- [ ] **Drukarka**: brak timingu BUSY/ACK/handshake Centronics — kosmetyczne, nieblokujące.
- [ ] Sprawdzić czy `Trs80FdcWiring`'s WAIT-line trik (Model I nie ma linii WAIT na FDC jak Kaypro/Model4 —
      **do zweryfikowania**: `personal-003` nie wspomina WAIT dla Model I w statusie, tylko dla
      Model IV Native — prawdopodobnie Model I FDC nie potrzebuje `BeginWait()`/`WaitAsserted` wcale,
      tylko proste DRQ/INTRQ polling przez ROM. Nie kopiować mechanizmu WAIT z Kaypro bez potwierdzenia.

## D. Integracja CLI (`src/PetEmulator.Cli/`)

- [ ] `Trs80DebuggerSession.cs` — wzorowany na `Vic20DebuggerSession.cs`: `Execute(commandLine)` z
      komendami `roms`, `disk` (JV1/DMK auto-detect — `Jv1DiskImage`/`DmkDiskImage` już mają sniffer
      `LooksLikeDmk`), `tape`, `key`, `status`, fallback do `MachineDebugger` generyczny (`trace`/
      `watch`/`dump`/`break-*`).
- [ ] Nowy verb `trs80-debug` w `Program.cs` (`CliCommandFactory.Create()`), analogiczny do
      `vic20-debug`/`RunVic20DebugScript` → `RunTrs80DebugScript`.
- [ ] Ewentualnie `trs80-dir` CLI verb analogiczny do istniejącego `d64-dir`, ale nad
      `NewDos80FileSystem`/`LsDos6FileSystem` (repo ma już te klasy — tylko brakuje CLI wrappera).

## E. Integracja Desktop (`src/PetEmulator.Desktop/`)

- [ ] `ViewModels/Trs80MachineViewModel.cs` — wzorowany na `KayproMachineViewModel.cs` (najbliższy:
      `Tick()` = `_machine.Run(20_000)` + render + status text), implementuje `IMachineViewModel` +
      `IDiskDriveViewModel` (+ tape interfejs jeśli istnieje coś jak `IDatasetteViewModel` dla kasety).
- [ ] `Views/Trs80MachineView.axaml` + `.axaml.cs` — para Avalonia, 1:1 wzorzec
      `KayproMachineView.axaml`/`Vic20MachineView.axaml`.
- [ ] `MainWindowViewModel.cs` — dodać `ModuleMenuEntry("TRS-80 Model I", () => new Trs80MachineViewModel(_romsRoot))`
      do `ModuleChoices`, plus `case Trs80MachineViewModel trs80: ...` w każdym switchu, w którym
      uczestniczy Kaypro (dysk) i Vic20 (taśma, jeśli kaseta wspierana).

## F. Testy

- [ ] Sprawdzić framework: `PetEmulator.Trs80.Tests` już używa **NUnit + FluentAssertions** (nie
      xUnit jak personal-003) — testy portowane z personal-003 trzeba przepisać na NUnit-owe
      asercje/atrybuty, nie kopiować 1:1.
- [ ] Priorytet portowania testów z personal-003 (`tests/TRS80.Machines.ModelI.Tests/`,
      `TRS80.Memory.Tests/`, `TRS80.Devices.Tests/` — dotyczące Model I): boot smoke test, keyboard
      matrix mapping, memory map dispatch (brak kolizji adresów), cassette round-trip (CSAVE→CLOAD).
- [ ] Nowy test: `Trs80DiskImageAdapterTests.cs` — potwierdzić że adapter `Jv1DiskImage`→`IFD1791DiskImage`
      poprawnie czyta istniejący `roms/trs80/newdos80-sssd-system.jv1` przez `FD1791`/`FD1793`.
- [ ] Boot-smoke test analogiczny do `EveryImplementedProfile_BootsWithoutThrowing_ForBoundedSteps`
      (wzorzec z PET testów) dla `Trs80Machine`.

## G. Gate'y GitNexus (obowiązkowe wg CLAUDE.md tego repo)

- [ ] `impact()` przed każdą edycją współdzielonych symboli, które port dotyka (`FD1791`/`FD1793`
      jeśli cokolwiek się w nich zmienia — nie powinno, ale zweryfikować), `Z80Cpu` (jeśli trzeba
      cokolwiek rozszerzyć — nie powinno).
  - **UNKNOWN risk = nierozstrzygnięte**: pusty zbiór callerów nie znaczy "bezpiecznie" — potwierdzić
      grepem, nie tylko impact().
- [ ] `detect_changes({scope: "all"})` przed każdym commitem tej pracy.
- [ ] Po dodaniu `Trs80Machine` do grafu — rozważ dodać wpis do `docs/trs80/` analogiczny do
      `docs/vic20/`, `docs/kaypro/` (ten import sam w sobie zasługuje na `docs/trs80/model1-status.md`
      po zakończeniu, wzorem `03_CURRENT_IMPLEMENTATION_STATUS.md` z personal-003, ale po polsku/wg
      konwencji tego repo).

## Weryfikacja end-to-end (po implementacji)

1. `dotnet build src/PetEmulator.Cli` — kompiluje się czysto.
2. `dotnet test tests/PetEmulator.Trs80.Tests` — zielone, zero regresji istniejących testów JV1/DMK/
   NewDos80/LsDos6.
3. `dotnet run --project src/PetEmulator.Cli -- trs80-debug <script>` — boot Model I z realnym ROM-em,
   `trace 200000`+ do READY prompt (analogicznie do VIC-20 boot-wait w `docs/vic20/via-nmi-irq-diagnosis.md`
   Methodology), potwierdzić ekran przez `dump 3C00 3FFF` zdekodowany do ASCII.
4. Załadować `roms/trs80/newdos80-sssd-system.jv1` przez FDC, potwierdzić że `NewDos80FileSystem`
   (już istniejący w repo) widzi tę samą zawartość read przez emulowany WD1771 co bezpośredni parser
   dysku — cross-check dwóch niezależnych ścieżek do tych samych bajtów.
5. Desktop: uruchomić `PetEmulator.Desktop`, wybrać "TRS-80 Model I" z menu, potwierdzić że okno się
   renderuje i klawiatura reaguje.
