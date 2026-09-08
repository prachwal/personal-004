---
name: cpu6502-worker
description: >
  Implements changes inside src/PetEmulator.Cpu6502/ — opcode handlers,
  addressing modes, variants, quirks, cycle tables. Use for adding/fixing
  opcodes, migrating a family off the legacy big-switch into a table
  handler, adding a new CPU variant (65C02, 6510), or touching flags/BCD/
  interrupt logic. Not for PET machine/device code outside this project.
tools: [Read, Edit, Write, Grep, Glob, Bash, mcp__gitnexus__impact, mcp__gitnexus__detect_changes, mcp__gitnexus__context, mcp__gitnexus__query, mcp__gitnexus__explain, mcp__gitnexus__rename]
---

Domain: `src/PetEmulator.Cpu6502/`. Target design: `docs/architecture.md`. Root `CLAUDE.md` gates still apply — impact before edit, detect_changes before handoff, never treat `UNKNOWN` risk as safe, never rename by find/replace.

## Map (don't re-discover each time)

- `Processor/OpcodeTable.cs` + `OpcodeDefinition.cs` — 256-slot dispatch, `Set/Remove/Derive/Seal`. Sealed tables are immutable; runtime `Set()` is test/experiment only, never production mutation.
- `Processor/OpcodeTables.cs` — builds the base NMOS table via `CreateNmosTable()`, which calls `BaseCyclesFor()` to populate cycle counts in `OpcodeDefinition`. Assigns each opcode's family handler via the `*Opcodes` byte sets (`SimpleOpcodes`, `LoadStoreOpcodes`, `RmwOpcodes`, `ArithmeticCompareLogicOpcodes`, `BranchOpcodes`, `ControlFlowOpcodes`, `IllegalRmwOpcodes`, `NopKilOpcodes`, `UnstableOpcodes`, `IllegalLoadStoreOpcodes`).
- `Cpu6502Variant.cs` — `CpuQuirk` flags (`DecimalArithmetic`, `JmpIndirectPageWrap`). Add new variant behavior as a quirk flag + property (see `Cpu6502.Properties.cs`), never as `if (variant == ...)` in a handler.
- `Cpu6502Classic.cs` / `Cpu6502Nes.cs` — variants built via `OpcodeTables.CreateXxxVariant()`. A new variant (65C02, 6510) follows this shape: derive a table, pick quirks, one sealed class.
- `Cpu6502.CycleStepped.Core.cs` — `StepInstructionCore()` outer loop, interrupt boundary servicing. Reads cycle counts at runtime via `_opcodeTable[opcode].BaseCycles`.

## Known debt — don't make it worse

Migration is mid-flight (see `docs/architecture.md` "Plan migracji"): `SimpleOpcodes`/`AccumulatorStackOpcodes`/`LoadStoreOpcodes`/`RmwOpcodes` already have one handler per table entry. `ArithmeticCompareLogic`/`Branches`/`ControlFlow`/`IllegalRMW`/`NopKil`/`Unstable`/`IllegalLoadStore` still route through a per-family big-switch keyed on `(opcode << 3 | cycle)`. When asked to migrate a family: move it to a direct per-opcode handler, keep the family's existing cycle/quirk tests green, don't migrate two families in one change (plan explicitly calls for one family at a time).

## Workflow

1. `impact({target, direction:"upstream"})` before editing any handler/opcode/variant symbol. Treat `UNKNOWN` as unresolved — confirm with `Grep` before proceeding.
2. Edit. Match existing partial-class/file split (one concern per `Cpu6502.*.cs` file) — don't collapse families back into one file.
3. Run the relevant test project (`dotnet test tests/PetEmulator.Cpu6502.Tests`) — opcode-space completeness, cycle timing, and ROM tests (nestest/Klaus Dormann) all live there per `docs/architecture.md` "Testowalnosc".
4. `detect_changes({scope:"all"})` before reporting done or before commit.
