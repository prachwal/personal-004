# GitNexus Engineering Plan

> Task: Add a "Chip Tester" module to PetEmulator.Desktop - a live, interactive per-chip debugger (register/pin state + waveform timeline, manual poke, Run/Pause/Step) selectable alongside PET/VIC-20 in the Machine menu, seeded by a new common `IShellModule` shell contract.
> Evidence verified at commit 2c1b629460a48898e9d502601c8228f54aa47012; GitNexus index refreshed this session (`--index-only`, no `--pdg`) - `impact()` afterward still reported 13 commits behind (a recurring local-augment/multi-process staleness-display quirk this session, not a real gap: `detect_changes` and direct file reads at the pinned commit above confirm the graph facts used below). No PDG layer indexed for this repo - §5 is source-derived, not graph-derived.
> Evidence provenance schema 2; global dirty digest sha256:0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd; cited-path manifest 8 sorted entries; exact generated plan path excluded.

## 1. Objective

Give the Desktop shell a third selectable module, "Chip Tester": a two-pane
MVVM window (left: TreeView of chips grouped by predefined debug scenarios;
right: a swappable, live, interactive debugger for the selected scenario -
register/pin state, a scrollable signal-timeline visualization, manual
register-poke/pin-set controls, and Run/Pause/Step). It must plug into the
existing "podmiana komponentu" (swap-by-concrete-type `DataTemplate`)
mechanism the shell already uses for PET/VIC-20, under one shared contract
for "module/emulator" - not a third `if`/`is`-branch bolted onto the shell.

## 2. Current Behaviour

`MainWindowViewModel` [verified, `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs:1-163`]
owns exactly one `CurrentMachine : IMachineViewModel` at a time
(`[ObservableProperty] private IMachineViewModel _currentMachine`, line 29).
`MachineChoices : IReadOnlyList<MachineMenuEntry>` (line 51) is built once in
the constructor from `PetProfileCatalog.All` + a single VIC-20 entry (lines
36-41); `SwitchMachineCommand` (lines 62-68) disposes the old instance and
replaces `CurrentMachine` wholesale. A `DispatcherTimer` (20ms, lines 45-47)
calls `CurrentMachine.Tick()` unconditionally every frame. `Reset`/`HandleKey`
(lines 56-57, 156) delegate straight through the interface. `LoadTape`/
`LoadDisk`/`NewDisk`/`NewTape` (lines 90-154) each do a `switch`/`is` on the
two known concrete types - the one place today that is NOT interface-generic,
because those operations aren't part of `IMachineViewModel`.

`IMachineViewModel` [verified, `src/PetEmulator.Desktop/ViewModels/IMachineViewModel.cs:25-75`]
is screen-shaped: `WindowTitle`, `StatusText`, `PixelWidth`/`PixelHeight`/
`PixelAspect`, `FrameBuffer` (ARGB8888 blitter buffer), `Devices`, `Extra`,
`FrameReady`/`GeometryChanged` events, `Tick()`, `Reset()`, `HandleKey(Key,
HostKeyEventKind)`. Two implementers only [`context`/`impact` confirmed,
§4]: `PetMachineViewModel` and `Vic20MachineViewModel`, both
`sealed partial class : ObservableObject, IMachineViewModel[, IDatasetteViewModel]`
using `CommunityToolkit.Mvvm` (`[ObservableProperty]`/`[RelayCommand]`)
[verified, `PetMachineViewModel.cs:1-23`].

`MainWindow.axaml` [verified, `src/PetEmulator.Desktop/Views/MainWindow.axaml:1-56`]
has a flat `MenuItem Header="_Machine" ItemsSource="{Binding MachineChoices}"`
(lines 26-35) whose `Command`/`CommandParameter` bind through
`SwitchMachineCommand`, and a `ContentControl x:Name="MachineHost"
Content="{Binding CurrentMachine}"` (lines 38-46) with two
`DataTemplate DataType="viewModels:...MachineViewModel"` entries selecting
`PetMachineView`/`Vic20MachineView` purely by `CurrentMachine`'s concrete
runtime type - no code-behind branching.

`MachineMenuEntry` [verified, `src/PetEmulator.Desktop/Models/MachineMenuEntry.cs`]
is `record MachineMenuEntry(string Label, Func<IMachineViewModel> Create)` -
tied to `IMachineViewModel` by its `Create` signature, so it cannot hold a
factory for anything that isn't screen-shaped without a type change.

