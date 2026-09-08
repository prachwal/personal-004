# psudo-cpu

- Zrodlo: `/home/prachwal/source/emulators/psudo-cpu`.
- Stos: .NET; `Emulator.sln`.
- Rdzenie: `Emulator.Cpu.Mos6502/` i `Emulator.Cpu.Z80/`; warianty 6502 Classic/NES.
- PET: `Emulator.Machine/`, ROM-y PET w `roms/pet/`, `fonts/petscii.bin`, `roms/pet_chars.bin`.
- UI: `Emulator.Avalonia/`; CLI `Emulator.Cli/`; `Emulator.Worker/` i abstrakcje urzadzen.
- Kandydat reuse: prosty podzial core/machine/CLI/Avalonia i duzy zestaw ROM-ow PET; sprawdzic duplikacje z `personal-001`.
