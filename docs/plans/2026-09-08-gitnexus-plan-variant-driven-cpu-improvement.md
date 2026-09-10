# GitNexus Engineering Plan

> Task: Usprawnienie wariantów CPU 6502 i tablic opcode
> Evidence verified at commit eafd8a3ff9c0d9c299ce4aed565ba2db71c4e073; GitNexus index refreshed this session with `node .gitnexus/run.cjs analyze --index-only`.
> Evidence provenance schema 2; global dirty digest sha256:0b82774590462b01090e22128fa29585f5c9606fea1a484e9507e2b02da43763; cited-path manifest 13 sorted entries; exact generated plan path excluded.

## 1. Objective

Zbudować warianty CPU jako kompletne, immutable definicje obejmujące tablicę opcode i cechy sprzętu, usunąć mutowalną konfigurację wariantu z publicznego CPU oraz dalej migrować wykonanie instrukcji z legacy dispatchu bez utraty zgodności cyklowej.

## 2. Current Behaviour

- [verified] `Cpu6502` przechowuje `_opcodeTable`, `Variant`, rejestry oraz publiczne `DecimalModeEnabled` i `HasJmpIndirectBug` w `Processor/Cpu6502.CycleStepped.Core.cs:14-27` i `Processor/Cpu6502.Properties.cs:24-35`.
- [verified] `Cpu6502Classic` i `Cpu6502Nes` przekazują wariant tworzony przez `OpcodeTables`, bez przypisań w ciele konstruktora (`Cpu6502Classic.cs:14-17`, `Cpu6502Nes.cs:14-17`).
- [verified] `StepInstructionCore` pobiera opcode, wykonuje handler z tablicy aż do `_sync`, a następnie zwiększa licznik instrukcji (`Processor/Cpu6502.CycleStepped.Core.cs:66-103`).
- [verified] Domyślna tablica zawiera 256 wpisów, metadane mnemonic/mode/length/cycles, ale większość handlerów deleguje do `ExecuteLegacyCycle` (`Processor/OpcodeTables.cs:13-43`).
- [verified] Wydzielone są już handler-y grup prostych oraz akumulator/stos (`Processor/Cpu6502.CycleStepped.SimpleOpcodes.cs`).

## 3. Relevant Architecture

- [verified] Moduł `Processor` zawiera 128 symboli i 100% cohesion; warianty są plikami root projektu, ale zachowują namespace `Cpu6502.Variants`.
- [verified] `OpcodeTable` jest tablicą 256 elementów z `Set`, `Remove` i `Derive`; obecnie publicznie mutowalna (`Processor/OpcodeTable.cs:4-28`).
- [verified] `Cpu6502Variant` zawiera nazwę, tabelę i dwa boolean-y (`Processor/Cpu6502Variant.cs:3-20`).
- [inferred] Najmniejsza bezpieczna architektura to immutable `Cpu6502Variant` + osobny builder/factory dla tablic; runtime customization powinno pozostać narzędziem testowym.
- [verified] CPU zależy tylko od `IMemoryBus`; nie zawiera zależności PET.

## 4. GitNexus Findings

- [graph: query] `query("CPU 6502 variants opcode table dispatch...")` znalazł przepływy `CreateNmosVariant -> OpcodeTable/OpcodeDefinition/Set/ParseMode` i testy `OpcodeTableTests`, `QuirkTests`, `BranchJumpTests`.
- [graph: impact] `CreateNmosVariant`, upstream depth 3: 10 symboli, risk LOW; bezpośredni konsumenci są w testach i konstruktorze wariantu.
- [graph: impact] `CreateNesVariant`, upstream depth 3: 4 symbole, risk LOW; bezpośredni konsument to `Cpu6502Nes`.
- [graph: impact] `StepInstructionCore`, upstream: 2 direct callers (`StepInstruction`, `Tick`), 2 affected processes, risk LOW.
- [graph: context] `Processor` jest głównym klastrem CPU; testy CPU mają 360 symboli i obejmują ROM, cykle, quirks i opcode table.
- [verified] `dotnet test PetEmulator.slnx` jest działającą komendą; obecny wynik to 280 passed, 1 skipped.

## 5. Statement-Level PDG Findings

