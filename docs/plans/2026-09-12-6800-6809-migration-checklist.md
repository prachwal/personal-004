# Migracja 6800 -> 6809 — checklista postępu

Rodzina 6800/6809: migracja bazowa zakończona; 6809 pozostaje rozszerzeniem 6800.

## Status wspólnego Core

- [x] `M6800Cpu` używa wspólnego lifecycle, stanu i tabeli opcode’ów Core.
- [x] `M6809Cpu` dziedziczy po `M6800Cpu` i zachowuje strony opcode’ów 10/11.
- [x] Zachowane są DP, Y/U/S, FIRQ/NMI/IRQ oraz formaty ramek stosu.
- [x] Wspólne instrukcje 6800 są implementowane w bazie i rozszerzane przez 6809.
- [x] Testy CPU6800: 11/11; testy CPU6809: 91/91.
- [x] Test binarny MC6800 i test binarny 6809 przechodzą.
- [ ] Przenieść pozostałe rodzinne różnice do jawnych capability Core.
- [ ] Wykonać końcową regresję całego rozwiązania po migracji Z80.

- [x] Etap 1: baseline build/test i zapis zachowania wyjściowego — build OK; CPU6809 90/90; PET 255/255
- [x] Etap 2: projekt Cpu6800 oraz projekt testów — solution build OK; test assembly ładuje się bez testów
- [x] Etap 3: M6800State i M6800Flags — 3/3 testy stanu i flag przechodzą
- [x] Etap 4: metadata opcode'ów i tablice instrukcji — M6800AddressingMode, definition i table
- [x] Etap 5: lifecycle M6800Cpu — fetch/reset/StepInstruction/cycle contract; 4 testy Cpu6800
- [x] Etap 6: M6809Cpu dziedziczy po M6800Cpu — build solution OK; CPU6809 90/90; PET 255/255
- [x] Etap 7: migracja wspólnych instrukcji i adresowania — wspólne direct/extended load helpers; 6809 zachowuje DP przez hook; CPU6809 90/90
- [x] Etap 8: integracja tablic 6800/6809 i jawne nielegalne opcode'y — metadata page-0/page-10/page-11; unsupported prefixed entries są jawne, zachowane 2 cykle
- [x] Etap 9: regresja SuperPET i testy integracyjne — PET 255 aktywnych przypadków bez błędów; przypadki long-running pozostają skipped
- [x] Etap 10: pełne testy MC6800 — Cpu6800.Tests 10/10, w tym test skompilowanego programu binarnego
- [x] Etap 11: końcowa analiza GitNexus i przegląd diffu — indeks aktualny; detect-changes all: No changes detected
- [x] Etap 12: Definition of Done i przekazanie — build/testy/regresja zakończone; branch gotowy do przeglądu

## Stan

- branch: refactor/6800-6809-core-architecture
- aktualny etap: zakończony
- ostatnia weryfikacja: GitNexus up-to-date; detect-changes all: No changes detected
- uwagi: plan migracji znajduje się w 2026-09-12-gitnexus-plan-6800-6809-core-migration.md

## Kontynuacja: pełny zestaw instrukcji w Cpu6800

- [x] Wspólne ALU A/B — `M6800Alu.cs`
- [x] Wspólne unaryczne RMW — `M6800Unary.cs`
- [x] Wspólne branche krótkie oraz `BSR/RTS` — `M6800Branches.cs`
- [x] Wspólne `LDA/LDB/STA/STB` — `M6800LoadStore.cs`
- [x] Wspólne instrukcje rejestru `X` — `M6800IndexRegister.cs`
- [x] Przenieść pozostałe instrukcje MC6800 i usunąć shims z `M6809Cpu` — usunięto duplikaty ALU, RMW, branch, RTS oraz A/B/X load/store; CPU6809 wskazuje na bazowe implementacje Cpu6800
- [x] Zbudować niezależną tablicę opcode’ów i procesor MC6800 — konkretna `M6800Cpu` posiada bazową tablicę wykonawczą i pełną mapę metadanych 256 opcode’ów
- [x] Dodać pełną regresję instrukcji MC6800 — sweep wszystkich zaimplementowanych opcode’ów oraz testy adresowania; Cpu6800 `10/10`

### Test binarny MC6800

- Źródło: `tests/PetEmulator.Cpu6800.Tests/TestData/mc6800_regression_test.asm`
- Binarium: `tests/PetEmulator.Cpu6800.Tests/TestData/mc6800-functional-test.bin`
- Test harness: `tests/PetEmulator.Cpu6800.Tests/M6800BinaryTests.cs`
- Kompilacja:
  `sdas6808 -lo mc6800_regression_test.asm`
  `sdld6808 -i -b CODE=0x0100 mc6800_regression_test`
  `objcopy -I ihex -O binary mc6800_regression_test.ihx mc6800-functional-test.bin`
- Program ładuje się pod `$0100`, kończy `WAI`, a kod błędu zapisuje pod `$0200`; `0` oznacza sukces.

Stan kontynuacji: zakończony; regresja MC6800 gotowa; solution build, Cpu6800 `11/11`, Cpu6809 `91/91`.
