# Migracja Z80 do wspólnego Core — plan wykonawczy i checklista

> Stan planu: analiza zakończona; implementacja jeszcze się nie rozpoczęła.
> Weryfikacja źródeł: commit `98a1544e3463789d0c70d941b36fc557383359c3`.
> GitNexus: indeks po próbie odświeżenia pozostał niepewny/niekompletny; analiza zgłosiła przekroczenia limitów przepływów. Zależności poniżej są oparte przede wszystkim na bieżącym kodzie i testach.

## 1. Cel i kryteria zakończenia

- [ ] `Z80Cpu` dziedziczy po `CpuProcessorBase<Z80State>` i nadal zachowuje kompatybilne publiczne API (`Step()`, `StepInstruction()`, `Registers`, `Hooks`, konstruktor oraz `IProcessor`).
- [ ] Stan architektoniczny Z80 jest w `Z80State : CpuState`; snapshot/restore obejmuje rejestry, HALT, IFF1/IFF2, IM, opóźnienie EI, krawędź NMI i licznik R.
- [ ] Wszystkie opcode’y są rejestrowane w `OpcodeTable<Z80State>` przez `OpcodeKey(Page, Opcode)`: strona 0, CB, ED, DD i FD; warianty DD/FD+CB mają jawny model klucza albo rodzinny decoder, bez lokalnych słowników runtime.
- [ ] `CpuProcessorBase` wykonuje wspólny lifecycle i liczniki dokładnie raz; Z80 zachowuje kolejność `WAIT → NMI edge → INT → HALT refresh → fetch/execute`.
- [ ] `IBus`, `BusCycle`, WAIT, refresh, interrupt acknowledge, pełne 16-bitowe porty i obserwator cykli pozostają capability Z80; nie są spłaszczone do samego `BusAccess`.
- [ ] Debugger/monitoring Core działa przez `ExecutionObserver`, a istniejące `Hooks` pozostają kompatybilnym adapterem diagnostycznym.
- [ ] Pełna regresja CPU Z80, testy binarne/dokumentacyjne, testy Core, build rozwiązania i końcowa analiza zmian przechodzą.

## 2. Fakty wyjściowe

- [verified] `Z80Cpu` jest obecnie samodzielną implementacją `IProcessor` i `IDebuggableProcessor`, posiada własny zegar, dwa słowniki dispatchu, ręczne liczniki i ręczny lifecycle (`lib/PetEmulator.CpuZ80/Cpu/Z80Cpu.cs:10-18`, `:29-38`, `:52-85`).
- [verified] `Step()` uruchamia hooki, obsługuje WAIT, detekcję zbocza NMI, INT, HALT refresh oraz pobranie opcode’u; ten porządek jest kontraktem kompatybilności (`Z80Cpu.cs:104-153`).
- [verified] Rejestracja używa `RegisterOpcode`/`RegisterEdOpcode` i słowników `Dictionary<byte, Func<int>>` (`Z80Cpu.cs:156-380`, `:382-390`).
- [verified] Core ma już typed state, opcode table, execution context, step result, trace i observer (`lib/PetEmulator.Core/Cpu/*.cs`, `lib/PetEmulator.Core/Abstractions/ICpuExecutionObserver.cs`).
- [verified] Z80 ma bogatszy model magistrali: opcode fetch, pamięć, I/O, acknowledge, refresh oraz machine cycle/T-state (`lib/PetEmulator.CpuZ80/Bus/BusCycle.cs:3-28`, `Z80Cpu.cs:1301-1430`).
- [verified] `IBus` zachowuje osobny odczyt opcode’u, 8/16-bit I/O i acknowledge, a `IIoBus` adaptuje `IPortBus` (`Bus/IBus.cs:3-17`, `Bus/IIoBus.cs:3-12`).
- [verified] Zestaw testów obejmuje opcode matrix, flag matrix, prefiksy, I/O, WAIT, przerwania, timing, obserwator magistrali, `z80doc.tap` i `zexdoc.com` (`tests/PetEmulator.CpuZ80.Tests/`).

## 3. Zasady architektoniczne

