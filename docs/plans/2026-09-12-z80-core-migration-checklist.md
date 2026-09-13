# Migracja Z80 do wspólnego Core — checklista zadań

Status ogólny: częściowo zakończona. Z80 korzysta już z `CpuProcessorBase` i `Core.OpcodeTable`, ale właściwy typ stanu oraz część kontraktów debugowania nadal wymagają ujednolicenia.

## 1. Stan i baza procesora

- [x] `Z80Cpu` korzysta z `CpuProcessorBase`.
- [x] `Z80State` dziedziczy po `CpuState`.
- [x] Zmienić bazę procesora z `CpuProcessorBase<Z80Registers>` na `CpuProcessorBase<Z80State>`.
- [x] Zachować `Z80Registers` jako kompatybilny publiczny widok stanu, bez drugiej kopii rejestrów.
- [x] Przenieść wszystkie pola architektoniczne do `Z80State`: rejestry główne, alternatywne, `IX/IY`, `I/R`, `IFF1/IFF2`, `IM`, opóźnienie `EI`, stan NMI i HALT.
- [x] Przenieść snapshot/restore do wspólnego mechanizmu `CpuState`.
- [x] Dodać test round-trip snapshotu dla pełnego stanu Z80.
- [x] Dodać test snapshotu w trakcie `WAIT`, `HALT`, prefiksu i opóźnienia `EI`.

## 2. Wspólny lifecycle Core

- [x] Wspólny zegar i liczniki instrukcji są dostępne przez Core.
- [x] `StepInstruction`, `CycleCount` i `InstructionCount` działają przez bazę Core.
- [x] WAIT, NMI, INT i HALT są obsługiwane przez hooki lifecycle Z80.
- [x] Potwierdzić testami kolejność: `WAIT → NMI edge → INT → HALT refresh → fetch/execute`.
- [x] Usunąć redundantne lokalne resetowanie stanu lifecycle; pozostały hooki rodzinne i delegacje kompatybilności API.
- [x] Potwierdzić, że każdy krok zwiększa zegar i licznik dokładnie raz.

## 3. Opcode’y i rejestracja

- [x] Opcode’y bazowe, CB, ED, DD i FD są przechowywane w `Core.OpcodeTable`.
- [x] Prefiksy DD/FD oraz DD/FD+CB mają jawny decoder rodzinny.
- [x] Zachowano rozszerzenia Z80: IXH/IXL, IYH/IYL, displacement, I/O i nieużywane ED.
- [x] Główny punkt rejestracji opcode’ów przeniesiono do partiala `Z80Cpu.OpcodeRegistration`.
- [x] Bazowe opcode’y przeniesiono do `Z80Cpu.OpcodeRegistration.Base`.
- [x] Opcode’y ED przeniesiono do `Z80Cpu.OpcodeRegistration.Ed`.
- [x] Opcode’y CB przeniesiono do `Z80Cpu.OpcodeRegistration.Cb`.
- [x] Opcode’y indeksowane DD/FD przeniesiono do `Z80Cpu.OpcodeRegistration.Indexed`.
- [x] Rejestrację I/O (`IN/OUT` bazowe i ED) przeniesiono do `Z80Cpu.OpcodeRegistration.Io`.
- [x] Rozdzielić rejestrację opcode’ów na partiale: Base, CB, ED, Indexed i I/O.
- [x] Usunąć prywatne wrappery `RegisterEdOpcode` i `RegisterPageOpcode`; chroniony `RegisterOpcode` zachowano dla rozszerzeń potomnych.
- [ ] Ujednolicić metadane `OpcodeDefinition`: mnemonic, długość, tryb adresowania i timing.
- [ ] Dodać test kompletności stron opcode’ów i braku kolizji kluczy.
- [ ] Dodać test rozszerzania tabeli w klasie potomnej przez `ConfigureOpcodes`/`Replace`/`Derive`.
- [ ] Potwierdzić, że tabela bazowa pozostaje niemutowalna po utworzeniu wariantu potomnego.

## 4. Magistrala, I/O i timing

- [x] Pamięć Z80 korzysta z adaptera Core.
- [x] Porty, WAIT, refresh, acknowledge przerwań i obserwator cykli zachowują specyfikę Z80.
- [ ] Wprowadzić jawny capability adapter Z80 dla `WAIT`, interrupt acknowledge i bogatego `BusCycle`.
- [ ] Dodać test pełnych 16-bitowych adresów portów.
- [ ] Dodać test braku portu i domyślnego mapowania I/O.
- [ ] Dodać test porównujący ślad `BusCycle` przed i po migracji dla Base, CB, ED, DD/FD, I/O i interruptów.
- [ ] Potwierdzić, że Core watchpoint i istniejący `MemoryWatch` nie generują podwójnych zdarzeń.

## 5. Debugger i monitoring

