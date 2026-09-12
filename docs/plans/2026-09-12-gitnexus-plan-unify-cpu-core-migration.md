# GitNexus Engineering Plan

> Task: Migrate 6502, 6800, 6809 and Z80 to one shared Core.
> Evidence verified at commit 4675e59e7dbe00efc21bac3d8b7b62c870328540; GitNexus index is 1 commit behind; --pdg refresh attempted and exited 1, so graph claims are lower-bound.
> Evidence provenance schema 2; dirty digest 0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd; cited manifest 26 entries.

## 1. Objective

[verified] All four CPUs must use PetEmulator.Core contracts without losing CPU-specific timing, interrupt, I/O, debug or regression behavior.

Acceptance: every CPU implements IProcessor; Z80 uses Core.IMemoryBus; every CPU implements IDebuggableProcessor; I/O, FIRQ, WAIT and cycle tracing are optional capabilities; existing CPU and binary tests stay green.

## 2. Current Behaviour

[verified] IProcessor defines Halted, CycleCount, InstructionCount, Reset, StepInstruction, SetIRQ and SetNMI (lib/PetEmulator.Core/IProcessor.cs:4-19).

[verified] 6502 is the integration pattern: IProcessor + IDebuggableProcessor, Core.IMemoryBus and injected Core.IClock (lib/PetEmulator.Cpu6502/Processor/Cpu6502.PublicMethods.cs:8-18; Cpu6502.Fields.cs:67-101).

[verified] 6800 already implements Core interfaces but counts cycles in M6800State and has empty base SetIRQ/SetNMI (lib/PetEmulator.Cpu6800/M6800Cpu.cs:6-8,35-88). 6809 inherits it and adds FIRQ/extended state (lib/PetEmulator.Cpu6809/M6809Cpu.cs:7-145).

[verified] Z80 is isolated: local IBus, IMemoryBus, IIoBus, IInterruptLines and Step(), with no Core project reference (lib/PetEmulator.CpuZ80/Cpu/Z80Cpu.cs:8-109).

## 3. Relevant Architecture

[verified] IMachine and debugger consume CPU-agnostic IProcessor and Core.IMemoryBus (lib/PetEmulator.Core/IMachine.cs:11-35). Core.IClock is a monotonic counter; Z80.IEmulationClock also owns start/pause/stop (lib/PetEmulator.Core/IClock.cs:3-11; lib/PetEmulator.CpuZ80/Timing/IEmulationClock.cs:3-14).

[verified] Core.BusAccess is a simple memory access, while Z80.BusCycle carries cycle kind, machine cycle and T-state (lib/PetEmulator.Core/BusAccess.cs:3-11; lib/PetEmulator.CpuZ80/Bus/BusCycle.cs:3-27).

[inferred] Keep IProcessor minimal. Model I/O, FIRQ, WAIT and cycle-aware bus as opt-in Core capabilities.

## 4. GitNexus Findings

[graph] impact IProcessor upstream depth 2 found 42 symbols, 32 direct dependents, 13 processes and 11 modules; risk CRITICAL/lower-bound.

[graph] impact Core IMemoryBus upstream depth 2 found 178 symbols, 76 direct dependents, 15 processes and 17 modules; risk CRITICAL/lower-bound.

[graph] impact IClock upstream depth 2 found 7 symbols and 2 direct dependents, mainly CPU6502.Tests.

[verified] Source confirms 6800/6809 already use Core and Z80 uses duplicate contracts. The graph is stale by one commit.

## 5. Statement-Level PDG Findings

[assumed] PDG is not evidence because the refresh exited 1. Re-run it before implementation.

[verified] M6809.Step order is NMI, FIRQ, IRQ, HALT, fetch, execute (lib/PetEmulator.Cpu6809/M6809Cpu.cs:95-145).

[verified] Z80 checks WAIT before NMI/INT, detects NMI edge and emits refresh while HALTed (lib/PetEmulator.CpuZ80/Cpu/Z80Cpu.cs:68-109). This ordering is a compatibility constraint.

## 6. Proposed Changes

- Core: add optional port-bus, cycle-observer, WAIT and FIRQ capability interfaces; do not make Z80-only features mandatory on IProcessor.
- Core: keep IMemoryBus Read/Write signatures; define counter and interrupt semantics; add optional cycle-step result only if required by machine integration.
- Z80: reference Core, remove local IMemoryBus, adapt RamMemory/RomMemory/SystemBus, implement IProcessor and IDebuggableProcessor, inject Core.IClock, add counters and StepInstruction.
- Z80: preserve I/O, WAIT, interrupt acknowledge, refresh, BusCycle order and T-states through capabilities/adapters.
- 6800/6809: use shared clock and capabilities while retaining 6800 -> 6809 inheritance, FIRQ, S/U frames, E/F flags and vectors.
- Debugger, DebugApi, CLI, Desktop and machines: consume Core only; remove CPU-specific casts after adapters pass.
- Delete duplicate contracts only after full regression.

