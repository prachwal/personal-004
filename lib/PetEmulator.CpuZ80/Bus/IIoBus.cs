namespace PetEmulator.CpuZ80.Bus;

public interface IIoBus
{
    byte Read(byte port);
    byte Read(ushort port) => Read((byte)port);
    void Write(byte port, byte value);
    void Write(ushort port, byte value) => Write((byte)port, value);
}
