using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;

namespace PetEmulator.CpuZ80.Cpu;

internal sealed class Z80MemoryBusAdapter(IBus bus) : IMemoryBus
{
    public byte Read(ushort address) => bus.ReadMemory(address);

    public void Write(ushort address, byte value) => bus.WriteMemory(address, value);
}

internal sealed class Z80PortBusAdapter(IBus bus) : IPortBus
{
    public byte Read(ushort port) => bus.ReadPort(port);

    public void Write(ushort port, byte value) => bus.WritePort(port, value);
}
