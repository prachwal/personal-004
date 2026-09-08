# COSMAC

- Zrodlo: `/home/prachwal/source/emulators/COSMAC`.
- Stos: .NET; RCA CDP1802/COSMAC.
- UI: GUI i terminal; `src/Cdp1802.Cli/InteractiveDebugger.cs`.
- Debugger: `src/Cdp1802.Core/Debugger.cs` i `RemoteDebugger.cs`.
- Testy: `tests/Cdp1802.Tests/DebuggerTests.cs`, `DebuggerAdvancedTests.cs`, `RemoteDebuggerTests.cs`.
- Kandydat reuse: wzorzec lokalnego/zdalnego debuggera, nie jest to implementacja 6502/PET.
