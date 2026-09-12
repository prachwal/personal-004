# GitNexus Engineering Plan

> Task: Migrate the current Motorola 6809 emulator to a 6800 base CPU with 6809 implemented as an extension.
> Evidence verified at commit 054a04283754950477e90967f62a7bfda51e56f5; branch refactor/6800-6809-core-architecture.
> GitNexus index: 4 commits behind HEAD; analyze --index-only --pdg was attempted but failed with exit code 1 and left the index marked incremental-in-progress, so graph evidence is source-weighted.
> Evidence provenance schema 2; global dirty digest 0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd; cited-path manifest 20 entries; exact generated plan path excluded.

## 1. Objective

Introduce a reusable M6800 CPU core and make M6809Cpu derive from it, preserving current 6809 behaviour, IProcessor/debugger compatibility, SuperPET switching, and all existing 6809 tests. The base must be useful for a real MC6800 implementation, not merely be a renamed 6809 superclass.

## 2. Current Behaviour

- [verified] M6809Cpu currently owns memory, state, interrupt latches, opcode tables, fetch/addressing, instruction implementations, stack frames, and public processor APIs in one 2,450-line class (lib/PetEmulator.Cpu6809/M6809Cpu.cs:6-2450).
- [verified] Step handles NMI, FIRQ, IRQ, SYNC/CWAI halt, fetch, delegate execution, and aggregate cycle accounting (M6809Cpu.cs:90-144); StepInstruction is an alias (M6809Cpu.cs:2396).
- [verified] Opcode dispatch is three Func<int>[] tables: page 0 plus prefixed pages 0x10 and 0x11; unfilled prefixed entries default to a 2-cycle NOP (M6809Cpu.cs:146-159).
- [verified] M6809State contains A/B/DP/X/Y/U/S/PC, flags, halted state, cycles, and the D=A:B property (M6809State.cs:4-39).
- [verified] PetMachine exposes the concrete M6809Cpu, selects it through IProcessor, and advances PET peripherals from the CPU cycle delta (src/PetEmulator.Pet/PetMachine.cs:21-35, 325-401).

## 3. Relevant Architecture

- [verified] Shared contracts are CPU-neutral: IMemoryBus exposes byte reads/writes; IProcessor exposes reset, instruction stepping, cycle/instruction counters and IRQ/NMI; IDebuggableProcessor exposes named registers (lib/PetEmulator.Core/IMemoryBus.cs:1-8, IProcessor.cs:3-18, IDebuggableProcessor.cs:3-12).
- [verified] The 6502 reference architecture separates CPU lifecycle/state from opcode metadata and variant tables (lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.Core.cs:18-30, OpcodeDefinition.cs:23-32, Cpu6502Variant.cs:13-27).
- [inferred] The correct 6800/6809 boundary is narrower than putting all current 6809 code in a base class: A/B/X/PC, byte/word fetch, common flags, common memory operations and genuinely shared 6800 instructions belong in the base; DP, Y/U/S, D-specific behaviour, indexed postbyte decoding, page prefixes, FIRQ, 6809 vectors and full/fast frames remain in 6809.
- [verified] SuperPET is a high-risk integration boundary: the machine holds the concrete M6809Cpu, selects it alongside the 6502, and tests its firmware reset/CPU switching (PetMachine.cs:81-87, 264-323; tests/PetEmulator.Pet.Tests/PetMachineTests.cs:207-275).

## 4. GitNexus Findings

- [graph, context(M6809Cpu)] M6809Cpu has lower-bound incoming dependencies because interface dispatch is unresolved; IProcessor and IDebuggableProcessor callers may be missing.
- [graph, impact(M6809Cpu, upstream, maxDepth=3, summaryOnly=true)] Impact is CRITICAL: 163 affected symbols, 96 direct, 11 modules, 3 affected processes. Direct consumers include PetMachine, CPU6809 tests, CLI/debug tracing, diagnostics and shared processor contracts. This is a lower bound because the index is stale and interface dispatch is incomplete.
- [graph, query(M6809Cpu ... machine integration)] Relevant flows include PetMachine.DiagnoseSuperPet6809Startup to M6809Cpu.Step and machine RunUntil/trace paths; tests cover addressing, prefixes, interrupts, opcode sweeps and SuperPET integration.
- [graph limitation] Step impact by compound name returned UNKNOWN/not found; use concrete M6809Cpu impact and source verification instead of treating the empty result as safety evidence.
- [verified] Existing tests are split by semantic area: M6809StateTests, AddressingTests, PrefixTests, IntegrationTests, InterruptTests and PetMachineTests.

