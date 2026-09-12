# Migracja rodziny 6502 do wspólnego Core — checklista

Status: pełna migracja rodziny 6502 do wspólnego Core zakończona.

## Architektura

- [x] `Cpu6502` dziedziczy po `CpuProcessorBase<CpuState>`.
- [x] `CpuState` implementuje wspólny kontrakt stanu i snapshot/debug restore.
- [x] Wspólny zegar, liczniki, IRQ/NMI i obserwator wykonania są podłączone.
- [x] Publiczne `Cpu6502.Opcodes` wskazuje na tabelę Core.
- [x] Dispatch cyklowy korzysta z definicji Core.
- [x] Silnik cyklowy i kontrakt `IProcessor` korzystają z tej samej instancji zegara Core.
- [x] Domyślne tablice wariantów Core są publikowane jako sealed registry.
- [x] `Cpu6502Variant.OpcodeTable` jest publicznym kontraktem rozszerzania wariantu.
- [x] Natywna tablica NMOS Core nie tworzy legacy definicji.
- [x] Usunąć `Processor/OpcodeTable.cs`.
- [x] Usunąć `Processor/OpcodeDefinition.cs`.
- [x] Zbudować wszystkie tabele wariantów bez tworzenia legacy definicji.
- [x] Usunąć adapter legacy → Core z `OpcodeTables.cs`.

## Warianty

- [x] MOS 6502 / NMOS.
- [x] MOS 6507 / Atari.
- [x] MOS 6510 / Commodore.
- [x] Ricoh 2A03 / NES.
- [x] WDC 65C02.
- [x] Rockwell/WDC R65C02S.
- [x] BCD, JMP indirect bug, CMOS BCD timing i Rockwell bit operations pozostają wariantowe.

## Testy

- [x] Testy tabel Core wszystkich wariantów: 32/32.
- [x] Regresja CPU 6502 poza testami Klaus: 349/349.
- [x] Testy binarne 6502 są w projekcie (`6502_functional_test.bin`).
- [x] Pełny test Klaus non-BCD: 1/1.
- [x] Pełny test Klaus BCD: 1/1.
- [x] Pełna regresja CPU: 333 zaliczone, 1 pominięty test NES oznaczony `Ignore`.
- [x] Brak legacy `OpcodeTable`/`OpcodeDefinition` w bibliotece CPU 6502.

## Ryzyka

- Publiczny kontrakt tabel wariantów jest teraz wyłącznie `PetEmulator.Core.OpcodeTable<CpuState>`.
- Model cyklowy 6502 zachowuje prywatne pola CPU; nie jest jeszcze w pełni oparty na samym `CpuState`.
