using PetEmulator.Core;

namespace PetEmulator.Desktop.Infrastructure;

public sealed class FlatMemoryBus(int size) : IMemoryBus
{
    private readonly byte[] _ram = new byte[size];

    public byte Read(ushort address) => _ram[address];

    public void Write(ushort address, byte value) => _ram[address] = value;
}
