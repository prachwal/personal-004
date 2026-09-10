# GitNexus Engineering Plan

> Task: Give Chip Tester's per-chip debug sessions richer, chip-specific visual output and control - a real rendered image preview for MT6545 (CRTC), a combined image+audio demo for MOS6560 (VIC), and more visible/interactive controls for the register/pin-only chips (MOS6522, MT6520, MOS2114) - reusing the existing rendering pipeline rather than building a second one.
> Evidence verified at commit 9ec14ad7ca12ca7ddba493bf2498750d95a133b2 (working tree carries one uncommitted, unrelated change - `src/PetEmulator.Desktop/PetEmulator.Desktop.csproj`, an explicit `PetEmulator.Audio` `ProjectReference` fixing a runtime `FileNotFoundException`, not yet committed pending the user's go-ahead; excluded from this plan's proposed changes); GitNexus index refreshed this session (`--index-only`, no `--pdg`) - `impact()` afterward still reported commits-behind (the same recurring local-augment/multi-process staleness-display quirk noted in this session's two prior plans, not a real gap - direct source reads below are authoritative).
> Evidence provenance schema 2; global dirty digest sha256:c63651684874cf9383da12da4b680422b8748005e7087b6da3a4cc972aae3201; cited-path manifest 10 sorted entries; exact generated plan path excluded.

## 1. Objective

Extend the Chip Tester module (landed this session, `docs/plans/2026-09-10-gitnexus-plan-chip-tester-module.md`, commits `ea4ec85`-`9ec14ad`) so chips that produce a real visual or audible output actually show/play it live, with register/pin edits taking effect immediately and visibly:

1. **MT6545 (CRTC)** generates character-based video timing/addressing; give it a small rendered screen preview, driven by a synthetic backing store the demo controls directly.
2. **MOS6560 (VIC, the VIC-20's video+sound chip)** already has both raster video decode and `IAudioSource` (this session's earlier audio work); give it one demo scenario showing image and live audio together.
3. **MOS6522 (VIA), MT6520 (PIA), MOS2114 (color RAM)** - register/pin-only chips with no image/audio - make their existing immediate-effect editing more visibly interactive (toggle switches over raw hex, a live Timer1/Timer2 visualization for the VIA, a color-swatch grid for the color RAM).

**Correction, stated up front**: the request also asked for VIC "sprites". `lib/PetEmulator.Chips/MOS6560.cs`'s full public surface [verified, read in full this session] has no sprite-related registers, properties, or logic anywhere - only 16 memory-mapped registers, raster/video decode properties, and the 3-oscillator-plus-noise audio path added earlier this session. The real MOS6560/6561 ("VIC-I"), used in an unexpanded VIC-20, has no hardware sprite unit - hardware sprites are a VIC-II feature (Commodore 64), a different, later chip this repo does not implement. This plan scopes the VIC-20 demo to image + audio only; adding a VIC-II-style sprite chip is out of scope entirely (not a Chip Tester UI gap - the underlying hardware model doesn't exist in this repo).

## 2. Current Behaviour

`IChipDebugSessionViewModel` [verified, `src/PetEmulator.Desktop/ViewModels/IChipDebugSessionViewModel.cs:1-30`] is the right-panel contract: `Registers`/`Pins` (`ObservableCollection`), `Timeline : IWaveformSource`, `IsRunning`, `Run`/`Pause`/`Step`/`Reset` commands, `PokeRegister(name, value)`/`SetPin(name, level)`, `LoadStimulus(IReadOnlyList<ChipStimulus>)`. No image or audio member exists today - every chip's right panel shows the same fixed layout (status text, Run/Pause/Step/Reset buttons, `WaveformControl`, two `ItemsControl`s for registers/pins) [verified, `src/PetEmulator.Desktop/Views/ChipTesterView.axaml:20-40`], via one shared `DataTemplate DataType="viewModels:IChipDebugSessionViewModel"` matched by Avalonia's interface-aware `DataType` resolution - **not** per-concrete-type templates (a deliberate simplification from the original Chip Tester plan, confirmed by reading the file directly).

