# personal-002

- Zrodlo: `/home/prachwal/source/emulators/personal-002`.
- Stos: .NET 10; `personal-002.slnx`, centralne pakiety.
- Zakres: multi-machine i duzy katalog chipow; M6502 w `lib/Cpu/MosTechnology/M6502/`.
- UI: `lib/Terminal/` i `lib/Terminal.Avalonia/`; parser ANSI/VT100 i buffer terminala.
- API/debugger: `lib/Terminal.Avalonia/DebugApi.cs`, opis `docs/guides/debug-api.md`.
- Testy 6502: `tests/M6502.Tests/`, ROM funkcjonalny i decimal test.
- Kandydat reuse: terminal/Avalonia, DebugApi, kontrakty chipow i testy diagnostyczne.
