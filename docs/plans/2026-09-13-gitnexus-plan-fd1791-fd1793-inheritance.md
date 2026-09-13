# GitNexus Engineering Plan

> Task: refactor the floppy-controller model into `FD1791` base plus `FD1793` extension
> Evidence verified at commit `a731cf1324eeec716b18c3af45f1252f17b8706a`; GitNexus index refreshed this session (`--index-only`) with bounded/truncated C# flow output.
> Evidence provenance schema 2; the canonical dirty digest and cited-path manifest are embedded in §11.

## 1. Objective

Create a maintainable inheritance hierarchy in which `FD1791` owns the common WD179x command, register, transfer, status and timing behavior, while `FD1793 : FD1791` exposes only the verified 1793-specific extension. Preserve the existing logical API and regression behavior during migration, then remove duplicated or chip-inappropriate logic from the base and move board wiring into adapters.

The target is not a textual copy. The base must represent the real FD1791 contract; the derived class must represent the true-data-bus FD1793 variant. The current `FD1793` class is `sealed` and contains all behavior in one file, so the first structural change is required before inheritance is possible.

## 2. Current Behaviour

- [verified] `FD1793` is a `sealed` concrete class with registers, status flags, four-drive storage, command execution, transfer state, timing, WAIT and disk-image dispatch in one file (`lib/PetEmulator.Chips/FD1793.cs:3-467`).
- [verified] Its public API exposes logical register access through `Read(byte)`, `Write(byte)`, `ReadStatus`, `ReadData`, `WriteCommand`, `WriteData`, `Tick`, `Reset` and `InsertDisk`.
- [verified] Current disk contracts are named `IFD1793DiskImage` and `IFD1793SidedDiskImage`; both are consumed by the chip and Kaypro raw image (`lib/PetEmulator.Chips/IFD1793DiskImage.cs:3-33`, `src/PetEmulator.Kaypro/KayproDiskImage.cs:5-63`).
- [verified] `KayproBus` constructs `FD1793` directly and exposes it publicly; Kaypro tests and 12 chip test scenarios directly reference its constants and members.
- [verified] Current tests cover Type I–IV commands, sector transfers, density, deleted-data mark, multi-record, write protection, timeout, READ ADDRESS, drive selection, WAIT and reset (`tests/PetEmulator.Chips.Tests/FD1793Tests.cs:10-300`).
- [source-derived, external documentation] The FD179X data sheet and Kaypro hardware notes identify FD1791 and FD1793 as command-compatible variants where the important hardware distinction is inverted versus true data-access lines; this must be modeled at a bus boundary, not by duplicating command logic.

## 3. Relevant Architecture

- [verified] `PetEmulator.Chips` is the generic chip layer; it must not depend on Kaypro or a concrete port map.
- [verified] `IFD1793DiskImage` currently mixes the generic sector medium contract with the name of one chip variant. A base `IFD1791DiskImage`/`IFD179xDiskImage` contract is needed before the base class can be reusable.
- [verified] `KayproBus` owns board-specific drive selection, system-port semantics and interrupt-line wiring; those concerns must remain outside `FD1791`/`FD1793`.
- [inferred] The safest compatibility boundary is: logical register API remains unchanged; raw electrical bus polarity is explicit through an adapter or narrowly scoped bus-access methods. Existing callers should not unexpectedly receive inverted bytes.
- [inferred] `FD1793` should initially add only the verified true-DAL behavior and any separately proven side-select capability. It must not inherit unrelated Kaypro WAIT or drive-latch semantics.

## 4. GitNexus Findings

- [graph] `impact FD1793 --direction upstream`: CRITICAL, exact; 81 impacted symbols, with 66 direct relationships across `PetEmulator.Chips.Tests`, `PetEmulator.Chips` and `PetEmulator.Kaypro`.
- [graph] Direct consumers include `KayproBus`, `KayproDiskImage`, Kaypro SIO compatibility code, chip tests and several imported project files. The graph sees interface/import boundaries but does not prove every runtime dispatch path.
- [graph] `FD1793` is also mentioned by the shared interrupt contract documentation and by chip test/debug infrastructure; do not rename or remove the old public name without a compatibility pass.
- [source] Textual search confirms direct construction only in `KayproBus` and direct test use in `FD1793Tests`, while the disk interface is implemented by Kaypro media.
- [risk] Because GitNexus reports CRITICAL impact, implementation must proceed behind a complete baseline and staged compatibility migration. Any high-risk public API removal requires explicit user review before deletion.

