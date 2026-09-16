namespace PetEmulator.Core;

/// <summary>Common versioned state shared by every concrete machine snapshot.</summary>
public abstract class MachineSnapshot : IMachineSnapshot
{
    public int Version { get; set; } = 1;

    public CpuDebugSnapshot Cpu { get; set; } = null!;
}
