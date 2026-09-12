using PetEmulator.Core;

namespace PetEmulator.Cpu6809.Tests.M6809;

internal sealed class RamMemoryBus(int size) : IMemoryBus
{
    private readonly byte[] _memory = new byte[size];

    public byte Read(ushort address) => _memory[address];

    public void Write(ushort address, byte value) => _memory[address] = value;
}
