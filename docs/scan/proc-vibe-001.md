# proc-vibe-001

- Zrodlo: `/home/prachwal/source/emulators/proc-vibe-001`.
- Stos: .NET; `ProcVibe.slnx`; biblioteka CPU i GUI.
- 6502: `src/ProcVibe.Cpu.Mos6502/`, opcode table, testy opcode, undocumented i Klaus functional.
- GUI: `src/ProcVibe.Gui/` z wieloma widokami chipow, terminali, CPU i `MonitorScreenView`.
- Fonty: `lib/fonts/pet-chargen/chargen.bin`, `kaypro-font/81-146a.rom`, `osborne-font/char.ua15`; README opisuje formaty.
- ROM/testy: `lib/klaus6502/` functional/decimal/65C02, `lib/vic2-gfx/`, testy CPU i GUI.
- Kandydat reuse: test harness 6502, loader fontow, widoki monitora i obsluga fontow PET/Kaypro/Osborne.