## 5. Statement-Level PDG Findings

No load-bearing PDG slice was available. The analyzer reported callable-value caps and truncated process walks; graph absence must not be treated as proof of no callers.

Source-derived state dependencies:

- [inferred] `_status`, `_dataRequestPending`, `_transfer`, `_transferIndex`, `_pendingCommand` and `IntrqAsserted` form one transfer state machine; moving only selected methods would break invariants between DRQ, deadlines and completion.
- [inferred] `ReadData`/`WriteData` must remain ordered with `RequestData`, `ClearDataRequest` and `Complete`; these methods should migrate as one unit into the base.
- [inferred] `SelectedDisk`, side dispatch and density checks are medium-policy hooks. They should be virtual/protected seams, not duplicated in `FD1793`.
- [inferred] `DriveSelect` bit 6 currently asserts WAIT as a board-latch side effect. It should be extracted to the Kaypro adapter or an explicit controller-line adapter before claiming pure FD1791 behavior.
- [inferred] The current logical `Read`/`Write` methods do not model electrical data-bus inversion. Adding polarity to those methods would silently break all existing tests; introduce a separate explicit bus-facing path or adapter.

## 6. Proposed Changes

### 6.1 Public contracts

- Add `IFD1791DiskImage` as the common single-sided sector contract.
- Add `IFD1791SidedDiskImage` only if side addressing is a common media capability rather than an FD1793-only feature; otherwise keep side support in the extension contract.
- Retain `IFD1793DiskImage` and `IFD1793SidedDiskImage` as obsolete compatibility interfaces extending the new common contract for one migration cycle, or use type aliases if the project’s API policy permits.
- Update XML documentation and all generic references from “required by FD1793” to “required by FD179x base”.

### 6.2 `FD1791` base

Create `lib/PetEmulator.Chips/FD1791.cs` by moving, not retyping, the verified common behavior:

- register offsets and common status flags;
- constructor timing validation and common timing fields;
- logical track/sector/data registers;
- RESTORE, SEEK, STEP, STEP IN and STEP OUT;
- READ SECTOR, WRITE SECTOR, READ ADDRESS and FORCE INTERRUPT;
- DRQ, INTRQ, BUSY, LOST DATA, RECORD NOT FOUND, WRITE PROTECT and TRACK 00;
- single-side/default media dispatch and sector-size/address-mark logic;
- reset preserving mounted media;
- test-visible diagnostics already exposed by the current class.

Make the class non-sealed. Keep mutable implementation state private unless a derived hook genuinely needs it; prefer protected virtual methods/properties such as `GetSelectedDisk`, `TryReadSector`, `TryWriteSector`, `GetDataBusMode` and `OnDriveSelectChanged` over exposing fields.

### 6.3 `FD1793` extension

Change `FD1793` to `public sealed class FD1793 : FD1791` and reduce it to the verified delta:

- true data-access-line behavior through an explicit raw-bus adapter/hook;
- any side-select or double-sided behavior proven by the selected FD1793 documentation and current tests;
- FD1793-specific defaults/constants only where they differ from FD1791.

Do not keep duplicate command, transfer, timing or status implementations in `FD1793`. If no logical behavior differs after the bus boundary is modeled, the derived class may be intentionally small and document that the silicon difference is electrical, not a second command engine.

### 6.4 Electrical bus boundary

Introduce one explicit abstraction, chosen during implementation after reviewing the datasheet:

```text
logical register API: Read/Write -> non-inverted values
electrical bus API: ReadBus/WriteBus or IFd179xDataBus -> FD1791 inverted / FD1793 true
machine adapter: Kaypro board wires the selected polarity
```

The default `FD1791` profile must expose inverted DAL behavior only through this electrical path. Existing chip tests and current Kaypro logical register callers remain stable until Kaypro is deliberately migrated to the hardware adapter.

### 6.5 Remove excess from the chip model

After the base/derived tests are green, remove or relocate:

- Kaypro-specific WAIT assertion from `FD1791`/`FD1793` into a board/system-latch adapter;
- implicit four-drive selection if it is not part of the selected chip contract;
- side selection from the base if it is proven FD1793/board-specific;
- duplicate density/side/media logic left in the derived class after hook extraction;
- obsolete `IFD1793`-only names from generic code, while retaining compatibility shims where public callers require them.

Deletion is allowed only after `rg`, GitNexus impact and full regression confirm no callers outside the planned compatibility layer.

## 7. Implementation Sequence

