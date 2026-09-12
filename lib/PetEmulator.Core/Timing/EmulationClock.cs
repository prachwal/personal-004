namespace PetEmulator.Core;

/// <summary>Default monotonic emulation clock used by processors.</summary>
public sealed class EmulationClock : IClock
{
    public ulong CycleCount { get; private set; }

    public void Reset() => CycleCount = 0;

    public void Advance(ulong cycles) => CycleCount = checked(CycleCount + cycles);
}
