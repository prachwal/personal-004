# cpu-vibe-012

- Zrodlo: `/home/prachwal/source/emulators/cpu-vibe-012`.
- Stos: .NET; platforma abstrakcji z rozdzieleniem `Cpu`, `Bus`, `Devices`, `Display`, `Terminal`, `Input`, `Storage`, `Machine`, `Serialization`.
- Rdzenie: platforma multi-CPU, w tym 6502, Z80, 8080, 6800/6809, CDP1802 i CHIP-8.
- Debugger: `src/CpuVibe.Cdp1802.System/System/Debugger.cs` i `RemoteDebugger.cs`.
- Kandydat reuse: najnowszy wzorzec podzialu platformy; zweryfikowac kompletne projekty i testy przed adopcja.
