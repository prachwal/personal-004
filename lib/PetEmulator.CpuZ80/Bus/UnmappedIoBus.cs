namespace PetEmulator.CpuZ80.Bus;

public sealed class UnmappedIoBus : IIoBus
{
    public byte Read(byte port) => 0xFF;

    public void Write(byte port, byte value)
    {
    }
}