## 7. Implementation Sequence

1. Record baseline build and targeted CPU regressions.
2. Add Core contract tests for lifecycle, counters, reset, interrupts and debug registers.
3. Add additive capability interfaces.
4. Move Z80 memory to Core.IMemoryBus; run bus/memory tests.
5. Move Z80 I/O, interrupts, cycle observer and clock; preserve z80doc, ZEXDOC and timing.
6. Add Z80 IProcessor/IDebuggableProcessor and verify HALT, WAIT, NMI, INT and counters.
7. Unify 6800/6809 clock and decide/test real MC6800 IRQ/NMI semantics.
8. Migrate debugger and machine integrations.
9. Remove duplicate adapters and update docs.
10. Run full regression and compare trace/cycle baselines.

## 8. Test Strategy

- Core: tests/PetEmulator.Core.Tests/MachineContractTests.cs plus contract tests.
- 6800: M6800CpuTests, state tests and mc6800 functional/regression binary.
- 6809: M6809/InterruptTests, addressing/prefix/integration and schwotzer-cputest.bin.
- Z80: BusTests, InterruptTests, Z80CpuTests, Z80TimingTests, z80doc.tap and opt-in zexdoc.com.
- Integration: Debugger, DebugApi, CLI/Desktop and machine tests for every CPU.
- Verify with dotnet build PetEmulator.slnx --no-restore and targeted dotnet test commands; finish with full dotnet test.

## 9. Risk and Impact Analysis

- [graph] IProcessor and IMemoryBus have critical blast radius across machines, debugger, CLI, Desktop and tests.
- [verified] Main behavioral risk is timing/interrupt ordering, not compilation: M6809 FIRQ/NMI and Z80 WAIT/refresh/I/O.
- [verified] Z80 currently has 516/516 passing tests including z80doc.
- [inferred] Do not move all Z80 types into the minimal Core; use capabilities.
- Keep observers opt-in and avoid per-cycle allocations. Keep CPU-specific public API during transition.

## 10. Files Expected to Change

| File | Symbols | Reason |
| --- | --- | --- |
| lib/PetEmulator.Core | IProcessor, IMemoryBus, IClock, capabilities | shared contracts |
| lib/PetEmulator.CpuZ80 | Z80Cpu, bus, interrupts, timing, memory | migrate local contracts |
| lib/PetEmulator.Cpu6800 | M6800Cpu, M6800State | shared clock/interrupts |
| lib/PetEmulator.Cpu6809 | M6809Cpu, M6809State | preserve 6809 extensions |
| lib/PetEmulator.Debugger, lib/PetEmulator.DebugApi | debugger/trace | CPU-agnostic integration |
| src/PetEmulator.Cli, src/PetEmulator.Desktop, machines | integrations | remove CPU casts |
| tests/PetEmulator.Core.Tests and CPU test projects | contract/regression tests | compatibility proof |

## 11. Reusable Implementation Context

