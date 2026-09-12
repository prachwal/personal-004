# Migracja rodziny Z80 do wspólnego Core — checklista

Status: częściowa integracja kontraktów; pełna migracja do `CpuProcessorBase` jeszcze się nie rozpoczęła.

## Kontrakty i stan

- [x] Projekt Z80 referuje `PetEmulator.Core`.
- [x] `Z80Cpu` implementuje `IProcessor` i `IDebuggableProcessor`.
- [x] Pamięć RAM/ROM używa `Core.IMemoryBus`.
- [x] Zegar używa `Core.IClock`.
- [x] Rejestry i stan debuggera są dostępne przez `GetRegisters()`.
- [ ] Wprowadzić `Z80State : Core.CpuState`.
- [ ] Przenieść snapshot/restore do wspólnego stanu.
- [ ] Zastąpić ręczne liczniki lifecycle bazą Core.

## Opcode’y i wykonanie

- [x] Istnieją osobne dispatchery opcode’ów bazowych i prefiksu ED.
- [x] Zachowane są prefiksy CB/DD/FD/ED oraz semantyka Z80.
- [ ] Przenieść opcode’y do `Core.OpcodeTable<Z80State>`.
- [ ] Zachować rozszerzenia prefiksowe jako rodzinne strony opcode’ów.
- [ ] Usunąć lokalne słowniki `opcodes` i `edOpcodes` po regresji.

## Magistrala i timing

- [x] Zachować lokalną magistralę `IBus` jako capability Z80.
- [x] Zachować `BusCycle`, machine cycle, T-states i obserwator cykli.
- [x] Zachować WAIT, HALT, refresh, I/O i acknowledge przerwań.
- [ ] Zintegrować lifecycle Core bez utraty kolejności WAIT → NMI → INT → HALT refresh.
- [ ] Dodać wspólny adapter obserwatora Core dla bogatego `BusCycle`.

## Testy

- [x] Istnieją testy opcode’ów bazowych, CB, ED, indexed i flag matrix.
- [x] Istnieją testy magistrali, pamięci, WAIT, interruptów i timingów.
- [x] W repozytorium są `z80doc.tap` i `zexdoc.com`.
- [ ] Wykonać aktualną pełną regresję po zmianach Core.
- [ ] Potwierdzić wynik ZEXDOC jako obowiązkową regresję migracji.

## Ryzyka

- Z80 ma bogatszy timing niż `Core.BusAccess`; nie wolno spłaszczyć `BusCycle`.
- I/O, WAIT i interrupt acknowledge muszą pozostać capability, a nie obowiązkowym API każdego CPU.
