# personal-003

- Zrodlo: `/home/prachwal/source/emulators/personal-003`.
- Stos: .NET; `TRS80.slnx`; platforma TRS-80, Kaypro, Osborne i inne maszyny.
- GUI: `src/Retro.Desktop/` z Avalonia, konfiguracja maszyny, klawiatura i style.
- Debugger: `src/Retro.Debugger/`, projekt scriptable trace/debug; `src/Retro.Configuration/DebugApi.cs` oraz `tests/Retro.Configuration.Tests/DebugApiTests.cs`.
- ROM-y: `roms/` zawiera obrazy TRS-80, Kaypro, Osborne, CPC, MSX i Spectrum oraz chargen.
- Dokumentacja: `TRS-80_COMPLETE_DOCUMENTATION/` obejmuje architekture, Avalonia, ROM-y, urzadzenia i debugowanie.
- Kandydat reuse: wzorzec desktop composition, Debug API, debugger i organizacja ROM/profile; nie zawiera potwierdzonej implementacji PET/6502 jako celu tej iteracji.
