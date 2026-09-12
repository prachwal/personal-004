# Plan: wspólna abstrakcja CPU, State i opcode

## Cel

Zbudować w `PetEmulator.Core` abstrakcyjną bazę procesora, z której mogą korzystać 6502, MC6800/MC6809 i Z80, bez udawania, że ich rejestry, magistrale, przerwania i timing są takie same. Następnie przenieść opis opcode’ów do jednego rozszerzalnego mechanizmu rejestracji, w którym klasa potomna może dodawać lub zastępować instrukcje.

Najważniejsza reguła: wspólna baza kontroluje lifecycle kroku, ale nie zna rejestrów, flag, endianowości, sposobu dekodowania ani semantyki rodziny.

## Stan obecny

- `IProcessor` jest publicznym kontraktem lifecycle, liczników i IRQ/NMI.
- 6502 ma własną klasę bazową `Cpu6502`, warianty pochodne i własny `OpcodeDefinition` z handlerem zależnym od `Cpu6502`.
- `M6800Cpu` ma własny `M6800State`, `M6800OpcodeTable` i implementacje instrukcji w partial classes.
- `M6809Cpu : M6800Cpu` rozszerza stan o DP/Y/U/S, dodaje strony opcode’ów i FIRQ.
- Z80 zachowuje własny `IBus`, `BusCycle`, WAIT, I/O, refresh i tryby przerwań.

Wniosek: można ujednolicić lifecycle i infrastrukturę opcode’ów, ale nie należy przenosić do Core rodzinnych rejestrów ani wymuszać jednej implementacji magistrali.

## Proponowane drzewo typów

```text
IProcessor
└── CpuProcessorBase<TState>
    ├── Cpu6502
    │   ├── Classic6502
    │   ├── Cmos65C02
    │   ├── WdcR65C02S
    │   ├── Commodore6510
    │   ├── Nes6502
    │   └── Atari6507
    ├── M6800Cpu
    │   └── M6809Cpu
    └── Z80Cpu
```

Stan:

```text
CpuState
├── Cpu6502State
├── M6800State
│   └── M6809State
└── Z80State
```

`M6809State : M6800State` pozostaje zachowane. Migracja nie powinna usuwać relacji 6800→6809; zmienia się tylko wspólna infrastruktura pod spodem.

## 1. Abstrakcyjny State

State ma przechowywać stan architektoniczny i minimalny stan lifecycle. Nie powinien przechowywać osobnego źródła czasu, ponieważ obecne `M6800State.Cycles` i Core `IClock` mogą się rozjechać.

```csharp
public abstract class CpuState
{
    public bool Halted { get; internal set; }

    public virtual void Reset()
    {
        Halted = false;
    }

    public abstract IReadOnlyDictionary<string, ushort> GetRegisters();
}
```

W praktycznej migracji `GetRegisters` może pozostać kontraktem `IDebuggableProcessor`, a State może udostępniać chroniony/publiczny projekt debugowy zależnie od istniejących konsumentów. Istotne jest, aby Core nie zakładał, że każdy CPU ma A/B/X albo 16-bitowy PC.

```csharp
public sealed class M6800State : CpuState
{
    public byte A { get; internal set; }
    public byte B { get; internal set; }
    public ushort X { get; internal set; }
    public ushort StackPointer { get; internal set; }
    public ushort ProgramCounter { get; internal set; }
    public M6800Flags Flags { get; } = new();

    // Tymczasowa projekcja kompatybilności, nie źródło czasu.
    public long Cycles { get; internal set; }

    public override void Reset()
    {
        base.Reset();
        A = B = 0;
        X = StackPointer = ProgramCounter = 0;
        Flags.Reset();
        Cycles = 0;
    }
}
```

Docelowo `Cycles` należy usunąć ze stanu 6800 po migracji wszystkich konsumentów; licznik procesora i `IClock` mają jedno źródło prawdy. `M6809State` nadpisuje/rozszerza reset oraz stack pointer bez duplikowania stanu bazowego.

## 2. Abstrakcyjny Processor

Baza powinna posiadać jeden stabilny publiczny lifecycle. Nie należy pozwolić, by każda rodzina nadpisywała całe `Step()` i omijała wspólne zasady liczników lub zegara. Różnice wystawiamy jako chronione hooki.

