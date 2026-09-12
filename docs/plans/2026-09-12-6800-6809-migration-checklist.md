# Migracja 6800 -> 6809 — checklista postępu

- [x] Etap 1: baseline build/test i zapis zachowania wyjściowego — build OK; CPU6809 90/90; PET 255/255
- [x] Etap 2: projekt Cpu6800 oraz projekt testów — solution build OK; test assembly ładuje się bez testów
- [ ] Etap 3: M6800State i M6800Flags
- [ ] Etap 4: metadata opcode'ów i tablice instrukcji
- [ ] Etap 5: lifecycle M6800Cpu
- [ ] Etap 6: M6809Cpu dziedziczy po M6800Cpu
- [ ] Etap 7: migracja wspólnych instrukcji i adresowania
- [ ] Etap 8: integracja tablic 6800/6809 i jawne nielegalne opcode'y
- [ ] Etap 9: regresja SuperPET i testy integracyjne
- [ ] Etap 10: pełne testy MC6800
- [ ] Etap 11: końcowa analiza GitNexus i przegląd diffu
- [ ] Etap 12: Definition of Done i przekazanie

## Stan

- branch: refactor/6800-6809-core-architecture
- aktualny etap: 3
- ostatnia weryfikacja: etap 2 zakończony; build 0 ostrzeżeń / 0 błędów
- uwagi: plan migracji znajduje się w 2026-09-12-gitnexus-plan-6800-6809-core-migration.md
