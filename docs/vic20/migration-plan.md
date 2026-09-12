# Plan migracji VIC-20

**Status: v1 zaimplementowany i działa** (boot do BASIC READY, klawiatura, real ROM-y, debug
tooling za darmo - patrz `docs/vic20/testing-strategy.md`). Kroki 0-10, 12 (Layer 0/1/2/3) i 13
zrobione. Krok 11 (rendering Desktop) świadomie pominięty - poza zakresem "silnik działa"; nikt
jeszcze o niego nie prosił (YAGNI).

Kontekst: kandydat źródłowy `cpu-vibe-001` (`Cpu.Vic20`) - jedyny z realnym dowodem boota do BASIC
READY (`Vic20BootTests`) na prawdziwych ROM-ach. **Nic stamtąd nie kompiluje się 1:1** - inny CPU
core (`Mos6502.Core`/`MachineBoard`/`IDevice` vs ten repo's `Cpu6502Classic`/`IProcessor`/
`IMemoryBus`). Import = przepisać logikę/dane pod wzorzec `PetMachine`, nie copy-paste plików.
Cel v1: `Vic20Machine` bootuje do BASIC READY na realnym KERNAL-u, klawiatura działa, ekran
tekstowy (w tym multicolor) się renderuje. Poza zakresem v1: cartridge, PAL (NTSC only), banking
presets (start: tylko "unexpanded", $2000-$7FFF/$A000 wolne).

## Krok 0 — decyzja: `BusAccess`/`Observer` współdzielony czy zduplikowany

`BusAccess`/`PetMemoryBus.Observer`/`InstructionTracer` (`docs/pet/debug-tools.md`) są dziś
w `PetEmulator.Pet`, ale nic w nich nie jest PET-specyficzne - to generyczny "obserwuj każdy
Read/Write" wzorzec. VIC-20 to drugi konsument: albo (a) przenieść `BusAccess`+`Observer`-shape
do `PetEmulator.Core` teraz (jeden ruch, `impact()` na `PetMemoryBus`/`BusAccess` obowiązkowy,
`PetEmulator.Pet` dostaje `using PetEmulator.Core` zamiast własnej definicji - zero zmiany
zachowania), albo (b) zduplikować `Vic20MemoryBus.Observer` z własnym `BusAccess` copy-paste.
(a) unika drugiej definicji tego samego rekordu w dwóch namespace'ach na zawsze - **rekomendacja:
(a)**, zrobić to jako pierwszy, izolowany commit przed czymkolwiek innym (najmniejsze ryzyko,
łatwo zweryfikować pełną regresją PET zanim doda się VIC-20).

## Krok 1 — szkielet projektu

Nowy `src/PetEmulator.Vic20/` (mirror layoutu `PetEmulator.Pet/`: `Keyboard/`, `Video/`,
`Vic20Machine.cs`, `Vic20MemoryBus.cs`, `Vic20Profile.cs`). Referencja do `PetEmulator.Core` +
`PetEmulator.Cpu6502` (już CPU-agnostyczne, zero zmian potrzebnych tam). Dodać do
`PetEmulator.slnx` + `tests/PetEmulator.Vic20.Tests/`.

## Krok 2 — chip: `MOS6560`

Przepisać z referencją na `cpu-vibe-001`'s `MOS6560Chip.cs` (191 linii, samodzielny: 16-rejestrowa
tablica + computed properties na Raster/Columns/Rows/ScreenAddr/CharAddr/AuxColor/ScreenColor/
ReverseMode + audio-oscylatory) w kształcie zgodnym z istniejącymi chipami tego repo
(`MOS6522`/`MT6545`: `BaseAddress`, `Length`, `Read`/`Write`, `Tick(cycles)` zamiast `Update()`
per-cycle). NTSC-only na start (`MOS6560Constants.Phi2Ntsc`/`CyclesPerLineNtsc`/
`TotalScanlinesNtsc` - stałe, importowalne wprost, to fakty sprzętowe). Audio jest dostarczane przez
`PetEmulator.Audio` i odtwarzane z `Vic20MachineViewModel`.