1. **Freeze baseline** — run current FD1793 tests, Kaypro tests and build; record public API and behavior. Add characterization tests for every branch before moving code.
2. **Verify hardware delta** — record FD1791/FD1793 data-bus polarity, density, side-select and signal differences from the Western Digital data sheet and Kaypro board documentation. Resolve contradictions before choosing hooks.
3. **Introduce common disk contracts** — add base interface names and compatibility inheritance; update test doubles and Kaypro image without changing runtime behavior.
4. **Extract the base mechanically** — move the complete state machine into non-sealed `FD1791`; initially preserve existing semantics exactly, including any behavior later marked for relocation.
5. **Derive FD1793** — make `FD1793 : FD1791`; keep only verified overrides and compatibility constants. Run the complete old FD1793 suite after each extraction boundary.
6. **Add electrical bus tests** — test inverted FD1791 and true FD1793 bus values independently from logical register tests; add Kaypro adapter tests for the selected board.
7. **Move board glue** — extract WAIT/drive-latch/side wiring from chip classes into a Kaypro FDC/board adapter; update `KayproBus` without changing port addresses.
8. **Delete excess** — remove dead private helpers, duplicate state and obsolete generic FD1793 naming after impact/text search and all tests pass.
9. **Documentation and compatibility** — update `docs/chips/FD1793.md`, add `docs/chips/FD1791.md` or consolidate into a WD179x document, document inheritance and limits, and mark compatibility interfaces/deprecations.
10. **Final verification** — targeted tests, chip coverage, Kaypro regression, full solution build/tests, `git diff --check`, GitNexus detect-changes and staged-scope review before commit.

## 8. Test Strategy

- Baseline characterization: every existing `FD1793Tests` case must pass unchanged or be renamed only after behavior is proven identical.
- `FD1791Tests`: registers, reset, Type I commands, Type II/III transfers, Type IV interrupt, DRQ deadlines, status flags, density and write protection.
- Inheritance tests: `FD1793` is assignable to `FD1791`; inherited commands behave identically; only documented extension hooks differ.
- Data-bus tests: logical `Read`/`Write` stay stable; explicit raw bus access verifies FD1791 inverted and FD1793 true values for all 256 byte values.
- Media contract tests: old `IFD1793DiskImage` implementations still work through the compatibility layer; new `IFD1791DiskImage` works with both classes.
- Adapter tests: Kaypro system-port drive select, WAIT, side/density lines and interrupt signals are handled outside the generic chip.
- Negative tests: invalid register, no disk, missing sector, wrong density, write-protected disk, DRQ timeout, FORCE INTERRUPT, reset during transfer and malformed adapter configuration.
- Regression commands:

```text
dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --no-restore --disable-build-servers -m:1 --verbosity minimal
dotnet test tests/PetEmulator.Kaypro.Tests/PetEmulator.Kaypro.Tests.csproj --no-restore --disable-build-servers -m:1 --verbosity minimal
dotnet build PetEmulator.slnx --no-restore --disable-build-servers -m:1
dotnet test PetEmulator.slnx --no-restore --disable-build-servers -m:1 --verbosity minimal
```

## 9. Risk and Impact Analysis

- **Critical API impact:** 81 GitNexus dependents; keep old `FD1793` name and public members during migration.
- **State-machine risk:** splitting transfer methods can lose DRQ/INTRQ/deadline invariants; extract cohesive regions and test after each move.
- **Hardware-model risk:** FD1791/FD1793 distinction is primarily data-bus electrical polarity; modeling it in logical register methods would regress existing consumers.
- **Semantic risk:** current WAIT and drive selection are partly board-latch behavior, not necessarily chip behavior; move them only with Kaypro adapter tests.
- **Compatibility risk:** renaming `IFD1793DiskImage` can break Kaypro and hidden consumers; use derived compatibility interfaces and deprecation first.
- **Coverage risk:** existing 100% coverage claims for FD1793 do not automatically cover new FD1791 hooks; each virtual branch needs a base and derived test.
- **Performance risk:** no expected regression if state remains array-based and no per-byte allocation is introduced.

## 10. Files Expected to Change