PDG jest niedostępny. Trzy próby `pdg_query` dla `StepInstructionCore` (`controls`, `_currentOpcode`, `_sync`) zwróciły `no PDG layer`; nie rekonstruować zależności ręcznie jako danych PDG.

- [verified] Source-level execution order is: interrupt boundary -> opcode fetch -> handler loop -> instruction count -> post-instruction IRQ boundary (`Processor/Cpu6502.CycleStepped.Core.cs:66-103`).
- [inferred] Każdy nowy handler musi zachować moment ustawienia `_sync`, wszystkie odczyty/zapisy busa i post-instruction interrupt boundary.
- [verified] Legacy path kończy nierozpoznaną instrukcję przez `_sync = true` (`Processor/Cpu6502.CycleStepped.Core.cs:181-227`).

## 6. Proposed Changes

1. `lib/PetEmulator.Cpu6502/Processor/Cpu6502Variant.cs`, `OpcodeTables.cs`: zastąpić dwa boolean-y skalowalnym `CpuQuirk` lub immutable `CpuBehavior`; factory ma zwracać kompletne warianty `Nmos6502`, `Nes2A03`.
2. `lib/PetEmulator.Cpu6502/Processor/Cpu6502.Properties.cs`: usunąć publiczną mutowalność cech wariantu; jeśli kompatybilność API jest wymagana, pozostawić tylko read-only projection z `Variant` i oznaczyć starą ścieżkę do usunięcia po migracji.
3. `lib/PetEmulator.Cpu6502/Processor/OpcodeTable.cs`: wprowadzić builder/factory i zamkniętą tabelę po budowie; `Derive` kopiuje bazę, a instancje CPU nie współdzielą mutowalnego stanu.
4. `lib/PetEmulator.Cpu6502/Processor/OpcodeTables.cs`: współdzielić gotową immutable tablicę NMOS; wariant NES powinien używać `Derive` do usuwania/podmiany niedostępnych lub zmienionych opcode, zamiast tylko ustawiać flags.
5. `lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.*.cs`: migrować kolejne grupy handlerów tabeli, jedna rodzina naraz; nie usuwać legacy dispatchu przed zgodnością ROM/cycle tests.
6. `lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.Core.cs`: po pełnej migracji uprościć `StepInstructionCore` do jednego lookupu i handler result, ale zachować IRQ/NMI i page-cross timing.

## 7. Implementation Sequence

1. Dodać testy kontraktu wariantu: NMOS ma `DecimalArithmetic` i `JmpIndirectPageWrap`; NES nie ma obu; obie tablice mają 256 wpisów.
2. Wprowadzić `CpuQuirk`/`CpuBehavior` jako immutable dane i przepiąć arithmetic oraz indirect addressing na ten obiekt. Ryzyko: średnie, bo dotyka BCD i JMP; testy `BcdTests`, `QuirkTests`, `BranchJumpTests` muszą przejść.
3. Zbudować jedną statyczną immutable tablicę NMOS i immutable derived table dla NES. Nie współdzielić obiektu, jeśli builder pozwala na `Set/Remove` po publikacji.
4. Przenieść aktualne listy `SimpleOpcodes` i `AccumulatorStackOpcodes` do deklaracji tabeli, tak aby handler był częścią definicji opcode, nie osobnym warunkiem w factory.
5. Migrować instrukcje load/store, arithmetic/logic, branches/control-flow i RMW w osobnych zmianach. Po każdej rodzinie uruchomić testy jednostkowe i ROM.
6. Dodać test enumerujący handler provenance: opcode zmigrowane nie wskazują legacy handlera; wszystkie pozostałe są jawnie oznaczone jako legacy.
7. Dopiero po pełnej zgodności usunąć `ExecuteLegacyCycle`, stare `GetInstructionCycles` i tymczasowe listy migracyjne.
8. Na końcu zaktualizować dokumentację architektury, benchmark lookupu oraz indeks GitNexus.

## 8. Test Strategy