```csharp
public abstract class CpuProcessorBase<TState> : IProcessor, IDebuggableProcessor
    where TState : CpuState
{
    protected CpuProcessorBase(TState state, IMemoryBus memory, IClock clock)
    {
        State = state;
        Memory = memory;
        Clock = clock;
        Opcodes = new OpcodeTable<TState>();
        ConfigureOpcodes(Opcodes);
    }

    protected TState State { get; }
    protected IMemoryBus Memory { get; }
    protected IClock Clock { get; }
    protected OpcodeTable<TState> Opcodes { get; }

    public bool Halted => State.Halted;
    public ulong CycleCount => Clock.CycleCount;
    public ulong InstructionCount { get; private set; }

    public void Reset()
    {
        State.Reset();
        Clock.Reset();
        InstructionCount = 0;
        OnReset();
    }

    public void StepInstruction()
    {
        var result = ExecuteStep();
        if (result.InstructionCompleted)
            InstructionCount++;
    }

    protected virtual CpuStepResult ExecuteStep()
    {
        BeforeStep();

        if (TryServiceInterrupt(out var interruptResult))
            return Complete(interruptResult);

        if (TryHandleWait(out var waitResult))
            return Complete(waitResult);

        var opcode = FetchOpcode();
        var definition = DecodeOpcode(opcode);
        var result = ExecuteOpcode(definition);
        return Complete(result);
    }

    protected virtual void BeforeStep() { }
    protected virtual bool TryServiceInterrupt(out CpuStepResult result)
        => ReturnFalse(out result);
    protected virtual bool TryHandleWait(out CpuStepResult result)
        => ReturnFalse(out result);
    protected abstract OpcodeKey FetchOpcode();
    protected abstract OpcodeDefinition<TState> DecodeOpcode(OpcodeKey key);
    protected abstract CpuStepResult ExecuteOpcode(OpcodeDefinition<TState> definition);
    protected virtual void ConfigureOpcodes(OpcodeTable<TState> table) { }
    protected virtual void OnReset() { }
    protected virtual CpuStepResult Complete(CpuStepResult result)
    {
        Clock.Advance(result.Cycles);
        return result;
    }

    public abstract IReadOnlyDictionary<string, ushort> GetRegisters();

    private static bool ReturnFalse(out CpuStepResult result)
    {
        result = default;
        return false;
    }
}
```

To jest szkic kierunku, nie gotowy patch. W implementacji trzeba dopasować widoczność `State`, istniejący `Step()` oraz semantykę HALT/WAIT. Jeżeli istnieją konsumenci wywołujący `Step()`, baza powinna mieć publiczny adapter `Step()` albo zachować dotychczasową nazwę i używać `StepInstruction()` jako zgodności.

Nie wszystkie hooki muszą być `abstract`. Abstract powinny być tylko elementy bez sensownej wartości domyślnej: fetch/decode/execute. Lifecycle, reset, WAIT i interrupt hooks powinny mieć bezpieczne implementacje domyślne lub być nadpisywane przez rodzinę.

## 3. Wynik kroku i kontekst wykonania

```csharp
public readonly record struct CpuStepResult(
    ulong Cycles,
    bool InstructionCompleted,
    bool InterruptServiced = false,
    bool Waiting = false);

public sealed class CpuExecutionContext
{
    public required IMemoryBus Memory { get; init; }
    public IPortBus? Ports { get; init; }
    public IClock? Clock { get; init; }
    public object? CycleObserver { get; init; }
}
```

`CpuStepResult` ujednolica raportowanie kroku, ale nie narzuca liczby cykli. Z80 nadal może emitować bogaty `BusCycle`, a 6800/6809 mogą mieć prostszy model. `CpuExecutionContext` powinien być przekazywany tylko tam, gdzie jest potrzebny; nie wolno wkładać do niego zależności Z80 do Core.

## 4. Jeden standard opcode’ów, rozszerzalny w klasie potomnej

Wspólny standard powinien ujednolicić klucz, metadane i rejestrację, ale handler ma pozostać typowany stanem rodziny.

```csharp
public readonly record struct OpcodeKey(byte Page, byte Opcode)
{
    public static OpcodeKey Base(byte opcode) => new(0, opcode);
}

public sealed record OpcodeDefinition<TState>(
    OpcodeKey Key,
    string Mnemonic,
    byte Length,
    byte BaseCycles,
    string AddressingMode,
    Func<TState, CpuExecutionContext, CpuStepResult> Execute,
    bool HasPageCrossPenalty = false);

public sealed class OpcodeTable<TState>
{
    private readonly Dictionary<OpcodeKey, OpcodeDefinition<TState>> _entries = new();

    public void Add(OpcodeDefinition<TState> definition)
    {
        if (!_entries.TryAdd(definition.Key, definition))
            throw new InvalidOperationException($"Opcode {definition.Key} already registered.");
    }

    public void Replace(OpcodeDefinition<TState> definition)
        => _entries[definition.Key] = definition;

    public bool TryGet(OpcodeKey key, out OpcodeDefinition<TState> definition)
        => _entries.TryGetValue(key, out definition!);

    public IReadOnlyCollection<OpcodeDefinition<TState>> Entries => _entries.Values;
}
```