`ChipDebugSessionBase` [verified, `src/PetEmulator.Desktop/ViewModels/ChipTests/ChipDebugSessionBase.cs:1-138`] drives four of five chips (`Mos2114DebugSession`, `Mos6560DebugSession`, `Mt6520DebugSession`, `Mt6545DebugSession`) through a declarative `ChipDebugDefinition(Tick, Reset, ReadRegister, WriteRegister, Registers, Pins)` record - each concrete `*DebugSession` class only builds this definition (`CreateDefinition()`), the base owns the `DispatcherTimer`/`RunCycles`/`Refresh`/waveform-sampling machinery. `Mos6522DebugSession` [verified, `src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6522DebugSession.cs:1-142`] is a known, already-flagged exception - it duplicates that same machinery standalone rather than using the base class (a pre-existing minor DRY debt this session already surfaced and left as-is; **out of scope here**, see §12).

`Mos6560DebugSession` [verified, `src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6560DebugSession.cs:1-23`] currently only exposes `MOS6560`'s 16 raw registers as hex `RegisterRow`s (`R0`-`RF`) via `ChipDebugDefinition` - it does not construct a `Vic20RasterDisplay`, does not wire `IAudioOutput`, and (per `ChipDebugSessionBase`'s fixed `Tick = () => chip.Tick(1)`, one PHI2 cycle per debug-session step) never calls `MOS6560.Render(Span<AudioFrame>)` at all today - the chip's audio path is entirely unexercised from Chip Tester.

`Mt6545DebugSession` [verified, `src/PetEmulator.Desktop/ViewModels/ChipTests/Mt6545DebugSession.cs:1-32`] exposes `R0`-`R7` plus `HSync`/`VSync`/`DisplayEnable`/`VerticalBlanking` as read-only pins - no rendered image; the chip's `SelectedRegister`/indexed-register read/write protocol (`Write(0, index)` then `Write(1, data)`/`Read(1)`) is already correctly modeled in `ParseRegister`/`ReadRegister` [verified, lines 24-30].