~~~json
{
  "schema_version": 2,
  "head_commit": "4675e59e7dbe00efc21bac3d8b7b62c870328540",
  "generated_plan_path": "docs/plans/2026-09-12-gitnexus-plan-unify-cpu-core-migration.md",
  "global_dirty_digest": {
    "algorithm": "sha256",
    "canonicalization": "gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records",
    "value": "0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd"
  },
  "cited_path_manifest": [
    {
      "path": "PetEmulator.slnx",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:36b87d3a0a0e19a74db84e7e56c821f8e04b180cc3a90617726ceb74e07d6c98",
      "index_digest": "sha256:36b87d3a0a0e19a74db84e7e56c821f8e04b180cc3a90617726ceb74e07d6c98",
      "worktree_digest": "sha256:36b87d3a0a0e19a74db84e7e56c821f8e04b180cc3a90617726ceb74e07d6c98",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Core/BusAccess.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:e97f7e4b545830493a9e81b1ec23555ef18b574380e8849f53d2efb3e4be3e02",
      "index_digest": "sha256:e97f7e4b545830493a9e81b1ec23555ef18b574380e8849f53d2efb3e4be3e02",
      "worktree_digest": "sha256:e97f7e4b545830493a9e81b1ec23555ef18b574380e8849f53d2efb3e4be3e02",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Core/IClock.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:bdc7cf1c163c1011fd36e212c05771f786f4d6dc97dfab8a74f978c0f6e52050",
      "index_digest": "sha256:bdc7cf1c163c1011fd36e212c05771f786f4d6dc97dfab8a74f978c0f6e52050",
      "worktree_digest": "sha256:bdc7cf1c163c1011fd36e212c05771f786f4d6dc97dfab8a74f978c0f6e52050",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Core/IDebuggableProcessor.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:7b68c850b5bc1a6832d5b762066e6cbc3aa3fb47dae03151d7b69141feb939d0",
      "index_digest": "sha256:7b68c850b5bc1a6832d5b762066e6cbc3aa3fb47dae03151d7b69141feb939d0",
      "worktree_digest": "sha256:7b68c850b5bc1a6832d5b762066e6cbc3aa3fb47dae03151d7b69141feb939d0",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Core/IMachine.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:243aa8c8f9476533d3abf53b09ba3f3a7449248fd8c7ba2bab1e899d7f06b243",
      "index_digest": "sha256:243aa8c8f9476533d3abf53b09ba3f3a7449248fd8c7ba2bab1e899d7f06b243",
      "worktree_digest": "sha256:243aa8c8f9476533d3abf53b09ba3f3a7449248fd8c7ba2bab1e899d7f06b243",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Core/IMemoryBus.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:abcd3b013809c75b157bef86cc0df91fed038e294bd5dda534bd3a3188a89525",
      "index_digest": "sha256:abcd3b013809c75b157bef86cc0df91fed038e294bd5dda534bd3a3188a89525",
      "worktree_digest": "sha256:abcd3b013809c75b157bef86cc0df91fed038e294bd5dda534bd3a3188a89525",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Core/IProcessor.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:3837a5efaac9352e79df4d714de99dba9e9c38d9f49fe5e3fd5f692271aa9e7b",
      "index_digest": "sha256:3837a5efaac9352e79df4d714de99dba9e9c38d9f49fe5e3fd5f692271aa9e7b",
      "worktree_digest": "sha256:3837a5efaac9352e79df4d714de99dba9e9c38d9f49fe5e3fd5f692271aa9e7b",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Cpu6502/Processor/Cpu6502.Fields.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:bebce7088e4ced59a7d3564cb2bff63a402647488a33c36a94bb5a6c94bcd20a",
      "index_digest": "sha256:bebce7088e4ced59a7d3564cb2bff63a402647488a33c36a94bb5a6c94bcd20a",
      "worktree_digest": "sha256:bebce7088e4ced59a7d3564cb2bff63a402647488a33c36a94bb5a6c94bcd20a",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Cpu6502/Processor/Cpu6502.Properties.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:c67b72287276b3c75e26ba0d6efc9f6e32a2a14f5db7d08150066ec3cd5a0d1c",
      "index_digest": "sha256:c67b72287276b3c75e26ba0d6efc9f6e32a2a14f5db7d08150066ec3cd5a0d1c",
      "worktree_digest": "sha256:c67b72287276b3c75e26ba0d6efc9f6e32a2a14f5db7d08150066ec3cd5a0d1c",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Cpu6502/Processor/Cpu6502.PublicMethods.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:bcfd1f2618a171501e1a971a207978f7157b3d8e62dbffdd19afabc8dbeea95c",
      "index_digest": "sha256:bcfd1f2618a171501e1a971a207978f7157b3d8e62dbffdd19afabc8dbeea95c",
      "worktree_digest": "sha256:bcfd1f2618a171501e1a971a207978f7157b3d8e62dbffdd19afabc8dbeea95c",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Cpu6800/M6800Cpu.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:40daa0626bd9f5af2eff00713e0e74040e32e1130b80d5dae395fa6e7d9d48d8",
      "index_digest": "sha256:40daa0626bd9f5af2eff00713e0e74040e32e1130b80d5dae395fa6e7d9d48d8",
      "worktree_digest": "sha256:40daa0626bd9f5af2eff00713e0e74040e32e1130b80d5dae395fa6e7d9d48d8",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Cpu6800/M6800State.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:c69ad63fca07e886fd4f3f7d8535984a913a7e801056731fc184ea589fffe937",
      "index_digest": "sha256:c69ad63fca07e886fd4f3f7d8535984a913a7e801056731fc184ea589fffe937",
      "worktree_digest": "sha256:c69ad63fca07e886fd4f3f7d8535984a913a7e801056731fc184ea589fffe937",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Cpu6809/M6809Cpu.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:a3d98218f2fa2ea5f154acaad7eb123fdecbdf5f1527206fc926837560effc92",
      "index_digest": "sha256:a3d98218f2fa2ea5f154acaad7eb123fdecbdf5f1527206fc926837560effc92",
      "worktree_digest": "sha256:a3d98218f2fa2ea5f154acaad7eb123fdecbdf5f1527206fc926837560effc92",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.Cpu6809/M6809State.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:bd85ecdb332901312289f26389a29395064587a4cecbeb26998c4ee7603258c9",
      "index_digest": "sha256:bd85ecdb332901312289f26389a29395064587a4cecbeb26998c4ee7603258c9",
      "worktree_digest": "sha256:bd85ecdb332901312289f26389a29395064587a4cecbeb26998c4ee7603258c9",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.CpuZ80/Bus/BusCycle.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:244f948d534b644f370a2f43debdf3e0ba88ad3efc5bd3b7a5ca0c215a485b87",
      "index_digest": "sha256:244f948d534b644f370a2f43debdf3e0ba88ad3efc5bd3b7a5ca0c215a485b87",
      "worktree_digest": "sha256:244f948d534b644f370a2f43debdf3e0ba88ad3efc5bd3b7a5ca0c215a485b87",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.CpuZ80/Bus/IBus.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:1de9239cb0a8ec9bcae31bd8c5581cb47c80aa7a457fa3fe2b32273ed9174258",
      "index_digest": "sha256:1de9239cb0a8ec9bcae31bd8c5581cb47c80aa7a457fa3fe2b32273ed9174258",
      "worktree_digest": "sha256:1de9239cb0a8ec9bcae31bd8c5581cb47c80aa7a457fa3fe2b32273ed9174258",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.CpuZ80/Bus/IMemoryBus.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:d8d48998db6b0167ead2fca3042a3e86cf17ba4f5d31fe9bbb445c60c250fed0",
      "index_digest": "sha256:d8d48998db6b0167ead2fca3042a3e86cf17ba4f5d31fe9bbb445c60c250fed0",
      "worktree_digest": "sha256:d8d48998db6b0167ead2fca3042a3e86cf17ba4f5d31fe9bbb445c60c250fed0",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.CpuZ80/Cpu/Z80Cpu.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:829ac01abf9e63b092ee40b43d850ce3829570a5299b29ae0bc884b197bfe791",
      "index_digest": "sha256:829ac01abf9e63b092ee40b43d850ce3829570a5299b29ae0bc884b197bfe791",
      "worktree_digest": "sha256:829ac01abf9e63b092ee40b43d850ce3829570a5299b29ae0bc884b197bfe791",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.CpuZ80/Interrupts/IInterruptLines.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:7b23e78054b2fb8c8efc38d56a998ddf3240747bb3d87f7c724666fc975afbce",
      "index_digest": "sha256:7b23e78054b2fb8c8efc38d56a998ddf3240747bb3d87f7c724666fc975afbce",
      "worktree_digest": "sha256:7b23e78054b2fb8c8efc38d56a998ddf3240747bb3d87f7c724666fc975afbce",
      "untracked_digest": "absent"
    },
    {
      "path": "lib/PetEmulator.CpuZ80/Timing/IEmulationClock.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:a16573be9ac677f66e9f5772afe65cef8137da4dc9e0895d9c11c1382a92c0eb",
      "index_digest": "sha256:a16573be9ac677f66e9f5772afe65cef8137da4dc9e0895d9c11c1382a92c0eb",
      "worktree_digest": "sha256:a16573be9ac677f66e9f5772afe65cef8137da4dc9e0895d9c11c1382a92c0eb",
      "untracked_digest": "absent"
    },
    {
      "path": "tests/PetEmulator.Core.Tests/MachineContractTests.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:38953f0f5e1e3e20ea085980f188ba61ba93a1917f786250a987f0c022b46c95",
      "index_digest": "sha256:38953f0f5e1e3e20ea085980f188ba61ba93a1917f786250a987f0c022b46c95",
      "worktree_digest": "sha256:38953f0f5e1e3e20ea085980f188ba61ba93a1917f786250a987f0c022b46c95",
      "untracked_digest": "absent"
    },
    {
      "path": "tests/PetEmulator.Cpu6800.Tests/M6800CpuTests.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:b94199d1c87240ae1de595627d15c88119f24ba469f64f08c0dac59ccfe37a55",
      "index_digest": "sha256:b94199d1c87240ae1de595627d15c88119f24ba469f64f08c0dac59ccfe37a55",
      "worktree_digest": "sha256:b94199d1c87240ae1de595627d15c88119f24ba469f64f08c0dac59ccfe37a55",
      "untracked_digest": "absent"
    },
    {
      "path": "tests/PetEmulator.Cpu6809.Tests/M6809/InterruptTests.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:9f8cd1b41e4ed623505d5d68a8ea5a60fa801d72641b59382ea6231700026e8e",
      "index_digest": "sha256:9f8cd1b41e4ed623505d5d68a8ea5a60fa801d72641b59382ea6231700026e8e",
      "worktree_digest": "sha256:9f8cd1b41e4ed623505d5d68a8ea5a60fa801d72641b59382ea6231700026e8e",
      "untracked_digest": "absent"
    },
    {
      "path": "tests/PetEmulator.CpuZ80.Tests/BusTests.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:56590bf7122d986261967ee2006c0b4862b15696dce499dd97b5eb6214ef9edd",
      "index_digest": "sha256:56590bf7122d986261967ee2006c0b4862b15696dce499dd97b5eb6214ef9edd",
      "worktree_digest": "sha256:56590bf7122d986261967ee2006c0b4862b15696dce499dd97b5eb6214ef9edd",
      "untracked_digest": "absent"
    },
    {
      "path": "tests/PetEmulator.CpuZ80.Tests/InterruptTests.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:fc35ba0f7e174f30aeb45b30f3c407c751032f479ba10c662d76ee31522fc5a7",
      "index_digest": "sha256:fc35ba0f7e174f30aeb45b30f3c407c751032f479ba10c662d76ee31522fc5a7",
      "worktree_digest": "sha256:fc35ba0f7e174f30aeb45b30f3c407c751032f479ba10c662d76ee31522fc5a7",
      "untracked_digest": "absent"
    },
    {
      "path": "tests/PetEmulator.CpuZ80.Tests/Z80CpuTests.cs",
      "object_kind": {
        "head": "regular",
        "index": "regular",
        "worktree": "regular",
        "untracked": "absent"
      },
      "state": "clean",
      "rename_from": null,
      "rename_to": null,
      "head_digest": "sha256:be154bd03b749ee1659a62cb27c2ae8b0a49ce362db2de786222241d723c024c",
      "index_digest": "sha256:be154bd03b749ee1659a62cb27c2ae8b0a49ce362db2de786222241d723c024c",
      "worktree_digest": "sha256:be154bd03b749ee1659a62cb27c2ae8b0a49ce362db2de786222241d723c024c",
      "untracked_digest": "absent"
    }
  ]
}
~~~

