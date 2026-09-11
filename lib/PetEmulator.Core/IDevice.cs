namespace PetEmulator.Core;

/// <summary>Lifecycle contract for hardware attached to a machine.</summary>
public interface IDevice
{
    string Name { get; }

    void Reset();

    void Tick(ulong cycles);
}