## 5. Statement-Level PDG Findings

PDG evidence is unavailable: the attempted refresh failed with exit code 1, and the context resource still reports the old index plus incremental-in-progress. The executor must use source-level ordering below and re-run a fresh graph/PDG check before implementation.

- [verified] M6809Cpu.Step orders interrupt dispatch before halt handling and normal fetch/execute; the base lifecycle must preserve NMI, then FIRQ, then IRQ priority and halted wake-up semantics (M6809Cpu.cs:91-143).
- [verified] ResolveIndexed mutates X/Y/U/S for pre/post increments and arms NMI when S is written; this stateful decoder stays in the 6809 layer (M6809Cpu.cs:471-625).
- [verified] PushFullFrame and PushFastFrame mutate E and S and write different register frames; they must not be moved to a generic 6800 interrupt helper (M6809Cpu.cs:2374-2392).
- [verified] PetMachine.StepInstruction uses the CPU cycle delta to tick peripherals and then refreshes IRQ; changing cycle accounting affects every PET device (PetMachine.cs:381-401).

## 6. Proposed Changes

### 6.1 Add a separate base project

- Add lib/PetEmulator.Cpu6800/PetEmulator.Cpu6800.csproj, namespace PetEmulator.Cpu6800, referencing only PetEmulator.Core.
- Add the project to PetEmulator.slnx and make PetEmulator.Cpu6809 reference it. Keep Cpu6809 -> Cpu6800 -> Core; Core must not depend on either CPU.
- Keep IProcessor, IDebuggableProcessor and IMemoryBus unchanged in the first migration. This limits the high-risk change to the CPU assembly boundary.

### 6.2 Define 6800 state and flags

- Add M6800State with the genuinely common architectural state: A, B, X, 16-bit stack pointer, PC, shared condition flags, halted state and cycle count. Provide D only if it is a documented 6800-compatible alias; otherwise keep D in the 6809 state.
- Add M6800Flags for common 6800 condition bits and packing/unpacking. Do not force 6809 E/F semantics into the base.
- Change M6809State to derive from or compose the base state, retaining Y, U, S, DP, 6809 flags and existing public D behaviour. Preserve current state tests before adding new 6800 tests.
- Decide explicitly whether the base stack property is virtual (StackPointer) or whether base stack helpers are protected primitives supplied with a stack-register hook. 6809 must continue using S while MC6800 uses its own stack pointer.

### 6.3 Build a metadata-driven opcode layer

- Add M6800AddressingMode, M6800OpcodeDefinition, M6800OpcodeTable and table-builder helpers modelled on the 6502 metadata pattern, adapted to 6800 addressing modes and cycle rules.
- Keep execution delegates instance-bound if needed for protected CPU operations, while keeping opcode metadata immutable and inspectable. Avoid a large switch as the final dispatch mechanism.
- Define a base 6800 table containing only verified MC6800 instructions. Do not populate unknown entries with silent NOPs; use an explicit illegal/unimplemented definition that can be tested and diagnosed.
- Define a derived 6809 table by overlaying/replacing base entries and adding page-10/page-11 tables. Prefix bytes and indexed postbyte instructions must be explicit.
- Add an opcode manifest test for each CPU: every supported opcode has mnemonic, length, addressing mode, base cycles and a handler; unsupported opcodes fail deterministically.

### 6.4 Extract the base execution lifecycle