The five chips in scope [verified, `lib/PetEmulator.Chips/{MOS2114,MOS6522,
MT6520,MT6545,MOS6560}.cs`] all implement `IMemoryMappedDevice`
(`Read(ushort)`/`Write(ushort, byte)`/`Reset()`/`Tick(ulong cycles)`) but
otherwise differ: `MOS6522` (VIA) and `MT6520` (PIA) expose live register
properties (`ORA`/`ORB`/`DDRA`/`DDRB`/`ACR`/`PCR`/`IFR`/`IER`/
`Timer1Counter`/... for `MOS6522`) and boolean pin properties
(`CA1`/`CA2`/`CB1`/`CB2`, mixed get-only and get/set - `MOS6522.CA2`/`.CB2`
are plain `{ get; set; }`, `MOS6522.CA1` is a custom setter with edge
detection, lines 123-144); `MOS2114` (color RAM) exposes none beyond
`Read`/`Write`; `MT6545` (CRTC) exposes read-only live signals (`HSync`/
`VSync`/`DisplayEnable`/`MACounter`/...) and no pins; `MOS6560` gained
`IAudioSource` this session (`Render`, not pin/register state relevant here).
**No two chips share a uniform register/pin shape** - reflection over
`bool`/`byte` properties would blur read-only computed signals (e.g.
`MOS6522.IRQ`, `MT6545.HSync`) with genuinely pokable state (e.g.
`MOS6522.CA2`), which is unsafe for a "manual poke" control. See §6/§12.

## 3. Relevant Architecture

Desktop project structure: `ViewModels/` (interfaces + concrete VMs),
`Views/` (+ `Views/Controls/` for shared reusable UserControls like
`DatasetteControl`, `DiskDriveControl`, `PetScreenControl`), `Models/`
(plain records like `MachineMenuEntry`), `Infrastructure/`, `Services/`.
MVVM throughout via `CommunityToolkit.Mvvm` (`ObservableObject`,
`[ObservableProperty]`, `[RelayCommand]`) - no other DI/IoC container; the
shell wires everything by hand in `MainWindowViewModel`'s constructor and
`App.axaml.cs`. The one established swap pattern is "concrete-type
`DataTemplate` keyed off a property bound to an interface-typed field" -
used today for `CurrentMachine` and, per this plan, reused twice more
(module-level: `CurrentModule`; and inside Chip Tester: `SelectedScenario`).

## 4. GitNexus Findings

- `impact({target:"IMachineViewModel", direction:"upstream", maxDepth:3})`:
  risk LOW, `impactedCount: 2`, both at depth 1 -
  `PetMachineViewModel`/`Vic20MachineViewModel`, relation `IMPLEMENTS`,
  confidence 0.85, zero `affected_processes` (no traced execution flow
  crosses the interface boundary at the graph's current resolution). The
  tool's own `boundaries` note: "callers that bind via the interface... are
  not traced to the concrete symbol - actual impact may be higher" -
  addressed directly by source-reading `MainWindowViewModel`/`MainWindow.axaml`
  in §2 rather than trusting the graph alone for a UI-binding-heavy surface
  GitNexus can't fully see through (XAML `DataTemplate` matching, Avalonia
  compiled bindings).
- No PDG layer is indexed (`analyze --pdg` never run for this repo this
  session) - §5 below is source-derived findings, not a persisted
  statement-level slice.

## 5. Statement-Level Findings (source-derived, no PDG layer)

- `MainWindowViewModel`'s constructor (lines 36-43) is the only place that
  constructs `MachineMenuEntry`s and the initial `CurrentMachine` - adding a
  module type means adding one more entry here, structurally identical to
  the existing two.
- `SwitchMachineCommand` (lines 62-68) is generic over `IMachineViewModel`
  already (`entry.Create()` then `old.Dispose()`) - broadening the property
  type is the only change needed there, not new branching.