- [ ] Nie przenosić `BusCycle` do wspólnego Core tylko po to, aby ujednolicić nazwy. Dodać opcjonalny adapter/obserwator Z80 nad Core lifecycle.
- [ ] Nie tworzyć wspólnego stanu rejestrów dla Z80, 6502 i 68xx. Wspólne pozostają lifecycle, clock, memory, optional ports, snapshot i observer.
- [ ] Nie wykonywać wszystkich zmian w jednym dużym diffie. Każdy etap kończy się kompilacją i testami właściwego zakresu.
- [ ] Zachować publiczne wejście `Step()` jako alias do wspólnego `StepInstruction()` oraz zachować możliwość rozszerzenia opcode’ów w klasie potomnej.
- [ ] Dla opcode’ów prefiksowych preferować jeden standard `OpcodeKey`; DD/FD+CB, gdzie klucz nie mieści całej semantyki, obsłużyć jako jawny rodzinny decoder z testem kolizji i priorytetów.

## 4. Sekwencja migracji

### Etap 0 — Baseline i zabezpieczenie kontraktów

- [ ] Uruchomić `dotnet build PetEmulator.slnx --no-restore`.
- [ ] Uruchomić `dotnet test tests/PetEmulator.CpuZ80.Tests/PetEmulator.CpuZ80.Tests.csproj --no-restore --disable-build-servers` i zapisać liczbę testów.
- [ ] Uruchomić testy `PetEmulator.Core.Tests` oraz pełny `dotnet test PetEmulator.slnx --no-restore` jako baseline.
- [ ] Dodać/uzupełnić test kontraktu, że `Z80Cpu` przez `IProcessor` ma te same `CycleCount`, `InstructionCount`, `Reset` i `StepInstruction` co API konkretne.
- [ ] Dodać test kompatybilności klasy potomnej rejestrującej/zmieniającej opcode, zanim zostaną usunięte lokalne słowniki.

### Etap 1 — Model stanu Z80

- [ ] Utworzyć `Z80State : CpuState` z rejestrami A/F, B/C, D/E, H/L, I/R, PC/SP, IX/IY i parami alternatywnymi.
- [ ] Przenieść do stanu `Iff1`, `Iff2`, `InterruptMode`, `InterruptDelay`, `PreviousNmi` i stan HALT.
- [ ] Zdecydować i udokumentować, czy pola śledzenia magistrali (`traceMachineCycle`, T-state) są stanem debugowym chwilowym czy stanem snapshotu; do snapshotu włączyć tylko wartości potrzebne do deterministycznego wznowienia.
- [ ] Zachować `Z80Registers` jako publiczny widok kompatybilności delegujący do `Z80State` albo zastąpić go bez łamania testów; nie dublować dwóch niezależnych kopii rejestrów.
- [ ] Zaimplementować `GetRegisters`, `CaptureSnapshot`, `RestoreSnapshot` i testy round-trip dla rejestrów głównych, alternatywnych, IX/IY, I/R, IFF/IM i HALT.

### Etap 2 — Adapter magistrali i kontekstu wykonania

- [ ] W konstruktorze Z80 mapować `IBus.ReadMemory/WriteMemory` do Core `IMemoryBus`, a `IBus`/`IIoBus` do opcjonalnego `IPortBus`.
- [ ] Zachować `ReadOpcode` jako osobną ścieżkę Z80; Core `Memory` nie może przypadkiem zamienić fetchu opcode’u na zwykły odczyt danych.
- [ ] Wprowadzić rodzinny `IZ80BusCapabilities` lub równoważny prywatny adapter dla `WAIT`, `AcknowledgeInterrupt` i `IBusCycleObserver`; nie rozszerzać obowiązkowego API Core dla innych rodzin.
- [ ] Przetestować pełne adresy portów 16-bitowych, domyślne mapowanie `IIoBus` i brak portu (`ports == null`).
- [ ] Potwierdzić, że watchpoint Core nie dubluje istniejącego `MemoryWatch`; ustalić jedną ścieżkę zgłoszenia, a drugą pozostawić jako kompatybilny adapter.

### Etap 3 — Wspólny lifecycle bez zmiany semantyki opcode’ów

