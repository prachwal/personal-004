namespace PetEmulator.Core;

/// <summary>Snapshot exposed to debugger and monitoring integrations.</summary>
public sealed record CpuDebugSnapshot(
    CpuStateSnapshot State,
    ulong CycleCount,
    ulong InstructionCount)
{
    public IReadOnlyDictionary<string, ulong> Registers => State.Registers;

    public bool Halted => State.Halted;
}