Wariant/family rejestruje opcode’y następująco:

```csharp
protected override void ConfigureOpcodes(OpcodeTable<M6809State> table)
{
    base.ConfigureOpcodes(table);       // instrukcje odziedziczone z 6800
    RegisterM6809Page10(table);         // prefiks 0x10
    RegisterM6809Page11(table);         // prefiks 0x11
    Replace6800Variant(table);          // tylko, jeśli semantyka wymaga zmiany
}
```

Dla zachowania relacji `M6809Cpu : M6800Cpu` potrzebny będzie jeden z dwóch wariantów:

1. `M6800Cpu` i `M6809Cpu` używają wspólnego nietypowanego rejestru metadanych, a typed handler jest opakowany adapterem.
2. Baza procesora ma drugi parametr typu stanu rodziny, a `M6809Cpu` rejestruje odziedziczone instrukcje przez adapter `M6800State → M6809State`.

Preferowany jest wariant pierwszy na poziomie publicznego rejestru oraz osobne, typowane executory w rodzinach. Nie należy robić castów `CpuState`→konkretny CPU w każdym handlerze. Page/prefix jest częścią `OpcodeKey`, więc 6809 `0x10/0x11` i Z80 `CB/ED/DD/FD` mają ten sam mechanizm lookupu, ale rodzinny dekoder nadal kontroluje, ile bajtów prefiksu zużywa.

Zasady rejestru:

- instancja tabeli, nie globalny mutable static;
- `Add` wykrywa konflikt;
- `Replace` jest jawne i testowane;
- `TryGet` nie zna semantyki instrukcji;
- kolejność/opcode handlerów nie może zmieniać publicznych liczników cykli;
- brak jednej globalnej listy enumów dla wszystkich CPU.

## 5. Kolejność migracji

### Etap 0 — kontrakt i baseline

- Uruchomić build i pełny zestaw testów.
- Spisać zachowanie `Step`, `StepInstruction`, resetu, HALT, WAIT, IRQ/NMI/FIRQ, liczników i debug rejestrów.
- Dodać testy kontraktowe dla nowej bazy na sztucznym procesorze.

### Etap 1 — Core bez migracji rodzin

- Dodać `CpuState`, `CpuStepResult`, `CpuExecutionContext`, `CpuProcessorBase<TState>`.
- Dodać `OpcodeKey`, metadane i `OpcodeTable<TState>`.
- Nie zmieniać jeszcze publicznych implementacji 6502/6800/6809/Z80.
- Zweryfikować reset, jednorazowe naliczanie cykli i konflikt rejestracji.

### Etap 2 — 6502 jako implementacja referencyjna

- Wydzielić `Cpu6502State` z pól obecnego `Cpu6502`.
- Przenieść metadane z obecnego `OpcodeDefinition` do wspólnego formatu.
- Zachować warianty 6502 jako klasy pochodne rejestrujące/zmieniające opcode’y.
- Zachować dokładnie BCD, page-cross penalty, IRQ/NMI, `Tick()` i istniejące cykle.
- Dopiero po regresji usunąć starą tabelę.

### Etap 3 — MC6800

- Zmienić `M6800State : CpuState`; pola rejestrów pozostają własnością MC6800.
- Przenieść `M6800OpcodeTable` na `OpcodeTable<M6800State>` albo adapter kompatybilności.
- Przenieść `M6800Cpu.Step/StepInstruction` do hooków bazy.
- Pozostawić implementacje ALU, branch, load/store, unary i index jako rodzinne executory.
- `State.Cycles` utrzymywać tylko jako tymczasową projekcję zgodności i synchronizować z jednym zegarem.

### Etap 4 — MC6809 jako rozszerzenie MC6800

- Zachować `M6809State : M6800State` i `M6809Cpu : M6800Cpu`.
- Bazowa konfiguracja rejestruje opcode’y 6800; 6809 dopisuje page 10/11.
- Dodać do wspólnego klucza prefiks/page bez zmian w semantyce frame stack.
- Przenieść kolejność NMI → FIRQ → IRQ → HALT → fetch/execute do hooków bazy.
- Zabezpieczyć testami DP, D, Y/U/S, FIRQ/NMI/IRQ i dokładne cykle.

### Etap 5 — Z80

- Dodać `Z80State : CpuState`, początkowo jako cienki wrapper nad `Z80Registers`.
- Zachować `IBus`, I/O, `BusCycle`, `IBusCycleObserver`, WAIT, refresh i acknowledge interrupt jako capability Z80.
- Użyć `OpcodeKey.Page` dla prefiksów, ale pozostawić Z80 decoder odpowiedzialny za DD/FD/CB/ED i prefiksy wielobajtowe.
- Nie przenosić `BusCycle` do Core tylko dlatego, że istnieje wspólny `CpuStepResult`.
- Zachować IM0/IM1/IM2, edge-triggered NMI i pełne testy timing/I/O.

