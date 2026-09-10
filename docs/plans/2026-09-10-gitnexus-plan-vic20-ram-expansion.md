# GitNexus Engineering Plan

> Task: Design and add a VIC-20 cartridge mechanism for memory-expansion cartridges (RAM only, not ROM game cartridges) - the `Vic20ExpansionPreset` enum ($3K/$8K/$16K/$24K/All) already named and deliberately deferred in `docs/vic20-migration-plan.md` since this repo's original VIC-20 port. Real unexpanded VIC-20 memory blocks $0400-$0FFF/$2000-$3FFF/$4000-$5FFF/$6000-$7FFF/$A000-$BFFF go from open-bus to backed RAM depending on the selected preset, mirroring PET's multi-profile menu pattern.
> Evidence verified at commit 5ff237cc12c12603289f5a99d860e16e9b635088; GitNexus index refreshed this session (`--index-only`, no `--pdg`) - `impact()` afterward still reported commits-behind (the same recurring local-augment/multi-process staleness-display quirk noted throughout this session, not a real gap).
> Evidence provenance schema 2; global dirty digest sha256:48bcce5aec5c1b7d927b5dcc23c8b48f24affd3c8e7f94f597807dd7ebbe765b; cited-path manifest 8 sorted entries; exact generated plan path excluded.

## 1. Objective

Give `Vic20Machine`/`Vic20MemoryBus` real RAM-backed memory-expansion "cartridges" at the five standard VIC-20 expansion blocks, selectable per preset ($3K/$8K/$16K/$24K/All), exposed in the Desktop shell's module menu the same way PET already exposes multiple profiles. **Explicitly out of scope** (per the user's own wording, "kartridże pamięci jako ram" - memory cartridges as RAM): ROM game-cartridge loading (`.crt`/binary image loading into a block as read-only) is a separate, still-deferred feature - this plan only turns specific address ranges from open-bus into read/write RAM.

## 2. Current Behaviour

`Vic20MemoryMap` [verified, `src/PetEmulator.Vic20/Vic20MemoryMap.cs:1-28`] documents exactly this gap in its own class doc comment: "expansion-preset banking ($3K/$8K/$16K/$24K/All) deliberately out of scope for v1: an unexpanded machine boots BASIC and runs small programs exactly like real unexpanded hardware does." `docs/vic20-migration-plan.md` [verified, lines 63-65, 145-147] independently confirms the same, naming the exact enum this plan implements: `Vic20ExpansionPreset` enum, "$3K/$8K/$16K/$24K/All" - and states the trigger condition explicitly: "dodać gdy faktycznie ktoś chce uruchomić program wymagający rozszerzonej pamięci" (add when someone actually wants to run a program needing expanded memory) - the user's request today.

`Vic20MemoryBus` [verified, `src/PetEmulator.Vic20/Vic20MemoryBus.cs:1-128`] decodes reads/writes via a linear chain of `InRange(address, base, length)` checks (`ReadCore`/`WriteCore`, lines 52-74, 82-110), falling through to `OpenBus` (0xFF) / a silent no-op write when nothing matches. Line 108-109's own comment names the exact gap: "unmapped... including the $A000-$BFFF cartridge window and unexpanded blocks $2000-$7FFF/$A000-$BFFF - no cartridge support in v1." **Not mentioned in that comment but structurally identical**: $0400-$0FFF (the real VIC-20's "3K expansion" block, real hardware terminology) is also unmapped today - `ZeroPageRamSize` is only 0x0400 (ends at $03FF) and `BuiltinRamStart` is $1000, leaving $0400-$0FFF with no decode branch at all.

`Vic20Machine`'s constructor [verified, `src/PetEmulator.Vic20/Vic20Machine.cs:48-65`] already has the exact precedent for this plan's threading pattern: `Vic20DisplayConfig? displayConfig = null` (line 49) - an optional, defaulted trailing constructor parameter, stored and passed down (`DisplayConfig = displayConfig ?? Vic20DisplayConfig.Ntsc`, line 52) - this plan's `Vic20ExpansionPreset` parameter follows the identical shape.