- [ ] Zmienić deklarację `Z80Cpu` na `CpuProcessorBase<Z80State>` i podłączyć wspólny `Clock`, `Memory`, `ExecutionContext`, `InstructionCount` oraz `Halted`.
- [ ] Przenieść `Reset`, `SetIRQ`, `SetNMI`, `StepInstruction`, `CycleCount` i `InstructionCount` do kontraktu bazowego; lokalne implementacje zostawić tylko jako delegacje, jeśli wymagają kompatybilności binarnej.
- [ ] Zaimplementować `BeforeStep` dla hooków Z80 oraz `TryHandleWait` tak, aby WAIT kończył krok jako `CpuStepResult.Idle(1, waiting: true)` bez fetchu, interruptu i inkrementacji instrukcji.
- [ ] Zaimplementować `TryServiceInterrupt`: najpierw edge NMI, potem maskowalne INT z warunkiem IFF1 i opóźnieniem EI; wynik ma być `CpuStepResult.Interrupt(cycles)`.
- [ ] Zaimplementować HALT jako idle z refresh, bez inkrementacji `InstructionCount`, z zachowaniem 4 T-state.
- [ ] Zaimplementować rodzinny `CompleteStep`/timing hook, aby zegar był zwiększany dokładnie raz przez Core, a nie przez opcode i lifecycle jednocześnie.
- [ ] Dodać testy kolejności WAIT/NMI/INT/HALT oraz test zgodności liczników z dotychczasowym `Step()`.

### Etap 4 — Wspólny standard rejestracji opcode’ów

- [ ] Wydzielić rejestrację do partiali: `Z80Cpu.OpcodeRegistration.Base.cs`, `Cb.cs`, `Ed.cs`, `Indexed.cs` lub równoważnego podziału bez zmiany semantyki.
- [ ] Zastąpić `Dictionary<byte, Func<int>>` przez `OpcodeTable<Z80State>` i `OpcodeDefinition<Z80State>` z mnemonic, długością, bazowym timingiem i callbackiem wykonania.
- [ ] Zarejestrować stronę 0 przez `OpcodeKey.Base(opcode)`.
- [ ] Zarejestrować CB i ED przez osobne strony; dla DD/FD użyć stron 0xDD/0xFD, a dla DD/FD+CB wprowadzić jawne dekodowanie displacement/opcode bez przypadkowej kolizji z samą stroną.
- [ ] Zachować dynamiczne rodziny opcode’ów (`LD r,r`, ALU, INC/DEC, ED I/O, pary 16-bitowe) jako generatory wpisów Core, z testem kompletności i braku duplikatów.
- [ ] Wprowadzić `Mnemonic`, `Length`, `AddressingMode` i timing dla wpisów co najmniej na poziomie wymaganym przez debugger; nie udawać dokładności tam, gdzie timing zależy od warunku/prefiksu.
- [ ] Zachować rozszerzanie w klasie potomnej przez `ConfigureOpcodes`/`Replace`/`Derive`; dodać test, że opcode potomny nie mutuje zapieczętowanej tabeli bazowej.
- [ ] Dopiero po przejściu matrix tests usunąć `opcodes`, `edOpcodes`, `RegisterOpcode` i `RegisterEdOpcode`.

### Etap 5 — Decoder prefiksów i wykonanie

- [ ] Zaimplementować `FetchOpcode` jako jawny decoder zwracający `OpcodeKey` i zachowujący fetch/refresh dla każdego bajtu prefiksu.
- [ ] Zaimplementować `DecodeOpcode` dla stron 0, CB, ED, DD i FD z właściwym fallbackiem dla nieobsługiwanych ED oraz prefiksów indeksowych.
- [ ] Zachować wszystkie quirk’i Z80: IXH/IXL, `(IX+d)/(IY+d)`, DD/FD + CB + displacement, nieużywane ED jako 8 T-state, R bit 7 oraz flagi undocumented.
- [ ] Zaimplementować `ExecuteOpcode` tak, aby callbacki nadal używały jednego stanu i jednego kontekstu, a wynik zwracał cycles/instruction completion.
- [ ] Przepisać testy `Z80BaseOpcodeTests`, `Z80CbOpcodeTests`, `Z80EdOpcodeTests`, `Z80IndexedOpcodeTests` i wszystkie validation matrices bez osłabiania oczekiwań.
- [ ] Dodać test pełnej liczby zarejestrowanych opcode’ów na każdej stronie oraz test, że nieobsługiwane opcode’y zachowują dotychczasową politykę wyjątku/fallbacku.