`PetRasterDisplay` [verified, `src/PetEmulator.Pet/Display/PetRasterDisplay.cs:10-49`] and `Vic20RasterDisplay` [verified, `src/PetEmulator.Vic20/Display/Vic20RasterDisplay.cs:14-47`] both rasterize screen RAM into an ARGB8888 `uint[]` via `Render(uint[] frameBuffer)`, and both require a real `IMemoryBus` (`PetRasterDisplay` additionally needs a `PetProfile` and `IGlyphFont` loaded from a real character-ROM file - see `PetMachineViewModel`'s constructor, `src/PetEmulator.Desktop/ViewModels/PetMachineViewModel.cs:86-95`, `new PetCharacterRomLoader().Load(Path.Combine(romsRoot, profile.RomDirectory, profile.CharacterRomPath))`; `Vic20RasterDisplay` needs only the memory bus and the live `MOS6560` instance). **No standalone, flat-RAM `IMemoryBus` implementation exists anywhere in this repo** [checked: grep for `IMemoryBus` implementers found only real, fully machine-wired classes] - every existing consumer of these two display classes gets its `IMemoryBus` from a real running machine.

`Vic20MachineViewModel` [verified, `src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs:1-66`] shows the exact reusable pattern for wiring both video and audio to one `MOS6560` instance: `_display = new Vic20RasterDisplay(_machine.Memory, _machine.Vic); _audioOutput = AudioOutputFactory.CreateDefault(); _audioOutput.Start(_machine.Vic);` (lines 58-60) - `MOS6560` implementing `IAudioSource` (this session's earlier work) is exactly what `AudioOutputFactory.CreateDefault().Start(...)` takes.

`PetScreenControl` [confirmed via `IMachineViewModel`'s own doc comment, `src/PetEmulator.Desktop/ViewModels/IMachineViewModel.cs:16-19`] is "already machine-agnostic despite its name, just an ARGB8888 blitter" - directly reusable for any new small preview region needing to show a `uint[]` frame buffer, no changes needed to it.

`ChipTesterViewModel` [verified, `src/PetEmulator.Desktop/ViewModels/ChipTesterViewModel.cs:1-48`] constructs every `*DebugSession` via a zero-argument `Func<IChipDebugSessionViewModel> Create` inside `ChipScenarioEntry` (lines 12-19) - none of today's constructors take `romsRoot` or any other dependency, because none needs one yet. A CRTC preview needs a real character-ROM file, so this is the one place a real, non-optional new dependency threads through.

## 3. Relevant Architecture

This plan is purely additive on top of the already-landed Chip Tester architecture (§2's plan reference) - no change to `IShellModule`, `MainWindowViewModel`, or the shell-level module contract from the prior plan. The established "podmiana komponentu" pattern (concrete-type `DataTemplate` swap) is reused a third time here, but at the *sub-panel* level inside `ChipTesterView.axaml`'s existing single shared `IChipDebugSessionViewModel` template, mirroring `IMachineViewModel.Extra`'s own precedent [verified, `IMachineViewModel.cs:50-51`]: "Reserved, unused extension point... both PetMachineViewModel and Vic20MachineViewModel return null today" - the exact same shape this plan proposes for chip sessions that do have something extra to show.

## 4. GitNexus Findings

- `impact({target:"IChipDebugSessionViewModel", direction:"upstream", maxDepth:2})`: risk LOW, `impactedCount: 7` - depth 1: `ChipDebugSessionBase` (IMPLEMENTS), `Mos6522DebugSession` (IMPLEMENTS), `ChipStimulus` constructor (USES, confidence 0.53); depth 2: `Mos2114DebugSession`/`Mos6560DebugSession`/`Mt6520DebugSession`/`Mt6545DebugSession` (all EXTENDS `ChipDebugSessionBase`). Confirms exactly two symbols (`ChipDebugSessionBase`, `Mos6522DebugSession`) need to satisfy any new interface member directly; the four base-derived classes inherit it for free.
- Tool's own `boundaries` note (both `IChipDebugSessionViewModel` and `IShellModule`, 2 and 3 implementations respectively): interface-bound callers "not traced to the concrete symbol" - addressed by the direct source reads in §2 (`ChipTesterView.axaml`'s single shared `DataTemplate`, confirmed by reading the file, not inferred from the graph).
- No PDG layer indexed for this repo this session - §5 below is source-derived.

## 5. Statement-Level Findings (source-derived, no PDG layer)

- `ChipDebugSessionBase`'s `RunCycles` (lines 96-106) samples every *pin* defined in `_pinDefinitions` into the waveform every tick, but has no equivalent hook for periodic *frame rendering* or *audio pulling* - a new `Visual`-bearing session needs its own re-render trigger (simplest: re-render on every `Refresh()` call, which already runs after every tick/poke/reset - no new timer needed).
- `Mos6560DebugSession.CreateDefinition()`'s `Tick = () => chip.Tick(1)` (one PHI2 cycle per debug step) is far slower than `Vic20MachineViewModel`'s real per-frame budget (20,000 instructions ≈ many thousands of PHI2 cycles per 20ms tick) - at Chip Tester's existing 100-cycles-per-20ms-timer-tick rate (`ChipDebugSessionBase`'s `_timer.Tick += (_, _) => RunCycles(100)`, line ~28) audio would be essentially silent/near-DC (100 cycles ≈ a few microseconds of PAL/NTSC time per real 20ms wall-clock tick). This is a real, concrete implementation constraint for step 2 (§7), not a design question - the audio demo needs many more chip cycles per wall-clock tick than the current shared 100-cycle constant, independent of anything UI-side.

## 6. Proposed Changes

**Extend the contract, additively** - `IChipDebugSessionViewModel.cs`:

```csharp
public interface IChipDebugSessionViewModel : IShellModule
{
    // ...existing members unchanged...

    /// <summary>Reserved, unused-by-default extension point for a chip-specific visual/audio
    /// panel - mirrors IMachineViewModel.Extra's own precedent. Null for chips with nothing
    /// extra to show (MOS6522, MT6520, MOS2114 keep returning null).</summary>
    object? Visual { get; }
}
```

`ChipDebugSessionBase` gets `public virtual object? Visual => null;` (one line, all four base-derived sessions inherit "nothing extra" for free, matching §4's impact finding). `Mos6522DebugSession` gets the same one-line `null` property directly (it's the one class not on the base).

**New: `Views/Controls/ChipVisual/CrtcPreviewViewModel.cs`** (or inline in `Mt6545DebugSession.cs` - small enough either way, prefer separate file to keep `Mt6545DebugSession` focused):

```csharp
public sealed class CrtcPreviewViewModel(PetRasterDisplay display, uint[] frameBuffer)
{
    public int PixelWidth => display.PixelWidth;
    public int PixelHeight => display.PixelHeight;
    public uint[] FrameBuffer => frameBuffer;
    public void Refresh() => display.Render(frameBuffer);
}
```

`Mt6545DebugSession` overrides `Visual => _crtcPreview` (a private field built in its constructor); its `Refresh()` override calls `base.Refresh()` then `_crtcPreview.Refresh()`. Wiring needs a `romsRoot`-derived `IGlyphFont` (§2) and a new small `FlatMemoryBus` the demo pre-fills with a fixed test pattern (a short PETSCII string, e.g. "CHIP TESTER" centered) once at construction - the CRTC's own register pokes (start address, cursor position, etc., already exposed as `R0`-`R7`) then visibly move/alter what the preview shows on the next re-render, satisfying "natychmiastowy efekt".

**New: `Infrastructure/FlatMemoryBus.cs`** (Desktop-local, not `PetEmulator.Core` - nothing outside Chip Tester's demo scenarios needs it yet, ponytail: add where it's used, promote later only if something else needs it too):

```csharp
public sealed class FlatMemoryBus(int size) : IMemoryBus
{
    private readonly byte[] _ram = new byte[size];
    public byte Read(ushort address) => _ram[address];
    public void Write(ushort address, byte value) => _ram[address] = value;
}
```

**`Mos6560DebugSession` changes**: build a `Vic20RasterDisplay(bus, chip)` over a `FlatMemoryBus` pre-filled with a fixed character-cell test pattern (VIC-20 text screen, same idea as the CRTC preview); wire `IAudioOutput` via `AudioOutputFactory.CreateDefault().Start(chip)` (§2's `Vic20MachineViewModel` precedent - `MOS6560` already satisfies `IAudioSource`); expose both through `Visual` as a small `VicPreviewViewModel(FrameBuffer, PixelWidth, PixelHeight)` (image only - audio plays directly through the OS output device, nothing to bind in the view beyond maybe a mute/volume toggle, out of scope for v1 of this plan, see §12). Override `Dispose()` to also stop/dispose the `IAudioOutput` (mirrors `Vic20MachineViewModel.Dispose`'s existing pattern - check that method's exact body during implementation, not yet read this session).

**§5's cycle-rate finding applies here directly**: `Mos6560DebugSession` needs its own tick cadence, not `ChipDebugSessionBase`'s shared 100-cycles/20ms - either a session-level override of the run-cycle count, or a dedicated faster timer scoped to this one session. Left as an explicit open question rather than a hardcoded fix (§12) - the right number depends on `Phi2Ntsc`'s actual value and what "audible" needs, which this planning pass didn't compute.

**View wiring** - `ChipTesterView.axaml`: inside the existing shared `IChipDebugSessionViewModel` `DataTemplate` (§2, lines 20-40), add one more `Grid.Row` with a `ContentControl Content="{Binding Visual}"` and per-concrete-`Visual`-type `DataTemplate`s (`CrtcPreviewViewModel` → a `PetScreenControl`-hosting template, `VicPreviewViewModel` → likewise) - `Visual` being `null` for MOS6522/MT6520/MOS2114 means Avalonia's `ContentControl` simply renders nothing extra for those, zero special-casing needed in XAML.

**Register/pin UX for MOS6522/MT6520/MOS2114** (§1 point 3) - explicitly scoped smaller than the CRTC/VIC work per the user's own framing ("inne układy mają jakieś ficzery" - clearly secondary to the two named chips): a toggle-switch `DataTemplate` for boolean `PinRow.IsWritable == true` rows (replacing whatever raw-value editing exists today - not yet read this session, verify `PokeRegister`/`SetPin`'s current View-side invocation in `ChipTesterView.axaml` before implementing) and a `MOS2114`-specific `Visual` (a small `ItemsControl` grid of 16-color swatches reading each RAM nibble) are both real, concrete, but smaller deliverables - sequenced last (§7 step 4) precisely so the two explicitly-named demos (CRTC, VIC) land first regardless of how much of step 4 gets finished.

## 7. Implementation Sequence

1. **`IChipDebugSessionViewModel.Visual` + `ChipDebugSessionBase.Visual => null` + `Mos6522DebugSession.Visual => null` + `FlatMemoryBus`.** Zero visible behavior change - proves the additive interface change compiles across all five chips and the new reusable bus class exists, before any chip uses either for real.
2. **CRTC preview** (`CrtcPreviewViewModel`, `Mt6545DebugSession` wiring, `ChipTesterView.axaml`'s new `Visual` `ContentControl` + its `DataTemplate`, `ChipTesterViewModel`/`MainWindowViewModel` threading `romsRoot` into `new Mt6545DebugSession(romsRoot)`). Manual verification: open Chip Tester → MT 6545 CRTC → see the fixed test-pattern text rendered; poke a register that shifts the display/cursor start address and see the preview change immediately.
3. **VIC image+audio demo** (`Mos6560DebugSession` wiring, `VicPreviewViewModel`, the cycle-rate fix from §6, `romsRoot` threading if the VIC-20 font/ROM path is needed - check whether `Vic20RasterDisplay` needs a character ROM the way `PetRasterDisplay` does, or only `MOS6560` itself, before assuming the same `romsRoot` plumbing applies unchanged). Manual verification: open Chip Tester → MOS 6560 VIC → see a rendered pattern and hear audible oscillator/noise tone(s); poke a volume/oscillator register and hear/see the change immediately.
4. **Register/pin UX polish** (toggle switches for writable pins, MOS2114 color-swatch `Visual`, optional MOS6522 Timer1/Timer2 live visualization) - independently landable per chip, lowest priority per §6's own framing.

## 8. Test Strategy

No `PetEmulator.Desktop.Tests` project exists yet [unchanged finding from the prior Chip Tester plan] - this plan again does not invent one speculatively. `CrtcPreviewViewModel`/`VicPreviewViewModel`/`FlatMemoryBus` are the first genuinely unit-testable, Avalonia-rendering-independent pieces this plan introduces (a `FlatMemoryBus` round-trip test, and a `CrtcPreviewViewModel.Refresh()` test asserting the frame buffer changes after a register poke, are both cheap and real regression value if that test project gets created alongside step 2).

Existing coverage this touches: none of `lib/PetEmulator.Pet/Display`, `lib/PetEmulator.Vic20/Display`, or `lib/PetEmulator.Chips` gets modified by this plan (pure Desktop-side additive wiring) - their existing test suites (`PetEmulator.Chips.Tests`, any display-specific coverage) remain valid regression coverage, run via `dotnet test`.

Verification commands: `dotnet build` (whole solution), `dotnet test` (full suite - this session's established per-step discipline), manual smoke test of the Desktop app per each §7 step (no automated Avalonia UI test harness exists in this repo today, unchanged from the prior plan's finding).

## 9. Risk and Impact Analysis

- **d=1/d=2 dependents** (§4): fully accounted for in §6 - `ChipDebugSessionBase`'s one-line `virtual Visual => null` default means none of its four subclasses need any change unless they opt in (only `Mt6545DebugSession`/`Mos6560DebugSession` do, per §7 steps 2-3); `Mos6522DebugSession` gets the same one-line addition directly.
- **New non-optional constructor dependency** (`romsRoot` for the CRTC's real character-ROM font, §2/§6): threads through `ChipTesterViewModel`'s constructor and `MainWindowViewModel`'s `ModuleMenuEntry("Chip Tester", () => new ChipTesterViewModel())` call site - a small, mechanical, but real breaking change to `ChipTesterViewModel`'s own constructor signature (currently parameterless). Every other `*DebugSession` stays parameterless; only the two-with-`Visual` scenarios need it threaded to them specifically.
- **Audio cycle-rate risk** (§5, §6): flagged explicitly as unresolved rather than guessed - shipping a "demo" that's silent or a single inaudible click because the tick rate wasn't actually tuned would fail the user's own stated goal ("natychmiastowy efekt") worse than not shipping it yet.
- **Performance**: re-rendering a `PetRasterDisplay`/`Vic20RasterDisplay` frame on every `Refresh()` call (potentially every single-cycle `Step()`, or every batch during `Run()`) is more render work per tick than either real machine view model does (those render once per 20,000-instruction tick, not on every debug step) - likely fine at Chip Tester's small preview resolution and low tick rate, but worth a first-implementation sanity check rather than an assumption.
- **No chip-source changes anywhere in this plan** - `lib/PetEmulator.Chips/*`, `lib/PetEmulator.Pet/Display/*`, `lib/PetEmulator.Vic20/Display/*` are all read-only inputs; this plan is Desktop-side wiring only.

## 10. Files Expected to Change

| File | Symbols | Reason |
| ---- | ------- | ------ |
| `src/PetEmulator.Desktop/ViewModels/IChipDebugSessionViewModel.cs` | `IChipDebugSessionViewModel` | Add `Visual` extension point |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/ChipDebugSessionBase.cs` | `ChipDebugSessionBase` | `virtual Visual => null` default |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6522DebugSession.cs` | `Mos6522DebugSession` | `Visual => null` (not on the base class) |
| `src/PetEmulator.Desktop/Infrastructure/FlatMemoryBus.cs` (new) | `FlatMemoryBus` | Synthetic `IMemoryBus` for demo backing stores |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/CrtcPreviewViewModel.cs` (new) | `CrtcPreviewViewModel` | CRTC preview frame-buffer wrapper |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/Mt6545DebugSession.cs` | `Mt6545DebugSession` | Wire `PetRasterDisplay` + `Visual` override |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/VicPreviewViewModel.cs` (new) | `VicPreviewViewModel` | VIC preview frame-buffer wrapper |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6560DebugSession.cs` | `Mos6560DebugSession` | Wire `Vic20RasterDisplay` + `IAudioOutput` + `Visual` override + faster tick rate |
| `src/PetEmulator.Desktop/ViewModels/ChipTesterViewModel.cs` | `ChipTesterViewModel` (constructor) | Thread `romsRoot` through to the two `Visual`-bearing scenarios |
| `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs` | `ModuleChoices` ctor entry | `new ChipTesterViewModel(_romsRoot)` |
| `src/PetEmulator.Desktop/Views/ChipTesterView.axaml` | right-panel `DataTemplate` | Add `Visual` `ContentControl` + its two `DataTemplate`s |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/Mos2114DebugSession.cs` (step 4, lower priority) | `Mos2114DebugSession` | Color-swatch `Visual` |

## 11. Reusable Implementation Context

```json
{
  "task": "Add chip-specific visual/audio demos (CRTC image preview, VIC image+audio) and richer control UX to Chip Tester's per-chip sessions",
  "primary_symbols": [
    "Interface:src/PetEmulator.Desktop/ViewModels/IChipDebugSessionViewModel.cs:IChipDebugSessionViewModel",
    "Class:src/PetEmulator.Desktop/ViewModels/ChipTests/ChipDebugSessionBase.cs:ChipDebugSessionBase",
    "Class:src/PetEmulator.Desktop/ViewModels/ChipTests/Mt6545DebugSession.cs:Mt6545DebugSession",
    "Class:src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6560DebugSession.cs:Mos6560DebugSession"
  ],
  "d1_d2_dependents_to_preserve": [
    "Class:src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6522DebugSession.cs:Mos6522DebugSession",
    "Class:src/PetEmulator.Desktop/ViewModels/ChipTests/Mos2114DebugSession.cs:Mos2114DebugSession",
    "Class:src/PetEmulator.Desktop/ViewModels/ChipTests/Mt6520DebugSession.cs:Mt6520DebugSession"
  ],
  "reuse_precedent": {
    "font_and_profile_loading": "src/PetEmulator.Desktop/ViewModels/PetMachineViewModel.cs:86-95",
    "display_plus_audio_wiring": "src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs:58-60",
    "extension_point_pattern": "src/PetEmulator.Desktop/ViewModels/IMachineViewModel.cs:50-51 (Extra)"
  },
  "correction": "MOS6560/6561 has no hardware sprites - VIC-20 demo is image+audio only, see plan §1",
  "evidence_provenance": {
    "schema_version": 2,
    "head_commit": "9ec14ad7ca12ca7ddba493bf2498750d95a133b2",
    "generated_plan_path": "docs/plans/2026-09-10-gitnexus-plan-chip-tester-demos.md",
    "global_dirty_digest": {
      "algorithm": "sha256",
      "canonicalization": "gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records",
      "value": "c63651684874cf9383da12da4b680422b8748005e7087b6da3a4cc972aae3201"
    },
    "cited_path_manifest": [
      "lib/PetEmulator.Chips/MOS6560.cs",
      "src/PetEmulator.Desktop/ViewModels/ChipTesterViewModel.cs",
      "src/PetEmulator.Desktop/ViewModels/ChipTests/ChipDebugSessionBase.cs",
      "src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6522DebugSession.cs",
      "src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6560DebugSession.cs",
      "src/PetEmulator.Desktop/ViewModels/ChipTests/Mt6545DebugSession.cs",
      "src/PetEmulator.Desktop/ViewModels/IChipDebugSessionViewModel.cs",
      "src/PetEmulator.Desktop/ViewModels/PetMachineViewModel.cs",
      "src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs",
      "src/PetEmulator.Desktop/Views/ChipTesterView.axaml"
    ]
  }
}
```

## 12. Assumptions and Open Questions

1. **[open, blocking step 3]** Exact tick-rate fix for audible VIC audio (§5, §9) - this planning pass identified the constraint (100 cycles/20ms is inaudibly slow) but did not compute the right replacement number against `Phi2Ntsc`'s real value; resolve during implementation, not guessed here.
2. **[open, blocking step 3]** Whether `Vic20RasterDisplay`/`MOS6560` need a character-ROM font the way `PetRasterDisplay` does, or render purely from register+RAM state with no font dependency - not verified this session (only `PetRasterDisplay`'s font requirement was confirmed). Check before assuming step 3 needs the same `romsRoot`-threading as step 2.
3. **[assumed]** The CRTC/VIC preview test patterns are static, fixed content set once at session construction (§6) - not a scripted animation or live-linked to anything else. Matches "control with immediate effect" (register edits change what's shown) without requiring a content-authoring feature nobody asked for.
4. **[deferred, explicitly out of scope]** A volume/mute control for the VIC demo's live audio, and any UI for the optional `ChipStimulus` mechanism (already landed, unused) - neither was asked for.
5. **[deferred, explicitly out of scope]** `Mos6522DebugSession`'s pre-existing non-`ChipDebugSessionBase` duplication (flagged in the prior plan's review, still true at this plan's pinned commit) - this plan adds the same one-line `Visual => null` to it directly rather than using the refactor as an excuse to also merge it onto the base class; that merge is real, separable follow-up work, not bundled here.
6. **[note, not blocking]** The working tree's one uncommitted change (`PetEmulator.Desktop.csproj`'s explicit `PetEmulator.Audio` `ProjectReference`, from the runtime-crash fix earlier this session) is unrelated to this plan and is not proposed for reversion or inclusion - it's simply present in the dirty digest.

## 13. Definition of Done

- `IChipDebugSessionViewModel.Visual` exists; all five chip sessions compile with it (four inheriting `null` from the base, `Mos6522DebugSession` and the two demo chips explicit).
- Opening Chip Tester → MT 6545 CRTC shows a real rendered character-screen preview; poking a register that affects addressing/cursor visibly changes it without any other action.
- Opening Chip Tester → MOS 6560 VIC shows a rendered image AND plays audible sound simultaneously; poking a volume or oscillator-frequency register changes both, audibly and visibly, without restarting the scenario.
- MOS6522/MT6520/MOS2114 remain fully functional exactly as before this plan (no `Visual`, unchanged register/pin panels) unless/until step 4 lands.
- Full solution `dotnet build` and `dotnet test` stay green (no regression in any existing suite).