- Add abstract/protected M6800Cpu implementing IProcessor and IDebuggableProcessor where the common register surface is meaningful.
- Move into the base: memory ownership, reset lifecycle hook, instruction count, cycle count, StepInstruction, opcode fetch, big-endian word fetch/read/write helpers, common IRQ/NMI entry points only where hardware semantics are truly shared, and common halt/error handling.
- Preserve M6809Cpu.Step as a compatibility method delegating to the new lifecycle. StepInstruction remains the contract method used by machines and tests.
- Use protected virtual hooks for reset vector, interrupt service, opcode-table construction and state creation. Avoid calling overridable methods from a base constructor; pass prepared tables/state or initialize through a non-virtual factory path.

### 6.5 Move only verified 6800 instruction families

- First migrate common flag helpers, byte loads/stores, A/B arithmetic and logic, branches, jumps/subroutine/return primitives, stack byte/word primitives and direct/extended/immediate addressing shared by both CPUs.
- Keep 6809-only families in M6809Cpu: Y/U/S and DP operations, 6809 16-bit additions/comparisons, EXG/TFR register map, ABX, SEX, prefixed long branches, page-10/page-11 instructions, 6809 indexed postbyte modes, CWAI, SYNC, FIRQ and 6809 interrupt frames.
- Move code into focused files (M6800Cpu.Core.cs, State.cs, Addressing.cs, Arithmetic.cs, ControlFlow.cs, Stack.cs, OpcodeTables.cs) and corresponding M6809Cpu files. Do not use partial as a substitute for separating base versus extension responsibility.

### 6.6 Preserve machine/debugger integration

- Keep PetMachine._superPet6809Cpu concrete during the first migration so existing diagnostic code and public tests remain source-compatible; the M6809Cpu type still exists.
- Verify PetMachine.Processor continues to expose the active CPU through IProcessor, and that cycle deltas still tick PIA/VIA/CRTC/ACIA and the SuperPET IRQ line correctly.
- Update only project references and namespaces needed by the new project. Do not redesign PetMachine, CLI, Desktop or debugger contracts as part of the CPU extraction.

## 7. Implementation Sequence

1. Record a clean baseline: build the solution and run CPU6809 and PET targeted tests. Capture current cycle counts, PC/state and opcode-sweep results.
2. Re-index GitNexus on the implementation branch; resolve or record remaining stale/incomplete graph state before editing shared symbols.
3. Add Cpu6800 project, solution entry and test project; make the empty project compile without changing consumers.
4. Add M6800State/M6800Flags and characterization tests for reset, D/flag packing, fetch/read/write and state snapshot behaviour.
5. Add opcode metadata/table abstractions and base illegal-opcode policy; test table completeness independently of execution.
6. Introduce M6800Cpu with a minimal lifecycle and compatibility surface. Use a temporary 6800 smoke instruction set to prove dispatch, cycle counting and reset without touching SuperPET.
7. Convert M6809Cpu to inherit from M6800Cpu, initially keeping existing 6809 instruction bodies and tables in place. At this checkpoint all existing 6809 tests must pass.
8. Extract common instruction families one group at a time, adding 6800 tests and rerunning the complete 6809 test project after each group. Do not move indexed postbyte or interrupt-frame code until common behaviour is proven.
9. Replace remaining 6809 Func<int>[] dispatch with metadata-backed base/derived tables, including explicit page-10/page-11 extension tables and unsupported-opcode diagnostics.
10. Update project references and run SuperPET integration tests; confirm processor switching, Waterloo reset execution, diagnostics and peripheral ticking.
11. Add MC6800 opcode/addressing/integration tests and only then add optional 6800-derived CPU types. Keep unimplemented family variants out of this migration.
12. Re-index, run graph change detection, review the staged diff, and document intentionally deferred 6809 cycle-accuracy work.

## 8. Test Strategy

