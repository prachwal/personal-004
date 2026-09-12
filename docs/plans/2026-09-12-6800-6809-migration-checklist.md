# Migracja 6800 -> 6809 — checklista postępu

- [x] Etap 1: baseline build/test i zapis zachowania wyjściowego — build OK; CPU6809 90/90; PET 255/255
- [x] Etap 2: projekt Cpu6800 oraz projekt testów — solution build OK; test assembly ładuje się bez testów
- [x] Etap 3: M6800State i M6800Flags — 3/3 testy stanu i flag przechodzą
- [x] Etap 4: metadata opcode'ów i tablice instrukcji — M6800AddressingMode, definition i table
- [x] Etap 5: lifecycle M6800Cpu — fetch/reset/StepInstruction/cycle contract; 4 testy Cpu6800
- [x] Etap 6: M6809Cpu dziedziczy po M6800Cpu — build solution OK; CPU6809 90/90; PET 255/255
- [ ] Etap 7: migracja wspólnych instrukcji i adresowania
- [ ] Etap 8: integracja tablic 6800/6809 i jawne nielegalne opcode'y
- [ ] Etap 9: regresja SuperPET i testy integracyjne
- [ ] Etap 10: pełne testy MC6800
- [ ] Etap 11: końcowa analiza GitNexus i przegląd diffu
- [ ] Etap 12: Definition of Done i przekazanie

## Stan

- branch: refactor/6800-6809-core-architecture
- aktualny etap: 7
- ostatnia weryfikacja: solution build OK; Cpu6800 4/4; CPU6809 90/90; PET 255/255
- uwagi: plan migracji znajduje się w 2026-09-12-gitnexus-plan-6800-6809-core-migration.md
