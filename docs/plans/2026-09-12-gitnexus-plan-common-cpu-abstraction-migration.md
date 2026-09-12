# GitNexus Engineering Plan

> Task: Utworzyć wspólną abstrakcję CPU w `PetEmulator.Core` i migrować 6502, MC6800, MC6809 oraz Z80, zachowując specjalizacje rodzin.
> Evidence verified at commit `a2aad8f3c2c60a464f55ab102b9fa1ab76c214dd`; GitNexus index refreshed with `--pdg`, lecz część impact reports zgłasza one-commit staleness; staged cleanup starego zegara Z80 jest uwzględniony.
> Evidence provenance schema 2; global dirty digest `0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd`; cited manifest 36 entries; generated plan path excluded.

## 1. Objective

[verified] Wspólny Core ma obsługiwać lifecycle, zegar, pamięć, debugowanie, opcjonalne porty i przerwania. Opcode’y, rejestry, flagi, adresowanie i timing pozostają implementacjami rodzin.

## 2. Current Behaviour

[verified] `IProcessor` zawiera lifecycle, liczniki oraz IRQ/NMI; `IDebuggableProcessor` jest osobnym kontraktem rejestrów (`lib/PetEmulator.Core/IProcessor.cs:4-19`, `IDebuggableProcessor.cs:4-12`).

[verified] 6502 używa Core `IMemoryBus`/`IClock` i jest klasą częściową z wariantami pochodnymi. MC6800 implementuje Core i ma `M6800State` oraz własne tablice opcode’ów. MC6809 dziedziczy po MC6800 i dodaje rejestry, strony opcode’ów oraz FIRQ/NMI.

[verified] Z80 implementuje Core lifecycle, ale zachowuje CPU-specific `IBus`, `BusCycle`, WAIT, refresh, I/O i interrupt acknowledge. Pamięć Z80 jest już Core `IMemoryBus`.

[verified] Lokalny `CpuZ80/Timing/IEmulationClock` nie jest używany przez CPU; jego pliki i własne testy są staged do usunięcia.

## 3. Relevant Architecture

[inferred] Wspólna warstwa powinna być kompozycją: `CpuExecutionEngine` + `CpuExecutionContext` + strategie rodziny. Nie należy tworzyć uniwersalnego stanu rejestrów ani wspólnej tabeli opcode’ów.

[verified] Z80 `BusCycle` zawiera machine cycle i T-state, więc nie wolno zastąpić go prostym `BusAccess` (`lib/PetEmulator.CpuZ80/Bus/BusCycle.cs:3-29`).

## 4. GitNexus Findings

[graph] `impact IProcessor upstream depth 3` wykazał 93 zależności, 33 bezpośrednie, 10 procesów, 13 modułów; ryzyko CRITICAL.

[graph] `impact IMemoryBus upstream depth 2` wykazał 192 zależności, 80 bezpośrednich, 9 procesów, 19 modułów; ryzyko CRITICAL.

[graph] Bezpośredni konsumenci obejmują Core, Debugger, DebugApi, CLI, Desktop, maszyny, Chips i testy; zmiany kontraktów wymagają adapterów kompatybilności.

## 5. Statement-Level PDG Findings

[assumed] Nie wykonano osobnych bounded `pdg_query` dla funkcji kroku; przed edycją `Step`/`StepInstruction` executor musi wykonać `analyze --index-only --pdg` i slice PDG.

[verified] 6809 zachowuje kolejność NMI → FIRQ → IRQ → HALT → fetch/execute i cykle 19/10/19/2 (`lib/PetEmulator.Cpu6809/M6809Cpu.cs:95-145`).

[verified] Z80 zachowuje kolejność WAIT → NMI edge → INT → HALT refresh → fetch/execute (`lib/PetEmulator.CpuZ80/Cpu/Z80Cpu.cs:101-151`).

## 6. Proposed Changes

1. Dodać w Core `CpuStepResult`, `ICpuProcessor`, `CpuExecutionContext`, `IInstructionDecoder<T>`, `IInstructionExecutor<T>`, `ITimingModel<T>` i `ICycleObserver`.
2. Zachować `IProcessor.StepInstruction()` jako adapter kompatybilności do czasu zakończenia migracji.
3. Zdefiniować semantykę liczników: WAIT/HALT idle i sama obsługa przerwania nie zwiększają `InstructionCount`; wszystkie cykle zwiększają `IClock` dokładnie raz.
4. Użyć 6502 jako pierwszej implementacji referencyjnej, bez zmiany wariantowych opcode’ów i `Tick()`.
5. Przekształcić MC6800 w adapter engine + `M6800State` + własny decoder/executor/timing.
6. Zachować MC6809 → MC6800 jako rozszerzenie rodziny; dodać do engine własne page 10/11, stack frames i FIRQ.
7. Przekształcić Z80 w adapter engine, ale zachować `IBus`, `BusCycle`, WAIT, refresh, I/O i IM0/1/2 jako capability Z80.
8. Przejść przez Debugger, DebugApi, CLI, Desktop i maszyny; używać Core contracts i capability detection zamiast rzutowań na CPU.
9. Dopiero po regresji usunąć shims, lokalne zegary, duplikaty magistral i staged cleanup Timing Z80.