`PetProfileCatalog` [verified, `src/PetEmulator.Pet/PetProfileCatalog.cs:6-21`] is the exact UI-side precedent: named `static readonly` entries + an `All` list, consumed by `MainWindowViewModel`'s `ModuleChoices` (`PetProfileCatalog.All.Select(profile => new ModuleMenuEntry(...))`) to populate multiple menu entries for one machine kind. Today's VIC-20 entry is a single fixed `ModuleMenuEntry("VIC-20", ...)` [verified, `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs:38-43`] - this plan turns it into the same multi-entry pattern PET already uses.

## 3. Relevant Architecture

Real VIC-20 expansion blocks (standard hardware terminology, cross-checked against `Vic20MemoryMap`'s existing addresses):

| Block | Address range | Size | Preset that enables it |
| --- | --- | --- | --- |
| Block 0 | $0400-$0FFF | 3K | `ThreeK`, `All` |
| Block 1 | $2000-$3FFF | 8K | `EightK`, `SixteenK`, `TwentyFourK`, `All` |
| Block 2 | $4000-$5FFF | 8K | `SixteenK`, `TwentyFourK`, `All` |
| Block 3 | $6000-$7FFF | 8K | `TwentyFourK`, `All` |
| Block 5 | $A000-$BFFF | 8K | `All` only |

`All` = every block RAM-backed = 3+8+8+8+8 = 35K, matching real hardware's well-known "35K expansion"/"Around the World" cartridge naming for the maximum-RAM configuration - **not written in this repo's own docs**, cross-checked against real VIC-20 hardware knowledge; flagged as `[inferred]`, not `[verified]` against this repo's own sources (§12).

Block 5 ($A000-$BFFF) is real hardware's usual ROM-cartridge autostart window - giving it to `All` as RAM (not ROM) is consistent with this plan's explicit RAM-only scope (§1) and matches the real "35K" RAM-expander convention, which does use block 5 as RAM, distinct from a ROM game cartridge occupying the same address range.

## 4. GitNexus Findings

- `impact({target:"Vic20MemoryBus", direction:"upstream", maxDepth:2})`: risk **CRITICAL** (`impactedCount: 17`) - but every concrete d=1/d=2 dependent is either `Vic20Machine`'s constructor (the one call site this plan touches, additively), `Vic20MachineViewModel`'s constructor (also touched additively, §6), or test files exercising the bus through its existing public API unchanged. The CRITICAL label reflects `Vic20MemoryBus`'s general connectivity (15 total `IMemoryBus` implementations in the codebase, most unrelated), not this specific change's actual blast radius - every real dependent is accounted for in §6/§9 as either "no change needed" or "the one deliberate additive change."
- No PDG layer indexed - no §5 statement-level slice; this is address-decode wiring, not control/data-flow-critical logic.

## 5. Statement-Level Findings

Not applicable at this depth (§4) - `ReadCore`/`WriteCore`'s existing `InRange`-chain structure (§2, §3) is simple enough that the proposed additive blocks (§6) are a direct, mechanical extension of the same pattern already there five times over, verified by direct reading rather than requiring a PDG slice.

## 6. Proposed Changes

**New: `src/PetEmulator.Vic20/Vic20ExpansionPreset.cs`**:

```csharp
namespace PetEmulator.Vic20;

/// <summary>Real VIC-20 RAM-expansion cartridge presets - see Vic20MemoryMap's doc comment and
/// docs/vic20-migration-plan.md's own deferred-scope note (this enum's exact name/members were
/// already decided there). RAM only - ROM game-cartridge loading is a separate, still-deferred
/// feature.</summary>
public enum Vic20ExpansionPreset
{
    Unexpanded,
    ThreeK,
    EightK,
    SixteenK,
    TwentyFourK,
    All,
}
```

**`Vic20MemoryMap.cs`**: add the five block address/size constants (`Block0Start`/`Block0Size` = `0x0400`/`0x0C00`, `Block1Start`/`Size` = `0x2000`/`0x2000`, `Block2Start`/`Size` = `0x4000`/`0x2000`, `Block3Start`/`Size` = `0x6000`/`0x2000`, `CartridgeStart`/`Size` (block 5) = `0xA000`/`0x2000` - name it `CartridgeStart` not `Block5Start` since that's the address this repo's own existing comment (§2) already calls "the cartridge window").

**`Vic20MemoryBus.cs`**: constructor gains a trailing `Vic20ExpansionPreset expansionPreset = Vic20ExpansionPreset.Unexpanded` parameter (mirrors `Vic20Machine`'s `displayConfig` precedent, §2). Five new `private readonly byte[]? _block0`/`_block1`/`_block2`/`_block3`/`_cartridgeRam` fields, each allocated (`new byte[size]`) only when the preset enables it, otherwise `null`. `ReadCore`/`WriteCore` each gain five new `InRange(...) && _blockN is not null` branches, same shape as every existing branch - falling through to `OpenBus`/no-op exactly as today when a block's preset doesn't include it (so `Unexpanded` behaves byte-for-byte identically to today, no regression risk for the default case). `ClearRam()` also clears whichever block arrays are non-null.

**`Vic20Machine.cs`**: constructor gains a trailing `Vic20ExpansionPreset expansionPreset = Vic20ExpansionPreset.Unexpanded` parameter, threaded straight into `new Vic20MemoryBus(roms, _vic, _via1, _via2, _colorRam, expansionPreset)`.

**New: `src/PetEmulator.Vic20/Vic20ExpansionPresetCatalog.cs`** (mirrors `PetProfileCatalog`'s shape, §2/§3): named entries pairing each `Vic20ExpansionPreset` with a human label ("VIC-20 (unexpanded)", "VIC-20 +3K", "VIC-20 +8K", "VIC-20 +16K", "VIC-20 +24K", "VIC-20 +All (35K)") and an `All : IReadOnlyList<(Vic20ExpansionPreset Preset, string Label)>` (or a small record type, implementer's choice).

**`Vic20MachineViewModel.cs`**: constructor gains a trailing `Vic20ExpansionPreset expansionPreset = Vic20ExpansionPreset.Unexpanded` parameter, threaded to `new Vic20Machine(romsRoot, expansionPreset: expansionPreset)`. `WindowTitle`'s existing `"VIC-20 Emulator"` literal could optionally include the preset name - implementer's call, not load-bearing.

**`MainWindowViewModel.cs`**: the single `ModuleMenuEntry("VIC-20", ...)` (§2) becomes a `.Select(...)` over `Vic20ExpansionPresetCatalog.All`, exactly mirroring the existing `PetProfileCatalog.All.Select(...)` line immediately above it - same file, same pattern, one line apart.

## 7. Implementation Sequence

1. **`Vic20ExpansionPreset` enum + `Vic20MemoryMap` block constants.** No behavior change yet - just the vocabulary this plan builds on.
2. **`Vic20MemoryBus` wiring** (constructor param + 5 block arrays + decode branches + `ClearRam()`). `Unexpanded` (the default) must remain byte-for-byte identical to today - verify with the existing `Vic20MemoryBusTests.cs` suite unchanged, plus new tests per block/preset (§8).
3. **`Vic20Machine`/`Vic20MachineViewModel` threading** - both additive, optional, defaulted parameters; existing callers (tests, `Vic20DebuggerSession`, any other construction site `impact()` found in §4) need zero changes.
4. **`Vic20ExpansionPresetCatalog` + `MainWindowViewModel` menu wiring** - the user-visible piece: multiple "VIC-20 (...)" entries in the module menu, each constructing a machine with a different preset.

Each step independently landable; step 2 is the one with real regression risk (§9) and should get its own commit + full test run before step 3 builds on it.

## 8. Test Strategy

Extend `tests/PetEmulator.Vic20.Tests/Vic20MemoryBusTests.cs` [verified, real file, existing `CreateBus()` helper and `UnmappedAddress_ReadsAsOpenBus`/`Ram_IsReadWrite_AtBothRegions` tests to mirror]:

- One test per preset confirming its RAM blocks are read/write and blocks it doesn't include still read as open-bus (parametrized via `[TestCase]` over `Vic20ExpansionPreset` values, or five explicit tests - implementer's call).
- `Unexpanded` (default, no preset argument) must still pass every existing test in this file unchanged - the regression guard for "no behavior change for the common case."
- A boot-level regression: BASIC still boots to READY under every preset (reuse whatever boot-test helper `Vic20KeyboardBootTests.cs`/similar already has, confirmed to exist by `impact()`'s d=2 list, §4).

Verification commands: `dotnet build`, `dotnet test` (whole solution, this session's established discipline), manual smoke test of the Desktop menu (new VIC-20 preset entries appear and boot).

## 9. Risk and Impact Analysis

- **CRITICAL `impact()` label** (§4): reflects `Vic20MemoryBus`'s general popularity, not this specific additive change - every real dependent (`Vic20Machine`, `Vic20MachineViewModel`, all Vic20 test files, `Vic20DebuggerSession`) either needs the one deliberate additive edit (§6) or nothing at all, since every new constructor parameter is optional and defaults to today's exact behavior.
- **`Unexpanded`-default regression risk**: real, the one thing this plan must not get wrong - every new block array is `null` unless a preset explicitly enables it, and every new decode branch is gated on that null-check, so the default path is structurally identical to today's code, not just behaviorally similar. Verified by keeping the existing `Vic20MemoryBusTests.cs` suite passing unchanged (§8).
- **Block 5 ($A000-$BFFF) semantics**: this plan gives it to `All` as RAM; a *future* ROM-cartridge-loading feature (explicitly out of scope, §1) would need to reconcile "block 5 as RAM" vs "block 5 as loaded ROM image" - not a conflict this plan needs to resolve now (`Unexpanded` and the four narrower presets never touch block 5 at all), but worth a one-line note for whoever picks up ROM cartridges later.
- **No chip source changes** - this is pure `Vic20MemoryBus`/`Vic20Machine`/Desktop wiring; `MOS6560`/`MOS6522`/`MOS2114` etc. are untouched.

## 10. Files Expected to Change

| File | Symbols | Reason |
| ---- | ------- | ------ |
| `src/PetEmulator.Vic20/Vic20ExpansionPreset.cs` (new) | `Vic20ExpansionPreset` | The enum itself |
| `src/PetEmulator.Vic20/Vic20MemoryMap.cs` | new constants | Block 0/1/2/3/cartridge addresses |
| `src/PetEmulator.Vic20/Vic20MemoryBus.cs` | ctor, `ReadCore`, `WriteCore`, `ClearRam` | RAM block decode |
| `src/PetEmulator.Vic20/Vic20Machine.cs` | ctor | Thread the preset through |
| `src/PetEmulator.Vic20/Vic20ExpansionPresetCatalog.cs` (new) | `Vic20ExpansionPresetCatalog` | Menu-facing catalog |
| `src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs` | ctor | Thread the preset through |
| `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs` | `ModuleChoices` ctor | Multiple VIC-20 menu entries |
| `tests/PetEmulator.Vic20.Tests/Vic20MemoryBusTests.cs` | new tests | Per-preset RAM/open-bus coverage |

## 11. Reusable Implementation Context

```json
{
  "task": "Add Vic20ExpansionPreset RAM-cartridge support to Vic20MemoryBus/Vic20Machine, exposed as extra VIC-20 menu entries mirroring PetProfileCatalog",
  "primary_symbols": [
    "Class:src/PetEmulator.Vic20/Vic20MemoryBus.cs:Vic20MemoryBus",
    "Class:src/PetEmulator.Vic20/Vic20Machine.cs:Vic20Machine",
    "Class:src/PetEmulator.Vic20/Vic20MemoryMap.cs:Vic20MemoryMap"
  ],
  "reuse_precedent": {
    "optional_defaulted_ctor_param": "src/PetEmulator.Vic20/Vic20Machine.cs:49 (displayConfig)",
    "named_catalog_plus_menu": "src/PetEmulator.Pet/PetProfileCatalog.cs, src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs:38"
  },
  "block_layout": {
    "Block0": "0x0400-0x0FFF (3K, ThreeK+All)",
    "Block1": "0x2000-0x3FFF (8K, EightK+SixteenK+TwentyFourK+All)",
    "Block2": "0x4000-0x5FFF (8K, SixteenK+TwentyFourK+All)",
    "Block3": "0x6000-0x7FFF (8K, TwentyFourK+All)",
    "Cartridge_Block5": "0xA000-0xBFFF (8K, All only)"
  },
  "evidence_provenance": {
    "schema_version": 2,
    "head_commit": "5ff237cc12c12603289f5a99d860e16e9b635088",
    "generated_plan_path": "docs/plans/2026-09-10-gitnexus-plan-vic20-ram-expansion.md",
    "global_dirty_digest": {
      "algorithm": "sha256",
      "canonicalization": "gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records",
      "value": "48bcce5aec5c1b7d927b5dcc23c8b48f24affd3c8e7f94f597807dd7ebbe765b"
    },
    "cited_path_manifest": [
      "docs/vic20-migration-plan.md",
      "src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs",
      "src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs",
      "src/PetEmulator.Pet/PetProfileCatalog.cs",
      "src/PetEmulator.Vic20/Vic20Machine.cs",
      "src/PetEmulator.Vic20/Vic20MemoryBus.cs",
      "src/PetEmulator.Vic20/Vic20MemoryMap.cs",
      "tests/PetEmulator.Vic20.Tests/Vic20MemoryBusTests.cs"
    ]
  }
}
```

## 12. Assumptions and Open Questions

1. **[inferred, not verified against this repo's own docs]** `All`'s exact block set (all five blocks, including block 5, totaling 35K) - matches real VIC-20 hardware's well-known "35K expansion" convention, but this repo's own `docs/vic20-migration-plan.md` only names the preset, not which blocks each one covers. If wrong, it's a one-line fix to `Vic20ExpansionPresetCatalog`/the block-enable mapping, not a structural problem.
2. **[assumed]** `EightK` maps to Block 1 ($2000-$3FFF) specifically, not Block 5 - matches the common real "VIC 1110" 8K expander convention (Block 1) and keeps the preset progression cumulative (`SixteenK`/`TwentyFourK` building on the same blocks `EightK` starts), which block 5 (isolated, `All`-only per this plan) would break.
3. **[deferred, explicitly out of scope]** ROM game-cartridge loading (loading a real `.crt`/binary image as read-only into block 5 or elsewhere) - not asked for (§1), and this plan's block-5-as-RAM choice for `All` is compatible with, not a blocker for, that future work landing separately.
4. **[deferred, explicitly out of scope]** Runtime cartridge insert/eject while a machine is running - real hardware only recognizes expansion RAM at power-on; this plan's presets are chosen at machine construction (same moment as PET's profile choice), not swappable mid-session.

## 13. Definition of Done

- `Vic20ExpansionPreset` exists with all five real presets; `Unexpanded` (default) behaves byte-for-byte identically to today (existing `Vic20MemoryBusTests.cs` suite passes unchanged).
- Each non-`Unexpanded` preset's RAM blocks are genuinely read/write through `Vic20MemoryBus`, and blocks outside that preset still read as open-bus.
- The Desktop shell's module menu shows multiple VIC-20 entries (one per preset), each booting to real BASIC READY.
- Full solution `dotnet build`/`dotnet test` stay green (no regression in any existing suite, including the pre-existing unrelated `PetDiskEndToEndTests` failure staying the *only* failure).
