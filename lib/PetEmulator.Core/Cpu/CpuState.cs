namespace PetEmulator.Core;

/// <summary>
/// Common lifecycle state owned by an emulated processor.
/// Architectural registers remain in the concrete state type for each CPU family.
/// </summary>
public abstract class CpuState
{
    /// <summary>Indicates that the processor is currently halted.</summary>
    public bool Halted { get; set; }

    /// <summary>Resets common lifecycle state and is extended by concrete CPU states.</summary>
    public virtual void Reset()
    {
        Halted = false;
    }

    /// <summary>Returns the family-specific architectural register view for debugging.</summary>
    public abstract IReadOnlyDictionary<string, ulong> GetRegisters();
}
