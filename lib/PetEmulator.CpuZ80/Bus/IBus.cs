namespace PetEmulator.CpuZ80.Bus;

public interface IBus
{
    byte ReadMemory(ushort address);
    byte ReadOpcode(ushort address) => ReadMemory(address);
    void WriteMemory(ushort address, byte value);
    byte ReadPort(byte port);
    byte ReadPort(ushort port) => ReadPort((byte)port);
    void WritePort(byte port, byte value);
    void WritePort(ushort port, byte value) => WritePort((byte)port, value);

    // The default retains the original single-port interrupt response contract.
    byte AcknowledgeInterrupt() => ReadPort(0);

    /// <summary>Signals the Z80 RETI boundary to interrupt daisy-chain devices.</summary>
    void NotifyInterruptReturn() { }
}