- `tests/PetEmulator.Cpu6502.Tests/OpcodeTableTests.cs`: 256 entries, metadata, derived immutability, official/undocumented/undefined partition, handler provenance.
- `tests/PetEmulator.Cpu6502.Tests/Cpu6502Tests.cs`: construction of NMOS/NES variants and immutable behavior configuration.
- `tests/PetEmulator.Cpu6502.Tests/BcdTests.cs`: BCD enabled only for NMOS.
- `tests/PetEmulator.Cpu6502.Tests/QuirkTests.cs` and `BranchJumpTests.cs`: NMOS page-wrap JMP versus NES normal indirect JMP.
- Existing instruction tests: register/flag effects and exact cycle counts after each migrated family.
- Existing ROM tests: Klaus, `nestest`, functional test; no legacy removal before these pass.
- Verification: `dotnet test PetEmulator.slnx`; additionally run focused CPU tests after each family.

## 9. Risk and Impact Analysis

- `StepInstructionCore` has 2 direct callers (`StepInstruction`, `Tick`) and is the main execution boundary; any handler timing error affects both APIs.
- BCD and JMP behavior are externally observable and covered by dedicated tests; move them only after configuration tests exist.
- Current graph impact reports are LOW for variant factories and StepInstructionCore; the working tree contains a large staged import, so GitNexus `detect-changes` reports the aggregate change as MEDIUM, not as a clean isolated patch.
- PDG evidence is unavailable; rely on source and executable tests until a `--pdg` index is built.
- Sharing immutable tables reduces allocations but must not reintroduce global mutation; a mutable singleton is prohibited.
- No concurrency change is planned; immutable variant/table state should make sharing safe.

## 10. Files Expected to Change

| File | Symbols | Reason |
| --- | --- | --- |
| `lib/PetEmulator.Cpu6502/Processor/Cpu6502Variant.cs` | `Cpu6502Variant` | Immutable complete variant behavior |
| `lib/PetEmulator.Cpu6502/Processor/OpcodeTables.cs` | factories, table definitions | Shared NMOS and derived NES tables |
| `lib/PetEmulator.Cpu6502/Processor/OpcodeTable.cs` | `OpcodeTable` | Builder/immutable publication |
| `lib/PetEmulator.Cpu6502/Processor/Cpu6502.Properties.cs` | behavior projections | Remove public variant mutation |
| `lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.Core.cs` | constructor/dispatch | Consume variant and final handler model |
| `lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.*.cs` | family handlers | Incremental legacy migration |
| `lib/PetEmulator.Cpu6502/Cpu6502Classic.cs` | constructor | Use named NMOS variant |
| `lib/PetEmulator.Cpu6502/Cpu6502Nes.cs` | constructor | Use named NES variant |
| `tests/PetEmulator.Cpu6502.Tests/OpcodeTableTests.cs` | table tests | Metadata and handler contracts |
| `tests/PetEmulator.Cpu6502.Tests/Cpu6502Tests.cs` | variant tests | Behavior configuration |
| `tests/PetEmulator.Cpu6502.Tests/BcdTests.cs` | BCD cases | Regression coverage |
| `tests/PetEmulator.Cpu6502.Tests/QuirkTests.cs` | quirk cases | JMP/BCD variant coverage |

## 11. Reusable Implementation Context