## 7. Implementation Sequence

1. Baseline: build solution, wszystkie testy CPU i pełne `dotnet test`.
2. Core contracts i fake engine: testy resetu, cykli, instrukcji, WAIT, HALT, IRQ, NMI, FIRQ i debug registers.
3. 6502: adapter engine, zachowanie wariantów, `Tick`, BCD, IRQ/NMI i cykle.
4. MC6800: adapter, tabela instrukcji, `M6800State`, cycle accounting; nie rozstrzygać sprzętowej semantyki IRQ/NMI bez testu.
5. MC6809: rozszerzenia 6800, page 10/11, Y/U/S/DP, FIRQ/NMI/IRQ i frame layout.
6. Z80: lifecycle/counters, następnie I/O/WAIT/interrupt acknowledge, na końcu bogaty obserwator `BusCycle`.
7. Integracje: Debugger, DebugApi, CLI, Desktop, PET/VIC-20 i testy kontraktowe.
8. Cleanup: usunąć tylko potwierdzone nieużywane abstrakcje, wykonać pełny build/test i `detect_changes --scope all`.

## 8. Test Strategy

- Core: `tests/PetEmulator.Core.Tests/MachineContractTests.cs` oraz nowe testy engine.
- 6502: `tests/PetEmulator.Cpu6502.Tests/InterruptTests.cs`, warianty, opcode’y, cykle i round-trip stanu.
- 6800: `M6800CpuTests`, `M6800BinaryTests`, state/flags i licznik cykli.
- 6809: `M6809/InterruptTests.cs`, `IntegrationTests.cs`, prefix/page tests i binary tests.
- Z80: `Z80CpuTests.cs`, `InterruptTests.cs`, `Z80TimingTests.cs`, `BusTests.cs`, `MemoryTests.cs`, z80doc i opt-in ZEXDOC.
- Commands: `dotnet build PetEmulator.slnx --no-restore`; testy projektów CPU; na końcu `dotnet test PetEmulator.slnx --no-restore`.

## 9. Risk and Impact Analysis

- [graph] `IProcessor` i `IMemoryBus` są hubami CRITICAL; zmieniać je addytywnie.
- [verified] Największe ryzyko to ordering/timing przerwań, WAIT, refresh i frame layout, nie sama kompilacja.
- [inferred] Nie używać per-cycle allocations na gorącej ścieżce Z80.
- [assumed] Publiczne konstruktory i `Step()` muszą pozostać kompatybilne podczas przejścia.

## 10. Files Expected to Change

| Obszar | Zakres |
| --- | --- |
| `lib/PetEmulator.Core` | kontrakty, context, result, engine, capability |
| `lib/PetEmulator.Cpu6502` | adapter i strategie 6502 |
| `lib/PetEmulator.Cpu6800` | adapter i strategie MC6800 |
| `lib/PetEmulator.Cpu6809` | strategie rozszerzające MC6800 |
| `lib/PetEmulator.CpuZ80` | adapter, I/O, WAIT, cycle observer |
| Debugger/DebugApi/CLI/Desktop/machines | konsumenci kontraktów Core |
| `tests/*Cpu*Tests`, `tests/PetEmulator.Core.Tests` | kontrakty i regresja |

## 11. Reusable Implementation Context

```yaml
task_summary: common CPU abstraction with family-specific implementations
primary_symbols: [IProcessor, IMemoryBus, Cpu6502, M6800Cpu, M6809Cpu, Z80Cpu]
patterns: [composition, optional capabilities, typed family state, compatibility adapters]
index_limit: graph is lower-bound where stale warning appears; source is authoritative
working_tree: staged deletion of CpuZ80 Timing/IEmulationClock, Timing/EmulationClock and TimingTests
evidence_provenance:
  schema_version: 2
  head_commit: a2aad8f3c2c60a464f55ab102b9fa1ab76c214dd
  global_dirty_digest: 0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd
  cited_path_manifest_count: 36
  generated_plan_path: docs/plans/2026-09-12-gitnexus-plan-common-cpu-abstraction-migration.md
```

## 12. Assumptions and Open Questions

- Czy `Step()` ma stać się publicznym kontraktem, czy `StepInstruction()` ma pozostać stałą nazwą Core?
- Czy bogaty `BusCycle` przenieść do Core, czy utrzymać jako Z80 capability z adapterem?
- Jaka jest wymagana sprzętowa semantyka IRQ/NMI bazowego MC6800?
- Czy publiczne dziedziczenie `M6809Cpu : M6800Cpu` musi zostać zachowane po migracji engine?
- Deferred: micro-op DSL i benchmarki alokacji obserwatora.

## 13. Definition of Done

- [ ] Core engine nie referuje żadnego CPU.
- [ ] Wszystkie cztery rodziny wykonują instrukcje przez wspólny lifecycle.
- [ ] Zachowane są testy wariantów 6502, binarne 6800, interrupt/integration 6809 oraz timing/I/O/WAIT Z80.
- [ ] Debugger, DebugApi, CLI, Desktop i maszyny używają Core contracts.
- [ ] Nie ma nieużywanych duplikatów zegara, pamięci ani lifecycle.
- [ ] Pełny build/test oraz końcowy GitNexus `detect_changes` są czyste.
