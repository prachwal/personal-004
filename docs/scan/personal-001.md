# personal-001

- Zrodlo: `/home/prachwal/source/emulators/personal-001`.
- Stos: .NET; `MySolution.slnx`, centralne props i pakiety.
- 6502: `lib/Emulator.Cpu6502/`, pelna implementacja cyklowa z wariantem Classic i opcode nielegalnymi.
- PET: `lib/Emulator.Pet/` obejmuje maszyne, PIA, raster display, screen codes, klawiature, IEEE-488, datasette/tape, DOS i loader ROM.
- PET UI: `src/Emulator.Pet.Desktop/Composition/PetComposition.cs`; osobny Apple 1 i terminal.
- ROM/fonty: `roms/fonts/pet/pet-2001.bin`, `pet-4032.bin`; ROM Apple 1.
- Testy: PET, tape, IEEE, font loader, terminal display i CPU core.
- Kandydat reuse: najwyzszy priorytet dla PET i 6502 w C#.