### Etap 6 — Przerwania, WAIT, HALT i I/O

- [ ] Przenieść obsługę IM0/IM1/IM2, acknowledge i wektorowania do capability Z80, bez dodawania IM do `CpuProcessorBase`.
- [ ] Zachować `EI` delay, `DI`, `RETN/RETI`, IFF1/IFF2 i edge-triggered NMI; dodać testy po każdym kroku lifecycle.
- [ ] Zachować `IN`, `OUT`, blokowe `INI/IND/INIR/INDR/OUTI/OUTD/OTIR/OTDR` oraz 16-bitowe adresy portów.
- [ ] Zachować obserwację `InterruptAcknowledge`, `OpcodeFetch`, `Refresh`, memory read/write i I/O read/write z identycznym `MachineCycle`, `TState` i `TStates`.
- [ ] Zweryfikować, że interrupt i WAIT mają poprawne `CpuStepResult`, snapshot przed/po oraz brak fałszywego `InstructionCount`.

### Etap 7 — Debugger, monitoring i kompatybilność API

- [ ] Podłączyć `ExecutionObserver` do breakpointów, watchpointów, trace przed/po i obsługi wyjątków.
- [ ] Zachować `CpuHookCollection` jako warstwę kompatybilności albo przenieść jego scenariusze do observera; usunąć ją dopiero po migracji wszystkich callerów.
- [ ] Zachować `GetRegisters()` z nazwami `AF'`, `BC'`, `DE'`, `HL'`, `IFF1`, `IFF2`, `IM` używanymi przez debugger.
- [ ] Sprawdzić `Z80Disassembler` niezależnie od migracji CPU; ma używać metadanych tylko wtedy, gdy kontrakt nie zmienia jego publicznego API.
- [ ] Przejrzeć wszystkie rzutowania do `Z80Cpu`, `IBus` i `IDebuggableProcessor`; zamienić je na capability tylko tam, gdzie zmiana jest konieczna.

### Etap 8 — Testy regresyjne i binarne

- [ ] Uruchomić wszystkie istniejące testy Z80 po każdym etapie 3–7.
- [ ] Zachować pełne testy matrix: base, CB, ED, indexed, flag, block flag, wide flag i compatibility.
- [ ] Zachować testy magistrali, pamięci, `MemoryWatch`, hooków, WAIT i interruptów.
- [ ] Uruchomić `z80doc.tap` jako obowiązkową regresję dokumentacyjną.
- [ ] Uruchomić `zexdoc.com` jako obowiązkową regresję binarną; jeśli test pozostaje opt-in z powodu czasu, dodać osobny jawny command/filter i zapisać wynik.
- [ ] Dodać test round-trip snapshot w środku prefiksu/interrupt delay oraz test wznowienia po HALT/WAIT.
- [ ] Dodać test porównujący ślad `BusCycle` przed i po migracji dla reprezentatywnych instrukcji bazowych, CB, ED, DD/FD, I/O i interruptów.

### Etap 9 — Usunięcie duplikatów i integracja

- [ ] Po pełnej regresji usunąć nieużywane lokalne zegary i lokalne lifecycle; nie usuwać `IBus`, `BusCycle` ani capability potrzebnych do dokładnego Z80.
- [ ] Usunąć tylko potwierdzone martwe adaptery po `rg`, buildzie i testach.
- [ ] Zaktualizować istniejącą checklistę Z80 o faktyczne wyniki etapów, zamiast utrzymywać dwa sprzeczne statusy.
- [ ] Uruchomić `dotnet build PetEmulator.slnx --no-restore` oraz `dotnet test PetEmulator.slnx --no-restore --disable-build-servers`.
- [ ] Uruchomić GitNexus `detect_changes` dla staged/all; jeśli indeks nadal jest niekompletny, dołączyć wynik jako ograniczenie i potwierdzić zakres przez `git diff`, `rg` i testy.
- [ ] Wykonać commit dopiero po czystym diffie, `git diff --check`, pełnym buildzie i pełnej regresji.

## 5. Mapa plików