- Existing regression: run all tests/PetEmulator.Cpu6809.Tests tests, preserving state, page-0, prefix, addressing, integration, interrupt and sweep coverage.
- New tests/PetEmulator.Cpu6800.Tests: reset-vector loading, fetch/fetch16 endianness, stack semantics, flags, common A/B operations, direct/extended/immediate addressing, branches, subroutine/return, selected MC6800 IRQ/NMI policy, cycle counts and unsupported opcode behaviour.
- Inheritance tests: instantiate M6809Cpu through the base type where appropriate; prove 6809-only registers and prefix tables remain available only on the derived type.
- Compatibility tests: Step and StepInstruction execute exactly one instruction/interrupt dispatch; InstructionCount and CycleCount remain unchanged for equivalent programs.
- Machine regression: preserve SuperPet firmware exposure, reset execution, processor switching, memory-map selection, ACIA IRQ and diagnostic trace tests.
- Verified commands: dotnet build PetEmulator.slnx --no-restore; dotnet test tests/PetEmulator.Cpu6809.Tests/PetEmulator.Cpu6809.Tests.csproj --no-restore; dotnet test tests/PetEmulator.Pet.Tests/PetEmulator.Pet.Tests.csproj --no-restore.

## 9. Risk and Impact Analysis

- [critical] M6809Cpu has a GitNexus lower-bound impact of 163 symbols/96 direct dependents; interface-bound consumers are not fully resolvable. Re-run impact after each public API change.
- [high] Interrupt ordering and frame layout are hardware-specific. Moving them into the base can silently break Waterloo firmware; keep vector addresses and frame hooks in 6809 until a verified 6800 contract exists.
- [high] Indexed addressing mutates registers and arms NMI on S writes. It must not be generalized from 6809 postbytes into 6800 addressing by inheritance alone.
- [high] Aggregate cycle deltas drive every PET peripheral; instruction-result compatibility is insufficient.
- [medium] Changing M6809State inheritance or field/property shapes can break tests, diagnostics and direct State callers. Preserve a compatibility facade until consumers migrate.
- [medium] Replacing default-NOP fallback with explicit illegal-opcode errors may expose latent firmware assumptions; introduce diagnostics and update sweeps deliberately.
- [performance] Per-step metadata/delegate indirection must not add allocations; build tables once and use arrays/immutable records.

## 10. Files Expected to Change

