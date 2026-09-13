# Kaypro II

## Status

Pierwszy pakiet implementacji używa wspólnych kontraktów projektu:

- `src/PetEmulator.Kaypro/KayproMachine.cs` — kompozycja maszyny `IMachine`;
- `src/PetEmulator.Kaypro/KayproBus.cs` — pamięć 64 KiB, ROM monitora, VRAM i porty;
- `src/PetEmulator.Kaypro/KayproSio.cs` — pollingowy kanał konsoli SIO;
- `src/PetEmulator.Kaypro/KayproPio.cs` — rejestry/latch PIO drukarki i sterowania;
- `src/PetEmulator.Kaypro/KayproVideo.cs` — ekran tekstowy 80×24;
- `src/PetEmulator.Kaypro/KayproDiskImage.cs` — surowy obraz 40 ścieżek × 10 sektorów × 512 bajtów;
- `lib/PetEmulator.Chips/FD1793.cs` — wspólny model FDC, bez lokalnej kopii kontrolera.

Mapa I/O: `04–07` SIO, `08–0B` PIO/latch, `10–13` FD1793, `1C` port systemowy.
Bit 7 portu `1C` wybiera ROM, a bity `0–1` wybierają napęd.

## Następne elementy

- pełny protokół Z80 SIO i przerwania IM2;
- jawny model Z80 PIO oraz sygnałów drukarki/napędów;
- loader TD0 dla obrazu `kpii-149.td0`;
- ROM/font Kaypro z walidacją hashy;
- test bootu realnego ROM-u do promptu CP/M `A>`;
- integracja CLI/Desktop.
