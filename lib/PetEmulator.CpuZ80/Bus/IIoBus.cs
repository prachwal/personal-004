using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Bus;

public interface IIoBus : IPortBus
{
    byte Read(byte port);
    new byte Read(ushort port) => Read((byte)port);
    byte IPortBus.Read(ushort port) => ((IIoBus)this).Read(port);
    void Write(byte port, byte value);
    new void Write(ushort port, byte value) => Write((byte)port, value);
    void IPortBus.Write(ushort port, byte value) => ((IIoBus)this).Write(port, value);
}
