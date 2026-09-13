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
- [ ] Dodać test snapshotu w trakcie `WAIT`, `HALT`, prefiksu i opóźnienia `EI`.

## 2. Wspólny lifecycle Core

- [x] Wspólny zegar i liczniki instrukcji są dostępne przez Core.
- [x] `StepInstruction`, `CycleCount` i `InstructionCount` działają przez bazę Core.
- [x] WAIT, NMI, INT i HALT są obsługiwane przez hooki lifecycle Z80.
- [ ] Potwierdzić testami kolejność: `WAIT → NMI edge → INT → HALT refresh → fetch/execute`.
- [ ] Usunąć ewentualne pozostałe lokalne implementacje lifecycle, pozostawiając tylko delegacje kompatybilności API.
- [ ] Potwierdzić, że każdy krok zwiększa zegar i licznik dokładnie raz.

## 3. Opcode’y i rejestracja

- [x] Opcode’y bazowe, CB, ED, DD i FD są przechowywane w `Core.OpcodeTable`.
- [x] Prefiksy DD/FD oraz DD/FD+CB mają jawny decoder rodzinny.
- [x] Zachowano rozszerzenia Z80: IXH/IXL, IYH/IYL, displacement, I/O i nieużywane ED.
- [x] Główny punkt rejestracji opcode’ów przeniesiono do partiala `Z80Cpu.OpcodeRegistration`.
- [x] Bazowe opcode’y przeniesiono do `Z80Cpu.OpcodeRegistration.Base`.
- [x] Opcode’y ED przeniesiono do `Z80Cpu.OpcodeRegistration.Ed`.
- [x] Opcode’y CB przeniesiono do `Z80Cpu.OpcodeRegistration.Cb`.
- [ ] Rozdzielić rejestrację opcode’ów na partiale: Base, CB, ED, Indexed i I/O.
- [ ] Usunąć wrappery `RegisterOpcode`, `RegisterEdOpcode` i `RegisterPageOpcode`, jeśli nie są już potrzebne do kompatybilności.
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
- [ ] Podłączyć Z80 do wspólnego `ExecutionObserver`.
- [ ] Zachować hooki jako adapter kompatybilności dla istniejących callerów.
- [ ] Dodać breakpoint przed/po instrukcji i obserwację wyjątków.
- [ ] Dodać watchpoint pamięci i I/O przez wspólny kontrakt.
- [ ] Zweryfikować debug snapshot dla instrukcji, WAIT, HALT i interruptów.

## 6. Testy regresyjne

- [x] Testy bazowych opcode’ów, CB, ED, indexed i flag matrix.
- [x] Testy magistrali, pamięci, WAIT, interruptów i timingów.
- [x] Po zmianie typu bazowego wykonano 517 testów jednostkowych Z80 bez testów binarnych; 517/517 przeszło, czas 9 min 11 s.
- [x] Po przeniesieniu rejestracji opcode’ów do partiala wykonano 517 testów jednostkowych Z80 bez testów binarnych; 517/517 przeszło, czas 9 min 50 s.
- [x] Po wydzieleniu bazowych opcode’ów wykonano test rozszerzania rejestracji: 1/1, czas 43 ms.
- [x] Po wydzieleniu opcode’ów ED wykonano 25 testów ED i rozszerzania rejestracji; 25/25 przeszło, czas 1 s.
- [x] Po wydzieleniu opcode’ów CB wykonano 14 testów CB i rozszerzania rejestracji; 14/14 przeszło, czas 20 s.
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