- `_timer.Tick += (_, _) => CurrentMachine.Tick()` (line 46) is unconditional
  - Chip Tester's scenario session needs its own run/pause/step clock,
  independent of this 20ms shell timer (see §6, §12 open question #1).
- `LoadTape`/`LoadDisk`/`NewDisk`/`NewTape` (§2) already tolerate a
  `CurrentMachine` that matches neither known `is` branch (they silently
  no-op) - Chip Tester as a third, unmatched type needs no change to these
  methods, confirming the broadening is additive there too.

## 6. Proposed Changes

**New shared contract** - `src/PetEmulator.Desktop/ViewModels/IShellModule.cs`:

```csharp
public interface IShellModule : IDisposable
{
    string WindowTitle { get; }
    string StatusText { get; }
}
```

`IMachineViewModel` (`IMachineViewModel.cs:25`) changes its declaration to
`public interface IMachineViewModel : IShellModule` and drops its own now-
redundant `WindowTitle`/`StatusText` declarations (members, not the
contract) - `PetMachineViewModel`/`Vic20MachineViewModel` need **zero**
source changes; both already implement those two members structurally.

**Shell widening** - `MainWindowViewModel.cs`:

- `CurrentMachine` (line 29) → rename to `CurrentModule`, type
  `IShellModule`. `WindowTitle` binding in `MainWindow.axaml` (line 9,
  `{Binding CurrentMachine.WindowTitle}`) updates to `CurrentModule.WindowTitle`.
- `Reset()`/`HandleKey()` (lines 56-57, 156) become
  `if (CurrentModule is IMachineViewModel m) m.Reset()` /
  `m.HandleKey(...)` - no-op for Chip Tester, matching the existing
  tolerant pattern in `LoadTape`/`LoadDisk` (§5).
- `_timer.Tick` (line 46) becomes
  `if (CurrentModule is IMachineViewModel m) m.Tick();` - Chip Tester drives
  its own clock (§12 open question #1), not this one.
- `MachineChoices` → `ModuleChoices : IReadOnlyList<ModuleMenuEntry>`; add
  one `new ModuleMenuEntry("Chip Tester", () => new ChipTesterViewModel())`
  entry alongside the existing PET/VIC-20 entries (constructor, ~line 41).

**Model rename** - `Models/MachineMenuEntry.cs` →
`ModuleMenuEntry(string Label, Func<IShellModule> Create)` (only the
`Create` signature's generic parameter changes).

**View wiring** - `MainWindow.axaml`: rename the `ItemsSource`/`Content`
bindings to `ModuleChoices`/`CurrentModule` (mechanical), add one
`<DataTemplate DataType="viewModels:ChipTesterViewModel"><views:ChipTesterView /></DataTemplate>`
inside `MachineHost`'s existing `ContentControl.DataTemplates` (§2, lines
38-46).

**New: `ViewModels/ChipTesterViewModel.cs`**:

```csharp
public sealed partial class ChipTesterViewModel : ObservableObject, IShellModule
{
    public ObservableCollection<ChipTreeNode> Chips { get; }
    [ObservableProperty] private IChipDebugSessionViewModel? _selectedScenario;
    public string WindowTitle => "Chip Tester";
    public string StatusText => SelectedScenario?.StatusText ?? "Select a scenario";
    public void Dispose() => SelectedScenario?.Dispose();
}
public sealed record ChipTreeNode(string Name, IReadOnlyList<ChipScenarioEntry> Scenarios);
public sealed record ChipScenarioEntry(string Name, Func<IChipDebugSessionViewModel> Create);
```

Selecting a tree leaf disposes the previous `SelectedScenario` (same
dispose-on-swap discipline as `SwitchMachineCommand`) and assigns a fresh one.

**New: `ViewModels/IChipDebugSessionViewModel.cs`** (the right-panel
contract, one implementation per chip - **not** one reflective adapter; see
§2's "no uniform shape" finding and §12 open question #2 on exactly which
members are pokable per chip):

```csharp
public interface IChipDebugSessionViewModel : IShellModule
{
    ObservableCollection<RegisterRow> Registers { get; }
    ObservableCollection<PinRow> Pins { get; }
    IWaveformSource Timeline { get; }
    bool IsRunning { get; }
    IRelayCommand RunCommand { get; }
    IRelayCommand PauseCommand { get; }
    IRelayCommand StepCommand { get; }
    IRelayCommand ResetCommand { get; }
    void PokeRegister(string name, byte value);
    void SetPin(string name, bool level);
}
public sealed record RegisterRow(string Name, string Value);
public sealed record PinRow(string Name, bool Level, bool IsWritable);
```

**New Views**: `Views/ChipTesterView.axaml` (Grid/SplitView: left `TreeView`
bound to `Chips`, right `ContentControl` bound to `SelectedScenario` with
per-scenario-type `DataTemplate`s - same swap pattern as `MachineHost`),
`Views/Controls/WaveformControl.axaml(.cs)` (new custom canvas control - the
single largest new UI piece; scope/API is an open question, §12).

**Per-chip scenario view models** (one per chip in v1 scope - MOS2114,
MOS6522, MT6520, MT6545, MOS6560): `ViewModels/ChipTests/Mos6522DebugSession.cs`
etc., each a thin, explicit (not reflective) adapter naming exactly which
properties are registers vs. pins vs. read-only signals for that chip -
concrete list deferred to implementation per chip (§12).

## 7. Implementation Sequence

1. **`IShellModule` + `IMachineViewModel` re-parenting.** Add the new
   interface, change `IMachineViewModel`'s base, remove its now-duplicate
   member declarations. Build + run `PetEmulator.Desktop` and its tests
   (none exist yet for this project - see §8) to confirm the two existing
   implementers still compile unchanged.
2. **Shell widening.** `CurrentMachine`→`CurrentModule`, `MachineChoices`→
   `ModuleChoices`, `MachineMenuEntry`→`ModuleMenuEntry`, tolerant
   `Reset`/`Tick`/`HandleKey` type checks, `MainWindow.axaml` binding renames.
   No new module yet - PET/VIC-20 switching must still work identically
   (manual smoke test: launch, switch PET↔VIC-20, Reset, keyboard input).
3. **`ChipTesterViewModel` + `ChipTreeNode`/`ChipScenarioEntry` + empty
   `ChipTesterView.axaml`** (left tree only, right panel placeholder text).
   Wire the new `ModuleMenuEntry("Chip Tester", ...)` into step 2's
   `ModuleChoices`. Confirms the third `DataTemplate` slot and menu entry
   work end-to-end before any chip-specific code exists.
4. **`IChipDebugSessionViewModel` + `RegisterRow`/`PinRow` + one reference
   implementation** (recommend `MOS6522` first - richest state, both
   registers and mixed-writability pins, per §2). Plain register/pin
   display only, no waveform yet - proves the right-panel swap pattern and
   Run/Pause/Step/Poke plumbing against one real chip.
5. **`WaveformControl`** (new custom Avalonia control) + `IWaveformSource` +
   wiring it into the `MOS6522` session from step 4. The step most likely to
   need its own sub-plan given genuine UI-rendering unknowns (§12).
6. **Remaining four chip sessions** (`MOS2114`, `MT6520`, `MT6545`,
   `MOS6560`) - each follows step 4's now-proven pattern; independently
   landable per chip.
7. **Scenario stimulus scripting** (only if §12 open question #3 resolves to
   "yes, scenarios can carry scripted sequences") - a small event-sequence
   runner driven by the same clock as step 4's Run/Step.

## 8. Test Strategy

No `PetEmulator.Desktop.Tests` project exists yet [checked: `find tests -iname
"*Desktop*"` returned nothing] - this plan does not invent one speculatively;
recommend creating `tests/PetEmulator.Desktop.Tests` only if/when the first
pure-logic-testable class lands (step 1: `IShellModule`/`IMachineViewModel`
compiles; step 3-4: `ChipTesterViewModel`'s tree-selection/dispose-on-swap
logic and a `MOS6522` session's `PokeRegister`/`SetPin`/Run-Pause-Step state
machine are the first genuinely unit-testable surfaces here, independent of
Avalonia rendering).

Existing coverage this touches: `lib/PetEmulator.Chips/MOS6522.cs` etc. are
already covered by `tests/PetEmulator.Chips.Tests/*` - those chips are read
via their existing public API only (no chip source changes in this plan),
so that suite is regression coverage for "the tester didn't corrupt chip
state," run via `dotnet test tests/PetEmulator.Chips.Tests`.

Verification commands: `dotnet build` (whole solution - confirms the
interface re-parent compiles everywhere), `dotnet test` (full suite - the
established per-step discipline this session; see prior commits' verification
pattern), manual smoke test of the Desktop app for each step per §7 (no
automated UI test harness exists for Avalonia views in this repo today).

## 9. Risk and Impact Analysis

- **d=1 dependents** (§4): `PetMachineViewModel`, `Vic20MachineViewModel` -
  both accounted for in §6 as zero-source-change (interface re-parent only).
- **XAML binding breakage risk**: `MainWindow.axaml`'s `Title="{Binding
  CurrentMachine.WindowTitle}"` and the `MachineHost` `ContentControl` are
  compiled Avalonia bindings - renaming `CurrentMachine`→`CurrentModule`
  must update every XAML reference in the same step (step 2) or the build
  fails loudly (compiled bindings are compile-time checked in this repo's
  Avalonia setup), not silently.
- **No uniform chip shape** (§2): the strongest concrete risk in this plan -
  a tempting reflection-based generic adapter would either expose
  read-only computed signals as falsely pokable, or need per-property
  metadata attributes on the chip classes themselves (a `lib/
PetEmulator.Chips` change this plan deliberately avoids - those classes
  have their own established, tested public contract). Proposed mitigation
  (§6): explicit, hand-written per-chip session class, one per chip,
  small and boring by design.
- **Shell timer coupling** (§5): Chip Tester's own run/pause/step clock
  must not fight the existing 20ms `DispatcherTimer` guarding
  `IMachineViewModel.Tick()` - §6 guards that call with an `is`
  check, so a Chip Tester session simply never receives shell ticks; it
  needs its own timer/loop, scoped to when it's the current module and
  actually running (avoid a background timer leaking after `Dispose()`).
- **`WaveformControl` is the one genuinely open-ended UI risk** - no
  precedent in this codebase for a custom canvas-rendered timeline control;
  step 5 (§7) isolates it so steps 1-4 land and prove the rest of the
  architecture regardless of how long the control itself takes.

## 10. Files Expected to Change

| File | Symbols | Reason |
| ---- | ------- | ------ |
| `src/PetEmulator.Desktop/ViewModels/IShellModule.cs` | `IShellModule` (new) | New shared base contract |
| `src/PetEmulator.Desktop/ViewModels/IMachineViewModel.cs` | `IMachineViewModel` | Re-parent to `: IShellModule`, drop duplicate members |
| `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs` | `CurrentMachine`→`CurrentModule`, `MachineChoices`→`ModuleChoices`, `Reset`, `HandleKey`, timer tick, ctor | Widen shell to hold any `IShellModule`; tolerant machine-only ops |
| `src/PetEmulator.Desktop/Models/MachineMenuEntry.cs` | `MachineMenuEntry`→`ModuleMenuEntry` | `Create` factory typed to `IShellModule` |
| `src/PetEmulator.Desktop/Views/MainWindow.axaml` | `Title` binding, `_Machine` `MenuItem`, `MachineHost` `ContentControl` | Rename bindings, add third `DataTemplate` |
| `src/PetEmulator.Desktop/ViewModels/ChipTesterViewModel.cs` (new) | `ChipTesterViewModel`, `ChipTreeNode`, `ChipScenarioEntry` | Left-panel tree + selection/swap |
| `src/PetEmulator.Desktop/ViewModels/IChipDebugSessionViewModel.cs` (new) | `IChipDebugSessionViewModel`, `RegisterRow`, `PinRow` | Right-panel contract |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/*.cs` (new, one per chip) | e.g. `Mos6522DebugSession` | Per-chip explicit register/pin adapter |
| `src/PetEmulator.Desktop/Views/ChipTesterView.axaml(.cs)` (new) | - | Two-pane layout, tree + swap `ContentControl` |
| `src/PetEmulator.Desktop/Views/Controls/WaveformControl.axaml(.cs)` (new) | `WaveformControl`, `IWaveformSource` | Signal/register timeline visualization |

## 11. Reusable Implementation Context

```json
{
  "task": "Add a Chip Tester module to PetEmulator.Desktop under a new IShellModule contract shared with IMachineViewModel",
  "primary_symbols": [
    "Interface:src/PetEmulator.Desktop/ViewModels/IMachineViewModel.cs:IMachineViewModel",
    "Class:src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs:MainWindowViewModel",
    "Record:src/PetEmulator.Desktop/Models/MachineMenuEntry.cs:MachineMenuEntry"
  ],
  "d1_dependents_to_preserve": [
    "Class:src/PetEmulator.Desktop/ViewModels/PetMachineViewModel.cs:PetMachineViewModel",
    "Class:src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs:Vic20MachineViewModel"
  ],
  "chips_in_scope_v1": [
    "lib/PetEmulator.Chips/MOS2114.cs:MOS2114",
    "lib/PetEmulator.Chips/MOS6522.cs:MOS6522",
    "lib/PetEmulator.Chips/MT6520.cs:MT6520",
    "lib/PetEmulator.Chips/MT6545.cs:MT6545",
    "lib/PetEmulator.Chips/MOS6560.cs:MOS6560"
  ],
  "recommended_first_chip": "MOS6522",
  "evidence_provenance": {
    "schema_version": 2,
    "head_commit": "2c1b629460a48898e9d502601c8228f54aa47012",
    "generated_plan_path": "docs/plans/2026-09-10-gitnexus-plan-chip-tester-module.md",
    "global_dirty_digest": {
      "algorithm": "sha256",
      "canonicalization": "gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records",
      "value": "0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd"
    },
    "cited_path_manifest": [
      "lib/PetEmulator.Chips/MOS2114.cs",
      "lib/PetEmulator.Chips/MOS6522.cs",
      "lib/PetEmulator.Chips/MT6520.cs",
      "src/PetEmulator.Desktop/Models/MachineMenuEntry.cs",
      "src/PetEmulator.Desktop/ViewModels/IMachineViewModel.cs",
      "src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs",
      "src/PetEmulator.Desktop/ViewModels/PetMachineViewModel.cs",
      "src/PetEmulator.Desktop/Views/MainWindow.axaml"
    ]
  }
}
```

## 12. Assumptions and Open Questions

1. **[open, blocking step 5-7]** Time model for a running scenario: is
   Run/Step driven by a per-session `DispatcherTimer` at a fixed wall-clock
   rate (like the shell's 20ms), by "N chip cycles per Step click," or
   something else? Affects `IChipDebugSessionViewModel`'s `RunCommand`
   semantics directly.
2. **[open, blocking step 4-6]** Exactly which properties are "pins" (shown
   in `Pins`, `IsWritable` true) vs. "registers" (shown in `Registers`) vs.
   read-only derived signals (shown as neither, or as a read-only register)
   per chip - §2/§9 established no uniform shape exists; this plan proposes
   explicit per-chip session classes precisely to defer this decision to
   each chip's own implementation step rather than resolve it globally now.
3. **[assumed, per user's answer this session]** Scenarios may optionally
   carry a scripted stimulus sequence; not required for v1's first chip
   (step 4 ships attach-and-observe + manual poke only, no scripting).
4. **[open]** History buffer length (N) for `IWaveformSource` - needs a
   concrete default before `WaveformControl` (step 5) can be built; not
   resolved by this planning session.
5. **[open]** `WaveformControl`'s exact visual contract (which axis is
   time, how digital vs. multi-bit register values render, zoom/scroll
   interaction) - flagged in §9 as the plan's single largest UI unknown;
   recommend a short spike/mock before step 5's full implementation.
6. **[deferred, explicitly out of scope]** Reusing PET/VIC-20's existing
   `MOS6522`/`MT6520`/`MT6545`/`MOS2114` *instances* (i.e. inspecting a
   live running machine's chips) vs. Chip Tester always constructing its
   own fresh, isolated chip instance per scenario - this plan assumes the
   latter (isolated instances, §6's `ChipScenarioEntry.Create`), since nothing
   in the user's sketch or answers asked for live-machine introspection;
   flag if that's wanted, it changes the shell-coupling story substantially.

## 13. Definition of Done

- `IShellModule` exists; `IMachineViewModel` implements it; `dotnet build`
  succeeds solution-wide with zero source changes to `PetMachineViewModel`/
  `Vic20MachineViewModel`.
- "Chip Tester" appears in the shell's module menu; selecting it swaps the
  content pane to a two-pane view (left tree, right panel) without touching
  PET/VIC-20 switching behavior (manual regression: switch PET→VIC-20→PET
  still works identically to before this change).
- At least one chip (`MOS6522` per §7 step 4-5) has a working scenario:
  selecting it in the tree shows live register/pin state, Run/Pause/Step
  work, manual `PokeRegister`/`SetPin` visibly change displayed state, and
  a waveform/timeline renders the tracked signals over time.
- The remaining four chips (§7 step 6) each have at least one scenario
  following the same proven pattern.
- Full solution `dotnet test` stays green (no regression in
  `PetEmulator.Chips.Tests` or any other existing suite).
