namespace PetEmulator.Core;

/// <summary>Describes the observable result of one processor step.</summary>
public readonly record struct CpuStepResult(
    ulong Cycles,
    bool InstructionCompleted,
    bool InterruptServiced = false,
    bool Waiting = false,
    bool BreakpointHit = false,
    bool WatchpointHit = false)
{
    public static CpuStepResult Completed(ulong cycles)
        => new(cycles, InstructionCompleted: true);

    public static CpuStepResult Idle(ulong cycles, bool waiting = false)
        => new(cycles, InstructionCompleted: false, Waiting: waiting);

    public static CpuStepResult Interrupt(ulong cycles)
        => new(cycles, InstructionCompleted: false, InterruptServiced: true);

    public static CpuStepResult Breakpoint()
        => new(0, InstructionCompleted: false, BreakpointHit: true);
}
