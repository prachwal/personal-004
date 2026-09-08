# Poglebiona analiza kandydatow

## Werdykt

Najlepsza baza kompletnego emulatora PET w C# to `personal-001`. Ma juz CPU, RAM/ROM, PIA, VIA, CRTC, klawiature, IEEE-488, D64, Datasette, display, profile PET i testy integracyjne.

Najlepsze uzupelnienia:

- `proc-vibe-001`: testy 6502, fonty i uniwersalny monitor Avalonia.
- `personal-002`: wspolny Avalonia shell i Debug API HTTP/WebSocket.
- `personal-003`: framebuffer, dirty-cell rendering i debugger CLI.
- `cpu-vibe-001`: alternatywna, szeroka integracja PET/TUI.
- `psudo-cpu`: dashboard urzadzen, CLI i worker JSON Lines.

`cpu-rust` i `web-emulator` sa poza shortlista reuse; ich opisy pozostaja w `docs/scan/` jako archiwum.

## Komponenty

| Warstwa | Kandydat | Ocena |
| --- | --- | --- |
| CPU 6502 | `personal-001` | najlepsza baza C#, ale trzeba sprawdzic `Placeholder` i edge cases |
| Testy CPU | `proc-vibe-001` | Klaus functional, decimal, 65C02 i testy opcode; sprawdzic licencje GPLv3 |
| PET machine | `personal-001` | najszersza integracja i testy |
| PET/TUI | `cpu-vibe-001` | dobry wariant terminalowy, bez Avalonia i zdalnego API |
| Fonty | `proc-vibe-001` | wspolny `IGlyphFont`/`IFontLoader`, PET/Kaypro/Osborne |
| Avalonia shell | `personal-002` | wspolny lifecycle, `IMachine`, `RunFrame` i Debug API |
| Avalonia framebuffer | `personal-003` | poprawne kumulowanie dirty cells |
| Monitor rastra | `proc-vibe-001` | `MonitorScreenView`, palette, PAR i scaling |
| Debugger CLI | `personal-003` | trace, breakpoint, watch, memory, screen i VRAM |
| Debug API | `personal-002` | status, step, trace, memory, screen, snapshot i WebSocket |

## Najwazniejsze ryzyka

- `personal-001`: timing CRTC jest czesciowy; IEEE-488, D64 i TAP wymagaja testow wszystkich wariantow; ROM-y nie maja checksumow.
- `personal-001`: przed uznaniem CPU za finalny uruchomic niezalezny zestaw opcode, page crossing, cykli, IRQ/NMI i NMOS edge cases.
- `cpu-vibe-001`: PET jest szeroki, ale zalezy od TUI i nie ma wspolnego API z `personal-002`.
- `psudo-cpu`: test boot BASIC jest `Explicit`, a GUI nie korzysta konsekwentnie ze sciezki worker IPC.
- Fonty: screen codes/PETSCII musza miec jedno zrodlo prawdy; rozne iteracje maja wlasne mapowania i chargen.
- Debug API: brak uwierzytelniania i ryzyko blokowania watku UI przy dlugim trace.
- Nie laczyc komponentow przez bezposrednie odczyty konkretnego CPU. Potrzebny jest wspolny kontrakt kroku maszyny, zegara, IRQ/NMI, memory peek, display frame i snapshotu.

## Rekomendowany sklad

1. Uzyc `personal-001` jako bazy PET.
2. Dodac testy 6502 z `proc-vibe-001`, po weryfikacji licencji.
3. Ujednolicic ROM/font loader i mapowanie PETSCII.
4. Wydzielic kontrakty maszyny, zegara, przerwan, pamieci i obrazu.
5. Wybrac jeden monitor: `proc-vibe-001` dla uniwersalnego rastra albo `personal-003` dla dirty-cell framebuffer.
6. Dodac Debug API z `personal-002` dopiero po potwierdzeniu, ze krok obejmuje cala maszyne.

## Weryfikacja

W zrodlach kandydatow uruchomic:

```bash
dotnet test /home/prachwal/source/emulators/personal-001/test/Emulator.Pet.Tests/Emulator.Pet.Tests.csproj
dotnet test /home/prachwal/source/emulators/proc-vibe-001/tests/ProcVibe.Cpu.Mos6502.Tests/ProcVibe.Cpu.Mos6502.Tests.csproj
dotnet test /home/prachwal/source/emulators/psudo-cpu/Emulator.Tests/Emulator.Tests.csproj
```

Nastepnie porownac: reset vector, `READY.`, klawiature, kursor, PETSCII, VRAM, IRQ, BASIC LOAD, D64 LOAD, TAP LOAD, snapshot i trace.
