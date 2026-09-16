# Inwentaryzacja poprzednich iteracji

## Dokumentacja bieżącego projektu

- [Architektura i budowanie komputera z komponentów](architecture/overview.md) — wspólny model składania
  maszyny oraz przykłady PET i VIC-20.
- [Implementacja PET/CBM](pet/README.md) oraz [mapowanie portów PET](pet/io-ports.md) —
  profile, magistrala, PIA/VIA/CRTC, kasety, IEEE-488 i SuperPET.
- [Implementacja VIC-20](vic20/README.md) oraz [mapowanie portów VIC-20](vic20/io-ports.md) —
  VIC-I, VIA, IEC, cartridge, RAM, kaseta, joystick i audio.
- [Układy](chips/README.md) — osobne, pełne checklisty dla MOS2114, MT6520, MOS6522,
  MT6545, MOS6560, MC146818 i MOS6551.
- [Plan migracji VIC-20](vic20/migration-plan.md) i [strategia testów VIC-20](vic20/testing-strategy.md).
- [Narzędzia debugowania](pet/debug-tools.md) — wspólne dla maszyn implementujących kontrakty Core.
- [Checklisty migracji CPU](plans/2026-09-12-6502-core-migration-checklist.md) — status rodzin 6502, 6800/6809 i Z80.
- [Implementacja Kaypro II](kaypro/README.md) — maszyna Z80, FD1793, pamięć, SIO, video i status migracji.
- [CPC6128 — status implementacji](plans/2026-09-16-cpc6128-status.md), [architektura współdzielona CPC](plans/2026-09-16-cpc-architecture.md) i [otwarte punkty](plans/2026-09-16-open-items.md).

Ten katalog jest indeksem kodu i artefaktow z poprzednich projektow emulatorow. Nie kopiuje kodu, ROM-ow ani fontow. Sciezki wskazuja oryginalne lokalizacje do pozniejszego porownania.

Zakres: 28 projektow z `/home/prachwal/source/emulators` oraz `/home/prachwal/source/tui`, w tym `personal-003`, ktory powstal po bazowym spisie.

Poglebione porownanie komponentow i ryzyk: [deep-analysis.md](deep-analysis.md).

## Projekty

| Projekt | Stos / zakres | Najwazniejsze znaleziska |
| --- | --- | --- |
| [6502](scan/6502.md) | .NET, 6502, Apple 1 | rdzen cyklowy, Apple 1, Avalonia, ROM testowy |
| [COSMAC](scan/COSMAC.md) | .NET, RCA 1802 | GUI, TUI, debugger lokalny i zdalny |
| [cpu-emulator](scan/cpu-emulator.md) | .NET, multi-CPU | 6502, Z80, MC6800, framebuffer |
| [cpu-rust](scan/cpu-rust.md) | Rust workspace | 6502, PET, Apple 1, KIM-1, ROM-y i terminale |
| [cpu-vibe-001](scan/cpu-vibe-001.md) | .NET, CPU i maszyny | 6502, PET, Apple 1, VIC-20, TUI |
| [cpu-vibe-002](scan/cpu-vibe-002.md) | .NET, framework UI | TUI, Avalonia, moduly demonstracyjne |
| [cpu-vibe-003](scan/cpu-vibe-003.md) | .NET, CPU i GUI | Z80, 8080, 4004/4040, TUI, GUI |
| [cpu-vibe-004](scan/cpu-vibe-004.md) | .NET, framework | TUI modularne, debugowalne urzadzenia |
| [cpu-vibe-005](scan/cpu-vibe-005.md) | .NET, 6502/6510 | UI konsolowe, testy binarne i chipy |
| [cpu-vibe-006](scan/cpu-vibe-006.md) | .NET, multi-CPU | Avalonia TUI, fonty, debugger 1802 |
| [cpu-vibe-007](scan/cpu-vibe-007.md) | .NET, 8080/CHIP-8 | Avalonia terminal i demo |
| [cpu-vibe-008](scan/cpu-vibe-008.md) | .NET, multi-CPU | 8 rdzeni, szeroka baza maszyn |
| [cpu-vibe-009](scan/cpu-vibe-009.md) | .NET, TUI lab | 8080, CHIP-8, CP/M i testowe ROM-y |
| [cpu-vibe-010](scan/cpu-vibe-010.md) | .NET, Z80 Kaypro | Avalonia, terminal, font atlas, debugger |
| [cpu-vibe-011](scan/cpu-vibe-011.md) | .NET, Z80/8080/CHIP-8 | CP/M, IBM PC, Kaypro, Avalonia terminal |
| [cpu-vibe-012](scan/cpu-vibe-012.md) | .NET, platforma abstrakcji | CPU, bus, devices, display, debugger 1802 |
| [emu-devices-001](scan/emu-devices-001.md) | .NET, urzadzenia | abstrakcje urzadzen i terminal konsolowy |
| [personal-001](scan/personal-001.md) | .NET, 6502/PET | najbardziej kompletna implementacja PET w C# |
| [personal-002](scan/personal-002.md) | .NET 10, multi-machine | Terminal/Avalonia, DebugApi, wiele chipow |
| [personal-003](scan/personal-003.md) | .NET, TRS-80 | Avalonia, Retro.Debugger, Debug API, ROM-y |
| [probe-001](scan/probe-001.md) | .NET, RCA 1802 | monitor UT20, konsola i testy monitora |
| [proc-vibe-001](scan/proc-vibe-001.md) | .NET, biblioteka CPU | 6502, GUI, fonty PET/Kaypro/Osborne, test ROM-y |
| [psudo-cpu](scan/psudo-cpu.md) | .NET, 6502/Z80 | PET, Avalonia, CLI, ROM-y VICE |
| [test-vibe-app-06](scan/test-vibe-app-06.md) | .NET, 6502/PET | Avalonia screen, PET i Apple 1, kopia 6502 |
| [web-emulator](scan/web-emulator.md) | TypeScript/Vite/WASM | web GUI, monitory, DebugOverlay, fonty wielu maszyn |
| [tui-vibe-app-001](scan/tui-vibe-app-001.md) | .NET, TUI | PET/VIC-20, warstwa display i testy |
| [tui-vibe-app-002](scan/tui-vibe-app-002.md) | .NET, API/TUI | osobny projekt API i testy API |
| [tui-vibe-app-003](scan/tui-vibe-app-003.md) | .NET, TUI | 8080, CHIP-8, terminal i CLI |

## Priorytet do pozniejszego porownania

- 6502: `personal-001`, `6502`, `personal-002`, `psudo-cpu`, `proc-vibe-001`.
- PET: `personal-001`, `cpu-vibe-001`, `psudo-cpu`, `test-vibe-app-06`.
- GUI Avalonia: `personal-003`, `personal-002`, `personal-001`, `proc-vibe-001`, `6502`.
- Debugger i monitoring: `personal-003`, `personal-002`, `COSMAC`, `cpu-vibe-012`.
- ROM-y i fonty: `personal-003`, `psudo-cpu`, `proc-vibe-001`.

## Poza shortlista reuse

- [`cpu-rust`](scan/cpu-rust.md) - zachowany jako archiwalna referencja, wykluczony z dalszej shortlisty.
- [`web-emulator`](scan/web-emulator.md) - zachowany jako archiwalna referencja, wykluczony z dalszej shortlisty.

## Zasady dalszej oceny

Najpierw porownac API i testy, dopiero potem kod. Przy ROM-ach i fontach sprawdzic pochodzenie, licencje, format, rozmiar i mapowanie adresow. Nie zakladac kompatybilnosci implementacji tylko na podstawie wspolnej nazwy ukladu.