```yaml
implementation_context:
  task_summary: "Make CPU variants complete immutable definitions with variant-owned opcode tables and migrate execution away from legacy dispatch incrementally."
  acceptance_criteria:
    - "NMOS and NES are complete named variants with immutable behavior configuration."
    - "No shared mutable opcode singleton is used by CPU instances."
    - "Variant opcode tables expose correct 256-entry metadata and handler provenance."
    - "Existing cycle, quirk, ROM, and solution tests remain green after every migration step."
    - "Legacy dispatch is removed only after all opcode families are migrated and verified."
  evidence_provenance:
    schema_version: 2
    head_commit: "eafd8a3ff9c0d9c299ce4aed565ba2db71c4e073"
    generated_plan_path: "docs/plans/2026-09-08-gitnexus-plan-variant-driven-cpu-improvement.md"
    global_dirty_digest:
      algorithm: "sha256"
      canonicalization: "gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records"
      value: "0b82774590462b01090e22128fa29585f5c9606fea1a484e9507e2b02da43763"
    cited_path_manifest:
      - {path: "docs/architecture.md", state: staged, head_digest: absent, index_digest: "sha256:fa157244194d339788f30182477018aed9669a36426fe5e84e20593338df532a", worktree_digest: "sha256:fa157244194d339788f30182477018aed9669a36426fe5e84e20593338df532a"}
      - {path: "lib/PetEmulator.Cpu6502/Cpu6502Classic.cs", state: staged, head_digest: absent, index_digest: "sha256:9eba9d9057d88084df5393486dbec1924373226d16e4ad3b02fcdc3862b86c65", worktree_digest: "sha256:9eba9d9057d88084df5393486dbec1924373226d16e4ad3b02fcdc3862b86c65"}
      - {path: "lib/PetEmulator.Cpu6502/Cpu6502Nes.cs", state: staged, head_digest: absent, index_digest: "sha256:d87e1a06f917405992a43afd6923077c2d6d4ec2fdf893bd4ed2b537602c9900", worktree_digest: "sha256:d87e1a06f917405992a43afd6923077c2d6d4ec2fdf893bd4ed2b537602c9900"}
      - {path: "lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.Core.cs", state: staged, head_digest: absent, index_digest: "sha256:e2515222857f155c628fdbede199b90426b9d1cdcdec48fbfcfe3d345901511b", worktree_digest: "sha256:e2515222857f155c628fdbede199b90426b9d1cdcdec48fbfcfe3d345901511b"}
      - {path: "lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.SimpleOpcodes.cs", state: staged, head_digest: absent, index_digest: "sha256:7b1ee14c67e3aa94c5c9d2937ed0b64034a267923651403c5e2f620882c5ef42", worktree_digest: "sha256:7b1ee14c67e3aa94c5c9d2937ed0b64034a267923651403c5e2f620882c5ef42"}
      - {path: "lib/PetEmulator.Cpu6502/Processor/Cpu6502.Properties.cs", state: staged, head_digest: absent, index_digest: "sha256:4877111e5737a603b0bd472bb356191d4cbf9a16ebbe5342345172fe24d9003c", worktree_digest: "sha256:4877111e5737a603b0bd472bb356191d4cbf9a16ebbe5342345172fe24d9003c"}
      - {path: "lib/PetEmulator.Cpu6502/Processor/Cpu6502Variant.cs", state: staged, head_digest: absent, index_digest: "sha256:9f24f5bc8273c5f0a0151404628cc31c3213daffa6de0c8b8dfc5cbbbc212e97", worktree_digest: "sha256:9f24f5bc8273c5f0a0151404628cc31c3213daffa6de0c8b8dfc5cbbbc212e97"}
      - {path: "lib/PetEmulator.Cpu6502/Processor/OpcodeTable.cs", state: staged, head_digest: absent, index_digest: "sha256:b24c64d1a701a27f17678e9eb6d4af8b7ed8bced161386e634b04eea11c0eb0b", worktree_digest: "sha256:b24c64d1a701a27f17678e9eb6d4af8b7ed8bced161386e634b04eea11c0eb0b"}
      - {path: "lib/PetEmulator.Cpu6502/Processor/OpcodeTables.cs", state: staged, head_digest: absent, index_digest: "sha256:02f63040ca13d33dd8cdb46bffcbbfa55a48a0641785104859af9a76e950121a", worktree_digest: "sha256:02f63040ca13d33dd8cdb46bffcbbfa55a48a0641785104859af9a76e950121a"}
      - {path: "tests/PetEmulator.Cpu6502.Tests/BranchJumpTests.cs", state: staged, head_digest: absent, index_digest: "sha256:c76d6dc20d9f3b48d6def34993d8909cf5986a1a9b748fc61f00354d3fd22f0a", worktree_digest: "sha256:c76d6dc20d9f3b48d6def34993d8909cf5986a1a9b748fc61f00354d3fd22f0a"}
      - {path: "tests/PetEmulator.Cpu6502.Tests/Cpu6502Tests.cs", state: staged, head_digest: absent, index_digest: "sha256:45474a32bbf225e1163dcc3ea5b4a196791550508b3d91421e83ee4d68426ec6", worktree_digest: "sha256:45474a32bbf225e1163dcc3ea5b4a196791550508b3d91421e83ee4d68426ec6"}
      - {path: "tests/PetEmulator.Cpu6502.Tests/OpcodeTableTests.cs", state: staged, head_digest: absent, index_digest: "sha256:4869d3f23ce4fdde37e1750b7c26e44edae75ed15f037feacac27cd581466231", worktree_digest: "sha256:4869d3f23ce4fdde37e1750b7c26e44edae75ed15f037feacac27cd581466231"}
      - {path: "tests/PetEmulator.Cpu6502.Tests/QuirkTests.cs", state: staged, head_digest: absent, index_digest: "sha256:ed6ef1b492c96a125230bf7152add1cfab18e7aaf0a6e7e4d007528716a9963d", worktree_digest: "sha256:ed6ef1b492c96a125230bf7152add1cfab18e7aaf0a6e7e4d007528716a9963d"}
  primary_symbols:
    - {symbol: "Cpu6502Variant", file: "lib/PetEmulator.Cpu6502/Processor/Cpu6502Variant.cs", lines: "3-20", role: "variant behavior/value object"}
    - {symbol: "OpcodeTables.CreateNmosVariant", file: "lib/PetEmulator.Cpu6502/Processor/OpcodeTables.cs", lines: "7-8", role: "NMOS variant factory"}
    - {symbol: "OpcodeTables.CreateNesVariant", file: "lib/PetEmulator.Cpu6502/Processor/OpcodeTables.cs", lines: "10-11", role: "NES variant factory"}
    - {symbol: "Cpu6502.StepInstructionCore", file: "lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.Core.cs", lines: "66-103", role: "execution boundary"}
    - {symbol: "OpcodeTable", file: "lib/PetEmulator.Cpu6502/Processor/OpcodeTable.cs", lines: "4-28", role: "opcode table lifecycle"}
  related_symbols:
    - {symbol: "Cpu6502.DecimalModeEnabled", relationship: "read by arithmetic", relevance: "replace with variant behavior"}
    - {symbol: "Cpu6502.HasJmpIndirectBug", relationship: "read by indirect addressing", relevance: "replace with variant behavior"}
    - {symbol: "Cpu6502.ExecuteLegacyCycle", relationship: "handler fallback", relevance: "remove last"}
    - {symbol: "Cpu6502.ExecuteSimpleCycle", relationship: "opcode handler", relevance: "existing migration pattern"}
    - {symbol: "Cpu6502.ExecuteAccumulatorStackCycle", relationship: "opcode handler", relevance: "existing migration pattern"}
    - {symbol: "Cpu6502Nes", relationship: "uses CreateNesVariant", relevance: "direct consumer"}
    - {symbol: "Cpu6502Classic", relationship: "uses CreateNmosVariant", relevance: "direct consumer"}
  execution_path:
    - "Construct variant and select its opcode table."
    - "Construct CPU and copy/derive immutable behavior configuration."
    - "At instruction boundary, fetch opcode and reset cycle state."
    - "Lookup opcode handler and run cycle callbacks until handler sets sync."
    - "Update instruction count and service post-instruction IRQ boundary."
  pdg_constraints:
    - description: "No PDG layer is indexed; source ordering is authoritative until --pdg reindex."
      affected_statements: []
      implementation_consequence: "Preserve source-observed _sync, bus, and interrupt ordering and prove behavior with tests."
  architectural_patterns:
    - {pattern: "256-entry opcode table", example_location: "lib/PetEmulator.Cpu6502/Processor/OpcodeTable.cs", usage_guidance: "Use array lookup; publish immutable tables."}
    - {pattern: "Derived variant configuration", example_location: "lib/PetEmulator.Cpu6502/Processor/OpcodeTables.cs", usage_guidance: "Build NES from NMOS only where opcode/behavior differs."}
    - {pattern: "Incremental handler migration", example_location: "lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.SimpleOpcodes.cs", usage_guidance: "Migrate one family and keep legacy fallback until tests pass."}
  files_to_modify:
    - {file: "lib/PetEmulator.Cpu6502/Processor/Cpu6502Variant.cs", symbols: [Cpu6502Variant], intended_change: "immutable behavior/quirk definition"}
    - {file: "lib/PetEmulator.Cpu6502/Processor/OpcodeTables.cs", symbols: [CreateNmosVariant, CreateNesVariant, CreateNmos], intended_change: "shared base and derived variant tables"}
    - {file: "lib/PetEmulator.Cpu6502/Processor/OpcodeTable.cs", symbols: [OpcodeTable], intended_change: "builder and immutable publication"}
    - {file: "lib/PetEmulator.Cpu6502/Processor/Cpu6502.Properties.cs", symbols: [DecimalModeEnabled, HasJmpIndirectBug], intended_change: "remove public mutation"}
    - {file: "lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.Core.cs", symbols: [Cpu6502, StepInstructionCore, ExecuteLegacyCycle], intended_change: "consume variant and eventually remove fallback"}
    - {file: "lib/PetEmulator.Cpu6502/Processor/Cpu6502.CycleStepped.*.cs", symbols: [], intended_change: "incremental family handler migration"}
  tests:
    - {file: "tests/PetEmulator.Cpu6502.Tests/OpcodeTableTests.cs", scenarios: ["build NMOS -> 256 definitions", "derive NES -> base unchanged", "migrated opcode -> non-legacy handler"]}
    - {file: "tests/PetEmulator.Cpu6502.Tests/Cpu6502Tests.cs", scenarios: ["construct NMOS -> decimal and JMP quirks enabled", "construct NES -> both quirks disabled"]}
    - {file: "tests/PetEmulator.Cpu6502.Tests/BcdTests.cs", scenarios: ["D flag + ADC/SBC on NMOS -> BCD result", "D flag + ADC/SBC on NES -> binary result"]}
    - {file: "tests/PetEmulator.Cpu6502.Tests/QuirkTests.cs", scenarios: ["NMOS JMP ($xxFF) -> same-page high byte", "NES JMP ($xxFF) -> next-page high byte"]}
    - {file: "tests/PetEmulator.Cpu6502.Tests/BranchJumpTests.cs", scenarios: ["JMP indirect normal and page-boundary timing/target"]}
  verification_commands:
    - "dotnet test PetEmulator.slnx"
    - "node .gitnexus/run.cjs analyze --index-only"
    - "node .gitnexus/run.cjs detect-changes --scope all --repo ."
  risks:
    - "Do not change _sync or bus ordering while migrating handlers."
    - "Do not publish a mutable global opcode table."
    - "Do not remove legacy fallback before ROM and cycle tests pass."
    - "Current working tree is a large staged import; isolate future commits by family."
  assumptions:
    - "Public setters for DecimalModeEnabled and HasJmpIndirectBug are not required by external consumers; verify before removing them."
    - "NES undocumented opcode policy will be explicitly decided before the NES table diverges from NMOS."
  open_questions:
    - "Should compatibility read-only projections remain under the old property names, or should they be removed in the next breaking API change?"
    - "Which undocumented opcodes are supported by the target NMOS compatibility level?"
  avoid:
    - "Do not repeat full repository discovery."
    - "Do not replace cycle-accurate legacy behavior with instruction-level handlers without timing tests."
    - "Do not add PET/device dependencies to the CPU variant model."
```