- [x] `GetRegisters()` udostępnia rejestry Z80 oraz `IFF1`, `IFF2` i `IM`.
- [x] Istnieje kompatybilny `CpuHookCollection`.
- [x] Z80 korzysta ze wspólnego `ExecutionObserver` przez `CpuProcessorBase`; dodano test pełnego śladu instrukcji.
- [x] Zachować hooki jako adapter kompatybilności dla istniejących callerów; potwierdzone działaniem razem z `ExecutionObserver`.
- [x] Dodać breakpoint przed instrukcją i obserwację wyjątków przez wspólny `ExecutionObserver`.
- [x] Dodać watchpoint pamięci przez wspólny kontrakt `ExecutionObserver`; I/O pozostaje osobnym rozszerzeniem.
- [x] Zweryfikować debug snapshot dla instrukcji, WAIT, HALT i interruptów.

## 6. Testy regresyjne

- [x] Testy bazowych opcode’ów, CB, ED, indexed i flag matrix.
- [x] Testy magistrali, pamięci, WAIT, interruptów i timingów.
- [x] Po zmianie typu bazowego wykonano 517 testów jednostkowych Z80 bez testów binarnych; 517/517 przeszło, czas 9 min 11 s.
- [x] Po przeniesieniu rejestracji opcode’ów do partiala wykonano 517 testów jednostkowych Z80 bez testów binarnych; 517/517 przeszło, czas 9 min 50 s.
- [x] Po wydzieleniu bazowych opcode’ów wykonano test rozszerzania rejestracji: 1/1, czas 43 ms.
- [x] Po wydzieleniu opcode’ów ED wykonano 25 testów ED i rozszerzania rejestracji; 25/25 przeszło, czas 1 s.
- [x] Po wydzieleniu opcode’ów CB wykonano 14 testów CB i rozszerzania rejestracji; 14/14 przeszło, czas 20 s.
- [x] Po wydzieleniu opcode’ów DD/FD wykonano 48 testów indexed i rozszerzania rejestracji; 48/48 przeszło, czas 43 s.
- [x] Po wydzieleniu rejestracji I/O wykonano 19 testów I/O/timingu i rozszerzania rejestracji; 19/19 przeszło, czas 935 ms.
- [x] Po usunięciu prywatnych wrapperów wykonano testy Base/CB/ED/Indexed/I/O i rozszerzania rejestracji; 86/86 przeszło, czas 59 s.
- [x] Test kontraktu wspólnego `ExecutionObserver` dla Z80: 1/1 zaliczony.
- [x] Test wspólnego watchpointu pamięci Z80: 1/1 zaliczony.
- [x] Test breakpointu przed instrukcją Z80 przez wspólny observer: 1/1 zaliczony.
- [x] Test obserwacji wyjątku wykonania opcode’u Z80: 1/1 zaliczony.
- [x] Test współdziałania `CpuHookCollection` z observerem Core: 1/1 zaliczony.
- [x] Testy debug snapshotu Z80 dla HALT, WAIT i obsłużonego przerwania: 3/3 zaliczone.
- [x] Testy debug snapshotu Z80 dla prefiksu indeksowanego i opóźnienia EI: 2/2 zaliczone.
- [x] Test kolejności lifecycle Z80 `WAIT → NMI edge → INT → HALT refresh → fetch/execute`: 1/1 zaliczony.
- [x] Test delegacji resetu Z80 do wspólnego Core: 1/1 zaliczony.
- [x] Test dokładnego zwiększania zegara i licznika instrukcji Z80: 1/1 zaliczony.
- [x] `z80doc.tap` i `zexdoc.com` są obecne w repozytorium.
- [x] Test ZEXDOC ma inicjalizację wektora stosu CP/M pod `0006h`.
- [x] Test ZEXDOC raportuje postęp co 100 milionów instrukcji.
- [x] Test ZEXDOC oznacza checkpointy co 1 miliard instrukcji.
- [ ] Wykonać pełny przebieg ZEXDOC do zakończenia; ostatni przebieg przekroczył limit 30 minut.
- [ ] Zapisać końcowy wynik ZEXDOC i liczbę instrukcji/cykli.
- [ ] Uruchomić pełny `z80doc.tap` po zakończeniu migracji Core.
- [ ] Dodać test kontraktu `IProcessor` dla `Reset`, `StepInstruction`, `CycleCount` i `InstructionCount`.
- [ ] Dodać test kompatybilności wariantu potomnego z własnym opcode’em.

## 7. Porządki końcowe

- [ ] Usunąć potwierdzone martwe adaptery i lokalny kod lifecycle.
- [ ] Zaktualizować dokumentację architektury Z80 i status migracji.
- [ ] Zaktualizować tę checklistę wynikami testów zamiast pozostawiać niezweryfikowane pozycje.
- [ ] Wykonać `dotnet build PetEmulator.slnx --no-restore`.
- [ ] Wykonać `dotnet test PetEmulator.slnx --no-restore --disable-build-servers`.
- [ ] Wykonać analizę GitNexus zmian przed commitem.
- [ ] Wykonać commit dopiero po czystym diffie i pełnej regresji.

## Kryterium zakończenia

Migracja jest zakończona, gdy:

- `Z80Cpu` używa `CpuProcessorBase<Z80State>`;
- wszystkie opcode’y korzystają z jednego standardu `OpcodeTable`;
- snapshot, debugger, observer, WAIT, HALT, I/O i interrupty mają testy kontraktowe;
- testy jednostkowe, `z80doc.tap`, `zexdoc.com`, pełny build i pełna regresja rozwiązania przechodzą.
