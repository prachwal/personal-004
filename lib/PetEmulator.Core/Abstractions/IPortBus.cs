namespace PetEmulator.Core;

/// <summary>Optional port-space bus used by processors with I/O instructions.</summary>
public interface IPortBus
{
    byte Read(ushort port);

    void Write(ushort port, byte value);
}