| File | Symbols | Reason |
| --- | --- | --- |
| PetEmulator.slnx | project entries | Add Cpu6800 and tests projects |
| lib/PetEmulator.Cpu6800/* | M6800Cpu, M6800State, M6800Flags, opcode metadata/tables | Add base CPU and common infrastructure |
| lib/PetEmulator.Cpu6809/PetEmulator.Cpu6809.csproj | project reference | Depend on Cpu6800 |
| lib/PetEmulator.Cpu6809/M6809Cpu*.cs | M6809Cpu | Derive from base and retain 6809-only logic |
| lib/PetEmulator.Cpu6809/M6809State.cs, M6809Flags.cs | state/flags | Extend common state without losing compatibility |
| src/PetEmulator.Pet/PetMachine.cs | PetMachine | Only type adjustments required by compilation |
| tests/PetEmulator.Cpu6800.Tests/* | new base tests | Cover MC6800 contract |
| tests/PetEmulator.Cpu6809.Tests/* | existing regression tests | Cover inheritance and no regression |
| tests/PetEmulator.Pet.Tests/PetMachineTests.cs | SuperPET cases | Integration regression |

## 11. Reusable Implementation Context

implementation_context:
  task_summary: Create a reusable MC6800 base CPU and migrate M6809Cpu to derive from it without changing SuperPET or IProcessor behaviour.
  acceptance_criteria:
    - Cpu6800 is a separate solution project with no dependency on Cpu6809.
    - M6809Cpu derives from M6800Cpu and preserves current public APIs and all existing 6809 tests.
    - Common 6800 instruction/addressing/state logic is in the base; 6809-only registers, prefixes, indexed postbytes and interrupt frames remain derived.
    - SuperPET firmware, switching, diagnostics and peripheral cycle ticking remain green.
    - Opcode metadata is inspectable and unsupported opcodes are deterministic, not silent NOPs.
  evidence_provenance:
    schema_version: 2
    head_commit: 054a04283754950477e90967f62a7bfda51e56f5
    generated_plan_path: docs/plans/2026-09-12-gitnexus-plan-6800-6809-core-migration.md
    global_dirty_digest:
      algorithm: sha256
      canonicalization: gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records
      value: 0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd
    cited_path_manifest:
      - {path: Directory.Build.props, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:c92bda2499a2885dd9df7ed9ddac5bc99901388ce8b8a7834d08b5335b1bdea8, index_digest: sha256:c92bda2499a2885dd9df7ed9ddac5bc99901388ce8b8a7834d08b5335b1bdea8, worktree_digest: sha256:c92bda2499a2885dd9df7ed9ddac5bc99901388ce8b8a7834d08b5335b1bdea8, untracked_digest: absent}
      - {path: PetEmulator.slnx, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:ab1673dcc1154eca3d932231e990a2679f4643179097755f5f3be1c3b2aefb7a, index_digest: sha256:ab1673dcc1154eca3d932231e990a2679f4643179097755f5f3be1c3b2aefb7a, worktree_digest: sha256:ab1673dcc1154eca3d932231e990a2679f4643179097755f5f3be1c3b2aefb7a, untracked_digest: absent}
      - {path: lib/PetEmulator.Core/IDebuggableProcessor.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:7b68c850b5bc1a6832d5b762066e6cbc3aa3fb47dae03151d7b69141feb939d0, index_digest: sha256:7b68c850b5bc1a6832d5b762066e6cbc3aa3fb47dae03151d7b69141feb939d0, worktree_digest: sha256:7b68c850b5bc1a6832d5b762066e6cbc3aa3fb47dae03151d7b69141feb939d0, untracked_digest: absent}
      - {path: lib/PetEmulator.Core/IMemoryBus.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:abcd3b013809c75b157bef86cc0df91fed038e294bd5dda534bd3a3188a89525, index_digest: sha256:abcd3b013809c75b157bef86cc0df91fed038e294bd5dda534bd3a3188a89525, worktree_digest: sha256:abcd3b013809c75b157bef86cc0df91fed038e294bd5dda534bd3a3188a89525, untracked_digest: absent}
      - {path: lib/PetEmulator.Core/IProcessor.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:3837a5efaac9352e79df4d714de99dba9e9c38d9f49fe5e3fd5f692271aa9e7b, index_digest: sha256:3837a5efaac9352e79df4d714de99dba9e9c38d9f49fe5e3fd5f692271aa9e7b, worktree_digest: sha256:3837a5efaac9352e79df4d714de99dba9e9c38d9f49fe5e3fd5f692271aa9e7b, untracked_digest: absent}
      - {path: lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.Core.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:3223c78bafa4c5a51e77201a650a4850cf4aff0405fec6873626bf8a7655f4b2, index_digest: sha256:3223c78bafa4c5a51e77201a650a4850cf4aff0405fec6873626bf8a7655f4b2, worktree_digest: sha256:3223c78bafa4c5a51e77201a650a4850cf4aff0405fec6873626bf8a7655f4b2, untracked_digest: absent}
      - {path: lib/PetEmulator.Cpu6502/Processor/Cpu6502Variant.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:5ca71e18495bce7417de960be6ba86f0ab6298781b6c39d668b9ce949d37f60c, index_digest: sha256:5ca71e18495bce7417de960be6ba86f0ab6298781b6c39d668b9ce949d37f60c, worktree_digest: sha256:5ca71e18495bce7417de960be6ba86f0ab6298781b6c39d668b9ce949d37f60c, untracked_digest: absent}
      - {path: lib/PetEmulator.Cpu6502/Processor/OpcodeDefinition.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:a6852922f41f5c9427c742d7de84941562c78dd52eed55ba83fdf3f1d0168a86, index_digest: sha256:a6852922f41f5c9427c742d7de84941562c78dd52eed55ba83fdf3f1d0168a86, worktree_digest: sha256:a6852922f41f5c9427c742d7de84941562c78dd52eed55ba83fdf3f1d0168a86, untracked_digest: absent}
      - {path: lib/PetEmulator.Cpu6809/M6809Cpu.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:0f250926e86edc279c0bbb9dc07e870b67ffcb26132505e14890a4a9ebf1f02d, index_digest: sha256:0f250926e86edc279c0bbb9dc07e870b67ffcb26132505e14890a4a9ebf1f02d, worktree_digest: sha256:0f250926e86edc279c0bbb9dc07e870b67ffcb26132505e14890a4a9ebf1f02d, untracked_digest: absent}
      - {path: lib/PetEmulator.Cpu6809/M6809Flags.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:f867f61f181933847d555606bcfa7ee2f5210abe4d9a8c60b58f8c1b07883949, index_digest: sha256:f867f61f181933847d555606bcfa7ee2f5210abe4d9a8c60b58f8c1b07883949, worktree_digest: sha256:f867f61f181933847d555606bcfa7ee2f5210abe4d9a8c60b58f8c1b07883949, untracked_digest: absent}
      - {path: lib/PetEmulator.Cpu6809/M6809State.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:90fa043c7f0824760d50cbb09c78cdcb6b577673bfa7b6c761170186da306c8c, index_digest: sha256:90fa043c7f0824760d50cbb09c78cdcb6b577673bfa7b6c761170186da306c8c, worktree_digest: sha256:90fa043c7f0824760d50cbb09c78cdcb6b577673bfa7b6c761170186da306c8c, untracked_digest: absent}
      - {path: lib/PetEmulator.Cpu6809/PetEmulator.Cpu6809.csproj, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:39e2108333777d61480820c9587db7394aa84f74fb7fc5003086acf98925ea65, index_digest: sha256:39e2108333777d61480820c9587db7394aa84f74fb7fc5003086acf98925ea65, worktree_digest: sha256:39e2108333777d61480820c9587db7394aa84f74fb7fc5003086acf98925ea65, untracked_digest: absent}
      - {path: src/PetEmulator.Pet/PetMachine.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:bdfa906ef25ae5cddd46610949b8039cf8a6d26ea3692ca5b837fa122de11771, index_digest: sha256:bdfa906ef25ae5cddd46610949b8039cf8a6d26ea3692ca5b837fa122de11771, worktree_digest: sha256:bdfa906ef25ae5cddd46610949b8039cf8a6d26ea3692ca5b837fa122de11771, untracked_digest: absent}
      - {path: tests/PetEmulator.Cpu6809.Tests/M6809/AddressingTests.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:9e97cd46ca5429692da39768a86bc04aafd9d36b093ebcb1d7223b25d5543e88, index_digest: sha256:9e97cd46ca5429692da39768a86bc04aafd9d36b093ebcb1d7223b25d5543e88, worktree_digest: sha256:9e97cd46ca5429692da39768a86bc04aafd9d36b093ebcb1d7223b25d5543e88, untracked_digest: absent}
      - {path: tests/PetEmulator.Cpu6809.Tests/M6809/IntegrationTests.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:f86715c78afe99dd76cb2356bf29c4b083e1313a97cac6acdc6637d2d8708db5, index_digest: sha256:f86715c78afe99dd76cb2356bf29c4b083e1313a97cac6acdc6637d2d8708db5, worktree_digest: sha256:f86715c78afe99dd76cb2356bf29c4b083e1313a97cac6acdc6637d2d8708db5, untracked_digest: absent}
      - {path: tests/PetEmulator.Cpu6809.Tests/M6809/InterruptTests.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:9f8cd1b41e4ed623505d5d68a8ea5a60fa801d72641b59382ea6231700026e8e, index_digest: sha256:9f8cd1b41e4ed623505d5d68a8ea5a60fa801d72641b59382ea6231700026e8e, worktree_digest: sha256:9f8cd1b41e4ed623505d5d68a8ea5a60fa801d72641b59382ea6231700026e8e, untracked_digest: absent}
      - {path: tests/PetEmulator.Cpu6809.Tests/M6809/M6809StateTests.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:7d12179d5a20fd295ae5007d341b7b8fd6ac8c5e8b54c50675eeeaedadcdb753, index_digest: sha256:7d12179d5a20fd295ae5007d341b7b8fd6ac8c5e8b54c50675eeeaedadcdb753, worktree_digest: sha256:7d12179d5a20fd295ae5007d341b7b8fd6ac8c5e8b54c50675eeeaedadcdb753, untracked_digest: absent}
      - {path: tests/PetEmulator.Cpu6809.Tests/M6809/PrefixTests.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:98b6b7c14249efeb007c4b8d55fa5e600b8cb26690f9be72b8f7443d57730539, index_digest: sha256:98b6b7c14249efeb007c4b8d55fa5e600b8cb26690f9be72b8f7443d57730539, worktree_digest: sha256:98b6b7c14249efeb007c4b8d55fa5e600b8cb26690f9be72b8f7443d57730539, untracked_digest: absent}
      - {path: tests/PetEmulator.Cpu6809.Tests/PetEmulator.Cpu6809.Tests.csproj, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:2ab88b17592e9d6fb77736f7e48143ca4af36a2167b7c862fcd50e1cb51a8277, index_digest: sha256:2ab88b17592e9d6fb77736f7e48143ca4af36a2167b7c862fcd50e1cb51a8277, worktree_digest: sha256:2ab88b17592e9d6fb77736f7e48143ca4af36a2167b7c862fcd50e1cb51a8277, untracked_digest: absent}
      - {path: tests/PetEmulator.Pet.Tests/PetMachineTests.cs, object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}, state: clean, rename_from: null, rename_to: null, head_digest: sha256:d55d6b2a4672a90f8e4ea2a55f98d4068a0ea0d64c9823c5396a03d056e43db2, index_digest: sha256:d55d6b2a4672a90f8e4ea2a55f98d4068a0ea0d64c9823c5396a03d056e43db2, worktree_digest: sha256:d55d6b2a4672a90f8e4ea2a55f98d4068a0ea0d64c9823c5396a03d056e43db2, untracked_digest: absent}
  primary_symbols:
    - M6809Cpu at lib/PetEmulator.Cpu6809/M6809Cpu.cs:6-2450: migration target
    - M6809Cpu.Step at lib/PetEmulator.Cpu6809/M6809Cpu.cs:91-144: lifecycle
    - M6809State at lib/PetEmulator.Cpu6809/M6809State.cs:4-39: current state
    - PetMachine.StepInstruction at src/PetEmulator.Pet/PetMachine.cs:381-401: timing boundary
  related_symbols:
    - IProcessor: stable interface boundary
    - IDebuggableProcessor: debugger register contract
    - M6809Cpu.ResolveIndexed: derived stateful addressing
    - M6809Cpu.PushFullFrame and PushFastFrame: derived interrupt frames
  execution_path:
    - PetMachine selects the active IProcessor and keeps concrete M6809Cpu for SuperPET diagnostics.
    - PetMachine.StepInstruction calls IProcessor.StepInstruction, computes cycle delta, ticks peripherals, then refreshes CPU IRQ.
    - M6809Cpu dispatches interrupts, handles halt, fetches opcode, invokes page-0/page-10/page-11 handler, and adds returned cycles.
  pdg_constraints:
    - PDG unavailable after failed refresh; re-run before implementation and do not claim statement-level graph coverage.
  architectural_patterns:
    - stable CPU-neutral lifecycle contracts from lib/PetEmulator.Core/IProcessor.cs
    - metadata-backed opcode definitions from lib/PetEmulator.Cpu6502/Processor/OpcodeDefinition.cs
    - machine peripheral ticking from src/PetEmulator.Pet/PetMachine.cs:381-401
  files_to_modify:
    - PetEmulator.slnx: add Cpu6800 and test projects
    - lib/PetEmulator.Cpu6800/*: add base CPU and metadata
    - lib/PetEmulator.Cpu6809/*: derive and specialize
    - tests/PetEmulator.Cpu6800.Tests/*: add base tests
    - tests/PetEmulator.Cpu6809.Tests/*: preserve regression tests
    - src/PetEmulator.Pet/PetMachine.cs: adjust only if compilation requires it
  tests:
    - tests/PetEmulator.Cpu6809.Tests/M6809/IntegrationTests.cs: opcode sweeps, firmware validation, register/PC results
    - tests/PetEmulator.Cpu6809.Tests/M6809/InterruptTests.cs: NMI, FIRQ, IRQ, RTI, CWAI/SYNC
    - tests/PetEmulator.Pet.Tests/PetMachineTests.cs: firmware, switching, maps and IRQ integration
    - tests/PetEmulator.Cpu6800.Tests/*: base reset/fetch/stack/flags, common instructions, metadata and illegal opcodes
  verification_commands:
    - dotnet build PetEmulator.slnx --no-restore
    - dotnet test tests/PetEmulator.Cpu6809.Tests/PetEmulator.Cpu6809.Tests.csproj --no-restore
    - dotnet test tests/PetEmulator.Pet.Tests/PetEmulator.Pet.Tests.csproj --no-restore
  risks:
    - M6809Cpu impact is CRITICAL and GitNexus interface dispatch is lower-bound.
    - Interrupt frames and indexed postbytes must not be generalized prematurely.
    - Cycle deltas affect PET peripheral timing.
    - Per-step allocations from dispatch are forbidden.
  assumptions:
    - Target base is the original MC6800 set; confirm exact 6800/6802/6808 variant before implementation.
    - First migration preserves aggregate instruction-cycle timing; bus-cycle accuracy is separate.
    - No Core interface changes are required initially.
  open_questions:
    - Which exact Motorola 6800 family member and monitor behaviour is required first?
    - Should M6800State be inherited or composed by M6809State?
    - Should handlers use generic typed tables or instance-bound delegates with immutable metadata?
  avoid:
    - Do not copy the entire 6809 opcode set into M6800.
    - Do not move DP/Y/U/S, indexed postbyte, page-prefix, FIRQ or 6809 frame logic into the base without evidence.
    - Do not change IProcessor, PetMachine lifecycle or debugger contracts in the first migration.
    - Do not use silent default NOPs for missing opcode definitions.

## 12. Assumptions and Open Questions

Assumptions:

- The initial target is a canonical MC6800-compatible base; exact family variant, reset/interrupt vectors and cycle table must be confirmed before implementation.
- “Architecture from 6502” means separation of state, metadata, tables and instruction families, not copying 6502-specific cycle state.
- Aggregate instruction-cycle compatibility is the first finish line; per-bus-cycle accuracy is deferred unless existing machine timing tests require it.

Open questions:

- Confirm whether the desired first base is MC6800, MC6802, MC6808 or a project-specific superset.
- Decide whether M6800State is inherited by M6809State or composed behind a compatibility facade.
- Decide typed opcode-table representation after a small prototype measures compile complexity and allocations.
- Decide whether to introduce a processor-variant abstraction now or after the canonical 6800 core passes.

Deferred follow-ups: 6809 bus-cycle/per-access timing; additional 6800-family CPUs and undocumented opcode policy; richer shared register/state snapshots.

## 13. Definition of Done

- dotnet build PetEmulator.slnx --no-restore succeeds with zero warnings/errors.
- New Cpu6800 tests pass and document the selected MC6800 contract.
- M6809Cpu derives from M6800Cpu; all existing CPU6809 tests pass without weakened assertions.
- SuperPET firmware, switching, memory maps, diagnostics, ACIA/VIA/PIA IRQ propagation and cycle-driven peripherals pass.
- Opcode metadata is inspectable, complete for supported instructions and deterministic for unsupported instructions.
- No per-instruction dictionary/table allocation is introduced; the dispatch choice has focused allocation/benchmark coverage.
- GitNexus is re-indexed successfully, detect_changes --scope all is clean for intended scope, and the final diff contains no unrelated refactor.