**Impact-check**: nowy plik, brak istniejących callerów - bez ryzyka.

## Krok 3 — chip: Color RAM

`ColorRamDevice` (28 linii źródłowych) - trywialny 4-bitowy nibble RAM $9400-$97FF. Przepisać
1:1 w kształcie istniejących chipów (jak `Pia`: `BaseAddress`+`Read`/`Write`).

## Krok 4 — VIA reuse (zero nowego kodu)

`MOS6522` **już istnieje i jest generyczny** - VIC-20 ma dwa VIA-e ($9110 VIA1: klawiatura+
joystick+cassette, $9120 VIA2: klawiatura+serial) zamiast PET-owego PIA1/PIA2/VIA. Instancjonować
`new MOS6522("VIA1", 0x9110)` / `new MOS6522("VIA2", 0x9120)` bezpośrednio - żadnej nowej klasy
chipu. Sprawdzone: `PortAWritten`/`PortBInput` API identyczne z tym co `PetMachine` już robi na
PIA1 dla klawiatury PET.

## Krok 5 — pamięć/bus

`Vic20MemoryBus : IMemoryBus`, stałe z `Vic20MemoryMap` (importowalne wprost - realne adresy:
VIC $9000, VIA1 $9110, VIA2 $9120, color RAM $9400, char ROM $8000, BASIC $C000, KERNAL $E000).
V1 dekoduje tylko "unexpanded" layout (RAM $0000-$03FF + $1000-$1FFF, reszta open-bus) - presety
ekspansji ($3K/$8K/$16K/$24K/All, `Vic20ExpansionPreset` enum) to osobny, późniejszy krok (YAGNI:
BASIC boot i mały program działają bez nich, jak na realnym nierozszerzonym VIC-20).

**Impact-check**: `IMemoryBus` - sprawdzić przed implementacją mimo że to nowa klasa (interfejs
współdzielony z PET).

## Krok 6 — klawiatura

`Vic20KeyboardMatrix` (8x8) - kształt bliski istniejącemu `PetKeyboardMatrix`, ale row-select
przez ORA&DDRA (nie osobny "wybierz wiersz" zapis jak PIA1 PortA na PET) - patrz
`Vic20KeyboardViaBinding.SyncToVia`, wzorzec 1:1 z `PetMachine`'s PIA1 `PortAWritten`/
`PortAInput`/`PortBInput` wiring. `AtKeyboardKey` z `lib/PetEmulator.Core` jest globalnym katalogiem
fizycznych klawiszy; mapowanie AT → matrix jest nadpisywane przez `Vic20KeyboardMap`, a dane
row/col→znak pozostają w VIC-20 jako tabela ROM-zweryfikowana. Importowalne jako
tabela, zaadaptowane pod nowy `IVic20KeyboardMap` (analog `IPetKeyboardMap`) tak żeby
`TextTyper.Type` działał bez zmian (już CPU/maszyno-agnostyczny - sprawdzić przy implementacji).

## Krok 7 — ROM-y

`roms/vic20/{basic.bin, kernal.bin, vic20-chargen.bin}` - realne binarki z `cpu-vibe-001`
(tego samego rodzaju co `roms/pet/*`, ta sama decyzja licencyjna już podjęta w tym repo).
Sprawdzić czy `PetRomLoader`/`PetRomImage` jest generyczne wystarczająco do reuse, czy potrzeba
`Vic20RomLoader` - prawdopodobnie generyczne (ładuje `(adres, plik)` pary), do potwierdzenia przy
implementacji.

## Krok 8 — `Vic20Machine`

Mirror `PetMachine` 1:1 w strukturze (inny bus/chipy, ten sam kształt):
`StepInstruction()` → `Processor.StepInstruction()` → `via1.Tick(cycles)`/`via2.Tick(cycles)`/
`vic.Tick(cycles)` → `SetIRQ(via1.IRQ || via2.IRQ)`. `BusObserver`/`Devices`/`RunUntil`/
`RunUntilOrStalled` "za darmo" jeśli krok 0 (a) wybrany - identyczne API co `PetMachine` już ma.

