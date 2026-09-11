# Plan integracji monitoringu (CPU, PET, IEEE-488)

Kontekst: `PetEmulator.Core.IMachine` ma juz `Processor`/`Memory` (dodane pod Debug API i Debugger CLI —
patrz `PetEmulator.DebugApi`, `PetEmulator.Debugger`), ale **`PetMachine` jeszcze nie istnieje** —
uklady PET (`PetEmulator.Pet/Chips`, `Ieee488`, `CbmDos`, `Tape`, `Keyboard`, `Roms`) stoja osobno,
nic ich nie orkiestruje, a wlasny `Cpu6502` (namespace `Cpu6502`) nie implementuje jeszcze
`PetEmulator.Core.IProcessor`. Ten dokument to kolejnosc krokow, zeby monitoring (trace/watch/dump)
dzialal na realnym sprzecie, a nie tylko na fake `IMachine` w testach.

## Dlaczego kolejnosc ma znaczenie

Debug API i Debugger CLI juz umieja mowic do `IMachine`/`IProcessor`/`IMemoryBus` — ale to interfejsy
bez ciala. Podlaczenie ich do czegokolwiek realnego wymaga najpierw `PetMachine`, potem dopiero
rozszerzen w warstwie debug (rejestry CPU, zdarzenia magistrali).

```text
1. PetMachine (orchestrator)          <- blokuje wszystko ponizej
2. Cpu6502 : IProcessor (adapter)     <- odblokowuje realny trace/step
3. IDebuggableProcessor (rejestry)    <- odblokowuje PC/A/X/Y w trace, prawdziwy break-pc
4. Bus decoder + device Tick fan-out  <- odblokowuje watch na adresach PIA/VIA/CRTC
5. IEEE-488 / tape event hooks        <- odblokowuje monitoring protokolu, nie tylko pamieci
```

## Krok 1 — `PetMachine` (prerekwizyt)

Nowa klasa `PetEmulator.Pet.PetMachine : IMachine`, zbudowana z `PetProfile`:

- `IMemoryBus` = dekoder adresow: RAM (0x0000-RamSize), ROM-y z `PetRomLoader.Load(profil.RomDirectory, profil.RomManifest)`
  zmapowane pod swoje adresy, PIA1/PIA2/VIA/CRTC zmapowane pod swoje `BaseAddress` (juz wspierane —
  `Chips/*.cs` maja `BaseAddress` z poprzedniego portu).
- `IProcessor` = `Cpu6502` (patrz krok 2), skonstruowany z tym dekoderem jako `IMemoryBus`.
- `IReadOnlyList<IDevice>` = `[Pia1, Pia2, Via, Crtc, Datasette]` — `StepInstruction()` robi
  `Processor.StepInstruction()`, potem `foreach (var d in Devices) d.Tick(cyklyZuzyteWTymKroku)`
  (personal-001 wzorzec, patrz `architecture.md` sekcja "Maszyna").
- **Impact-check obowiazkowy** (`impact({target:"Cpu6502", direction:"upstream"})`,
  `impact({target:"IMachine", ...})`) przed dotknieciem tych symboli — `IMachine` juz raz
  rozszerzony w tej sesji (risk LOW, 1 implementacja testowa), ale stan mogl sie zmienic.

## Krok 2 — `Cpu6502 : IProcessor`

`Cpu6502` (namespace `Cpu6502`) juz ma kazdy czlonek `IProcessor` potrzebuje (`Halted`,
`CycleCount`, `InstructionCount`, `Reset()`, `StepInstruction()`, `SetIRQ`, `SetNMI`) — brakuje tylko
deklaracji interfejsu. Zmiana: dodac `using PetEmulator.Core;` + `: IProcessor` do
`Cpu6502.Properties.cs` (partial class declaration). Zero zmiany zachowania, tylko widocznosc
kontraktu. **Mimo to** — to edycja istniejacej klasy, `impact()` przed commitem obowiazkowy per
CLAUDE.md gate.

## Krok 3 — rejestry dla debuggera: `IDebuggableProcessor`

