# Implementacja VIC-20

Dokument główny: [io-ports.md](io-ports.md).

## Zakres

- VIC-I 6560/6561: obraz, raster, kolory i audio;
- VIA1/VIA2: klawiatura, joystick, User Port, IEC i kaseta;
- Color RAM, cartridge CRT/PRG, profile RAM i pluginy DLL;
- wirtualny joystick Desktop, IEC/D64 i profile testowe.

## Kod

- Maszyna i magistrala: `src/PetEmulator.Vic20/Vic20Machine.cs`, `src/PetEmulator.Vic20/Vic20MemoryBus.cs`.
- Cartridge: `src/PetEmulator.Vic20/Vic20Cartridge.cs` oraz `plugins/`.
- Układy wspólne: [../chips/README.md](../chips/README.md).

## Testy

- VIC-20: `tests/PetEmulator.Vic20.Tests/`.
- Desktop i joystick: `tests/PetEmulator.Desktop.Tests/`.
- Testy obejmują profile pamięci, mapowanie VIA/VIC-I, cartridge, IEC, kasetę, klawiaturę i audio.

## Otwarte punkty

- pełniejszy model MultiCartridge i arbitraż I/O2/I/O3;
- analogowe źródło paddle/light-pen;
- pełny User Port i adapter RS-232;
- testy boot/diagnostic dla każdego dodatkowego układu.