| File | Symbols | Reason |
| --- | --- | --- |
| `lib/PetEmulator.Chips/FD1791.cs` | new `FD1791` | common controller implementation |
| `lib/PetEmulator.Chips/FD1793.cs` | `FD1793` | derive from base and retain only verified extension |
| `lib/PetEmulator.Chips/IFD1793DiskImage.cs` | existing interfaces | compatibility layer or forwarding definitions |
| `lib/PetEmulator.Chips/IFD1791DiskImage.cs` | new contracts | common media boundary |
| `lib/PetEmulator.Chips/FD179xDataBus.cs` or adapter file | new bus strategy | explicit inverted/true DAL behavior |
| `tests/PetEmulator.Chips.Tests/FD1791Tests.cs` | new base tests | common behavior and base branches |
| `tests/PetEmulator.Chips.Tests/FD1793Tests.cs` | existing tests | derived behavior and compatibility |
| `tests/PetEmulator.Chips.Tests/FD179xInheritanceTests.cs` | new tests | inheritance and bus polarity |
| `src/PetEmulator.Kaypro/KayproBus.cs` | FDC construction/wiring | selected board adapter |
| `src/PetEmulator.Kaypro/KayproDiskImage.cs` | media interface | new common contract |
| `tests/PetEmulator.Kaypro.Tests/` | FDC/map regressions | board wiring and boot safety |
| `docs/chips/FD1793.md` and new/consolidated FD1791 docs | checklist | contracts, variants, limitations and migration status |

## 11. Reusable Implementation Context