`IProcessor` celowo nie ma PC/A/X/Y (CPU-agnostyczny kontrakt, zgodnie z `architecture.md`: "Nie
umieszczac w Core `Cpu6502State`"). Ale Debugger CLI juz teraz podmienil `break-pc` na
`break-cycle`/`break-instruction-count` wlasnie dlatego, ze PC nie jest dostepne generycznie — to
prowizorka do naprawienia tutaj.

Rozwiazanie zgodne z `architecture.md` ("opcjonalny dostep po nazwie dla debuggera"): nowy,
**opcjonalny** interfejs w `PetEmulator.Core`:

```csharp
namespace PetEmulator.Core;

/// <summary>Optional capability: a processor that exposes its architectural registers by name,
/// for debug tooling. Not every IProcessor needs to implement this.</summary>
public interface IDebuggableProcessor
{
    IReadOnlyDictionary<string, ulong> GetRegisters(); // e.g. "PC" -> 0xC000, "A" -> 0x00
}
```

`Cpu6502` implementuje to przez istniejacy `GetState()`/`CpuState` (mapowanie pol na dict, bez
wystawiania `CpuState` samego w sobie do Core). Debug API/Debugger CLI robia
`machine.Processor is IDebuggableProcessor dbg ? dbg.GetRegisters() : null` — generyczne narzedzia
nie psuja sie na procesorach bez tej zdolnosci (np. przyszly Z80), a `Cpu6502` dostaje prawdziwy
`break-pc`, PC/rejestry w kazdej linii trace.

## Krok 4 — watch na adresach chipow

`watch`/`watch-range` w `MachineDebugger` juz dziala na `IMemoryBus.Read` — wystarczy, ze
`PetMachine.Memory` (krok 1) poprawnie routuje adresy PIA/VIA/CRTC do ich `BaseAddress`-owanych
`Read`. Zero dodatkowej pracy w warstwie debug — to konsekwencja poprawnego dekodera, nie nowy
mechanizm.

## Krok 5 — IEEE-488 / tape: monitoring protokolu, nie tylko bajtow

`watch` na pamieci nie pokaze *dlaczego* PIA2 zmienilo wartosc — IEEE-488 to protokol (ATN/DAV/NRFD
linie), nie pojedynczy adres. Zaproponowane rozszerzenie (osobny krok, po ustabilizowaniu 1-4):

- `PetIeeeBus`/`PetIeeeBusBinding` (juz zaimportowane) dostaja opcjonalny
  `event Action<IeeeBusEvent>? Activity` (nazwa robocza) emitowany przy zmianie stanu linii
  sterujacych lub transferze bajtu.
- Debug API dostaje `GET /ieee/activity` (ring-buffer ostatnich N zdarzen, polling — bez WebSocket,
  zgodnie z decyzja z importu Debug API o pomijaniu `/ws` na razie).
- `PetDatasette` (Tape) — podobnie, zdarzenie na start/stop silnika i na kazdy zdekodowany impuls,
  do diagnozy problemow z ladowaniem `.tap`.

To NIE jest wymagane, zeby `watch`/`dump`/`trace` dzialaly na prawdziwym PET — to rozszerzenie dla
diagnozowania samego protokolu IEEE-488/tape, warte zrobienia dopiero gdy krok 1-4 stoi i faktycznie
ktos debuguje LOAD/SAVE.

## Co NIE robic (per `architecture.md` "Czego nie kopiowac")

- Nie wystawiac `Cpu6502State`/`Cpu6502Registers` bezposrednio w `PetEmulator.Core` — tylko przez
  `IDebuggableProcessor.GetRegisters()` (name-keyed, opaque dla Core).
- Nie dublowac `Dispatcher.UIThread` ani zadnego frontend-specific mechanizmu w warstwie debug — Debug
  API juz to naprawil (`Task.Run` + lock), IEEE-488 event hook (krok 5) ma isc tym samym torem.
- Nie budowac WebSocket/push-owego API zanim ktos faktycznie potrzebuje live-streamu zamiast
  pollingu — `personal-002` mial `/ws`, tutaj celowo pominiete przy imporcie Debug API.

## Kolejnosc pracy (skrocona)

1. `PetMachine` + impact-check na `Cpu6502`/`IMachine`.
2. `Cpu6502 : IProcessor` (1-linijkowa zmiana, ale gated).
3. `IDebuggableProcessor` w Core + implementacja w `Cpu6502`.
4. Podlaczenie `PetEmulator.DebugApi`/`PetEmulator.Debugger` do prawdziwego `PetMachine` (integration
   testy zamiast fake `IMachine`).
5. (Opcjonalnie, pozniej) IEEE-488/tape activity events.

Kazdy krok konczy sie `detect_changes({scope:"all"})` przed commitem, per CLAUDE.md gate.
