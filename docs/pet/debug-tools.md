# PET debug tools

What's available for diagnosing a problem on a real, booted `PetMachine` - what each tool sees,
what it doesn't, and how they compose. Written after the large-file LOAD stall investigation (see
`docs/pet/disk-testing-strategy.md`), which used an ad-hoc version of most of this by hand; these
are that work made reusable.

None of this touches `lib/PetEmulator.Cpu6502/` - every tool here is built at the
`PetEmulator.Pet`/`PetEmulator.Debugger` level, composing with `IProcessor`/`IDebuggableProcessor`/
`IMemoryBus` from the outside. That's not an accident: this repo's `CLAUDE.md` routes CPU-core
edits through dedicated subagents, and none of these tools need the core to change to do their job.

## Layer map

```text
IDebuggableProcessor.GetRegisters()   <- PC/A/X/Y/SP/P by name, optional capability
PetMemoryBus.Observer                 <- every real Read/Write, address+value
PetMachine.BusObserver                <- same, exposed at the machine level
PetIeeeBus.Activity                   <- line-change/byte events, IEEE-488 protocol level
        |
        v
InstructionTracer                     <- composes GetRegisters() + BusObserver into a PC+bus ring buffer
MachineDebugger (trace/break-pc/...)  <- composes GetRegisters() into a REPL command
PetMachine.RunUntilOrStalled          <- composes any progress signal (e.g. IeeeByteTransferCount) into stall detection
PetDebuggerSession (trace-log/...)    <- scripts InstructionTracer/RunUntilOrStalled from a command line
```

## `IDebuggableProcessor`

`lib/PetEmulator.Core/IDebuggableProcessor.cs`. Optional capability an `IProcessor` can implement:

```csharp
public interface IDebuggableProcessor
{
    IReadOnlyDictionary<string, ulong> GetRegisters(); // "PC", "A", "X", "Y", "SP", "P"
}
```

`Cpu6502` (and everything derived from it, including `Cpu6502Classic` - what `PetMachine` actually
runs) implements it. Every other tool below that needs PC/registers checks
`machine.Processor is IDebuggableProcessor dbg` rather than assuming it - a future CPU-agnostic
`IProcessor` just gets those tools' PC-dependent features quietly unavailable, not a crash.

## `PetMachine.BusObserver` / `PetMemoryBus.Observer`

`src/PetEmulator.Pet/PetMemoryBus.cs`, exposed on `PetMachine`. Fires
`Action<BusAccess>` (`BusAccess(bool IsWrite, ushort Address, byte Value)`) on every real
`Read`/`Write` the bus serves - RAM, ROM, or a chip register. Nullable, zero-cost when unset.
**Setting it replaces whatever was there before** - it's a plain property, not an event. Anything
that attaches on top of an already-in-use machine (e.g. `PetMachine`'s own constructor wires one up
for the disk-activity LED) must read the old value first and chain to it, or it silently breaks
whatever was already listening. `InstructionTracer` does this; write your own only if none of the
tools below fit.

## `PetIeeeBus.Activity`

`src/PetEmulator.Pet/Ieee488/PetIeeeBus.cs`. `event Action<IeeeBusActivity>?` - fires for every
control-line change (`Kind="line-change"`, e.g. `"ATN=true"`) and every byte transferred
(`Kind="byte"`, e.g. `"0x41 written"`). This is the *protocol* view IEEE-488 debugging actually
needs - a bus-level `Read`/`Write` at $E820/$E821 tells you a PIA2 register changed, not *why*
(LISTEN vs. a data byte vs. a handshake release). `PetMachine.IeeeByteTransferCount` (below) is
built on this, not on `BusObserver`.

## `InstructionTracer`

`src/PetEmulator.Pet/Diagnostics/InstructionTracer.cs`. A ring buffer of the last N instructions,
each with the PC it started at and every bus access it caused:

```csharp
var tracer = new InstructionTracer(machine); // capacity: 4,000 by default
tracer.Run(8_000);                           // steps the machine itself - see why, below
File.WriteAllText("trace.log", tracer.Render());
tracer.Detach();                             // restores whatever BusObserver was set before
```

```csharp
public readonly record struct TracedInstruction(long Index, ushort PC, IReadOnlyList<BusAccess> BusAccesses)
{
    public bool IsInterruptVectorFetch => /* a read at $FFFA/B, $FFFC/D, or $FFFE/F */;
}
```

- **Owns the step loop.** It needs to read PC *before* each instruction and `BusObserver` only
  fires *during* `StepInstruction()` - there's no way to attribute bus accesses to the instruction
  that caused them from outside a wrapper that controls stepping. Use `tracer.Run(n)` /
  `tracer.StepInstruction()` instead of `machine.Run(n)` while a tracer is attached.
- **Chains, not replaces**, whatever `BusObserver` was already set (see above) - safe to attach on
  a machine already wired for the disk-activity LED or anything else. `Detach()` restores it
  exactly; idempotent, safe in a `finally`.
