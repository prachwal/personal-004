namespace PetEmulator.Core;

/// <summary>
/// Coordinates a complete emulated computer, including its processor and devices.
/// </summary>
/// <remarks>
/// A machine step is intentionally larger than a CPU step: it advances the CPU and
/// all hardware connected to its bus. This keeps frontends independent of the CPU
/// implementation and leaves room for machines with different processors.
/// </remarks>
public interface IMachine
{
    /// <summary>The stable machine identifier used by hosts and frontends.</summary>
    string Name { get; }

    /// <summary>Whether the machine has everything required to execute.</summary>
    bool IsReady { get; }

    /// <summary>The total number of emulated clock cycles.</summary>
    ulong CycleCount { get; }

    /// <summary>Restores the machine and all devices to their reset state.</summary>
    void Reset();

    /// <summary>Executes one complete instruction and advances connected devices.</summary>
    void StepInstruction();

    /// <summary>Executes up to <paramref name="instructionCount"/> instructions.</summary>
    void Run(ulong instructionCount);
}
