# GitNexus Engineering Plan

> Task: Add four new demo modules to the Desktop shell, showing live/interactive what today is only proven by unit tests - a Media Tester (disk/tape load+save, byte stream, bus line waveform), a Font/Glyph Viewer (character-ROM rendering), a CPU Opcode Stepper (register/flag/memory diff per instruction), and a Keyboard Matrix demo (key press -> row/col -> port read). Same shell container pattern as Chip Tester (`IShellModule`, `ModuleMenuEntry`, concrete-type `DataTemplate` swap).
> Evidence verified at commit 127e79d1e13f68379eafd64f28bf41d8ee013411 (working tree carries two intentionally-untracked plan docs from this session's prior Chip Tester work - unrelated, excluded from this plan's proposed changes); GitNexus index refreshed this session (`--index-only`, no `--pdg`) - `impact()` calls below still occasionally reported commits-behind (the same recurring local-augment/multi-process staleness-display quirk noted in every plan this session, not a real gap - direct source reads are authoritative).
> Evidence provenance schema 2; global dirty digest sha256:28f4e57ba12f5faf52f6ee13c00b3f85f06b171ea3c0ad90ad4e3e5659cd1d58; cited-path manifest 14 sorted entries; exact generated plan path excluded.

## 1. Objective

Four independent demo modules, each landable and useful on its own, in priority order (per this session's own discussion with the user - Media Tester reuses the most already-built infrastructure and was named most valuable):

1. **Media Tester** - mount a real D64/T64/TAP file, watch LOAD/SAVE happen byte-by-byte (hex+ASCII stream) with the underlying bus line transitions (IEC for VIC-20, IEEE-488 for PET) rendered as a waveform, reusing `WaveformControl` from Chip Tester and the `Activity` event pattern already proven on `Vic20SerialBus`/`PetIeeeBus`/`PetDatasette`.
2. **Font/Glyph Viewer** - load a real character ROM (PET or VIC-20) and render every glyph in a grid, with reverse-video/multicolor toggles, directly exercising `PetRasterDisplay`/`Vic20RasterDisplay`'s glyph-decode logic outside a full machine - the same rendering pipeline Chip Tester's CRTC/VIC demos already reuse.
3. **CPU Opcode Stepper** - pick one of this repo's 5 CPU variants, single-step and see register/flag/memory changes live, via `IDebuggableProcessor.GetRegisters()` (already used for real debugging work this session).
4. **Keyboard Matrix demo** - press a key, see the real matrix row/column and the resulting port read, for either PET's or VIC-20's keyboard matrix.

## 2. Current Behaviour

The shell container this reuses [verified, unchanged since the Chip Tester plans]: `IShellModule` (`WindowTitle`, `StatusText`, `Dispose`) [`src/PetEmulator.Desktop/ViewModels/IShellModule.cs`], `MainWindowViewModel.CurrentModule : IShellModule` + `ModuleChoices : IReadOnlyList<ModuleMenuEntry>` [`src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs:28-42`], `MainWindow.axaml`'s `ContentControl "MachineHost"` picking a View purely by `CurrentModule`'s concrete type [`src/PetEmulator.Desktop/Views/MainWindow.axaml:41-52`]. Chip Tester [landed this session, `docs/plans/2026-09-10-gitnexus-plan-chip-tester-module.md`/`-demos.md`] is the second implementer of this pattern and the direct architectural precedent for all four new modules - its own internal shape (`ChipTesterViewModel`'s `Chips : ObservableCollection<ChipTreeNode>` left-tree + `SelectedScenario` right-panel swap, `TabControl` "Live"/"Opis" split added most recently) is reusable wholesale for Media Tester and Font/Glyph Viewer (both naturally have a left-tree of choices: which disk/tape file or which ROM), less directly for Opcode Stepper/Keyboard Matrix (single subject, no tree needed).

**Media Tester's building blocks, all pre-existing and unmodified by this plan**:

- `D64Image` [verified, `src/PetEmulator.Pet/CbmDos/D64Image.cs`]: `Load(path)`/`Load(byte[])`, `ReadDirectory()`, `ReadFile(entry)`, `CreateFormatted(name, id)`, `WriteSector`/`AddDirectoryEntry`/`SaveToBytes()` - a complete, standalone, machine-independent disk-image API.
- `PetIeeeDiskDrive` [verified, `src/PetEmulator.Pet/CbmDos/PetIeeeDiskDrive.cs`]: wraps `CbmDosEngine` behind `IIeeeDevice` (`OpenForRead`/`OpenForWrite`/`Close`/`Write`/`TryRead`) - the exact same drive class real `PetMachine` uses, directly attachable to a fresh `PetIeeeBus` with no CPU/machine needed (this session's own earlier debugging work drove it exactly this way, in isolation, to verify the DOS/command layer - see `docs/vic20/disk.md`'s "CbmDosEngine's own... implementation was verified correct, in isolation").
- `PetIeeeBus.Activity` [verified, `src/PetEmulator.Pet/Ieee488/PetIeeeBus.cs:7,39`]: `event Action<IeeeBusActivity>? Activity` (`IeeeBusActivity(string Kind, string Detail)`) - line-change and byte events, the exact shape `WaveformControl`'s `IWaveformSource`/`WaveformSample` already consumes for Chip Tester (adapting one to the other is the only new glue code needed for the PET/IEEE-488 side).
- `Vic20SerialBus.Activity` [this session's own earlier addition, `src/PetEmulator.Vic20/Serial/Vic20SerialBus.cs`]: identical shape (`Vic20SerialActivity(string Kind, string Detail)`), same reuse story for the VIC-20/IEC side.
- `Vic20Datasette`/`PetDatasette` [verified, `src/PetEmulator.Vic20/Tape/Vic20Datasette.cs`, `src/PetEmulator.Pet/Tape/PetDatasette.cs`]: `LoadTape(pulseCycles, name)`, `PressPlay`/`Stop`/`Rewind`, and `PetDatasette` additionally has its own `DatasetteActivity` event (same shape again). `PetTapePulseDecoder.DecodeBytes(pulseCycles)` [verified, `src/PetEmulator.Pet/Tape/PetTapePulseDecoder.cs:14`] already turns raw pulse timing into bytes standalone.

**Font/Glyph Viewer's building blocks**: `PetCharacterRomLoader.Load(path) : IGlyphFont` [verified, reused already by Chip Tester's CRTC demo, `src/PetEmulator.Desktop/ViewModels/ChipTests/Mt6545DebugSession.cs`] and `IGlyphFont`/`BitmapFont` [`src/PetEmulator.Pet/Fonts/`] - `IGlyphFont` exposes glyph bytes directly (exact shape not re-read this session at this depth - verify `IGlyphFont.cs`'s full member list during implementation before assuming it matches `PetRasterDisplay`'s internal usage 1:1). VIC-20's own character generation reads glyph bytes through `Vic20RasterDisplay`'s `IMemoryBus`-addressed `charAddr` region [verified this session, `src/PetEmulator.Vic20/Display/Vic20RasterDisplay.cs:108`] rather than a loaded font object - **this is the exact gap already flagged in the Chip Tester demos work** (the VIC demo's on-screen text is unreadable synthetic noise because no real VIC-20 character ROM was ever loaded into that memory region). This plan's Font/Glyph Viewer step is the natural place to source and verify a real VIC-20 character-ROM file exists under `romsRoot/vic20` and fix that known gap as a side effect - not a new problem, the existing one this session already surfaced.

**Opcode Stepper's building block**: `IDebuggableProcessor.GetRegisters() : IReadOnlyDictionary<string, ulong>` [verified, `lib/PetEmulator.Core/IDebuggableProcessor.cs:8-12`] - already the exact mechanism this session's own live debugging work used repeatedly (`Vic20Machine.BusObserver` + `((IDebuggableProcessor)machine.Processor).GetRegisters()["PC"]`). Five concrete CPU variants exist [verified, `lib/PetEmulator.Cpu6502/Cpu6502{Classic,Cmos65C02,WdcR65C02S,Commodore6510,Nes,Atari6507}.cs`] - confirm each implements `IDebuggableProcessor` during implementation (not re-verified at this depth this session).

**Keyboard Matrix's building block**: `PetKeyboardMatrix`/`Vic20KeyboardMatrix` [verified, `src/PetEmulator.Pet/Keyboard/PetKeyboardMatrix.cs`, `src/PetEmulator.Vic20/Keyboard/Vic20KeyboardMatrix.cs`] - both plain, standalone, machine-independent classes: `Press(row, col)`/`Release(row, col)`/`Reset()`/`ReadColumns(...)`, no CPU or memory bus needed to construct or drive one.

## 3. Relevant Architecture

All four modules are additive `IShellModule` implementations following Chip Tester's own precedent exactly - no change to the shell container itself (`IShellModule`, `MainWindowViewModel`, `MainWindow.axaml`'s dispatch mechanism) beyond adding four more `ModuleMenuEntry` rows. Each module owns its own small View + ViewModel pair under `src/PetEmulator.Desktop/`, mirroring `ChipTesterViewModel`/`ChipTesterView.axaml`'s existing shape where it fits (Media Tester, Font Viewer - both naturally have a "pick one of several things" left panel) and a simpler single-panel layout where a tree doesn't fit (Opcode Stepper, Keyboard Matrix).

## 4. GitNexus Findings

- `impact({target:"IShellModule", direction:"upstream"})`: not re-run this session at this specific commit (already established LOW risk with 3 implementers as of the prior Chip Tester plans; adding a 4th-7th implementer is the same additive shape, no new risk class). Re-verify at implementation time per each module's own PR if the graph has moved further.
- `query({search_query:"disk load save byte stream"})` / equivalent were not run this session for this plan - evidence here is source-derived from direct file reads (§2), consistent with `freshness: accept`'s compact-plan posture for a Feature-classed task with this much already-known context from earlier work in the same session.
- No PDG layer indexed - no §5 statement-level slice; this plan is architecture/wiring-level, not control/data-flow-critical.

## 5. Statement-Level Findings

Not applicable at this depth - see §4's freshness note. Each module's actual per-line wiring (how `PetIeeeBus.Activity` maps onto `WaveformSample`, exact `IGlyphFont` member shape, which of the 5 CPU variants' constructors need what) is left to implementation-time verification per §12's open questions, consistent with this plan's own evidence hierarchy (source beats graph, but this pass didn't re-read every byte of every file at full depth - it reused this session's own extensive prior verification of the same files wherever that already happened, and flags what it didn't).

## 6. Proposed Changes

### Step A - Media Tester (highest priority)

**New: `ViewModels/MediaTesterViewModel.cs`** (`IShellModule`) - left tree: "PET disk (D64)" / "VIC-20 disk (D64)" / "VIC-20 tape (TAP)" categories, each with an "Open file..." action (reuse `IFilePickerService`, already injected into `MainWindowViewModel` - thread it through the same way). Right panel per open media: file name/type, a "Directory" view (`D64Image.ReadDirectory()`), and LOAD/SAVE buttons that drive a fresh, isolated `PetIeeeBus`+`PetIeeeDiskDrive` (disk) or `Vic20Datasette` (tape) - **isolated instances, not the live running machine**, matching Chip Tester's own established precedent (§12 of the Chip Tester module plan already settled this question the same way).

**New: `ViewModels/MediaByteStreamViewModel.cs`** - subscribes to `PetIeeeBus.Activity`/`Vic20SerialBus.Activity`/`DatasetteActivity` (whichever applies), renders: (a) a running hex+ASCII log of bytes as they cross the bus, (b) an adapter turning line-change events into `WaveformSample`s for `WaveformControl` reuse (`Activity`'s `Kind`/`Detail` strings need per-bus-type parsing into named signal tracks - e.g. "ATN=assert"/"CLK=release" - concrete mapping left to implementation, not fully speced here to avoid guessing wrong ahead of looking at real `Activity` output shapes side by side).

**New: `Views/MediaTesterView.axaml`** - same left-tree/right-panel `ContentControl` swap layout as `ChipTesterView.axaml`.

### Step B - Font/Glyph Viewer

**New: `ViewModels/FontViewerViewModel.cs`** (`IShellModule`) - left tree: available character ROMs found under `romsRoot` (PET's, and - fixing the known VIC-20 gap from §2 - a real VIC-20 character ROM once located). Right panel: a grid of every glyph (128 or 256 cells depending on the font), each cell rendered via the SAME `RenderChar`-equivalent logic `PetRasterDisplay`/`Vic20RasterDisplay` already use (reuse those classes directly against a tiny synthetic 1-character `FlatMemoryBus`, one instance per grid cell, OR - simpler - a small local glyph-to-bitmap helper that doesn't need a full display class per cell; decide during implementation which reads cleaner). Toggles: reverse-video, and (VIC-20 only) multicolor mode.

**Side effect, explicitly in scope**: locate/verify a real VIC-20 character-ROM file under `romsRoot/vic20`, load it into `Mos6560DebugSession`'s (Chip Tester) synthetic memory the same way `Mt6545DebugSession` already loads a real PET font - this directly fixes the "VIC demo text renders as noise" gap flagged and left open in the Chip Tester demos work. One line of follow-through this plan explicitly claims rather than leaving dangling.

### Step C - CPU Opcode Stepper

**New: `ViewModels/OpcodeStepperViewModel.cs`** (`IShellModule`) - left tree: the 5 CPU variants (`Cpu6502Classic`, `Cpu6502Cmos65C02`, `Cpu6502WdcR65C02S`, `Cpu6502Commodore6510`, `Cpu6502Nes`, `Cpu6502Atari6507` - six, not five, correcting this plan's own §1 count against §2's verified list) each wrapping a small `FlatMemoryBus` (reuse from Chip Tester's `Infrastructure/FlatMemoryBus.cs`) pre-loaded with a short, hand-picked instruction sequence (or a simple assembler-free byte sequence covering a representative opcode set - addressing modes, flag effects). Right panel: register table (`GetRegisters()`), a memory-diff view (before/after each `Step()`), Run/Step/Reset matching Chip Tester's own command shape.

### Step D - Keyboard Matrix demo

**New: `ViewModels/KeyboardMatrixViewModel.cs`** (`IShellModule`) - left tree: "PET" / "VIC-20". Right panel: a visual matrix grid (rows x columns, PET 10x8, VIC-20 8x8), each cell highlighting live as the user clicks it (`Press`/`Release` on the real matrix class) or as a physical key is typed (reuse the existing `HostKeyEventKind` + keyboard-map translation classes already used by `PetMachineViewModel`/`Vic20MachineViewModel`), showing the resulting `ReadColumns(...)` byte alongside.

### Shell wiring (all four)

`MainWindowViewModel`'s `ModuleChoices` (constructor, `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs:36-42`) gets four more `ModuleMenuEntry` rows; `MainWindow.axaml`'s `ContentControl.DataTemplates` gets four more concrete-type templates - both purely additive, same mechanical pattern as adding Chip Tester itself.

## 7. Implementation Sequence

1. **Media Tester** (Step A) - land the disk side first (D64 load/directory/LOAD, reusing `PetIeeeBus`/`PetIeeeDiskDrive` exactly as this session's own earlier disk-bug debugging already proved works in isolation), then the byte-stream+waveform panel, then SAVE, then the VIC-20/tape side. Each of those four is independently landable and demoable - stop after any of them with something real to show.
2. **Font/Glyph Viewer** (Step B) - land the PET side first (character ROM already known to load correctly, per Chip Tester's CRTC demo), then locate/verify/wire the real VIC-20 character ROM (fixing the known Chip Tester gap as part of this step, not a separate follow-up).
3. **CPU Opcode Stepper** (Step C) - land one CPU variant first (`Cpu6502Classic` - simplest, no BCD/undocumented-opcode quirks to special-case in the demo), prove the register-table/memory-diff/Step pattern, then add the other five variants as a tree, each independently landable.
4. **Keyboard Matrix demo** (Step D) - lowest priority per the user's own earlier framing (implicitly, by not naming it first); land PET or VIC-20 alone first, whichever the implementer finds simpler given the existing keyboard-map classes' exact shape.

Each step, per this session's established discipline: `impact()` before editing any existing shared symbol (`MainWindowViewModel.ModuleChoices`, `MainWindow.axaml`'s `DataTemplates`), `detect_changes({scope:"all"})` and a full `dotnet test` before every commit, commit per sub-step not per whole module where a module's own steps are independently meaningful (Media Tester's four sub-parts above, in particular).

## 8. Test Strategy

No `PetEmulator.Desktop.Tests` project exists yet [unchanged finding, carried from both prior Chip Tester plans]. The genuinely new *testable-independent-of-Avalonia* logic this plan introduces - an `Activity`-event-to-`WaveformSample` adapter (Step A), a glyph-grid layout/indexing helper (Step B), a memory-diff computation (Step C) - are all real, cheap unit-test candidates if a Desktop test project gets created alongside whichever step lands first.

Existing coverage this touches: zero chip/CPU/bus/keyboard source changes anywhere in this plan (Media Tester, Opcode Stepper, and Keyboard Matrix are all read-only consumers of already-tested classes) - their existing suites (`PetEmulator.Pet.Tests`, `PetEmulator.Cpu6502.Tests`, `PetEmulator.Vic20.Tests`) remain valid, unaffected regression coverage. The one real source change (Step B's VIC-20 character-ROM fix inside `Mos6560DebugSession.cs`, from Chip Tester) touches Desktop-only code already covered by this session's own manual verification discipline, not an automated suite.

Verification commands: `dotnet build`, `dotnet test` (whole solution), manual smoke test per step (no Avalonia UI test harness in this repo, unchanged finding).

## 9. Risk and Impact Analysis

- **Scope**: this is the largest single implementation request this session - four genuinely separate feature areas sharing only the shell container pattern. The step ordering (§7) and "each sub-part independently landable" framing exist specifically so a partial landing (likely, given the size) still leaves something real and useful rather than four half-finished modules.
- **`IGlyphFont`'s exact shape not fully re-verified this session** (§2) - Font/Glyph Viewer's per-cell rendering approach may need adjustment once that interface's real members are read; flagged rather than guessed.
- **Six CPU variants, not five** - this plan's own §1 undercounted them against §2's verified list; the implementer should treat "all CPU variants in `lib/PetEmulator.Cpu6502/`" as the real scope, not a specific number typed into this document.
- **`Activity`-to-`WaveformSample` mapping is genuinely unspecified** (§6, Step A) - three different `Activity` event shapes exist (`IeeeBusActivity`, `Vic20SerialActivity`, `DatasetteActivity`), each with its own `Kind`/`Detail` string vocabulary observed only informally during earlier debugging sessions, never formally catalogued. This is real, unavoidable implementation-time discovery work, not a gap this planning pass could have closed without re-reading every `RaiseActivity(...)` call site across three different bus classes - explicitly left open (§12) rather than fabricated.
- **No chip/CPU/bus/keyboard source changes** (repeated from §8) - every module here is a read-only consumer; the one exception (Step B's VIC character-ROM fix) is Desktop-only and already well-understood from this session's own recent work on it.

## 10. Files Expected to Change

| File | Symbols | Reason |
| ---- | ------- | ------ |
| `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs` | `ModuleChoices` ctor | 4 new `ModuleMenuEntry` rows |
| `src/PetEmulator.Desktop/Views/MainWindow.axaml` | `ContentControl.DataTemplates` | 4 new concrete-type templates |
| `src/PetEmulator.Desktop/ViewModels/MediaTesterViewModel.cs` (new) | `MediaTesterViewModel` | Step A |
| `src/PetEmulator.Desktop/ViewModels/MediaByteStreamViewModel.cs` (new) | `MediaByteStreamViewModel` | Step A |
| `src/PetEmulator.Desktop/Views/MediaTesterView.axaml` (new) | - | Step A |
| `src/PetEmulator.Desktop/ViewModels/FontViewerViewModel.cs` (new) | `FontViewerViewModel` | Step B |
| `src/PetEmulator.Desktop/Views/FontViewerView.axaml` (new) | - | Step B |
| `src/PetEmulator.Desktop/ViewModels/ChipTests/Mos6560DebugSession.cs` | `Mos6560DebugSession` | Step B side-effect: real VIC-20 char ROM |
| `src/PetEmulator.Desktop/ViewModels/OpcodeStepperViewModel.cs` (new) | `OpcodeStepperViewModel` | Step C |
| `src/PetEmulator.Desktop/Views/OpcodeStepperView.axaml` (new) | - | Step C |
| `src/PetEmulator.Desktop/ViewModels/KeyboardMatrixViewModel.cs` (new) | `KeyboardMatrixViewModel` | Step D |
| `src/PetEmulator.Desktop/Views/KeyboardMatrixView.axaml` (new) | - | Step D |

## 11. Reusable Implementation Context

```json
{
  "task": "Four new Desktop demo modules (Media Tester, Font/Glyph Viewer, CPU Opcode Stepper, Keyboard Matrix), same IShellModule shell pattern as Chip Tester",
  "reuse_precedent": {
    "shell_container": "src/PetEmulator.Desktop/ViewModels/IShellModule.cs, MainWindowViewModel.cs:28-42, Views/MainWindow.axaml:41-52",
    "tree_plus_swap_panel_layout": "src/PetEmulator.Desktop/ViewModels/ChipTesterViewModel.cs, Views/ChipTesterView.axaml",
    "waveform_control": "src/PetEmulator.Desktop/Views/Controls/WaveformControl.cs (Chip Tester)",
    "flat_memory_bus": "src/PetEmulator.Desktop/Infrastructure/FlatMemoryBus.cs (Chip Tester)",
    "isolated_disk_drive": "src/PetEmulator.Pet/CbmDos/{D64Image,PetIeeeDiskDrive}.cs - already proven driven in isolation this session (docs/vic20/disk.md)",
    "activity_events": [
      "src/PetEmulator.Pet/Ieee488/PetIeeeBus.cs:7,39 (IeeeBusActivity)",
      "src/PetEmulator.Vic20/Serial/Vic20SerialBus.cs (Vic20SerialActivity, this session's own addition)",
      "src/PetEmulator.Pet/Tape/PetDatasette.cs:9 (DatasetteActivity)"
    ],
    "font_loading": "src/PetEmulator.Desktop/ViewModels/ChipTests/Mt6545DebugSession.cs (real PET char ROM via PetCharacterRomLoader)",
    "debuggable_processor": "lib/PetEmulator.Core/IDebuggableProcessor.cs:8-12",
    "keyboard_matrices": "src/PetEmulator.Pet/Keyboard/PetKeyboardMatrix.cs, src/PetEmulator.Vic20/Keyboard/Vic20KeyboardMatrix.cs"
  },
  "known_open_gap_to_fix_in_step_b": "Mos6560DebugSession.cs writes synthetic noise glyph bytes instead of a real VIC-20 character ROM - see docs/plans/2026-09-10-gitnexus-plan-chip-tester-demos.md's own flagged gap",
  "evidence_provenance": {
    "schema_version": 2,
    "head_commit": "127e79d1e13f68379eafd64f28bf41d8ee013411",
    "generated_plan_path": "docs/plans/2026-09-10-gitnexus-plan-desktop-subsystem-demos.md",
    "global_dirty_digest": {
      "algorithm": "sha256",
      "canonicalization": "gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records",
      "value": "28f4e57ba12f5faf52f6ee13c00b3f85f06b171ea3c0ad90ad4e3e5659cd1d58"
    },
    "cited_path_manifest": [
      "lib/PetEmulator.Core/IDebuggableProcessor.cs",
      "src/PetEmulator.Desktop/ViewModels/ChipTesterViewModel.cs",
      "src/PetEmulator.Desktop/ViewModels/ChipTests/Mt6545DebugSession.cs",
      "src/PetEmulator.Desktop/ViewModels/IShellModule.cs",
      "src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs",
      "src/PetEmulator.Desktop/Views/ChipTesterView.axaml",
      "src/PetEmulator.Desktop/Views/MainWindow.axaml",
      "src/PetEmulator.Pet/CbmDos/D64Image.cs",
      "src/PetEmulator.Pet/CbmDos/PetIeeeDiskDrive.cs",
      "src/PetEmulator.Pet/Ieee488/PetIeeeBus.cs",
      "src/PetEmulator.Pet/Keyboard/PetKeyboardMatrix.cs",
      "src/PetEmulator.Pet/Tape/PetDatasette.cs",
      "src/PetEmulator.Vic20/Display/Vic20RasterDisplay.cs",
      "src/PetEmulator.Vic20/Keyboard/Vic20KeyboardMatrix.cs"
    ]
  }
}
```

## 12. Assumptions and Open Questions

1. **[open, blocking Step A]** Exact `Activity` event `Kind`/`Detail` string vocabulary for all three bus types, and how each maps to named `WaveformSample` tracks - genuinely not catalogued anywhere, real implementation-time discovery (§9).
2. **[open, blocking Step B]** Whether a real VIC-20 character-ROM file already exists under `romsRoot/vic20` (likely, since a real VIC-20 KERNAL boots to readable BASIC text in this repo's own `Vic20MachineViewModel` - that readable text has to come from somewhere) - locate it during implementation rather than assuming a specific filename.
3. **[open, blocking Step B]** `IGlyphFont`'s exact member shape - not re-read at full depth this session (§9).
4. **[assumed]** All demo media (disk images, tapes, opcode test sequences) use freshly-constructed, isolated instances - never the live running PET/VIC-20 machine - matching Chip Tester's own already-settled precedent for the same question.
5. **[deferred, explicitly out of scope]** SAVE-direction demos for Media Tester writing back to a real on-disk file (vs. an in-memory `D64Image`/tape buffer only) - not asked for, not assumed.
6. **[deferred, explicitly out of scope]** Any new automated test project (§8) - real, cheap candidates identified, not created speculatively ahead of the first step that would use one.

## 13. Definition of Done

- All four modules appear in the shell's module menu; each opens without breaking PET/VIC-20/Chip Tester switching (regression: cycle through every module, confirm no crash).
- Media Tester (at minimum, per §7's own "stop after any sub-part" framing): loading a real D64 and viewing its directory works; ideally also a live byte-stream+waveform view of an actual LOAD.
- Font/Glyph Viewer shows a real, readable PET character set; VIC-20's readability gap (§2, §6) is either fixed here or explicitly still open in the final report - not silently unresolved.
- CPU Opcode Stepper: at least one CPU variant (`Cpu6502Classic`) steppable with visible register/memory changes.
- Keyboard Matrix: at least one machine's matrix (PET or VIC-20) visually responsive to key presses.
- Full solution `dotnet build`/`dotnet test` stay green (no regression in any existing suite) after every landed sub-step.