- **`IsInterruptVectorFetch`** flags a real, CPU-generic 6502 signal (a read at any of the six
  vector-table addresses) with no ROM/IRQ-timing knowledge needed - the cheap way to spot "an ISR
  just started here" in a trace. It doesn't (can't, without touching the CPU core) tell you when
  the ISR *ends* - look for the next vector fetch, or bracket the region you care about with
  `break-pc` at a known RTI-adjacent address instead.
- This is the tool that replaces the one-off scratch script used to root-cause the large-file LOAD
  stall (`docs/pet/disk-testing-strategy.md`) - same technique, now reusable instead of
  hand-written per bug.

## `PetMachine.IeeeByteTransferCount`

`src/PetEmulator.Pet/PetMachine.cs`. A cumulative counter of every `Activity` event with
`Kind == "byte"` since the machine was created - unlike `PollDiskActivity()` (which latches and
clears for a GUI's blink-once-per-poll LED), this only ever goes up. A natural "is the transfer
actually still moving" progress signal.

## `PetMachine.RunUntilOrStalled`

Same shape as the existing `RunUntil(condition, maxInstructions)`, plus a `progress` probe and a
`stallWindow`:

```csharp
public StallCheckResult RunUntilOrStalled(
    Func<IMemoryBus, bool> condition,
    Func<long> progress,
    ulong maxInstructions,
    ulong stallWindow = 50_000);

public readonly record struct StallCheckResult(bool ConditionMet, bool Stalled, ulong InstructionsRun);
```

Steps until `condition` is true (normal success, same as `RunUntil`), **or** `progress()` hasn't
changed for `stallWindow` instructions (`Stalled = true`, returns immediately - no need to run all
the way to `maxInstructions` to find out something stopped moving), or `maxInstructions` is
reached with neither. `progress` is caller-supplied on purpose - `IeeeByteTransferCount` is the
obvious choice for a LOAD/SAVE investigation, but any monotonic-ish counter works (a keyboard
buffer depth, a screen-scroll counter, anything relevant to a different stuck-transfer problem).

This replaces the manual bisection the LOAD stall took to find (halve the instruction budget,
rerun, eyeball a trace, repeat) with one call that reports where and whether it actually stalled.

## `MachineDebugger` additions: PC/register trace line, `break-pc`

`lib/PetEmulator.Debugger/MachineDebugger.cs`. `trace` now prints a second line per step when the
processor is debuggable:

```text
[0] cycles=1 instructions=1 halted=False
[0]   PC=C001 A=00 X=00 Y=00 SP=FD P=24
```

(silently omitted for a plain `IProcessor` with no `IDebuggableProcessor`). `break-pc <hex>` stops
`trace` once PC reaches the target - on a non-debuggable processor it returns an explicit
`"error: break-pc requires an IDebuggableProcessor"` instead of silently never firing.

## `PetDebuggerSession` commands

`src/PetEmulator.Cli/PetDebuggerSession.cs` - scripts the above from a `debug` script or CLI line
(see that class's doc comment for the full command list). Two new ones:

```text
trace-log <count> <path>              # InstructionTracer.Run(count) then writes Render() to path
superpet-diagnose [count]              # Reset Waterloo 6809, report ROM ranges/vector and trace startup
disk-stall-check <max> [stallWindow]  # RunUntilOrStalled against IeeeByteTransferCount, default window 50,000
```

Example - reproduce a stuck LOAD headlessly and get a trace file without writing a throwaway test:

```text
profile pet-2001-32
roms roms/pet
disk roms/pet/test-disks/games-1.d64 8
type LOAD"HELLO",8␊
disk-stall-check 3000000 50000
trace-log 8000 /tmp/load-trace.log
```

## Walidacja procesora 6809

Test binarny `tests/PetEmulator.Cpu6809.Tests/TestData/schwotzer-cputest.bin` pochodzi ze źródła
`MC6809 CPU Emulation Validation` W. Schwotzera, dostępnego w repozytorium flexemu:
`https://github.com/aladur/flexemu/blob/master/src/tools/cputest.txt`.

- obraz jest surowym programem ładowanym od `$8100`;
- test zastępuje procedury FLEX `PSTRNG`, `PUTCHR` i `WARMS` minimalnymi atrapami;
- sukces oznacza powrót przez `WARMS` oraz `ERRFLG == 0`;
- test uruchamia się razem z `PetEmulator.Cpu6809.Tests` i obejmuje również ścieżki EXG,
  DAA, SEX, LBSR oraz indeksowanie `[n16]`.

Źródło zostało złożone do formatu raw asemblerem LWTOOLS `lwasm` w trybie 6809. Zmiana obrazu
lub assemblera wymaga ponownego sprawdzenia adresu ładowania i oczekiwanego wektora powrotu.

## What's still missing

- **No ISR-end detection.** `IsInterruptVectorFetch` marks entry; there's no generic (CPU-core-free)
  way to mark the matching RTI. Bracket manually with `break-pc` at a known return address, or add
  one when a second investigation actually needs it (YAGNI for now - one investigation used a
  fixed instruction-count guess and that was enough).
- **No wall-clock/perf profiler.** Nothing here measures real time cost of a change (e.g. how much
  slower a larger `PetIeeeBus.SettleDelayCycles` makes a real transfer) beyond eyeballing a test
  run's duration.
