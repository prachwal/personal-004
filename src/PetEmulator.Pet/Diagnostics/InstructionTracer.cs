using PetEmulator.Core;

namespace PetEmulator.Pet.Diagnostics;

/// <summary>One instruction's worth of trace data: the PC it started at, and every real bus
/// access (<see cref="PetMemoryBus.Observer"/>) that instruction's execution caused. A vector
/// fetch (a read at $FFFA/$FFFB NMI, $FFFC/$FFFD RESET, or $FFFE/$FFFF IRQ/BRK - a real, generic
/// 6502 signal, not PET-specific) marks <see cref="IsInterruptVectorFetch"/>, the cheapest way to
/// spot "an ISR just started here" in a trace without touching CPU-core internals.</summary>
public readonly record struct TracedInstruction(long Index, ushort PC, IReadOnlyList<BusAccess> BusAccesses)
{
    public bool IsInterruptVectorFetch => BusAccesses.Any(a => !a.IsWrite && a.Address is 0xFFFA or 0xFFFB or 0xFFFC or 0xFFFD or 0xFFFE or 0xFFFF);
}

/// <summary>
/// Formalizes the ad-hoc PC+bus-access trace built by hand to root-cause the large-file LOAD
/// stall (see docs/pet/disk-testing-strategy.md) into a reusable tool: a ring buffer of the last
/// <paramref name="capacity"/> instructions, each with its PC (via the optional
/// <see cref="IDebuggableProcessor"/> capability) and every bus access it made (via
/// <see cref="PetMachine.BusObserver"/>). Owns the step loop itself - it needs to read PC
/// *before* each step, and <see cref="PetMachine"/>'s own bus event fires only *during* one, so
/// there's no way to attribute accesses to instructions from the outside.
///
/// Composes rather than replaces: chains onto whatever <see cref="PetMachine.BusObserver"/> was
/// already set (e.g. the disk-activity LED hookup in <see cref="PetMachine"/>'s constructor) and
/// restores it on <see cref="Detach"/>, so attaching a tracer to a machine already wired for
/// other diagnostics doesn't silently break them.
/// </summary>
public sealed class InstructionTracer
{
    private readonly PetMachine _machine;
    private readonly int _capacity;
    private readonly Action<BusAccess>? _previousObserver;
    private readonly Queue<TracedInstruction> _ring = [];
    private List<BusAccess> _current = [];
    private long _index;
    private bool _detached;

    public InstructionTracer(PetMachine machine, int capacity = 4_000)
    {
        ArgumentNullException.ThrowIfNull(machine);
        _machine = machine;
        _capacity = capacity;
        _previousObserver = machine.BusObserver;
        machine.BusObserver = access =>
        {
            _previousObserver?.Invoke(access);
            _current.Add(access);
        };
    }

    /// <summary>Every traced instruction still in the ring buffer, oldest first.</summary>
    public IReadOnlyList<TracedInstruction> Recent => [.. _ring];

    /// <summary>Steps exactly one instruction, recording its PC and bus accesses.</summary>
    public void StepInstruction()
    {
        var pc = _machine.Processor is IDebuggableProcessor dbg ? (ushort)dbg.GetRegisters()["PC"] : (ushort)0;
        _current = [];
        _machine.StepInstruction();
        _ring.Enqueue(new TracedInstruction(_index++, pc, _current));
        if (_ring.Count > _capacity)
            _ring.Dequeue();
    }

    public void Run(ulong instructionCount)
    {
        for (var i = 0UL; i < instructionCount; i++)
            StepInstruction();
    }

    /// <summary>Restores whatever <see cref="PetMachine.BusObserver"/> was set before this tracer
    /// attached. Idempotent - safe to call more than once (e.g. from a <c>finally</c> after an
    /// earlier failed attach).</summary>
    public void Detach()
    {
        if (_detached)
            return;
        _detached = true;
        _machine.BusObserver = _previousObserver;
    }

    /// <summary>Renders the buffered trace as text, one line per instruction plus one indented
    /// line per bus access - the exact format used for the hand-written
    /// read-procedure-trace.log this tool replaces. A caller writes the result to disk with
    /// <see cref="File.WriteAllText(string, string?)"/> when a persistent artifact is wanted.</summary>
    public string Render()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var instr in Recent)
        {
            sb.Append($"[{instr.Index}] PC={instr.PC:X4}");
            if (instr.IsInterruptVectorFetch)
                sb.Append(" (interrupt vector fetch)");
            sb.AppendLine();
            foreach (var access in instr.BusAccesses)
                sb.AppendLine($"    {(access.IsWrite ? "W" : "R")} ${access.Address:X4}=${access.Value:X2}");
        }

        return sb.ToString();
    }
}