```yaml
implementation_context:
  task_summary: Refactor FD1793 into FD1791 base plus FD1793 derived extension without breaking current logical APIs.
  acceptance_criteria:
    - FD1791 is a non-sealed reusable base containing the common WD179x state machine.
    - FD1793 inherits from FD1791 and contains no duplicate command/transfer engine.
    - Logical register behavior stays backward compatible.
    - FD1791 inverted and FD1793 true data-bus behavior is explicit and tested.
    - Kaypro WAIT/drive/side wiring is outside generic chip code.
    - Chip, Kaypro and full solution regressions pass.
  evidence_provenance:
    schema_version: 2
    head_commit: a731cf1324eeec716b18c3af45f1252f17b8706a
    generated_plan_path: docs/plans/2026-09-13-gitnexus-plan-fd1791-fd1793-inheritance.md
    global_dirty_digest:
      algorithm: sha256
      canonicalization: gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records
      value: 03d9345677d97c83c66b15c1bbfb9578f4c8783964f6e89049593aeca9f30ed4
    cited_path_manifest:
      - path: docs/README.md
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:ce4773c1e66ee22bfeabfc751d0fea7e1829a930f28c10b1f7a639107ab18eae
        index_digest: sha256:ce4773c1e66ee22bfeabfc751d0fea7e1829a930f28c10b1f7a639107ab18eae
        worktree_digest: sha256:ce4773c1e66ee22bfeabfc751d0fea7e1829a930f28c10b1f7a639107ab18eae
        untracked_digest: absent
      - path: docs/chips/FD1793.md
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:6a56ffa77ee11d87547c227d52dc4dd36e392d249543dadfa691d9c6472578f0
        index_digest: sha256:6a56ffa77ee11d87547c227d52dc4dd36e392d249543dadfa691d9c6472578f0
        worktree_digest: sha256:6a56ffa77ee11d87547c227d52dc4dd36e392d249543dadfa691d9c6472578f0
        untracked_digest: absent
      - path: docs/chips/README.md
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: sha256:5e655886b543787022bcb5e53eb507c73a3f56ef8980736cac4ae18a7c58ba1c
        index_digest: sha256:a23e8bdeb04c918e122752dd691054c9f0786e43caea4db84534de0f272a7a2f
        worktree_digest: sha256:a23e8bdeb04c918e122752dd691054c9f0786e43caea4db84534de0f272a7a2f
        untracked_digest: absent
      - path: lib/PetEmulator.Chips/FD1793.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:b6e1f3343fff4fd759b16d3026047f2b3259ba1f3f6aec38f8a91f5019ad48cc
        index_digest: sha256:b6e1f3343fff4fd759b16d3026047f2b3259ba1f3f6aec38f8a91f5019ad48cc
        worktree_digest: sha256:b6e1f3343fff4fd759b16d3026047f2b3259ba1f3f6aec38f8a91f5019ad48cc
        untracked_digest: absent
      - path: lib/PetEmulator.Chips/IFD1793DiskImage.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:155736c80e6a512cbe35c5efd07ae7330abc522d86422a644278daa9872e742b
        index_digest: sha256:155736c80e6a512cbe35c5efd07ae7330abc522d86422a644278daa9872e742b
        worktree_digest: sha256:155736c80e6a512cbe35c5efd07ae7330abc522d86422a644278daa9872e742b
        untracked_digest: absent
      - path: lib/PetEmulator.Chips/PetEmulator.Chips.csproj
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:9fa31f26ffa0a3c0953d3b4771e592784ed7f65b6159e7e2eb4f2a5b9365a0d3
        index_digest: sha256:9fa31f26ffa0a3c0953d3b4771e592784ed7f65b6159e7e2eb4f2a5b9365a0d3
        worktree_digest: sha256:9fa31f26ffa0a3c0953d3b4771e592784ed7f65b6159e7e2eb4f2a5b9365a0d3
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/KayproBus.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: staged
        rename_from: null
        rename_to: null
        head_digest: sha256:c76772bd2f000c1d766feedc2716f8f2cfe5d2aaf6adbbf70ab84b97eb3097ac
        index_digest: sha256:199eaf17644369793de2534ba045e7b1bf05d232c4e079058d6edeccfc8c0b9f
        worktree_digest: sha256:199eaf17644369793de2534ba045e7b1bf05d232c4e079058d6edeccfc8c0b9f
        untracked_digest: absent
      - path: src/PetEmulator.Kaypro/KayproDiskImage.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:ed4aeadb72a663ea89e7d310e38faa2a639e4f4673b6bcb07405d0378cd52914
        index_digest: sha256:ed4aeadb72a663ea89e7d310e38faa2a639e4f4673b6bcb07405d0378cd52914
        worktree_digest: sha256:ed4aeadb72a663ea89e7d310e38faa2a639e4f4673b6bcb07405d0378cd52914
        untracked_digest: absent
      - path: tests/PetEmulator.Chips.Tests/FD1793Tests.cs
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:68466dddb35a7656106dca10566322f4cf3510cbcb4a26cbbd5f7840f4587237
        index_digest: sha256:68466dddb35a7656106dca10566322f4cf3510cbcb4a26cbbd5f7840f4587237
        worktree_digest: sha256:68466dddb35a7656106dca10566322f4cf3510cbcb4a26cbbd5f7840f4587237
        untracked_digest: absent
      - path: tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:da882049150f148b6960940f353383b599aea3415fa35a5f78c9b1dee7e5ded1
        index_digest: sha256:da882049150f148b6960940f353383b599aea3415fa35a5f78c9b1dee7e5ded1
        worktree_digest: sha256:da882049150f148b6960940f353383b599aea3415fa35a5f78c9b1dee7e5ded1
        untracked_digest: absent
      - path: tests/PetEmulator.Kaypro.Tests/PetEmulator.Kaypro.Tests.csproj
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: sha256:08c77d990330f6b87324762890e8f8529c84955a57a0f96edc8e7dde6dc11aae
        index_digest: sha256:08c77d990330f6b87324762890e8f8529c84955a57a0f96edc8e7dde6dc11aae
        worktree_digest: sha256:08c77d990330f6b87324762890e8f8529c84955a57a0f96edc8e7dde6dc11aae
        untracked_digest: absent
  primary_symbols:
    - symbol: FD1793
      file: lib/PetEmulator.Chips/FD1793.cs
      lines: 3-467
      role: current monolithic controller and critical migration boundary
    - symbol: IFD1793DiskImage
      file: lib/PetEmulator.Chips/IFD1793DiskImage.cs
      lines: 3-33
      role: current media contract
    - symbol: FD1793Tests
      file: tests/PetEmulator.Chips.Tests/FD1793Tests.cs
      lines: 10-300
      role: behavior characterization and regression
    - symbol: KayproBus
      file: src/PetEmulator.Kaypro/KayproBus.cs
      lines: 9-143
      role: board-level FDC construction and signals
  related_symbols:
    - symbol: KayproDiskImage
      relationship: implements IFD1793DiskImage
      relevance: media compatibility
    - symbol: IInterruptLines
      relationship: documents FDC interrupt boundary
      relevance: interrupt integration
    - symbol: FD1793Tests.TestDisk
      relationship: test double for media contract
      relevance: migrate with interface compatibility
  execution_path:
    - CPU/KayproBus calls logical FDC register methods.
    - FD1791 state machine processes command and media transfer.
    - FD1793 overrides only verified variant hooks.
    - Board adapter handles electrical bus polarity, drive selection and WAIT.
  pdg_constraints:
    - description: No usable PDG slice; analyzer flow was truncated.
      affected_statements: []
      implementation_consequence: Preserve cohesive transfer-state extraction and recheck callers textually.
  architectural_patterns:
    - pattern: generic chip plus machine adapter
      example_location: src/PetEmulator.Kaypro/KayproBus.cs
      usage_guidance: keep board wiring out of lib/PetEmulator.Chips
    - pattern: logical register API with external bus mapping
      example_location: lib/PetEmulator.Chips/FD1793.cs
      usage_guidance: do not silently alter existing Read/Write semantics
  files_to_modify:
    - file: lib/PetEmulator.Chips/FD1791.cs
      symbols: [FD1791]
      intended_change: extract common controller state machine
    - file: lib/PetEmulator.Chips/FD1793.cs
      symbols: [FD1793]
      intended_change: derive and retain only verified extension
    - file: lib/PetEmulator.Chips/IFD1791DiskImage.cs
      symbols: [new common media contracts]
      intended_change: remove variant name from generic boundary
    - file: tests/PetEmulator.Chips.Tests/
      symbols: [FD1791Tests, FD179xInheritanceTests]
      intended_change: common, derived and electrical behavior coverage
  tests:
    - file: tests/PetEmulator.Chips.Tests/FD1793Tests.cs
      scenarios: [existing command/status/transfer matrix -> unchanged results]
    - file: tests/PetEmulator.Chips.Tests/FD1791Tests.cs
      scenarios: [common commands and errors -> correct base behavior]
    - file: tests/PetEmulator.Chips.Tests/FD179xInheritanceTests.cs
      scenarios: [FD1793 assignable to FD1791, logical equality, bus polarity delta]
    - file: tests/PetEmulator.Kaypro.Tests/
      scenarios: [board adapter drive/WAIT/DAL -> correct guest-visible signals]
  verification_commands:
    - dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --no-restore --disable-build-servers -m:1 --verbosity minimal
    - dotnet test tests/PetEmulator.Kaypro.Tests/PetEmulator.Kaypro.Tests.csproj --no-restore --disable-build-servers -m:1 --verbosity minimal
    - dotnet build PetEmulator.slnx --no-restore --disable-build-servers -m:1
    - dotnet test PetEmulator.slnx --no-restore --disable-build-servers -m:1 --verbosity minimal
    - node .gitnexus/run.cjs detect-changes --scope all --repo .
  risks: [critical public API impact, state-machine extraction, DAL polarity, Kaypro WAIT/drive glue, coverage regressions]
  assumptions:
    - what: FD1791/FD1793 command behavior is sufficiently common for inheritance
      how: verify against the primary WD179X data sheet before extraction
    - what: existing logical Read/Write values are already normalized
      how: preserve characterization tests and add explicit electrical bus tests
  open_questions:
    - Which side-select and density features belong to FD1791 in the selected datasheet revision?
    - Should compatibility interfaces remain for one release or be renamed immediately?
    - Is raw electrical bus access needed by any machine besides Kaypro?
  avoid:
    - Do not make FD1791 a copy with renamed symbols only.
    - Do not duplicate the state machine in FD1793.
    - Do not invert existing logical Read/Write behavior globally.
    - Do not move Kaypro-specific WAIT/drive semantics into generic chip code.
    - Do not delete old interfaces or constants before impact and full regression.
```