| Obszar | Główne pliki | Zakres |
| --- | --- | --- |
| Core | `lib/PetEmulator.Core/Cpu/CpuProcessorBase.cs`, `CpuState.cs`, `Opcode*.cs`, `CpuStep*.cs` | użyć istniejących kontraktów; zmiany tylko addytywne, jeśli Z80 ujawni brak capability |
| Stan Z80 | `lib/PetEmulator.CpuZ80/Cpu/Z80Registers.cs`, nowe `Z80State.cs` | jedna rzeczywista kopia stanu, snapshot/restore |
| CPU | `lib/PetEmulator.CpuZ80/Cpu/Z80Cpu.cs` i nowe partiale | lifecycle, decoder, opcode registration, wykonanie |
| Magistrala | `Bus/IBus.cs`, `Bus/IIoBus.cs`, `Bus/BusCycle.cs`, `Bus/SystemBus.cs` | zachowanie CPU-specific capabilities |
| Przerwania | `Interrupts/IInterruptLines.cs` oraz testy | WAIT, NMI, INT, acknowledge, IM0/1/2 |
| Testy | `tests/PetEmulator.CpuZ80.Tests/*.cs` | kontrakty, matrix, binaria, timing i snapshot |
| Dokumentacja | ten plan oraz istniejąca checklista Z80 | aktualny status i wyniki bez duplikacji |

## 6. Ryzyka i decyzje

- [risk] Największe ryzyko to podwójne naliczanie cykli po przejściu na `CpuProcessorBase`; każda ścieżka ma mieć jedno miejsce `Clock.Advance`.
- [risk] `BusCycle` jest bogatszy niż `BusAccess`; spłaszczenie zepsuje timing/debug trace.
- [risk] DD/FD+CB nie jest zwykłą stroną opcode’ów; decoder musi zachować displacement i rejestr indeksowy.
- [risk] Core impact jest CRITICAL i lower-bound z powodu dispatchu interfejsowego; po każdej zmianie trzeba sprawdzić tekstowych callerów i testy, nie tylko graf.
- [decision] `IBus` zostaje publicznym kontraktem kompatybilności; Core dostaje tylko minimalne adaptery memory/ports.
- [decision] `Z80State` jest typowanym stanem rodziny; nie rozszerzamy `CpuState` o rejestry Z80 ani o IM/WAIT.
- [deferred] Ewentualny mikro-opcode engine i optymalizacja alokacji observera pozostają poza tą migracją.

## 7. Definition of Done

- [ ] Brak runtime’owych słowników `opcodes`/`edOpcodes` w Z80.
- [ ] Brak drugiego zegara/licznika instrukcji poza Core lifecycle.
- [ ] Wszystkie strony opcode’ów i warianty prefiksów są w jednym standardzie Core albo w jawnie uzasadnionym decoderze rodzinnym.
- [ ] Snapshot/debugger/observer działają dla instrukcji, WAIT, HALT i interruptów.
- [ ] `BusCycle` i pełna semantyka Z80 pozostają przetestowane.
- [ ] Z80 test suite, z80doc, zexdoc, pełny build i pełne testy rozwiązania przechodzą.

## 8. Kontekst implementacyjny

```yaml
task: full Z80 migration to shared CpuProcessorBase/Core opcode standard
verified_at_commit: 98a1544e3463789d0c70d941b36fc557383359c3
graph_status: stale_or_incomplete_after_refresh_attempt
impact: Z80Cpu upstream CRITICAL; 60 direct, 287 total lower-bound
primary_files:
  - lib/PetEmulator.CpuZ80/Cpu/Z80Cpu.cs
  - lib/PetEmulator.CpuZ80/Cpu/Z80Registers.cs
  - lib/PetEmulator.CpuZ80/Bus/IBus.cs
  - lib/PetEmulator.CpuZ80/Bus/BusCycle.cs
  - lib/PetEmulator.Core/Cpu/CpuProcessorBase.cs
  - lib/PetEmulator.Core/Cpu/OpcodeTable.cs
  - tests/PetEmulator.CpuZ80.Tests/Z80TimingTests.cs
  - tests/PetEmulator.CpuZ80.Tests/Z80ExternalValidationTests.cs
evidence_provenance:
  schema_version: 2
  global_dirty_digest: 0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd
  cited_manifest_count: 29
  generated_plan_path: docs/plans/2026-09-12-gitnexus-plan-z80-core-migration.md
```
