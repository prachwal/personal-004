using PetEmulator.CpuZ80.Bus;

namespace PetEmulator.CpuZ80.Memory;

public sealed class RamMemory : IMemoryBus
{
    private readonly byte[] data;

    public RamMemory(int size = ushort.MaxValue + 1)
    {
        if (size != ushort.MaxValue + 1)
            throw new ArgumentOutOfRangeException(nameof(size), "The Z80 address space is exactly 64 KB.");

        data = new byte[size];
    }

    public byte Read(ushort address) => data[address];

    public void Write(ushort address, byte value) => data[address] = value;
}