## Krok 9 — pierwszy dowód: boot do READY

**Pierwsza bramka poprawności, przed czymkolwiek dalej.** Port metodologii
`Vic20BootTests.Boot_ReachesBasicReady_AndScreenHasText` na wzorzec tego repo (`RunUntil` +
liczenie niepustych znaków na ekranie zamiast szukania literalnego "READY.", bo VIC-20 layout
ekranu inny niż PET) - Layer 2 w stylu `docs/pet/disk-testing-strategy.md`. Dopóki to nie
przechodzi, reszta (rendering, klawiatura pełnym skryptem) nie ma sensu weryfikować.

## Krok 10 — debug tooling (za darmo)

`MachineDebugger` już CPU-agnostyczny (`trace`/`watch`/`dump`/`break-pc` - patrz
`docs/pet/debug-tools.md`) - działa na `Vic20Machine : IMachine` bez zmian. Nowy
`Vic20DebuggerSession` mirroring `PetDebuggerSession` (profile/roms/key/type/status), plus
`trace-log`/`disk-stall-check`-analogi jeśli krok 0 (a) wybrany (inaczej: duplikować albo pominąć
w v1).

## Krok 11 — rendering (Desktop)

**Nie reużyć `PetScreenControl` bez zmian** - VIC-20 to pixel framebuffer z multicolor (2-bit pary
pikseli per `Vic20Video.RenderChar`), PET to prosty tekst-grid przez font bitmapy. Nowy
`Vic20ScreenControl`, port algorytmu `RenderChar` (multicolor: `glyphRow >> (6 - x/2) & 0x03` →
4 kolory z ScreenColor/AuxColor/paper/ink). `Vic20Palette` (16 RGB) importowalna wprost.

## Krok 12 — pełna strategia testów

Mirror 4-warstwowej struktury z `docs/pet/disk-testing-strategy.md`:

- Layer 0: `MOS6560Tests` (register map/raster/columns/rows) - metodologia inspirowana
  `cpu-vibe-006`/`personal-002`'s MOS6560 testami (nie kopiowana - inny chip-shape tutaj).
- Layer 1: register-level bus integration (poke VIC/VIA rejestry bezpośrednio, bez KERNAL-a).
- Layer 2: krok 9 rozszerzony (klawiatura przez `TextTyper`, prosty program `PRINT`/`GOTO`).
- Layer 3: smoke script (`scripts/test-vic20-boot.sh`, mirror `scripts/test-pet-*-loading.sh`).

## Krok 13 — dokumentacja

`docs/desktop/debug-monitoring-plan.md` (albo rozszerzyć `docs/pet/debug-tools.md` na oba, jeśli krok 0 (a)) +
`docs/vic20/testing-strategy.md` po ukończeniu kroku 12.

## Bramki (każdy krok)

`impact()` przed edycją (szczególnie krok 0 - dotyka istniejącego `PetEmulator.Pet` kodu),
`detect_changes({scope:"all"})` przed commitem, pełna regresja (`dotnet test PetEmulator.slnx`)
przed każdym commitem, reindex GitNexus. Commit per krok (nie jeden wielki PR) - zgodnie z rytmem
tej sesji.

## Czego NIE robić w v1

- Cartridge loading, tape/disk (VIC-20 miał inny format taśmy niż PET - osobna decyzja później).
- Audio (3 oscylatory + noise - `MOS6560Chip.GetAudioSample` istnieje jako referencja, ale nic
  w "boot do READY" go nie wymaga).
- PAL mode, banking presets poza "unexpanded" - dodać gdy faktycznie ktoś chce uruchomić program
  wymagający rozszerzonej pamięci (YAGNI, nie przed).
- Kopiowanie `Vic20Machine`/`MachineBoard` z `cpu-vibe-001` bezpośrednio - inny CPU core, inny
  bus - struktura tak, kod nie.
