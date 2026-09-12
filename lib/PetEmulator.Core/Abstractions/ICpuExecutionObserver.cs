namespace PetEmulator.Core;

/// <summary>
/// Optional debugger/monitoring integration for a processor.
/// Breakpoint and watchpoint policy belongs to the observer, not to CPU families.
/// </summary>
public interface ICpuExecutionObserver
{
    /// <summary>Returns true when the next step should stop before execution.</summary>
    bool ShouldBreak(CpuDebugSnapshot snapshot);

    /// <summary>Returns true when this memory access should mark the current step as a watchpoint hit.</summary>
    bool ShouldBreakOnMemoryAccess(BusAccess access);

    /// <summary>Receives a completed instruction, interrupt, wait or breakpoint step.</summary>
    void OnStepCompleted(CpuStepTrace trace);

    /// <summary>Receives an exception raised while executing a step.</summary>
    void OnStepFailed(CpuDebugSnapshot snapshot, Exception exception);
}