## 12. Assumptions and Open Questions

### Confirmed

- Current implementation has no FD1791 class; only `FD1793` exists.
- `FD1793` is the critical public boundary with 81 indexed upstream dependents.
- Current tests already provide a strong behavior baseline that can be moved to `FD1791Tests`.
- The authoritative implementation change must preserve logical register semantics while exposing electrical bus polarity explicitly.

### To verify before coding

- Exact FD1791/FD1793 feature split from the primary data sheet revision used by the project.
- Whether side select is a chip feature, a board connection or both for the selected Kaypro board.
- Whether WAIT behavior belongs entirely to Kaypro system-latch wiring.
- API policy for retaining obsolete `IFD1793*` names.

## 13. Definition of Done

- [x] `FD1791` is a non-sealed base class with the complete common state machine.
- [x] `FD1793 : FD1791` contains only verified extension behavior and no duplicated transfer logic.
- [x] Common disk contracts exist, old names remain compatible or are deliberately migrated with no unresolved callers.
- [x] FD1791 inverted and FD1793 true data-bus behavior is explicit and covered for all byte values.
- [x] Kaypro board glue handles WAIT, drive selection, side and interrupt wiring outside generic chip code.
- [x] Existing FD1793 regression plus new FD1791/inheritance tests pass.
- [ ] Chip coverage, Kaypro tests, full build and full solution regression pass.
- [x] Documentation identifies what is modeled, what is board-specific and what remains outside scope.
