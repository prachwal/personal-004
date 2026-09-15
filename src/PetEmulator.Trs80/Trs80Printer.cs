using PetEmulator.Core;

namespace PetEmulator.Trs80;

public sealed class Trs80Printer : IMemoryBus
{
    public const byte ReadyStatus = 0x30;
    public IReadOnlyList<byte> Output => _output;
    public byte Status { get; set; } = ReadyStatus;
    private readonly List<byte> _output = [];
    public byte Read(ushort address) => address == Trs80MemoryMap.PrinterStart ? Status : (byte)0xFF;
    public void Write(ushort address, byte value) { if (address == Trs80MemoryMap.PrinterStart) _output.Add(value); }
}