## 12. Assumptions and Open Questions

- Assumption: removing public setters is acceptable; verify usages outside the indexed repository before implementation.
- Assumption: current NES behavior requires NMOS undocumented opcode support unless a compatibility target says otherwise.
- Open question: whether the project wants a source-compatible deprecation bridge for old public properties.
- Deferred: `Official6502`, `Cmos65C02`, `6510/8502`, debugger trace adapters, and machine/PET integration. Add only when a concrete consumer exists.
- PDG refresh was skipped because the current index has no PDG layer; use `node .gitnexus/run.cjs analyze --index-only --pdg` before relying on statement-level graph evidence.

## 13. Definition of Done

- `Cpu6502Variant` is immutable and owns complete behavior configuration plus opcode table.
- NMOS and NES tables are independently testable, non-global-mutable, and have correct metadata.
- CPU no longer exposes mutable variant switches, or the compatibility decision is documented and tested.
- At least one complete opcode family is migrated per implementation phase with no legacy handler for that family.
- `dotnet test PetEmulator.slnx` passes with no new skips or failures.
- Klaus, nestest, functional, BCD, JMP quirk, RMW, IRQ/NMI and cycle tests remain green.
- Legacy dispatch is removed only after all 256 opcode handlers are migrated and handler provenance tests prove no fallback remains.
