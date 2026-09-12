namespace PetEmulator.CpuZ80.Bus;

public interface IMemoryBus
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
}
