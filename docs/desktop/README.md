# Aplikacja Desktop

Dokumentacja warstwy Avalonia, integracji audio i monitorowania maszyny.

- [Multi-machine Desktop](multi-machine.md) — przełączanie PET/VIC-20 i moduły UI.
- [Backend audio WSL](audio-wsl-backend.md) — PulseAudio/WSLg i weryfikacja wyjścia.
- [Plan monitorowania i debugowania](debug-monitoring-plan.md) — obserwacja maszyn i urządzeń.

Kod aplikacji znajduje się w `src/PetEmulator.Desktop/`, a testy w
`tests/PetEmulator.Desktop.Tests/`.

## Drzewo menu

Aktualne menu główne aplikacji (`src/PetEmulator.Desktop/Views/MainWindow.axaml`):

```text
Menu
├── File
│   ├── Reset
│   ├── New Tape…
│   ├── Load Tape…
│   ├── New Disk…
│   ├── Load Disk…
│   └── Exit
└── Machine
    ├── PET 20xx
    │   ├── PET 2001-8 / BASIC 1 / 40x25
    │   └── PET 2001-32 / BASIC 2 / 40x25
    ├── CBM 30xx
    │   ├── CBM 3008 / BASIC 2 / 40x25
    │   ├── CBM 3016 / BASIC 2 / 40x25
    │   └── CBM 3032 / BASIC 2 / 40x25
    ├── CBM 40xx
    │   ├── CBM 4008 / CRTC 40n60 / BASIC 4 / 40x25
    │   ├── CBM 4016 / CRTC 40n60 / BASIC 4 / 40x25
    │   ├── CBM 4032 / BASIC 4 / 40x25
    │   ├── CBM 4032 / CRTC 40n50 / BASIC 4 / 40x25
    │   ├── CBM 4032 / CRTC 40b50 / BASIC 4 / 40x25
    │   └── CBM 4032 / CRTC 40b60 / BASIC 4 / 40x25
    ├── CBM 80xx
    │   ├── CBM 8032 / BASIC 4 / 80x25
    │   ├── CBM 8032 / CRTC 80b50 / BASIC 4 / 80x25
    │   ├── CBM 8016 / converted 80n50 editor / BASIC 4 / 80x25
    │   └── PET / converted unknown 80n editor / BASIC 4 / 80x25
    ├── SuperPET
    │   ├── 6502
    │   └── 6809
    └── VIC-20
Tools
├── Chip Tester
├── Media Tester
├── Font / Glyph Viewer
├── CPU Opcode Stepper
└── Keyboard Matrix
```

`Machine` jest generowane z `PetProfileCatalog.Available` bez profili SuperPET, które są
zgrupowane pod osobną pozycją `SuperPET`. `Tools` korzysta z osobnej listy modułów
pomocniczych. Wybrana pozycja z obu menu zastępuje `CurrentModule`; dla obu profili SuperPET oznacza to tę samą
platformę sprzętową, ale inny procesor wybrany przy uruchomieniu: MOS 6502 albo Waterloo 6809.
