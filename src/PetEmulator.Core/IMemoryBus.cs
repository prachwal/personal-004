namespace PetEmulator.Core;

/// <summary>Provides addressable byte access for a processor or device.</summary>
public interface IMemoryBus
{
    byte Read(ushort address);

    void Write(ushort address, byte value);
}
