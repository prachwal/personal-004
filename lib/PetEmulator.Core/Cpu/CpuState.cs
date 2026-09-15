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

    /// <summary>See <see cref="IDebuggableProcessor.EightBitRegisterNames"/>. Empty by default -
    /// override when <see cref="GetRegisters"/> mixes 8-bit and 16-bit registers.</summary>
    public virtual IReadOnlyCollection<string> EightBitRegisterNames => [];

    /// <summary>Captures architectural state for debugging or time-travel integrations.</summary>
    public virtual CpuStateSnapshot CaptureSnapshot()
        => new(new Dictionary<string, ulong>(GetRegisters()), Halted);

    /// <summary>
    /// Restores architectural state. CPU families override this when they support restore.
    /// </summary>
    public virtual void RestoreSnapshot(CpuStateSnapshot snapshot)
        => throw new NotSupportedException($"{GetType().Name} does not support state restore.");
}
