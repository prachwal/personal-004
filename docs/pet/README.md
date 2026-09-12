# Implementacja PET/CBM

Dokument główny: [io-ports.md](io-ports.md).

## Zakres

- profile PET 2001, CBM 3000/4000/8000 oraz profile rozszerzone;
- PIA1/PIA2, VIA, IEEE-488, kaseta, klawiatura i CRTC;
- SuperPET: MOS6551 ACIA pod adresem `$EFF0-$EFF3`, transport bajtowy i IRQ;
- ROM manifesty, mapowanie pamięci, profile Desktop i CLI.

## Kod

- Maszyna i magistrala: `src/PetEmulator.Pet/PetMachine.cs`, `src/PetEmulator.Pet/PetMemoryBus.cs`.
- Profile: `src/PetEmulator.Pet/PetProfile.cs`, `src/PetEmulator.Pet/PetProfileCatalog.cs`.
- Układy wspólne: [../chips/README.md](../chips/README.md).

## Testy

- PET: `tests/PetEmulator.Pet.Tests/`.
- CLI: `tests/PetEmulator.Cli.Tests/`.
- Desktop: `tests/PetEmulator.Desktop.Tests/`.
- Pełny test profilu SuperPET obejmuje ROM, mapowanie ACIA, transmisję bajtu i IRQ 6502.

## Otwarte punkty

- drugi datasette, pełny User Port i bankowanie 8096/8296;
- pełny boot firmware Waterloo oraz warianty dongla 6702 (mapa ROM, przełączanie
  6502/6809, bankowany RAM, podstawowy model 6702, wektor resetu i pierwsze 16
  instrukcji są już zweryfikowane);
- rzeczywisty transport TCP/terminal dla ACIA;
- testy boot/diagnostic dla każdego dodatkowego układu.
