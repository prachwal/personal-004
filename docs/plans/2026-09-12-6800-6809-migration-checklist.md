# Migracja 6800 -> 6809 — checklista postępu

- [x] Etap 1: baseline build/test i zapis zachowania wyjściowego — build OK; CPU6809 90/90; PET 255/255
- [x] Etap 2: projekt Cpu6800 oraz projekt testów — solution build OK; test assembly ładuje się bez testów
- [x] Etap 3: M6800State i M6800Flags — 3/3 testy stanu i flag przechodzą
- [x] Etap 4: metadata opcode'ów i tablice instrukcji — M6800AddressingMode, definition i table
- [x] Etap 5: lifecycle M6800Cpu — fetch/reset/StepInstruction/cycle contract; 4 testy Cpu6800
- [x] Etap 6: M6809Cpu dziedziczy po M6800Cpu — build solution OK; CPU6809 90/90; PET 255/255
- [x] Etap 7: migracja wspólnych instrukcji i adresowania — wspólne direct/extended load helpers; 6809 zachowuje DP przez hook; CPU6809 90/90
- [x] Etap 8: integracja tablic 6800/6809 i jawne nielegalne opcode'y — metadata page-0/page-10/page-11; unsupported prefixed entries są jawne, zachowane 2 cykle
- [x] Etap 9: regresja SuperPET i testy integracyjne — PET 255 aktywnych przypadków bez błędów; przypadki long-running pozostają skipped
- [x] Etap 10: pełne testy MC6800 — Cpu6800.Tests 5/5
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
- [x] Zbudować niezależną tablicę opcode’ów i procesor MC6800 — `M6800Processor` posiada własną tablicę wykonawczą i pełną metadanych mapę 256 opcode’ów
- [x] Dodać pełną regresję instrukcji MC6800 — sweep wszystkich zaimplementowanych opcode’ów oraz testy adresowania; Cpu6800 `9/9`

Stan kontynuacji: zakończony; `M6800Processor` i regresja MC6800 gotowe; solution build, Cpu6800 `9/9`, Cpu6809 `91/91`.