### Etap 6 — integracje i cleanup

- Zmienić debugger, DebugApi, CLI, Desktop i maszyny na `IProcessor`/capabilities zamiast rzutowań na konkretne CPU.
- Usunąć stare implementacje lifecycle, tylko gdy nie ma już referencji tekstowych ani grafowych.
- Usunąć projekcje kompatybilności dopiero po migracji testów i konsumentów.
- Zaktualizować dokumentację chipów i testów, bez duplikowania kontraktów w dokumentach maszyn.

## 6. Testy i kryteria akceptacji

- Core: reset, licznik instrukcji, zegar, HALT/WAIT, interrupt result, opcode add/replace/conflict.
- 6502: wszystkie warianty, BCD, page crossing, IRQ/NMI i testy cykli.
- 6800: `M6800CpuTests`, `M6800BinaryTests`, rejestry, flagi i cykle.
- 6809: interrupt, integration, page 10/11, rozszerzony stan i frame layout.
- Z80: CPU, interrupt, timing, bus, memory, WAIT, I/O i obserwator cykli.
- Build: `dotnet build PetEmulator.slnx --no-restore`.
- Finalnie: pełny `dotnet test PetEmulator.slnx --no-restore` oraz GitNexus `detect_changes`.

Definition of Done:

- [ ] Core nie zawiera nazw ani założeń żadnej rodziny CPU.
- [ ] Każda rodzina używa wspólnego lifecycle bazy.
- [ ] `M6800State` implementuje abstrakcyjny State, a `M6809State` rozszerza go bez duplikacji.
- [ ] Klasa potomna może bez edycji tabeli bazowej dodać i jawnie zastąpić opcode.
- [ ] Zachowane są timing, przerwania, HALT/WAIT, I/O i debug API.
- [ ] Nie ma dwóch źródeł prawdy dla zegara/cykli.
- [ ] Wszystkie dotychczasowe testy regresji przechodzą.

## Ryzyka i decyzje do potwierdzenia przed implementacją

- Czy publiczne `Step()` pozostaje nazwą główną, czy Core standaryzuje `StepInstruction()`?
- Czy publiczne dziedziczenie `M6809Cpu : M6800Cpu` jest wymaganiem kompatybilności, czy może zostać zastąpione kompozycją po migracji?
- Czy `GetRegisters()` ma pozostać mapą debugową procesora, czy stać się projekcją stanu?
- Czy `M6800State.Cycles` jest używane poza testami i musi mieć okres przejściowy?
- Czy wspólny registry ma być publiczny, czy tylko chroniony dla implementacji CPU?

Najbezpieczniejsza decyzja startowa: zachować istniejące publiczne API, wprowadzić bazę addytywnie, migrować 6502 jako wzorzec, następnie 6800→6809, a Z80 na końcu z jego własnymi capability magistrali.

## 14. Warstwa debugowania, snapshotów i monitoringu

Warstwa CPU udostępnia diagnostykę jako opcjonalną capability, bez zależności od konkretnego debuggera lub UI:

- `CpuStateSnapshot` przechowuje architektoniczne rejestry i `Halted`.
- `CpuDebugSnapshot` dodaje `CycleCount` i `InstructionCount`.
- `CpuProcessorBase<TState>.CaptureSnapshot()` i `RestoreSnapshot()` obsługują zapis/odtworzenie stanu; rodzina CPU implementuje właściwe odtworzenie rejestrów w swoim `CpuState`.
- `CpuStepTrace` opisuje stan przed i po kroku, opcode, mnemonic oraz `CpuStepResult`.
- `ICpuExecutionObserver` obsługuje breakpoint przed wykonaniem, zakończony krok i wyjątek.
- `IMemoryAccessObservable` oraz `ShouldBreakOnMemoryAccess` dostarczają opcjonalną bazę dla watchpointów przez istniejący `BusAccess`.
- `CpuStepResult` oznacza `BreakpointHit` i `WatchpointHit` bez narzucania sposobu prezentacji w debuggerze.

Kryteria testowe tej warstwy:

- snapshot obejmuje rejestry, HALT, zegar i licznik instrukcji;
- restore odtwarza stan, zegar i licznik;
- observer otrzymuje opcode/mnemonic, breakpoint i wyjątek;
- obserwowalna magistrala pozwala oznaczyć watchpoint;
- brak obserwatora nie zmienia wykonania CPU;
- wszystkie ścieżki przechodzą testy kontraktowe Core.
