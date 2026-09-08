namespace PetEmulator.Core;

/// <summary>Tracks emulated time independently from wall-clock scheduling.</summary>
public interface IClock
{
    ulong CycleCount { get; }

    void Reset();

    void Advance(ulong cycles);
}