~~~yaml
implementation_context:
  task_summary: Migrate all four CPUs to PetEmulator.Core.
  primary_symbols: [IProcessor, IMemoryBus, M6800Cpu, M6809Cpu, Z80Cpu]
  patterns: [6502 Core integration, 6800-to-6809 inheritance, opt-in observers]
  constraints: [preserve Z80 cycle order, preserve 6809 interrupt order, keep IProcessor minimal]
  tests: [Core contract tests, 6800 binaries, 6809 interrupt/binary tests, Z80 z80doc/ZEXDOC/timing tests]
  verification_commands: [dotnet build PetEmulator.slnx --no-restore, targeted dotnet test, full dotnet test]
  assumptions: [capabilities are additive, CPU APIs remain during transition]
  open_questions: [IPortBus versus IIoBus, BusCycle location, MC6800 IRQ/NMI semantics, Z80 counter semantics]
  avoid: [one universal register state, mandatory Z80 API on IProcessor, deleting duplicates before regression]
~~~

## 12. Assumptions and Open Questions

Assumptions: capability interfaces can be added additively; CPU-specific APIs remain temporarily; 6502 is the integration baseline.

Open questions: IPortBus versus IIoBus; whether BusCycle belongs in Core; whether SetIRQ/SetNMI remain on IProcessor; required MC6800 IRQ/NMI semantics; Z80 InstructionCount semantics for WAIT, HALT and interrupt service.

Deferred: KayPro/CP/M integration, shared disassembler, full snapshot/restore.

## 13. Definition of Done

- One Core.IMemoryBus and no Z80 duplicate.
- All four CPUs satisfy common lifecycle and debug contracts.
- Optional I/O, WAIT, FIRQ and cycle capabilities are tested.
- Existing opcode, timing, interrupt and binary regressions pass.
- Full build/test passes.
- GitNexus/PDG is refreshed before implementation.
