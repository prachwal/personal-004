using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;

namespace PetEmulator.Trs80;

public sealed class Trs80MemoryBus : IBus, IMemoryBus
{
    private readonly byte[] _rom = new byte[Trs80MemoryMap.RomEnd];
    private readonly byte[] _ram = new byte[ushort.MaxValue + 1];
    public Trs80MemoryBus(Trs80KeyboardMatrix keyboard, Trs80Printer printer, Trs80FdcWiring? fdc = null, Trs80CassettePlayer? cassette = null)
    { Keyboard = keyboard; Printer = printer; Fdc = fdc; Cassette = cassette; }
    public Trs80KeyboardMatrix Keyboard { get; }
    public Trs80Printer Printer { get; }
    // Settable so Trs80Machine can lazily attach a controller/deck after construction (hot-swap
    // disk/tape without rebuilding the whole machine) - see Trs80Machine.InsertDisk/LoadTape.
    public Trs80FdcWiring? Fdc { get; set; }
    public Trs80CassettePlayer? Cassette { get; set; }
    public Trs80MemorySnapshot CaptureState() => new() { Ram = _ram.ToArray() };

    public void RestoreState(Trs80MemorySnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Ram.Length != _ram.Length)
            throw new ArgumentException("Invalid TRS-80 RAM size.", nameof(state));
        state.Ram.CopyTo(_ram, 0);
    }
    public byte Read(ushort address) => address < Trs80MemoryMap.RomEnd ? _rom[address] :
        address is >= Trs80MemoryMap.PrinterStart and <= Trs80MemoryMap.PrinterEnd ? Printer.Read(address) :
        address is >= Trs80MemoryMap.FdcStart and <= Trs80MemoryMap.FdcEnd && Fdc is not null ? Fdc.Read(address) :
        address is >= Trs80MemoryMap.KeyboardStart and <= Trs80MemoryMap.KeyboardEnd ? Keyboard.Read(address) : _ram[address];
    public void Write(ushort address, byte value)
    { if (address < Trs80MemoryMap.RomEnd) return; if (address == Trs80MemoryMap.PrinterStart) { Printer.Write(address, value); return; } if (address is >= Trs80MemoryMap.FdcStart and <= Trs80MemoryMap.FdcEnd && Fdc is not null) { Fdc.Write(address, value); return; } if (address is >= Trs80MemoryMap.KeyboardStart and <= Trs80MemoryMap.KeyboardEnd) return; _ram[address] = value; }
    public byte ReadMemory(ushort address) => Read(address);
    public void WriteMemory(ushort address, byte value) => Write(address, value);
    public byte ReadPort(byte port) => port == Trs80MemoryMap.CassettePort && Cassette is not null ? Cassette.Read(port) : (byte)0xFF;
    public byte ReadPort(ushort port) => ReadPort((byte)port);
    public void WritePort(byte port, byte value) { if (port == Trs80MemoryMap.CassettePort) Cassette?.Write(port, value); }
    public void WritePort(ushort port, byte value) => WritePort((byte)port, value);
    public void LoadRom(ReadOnlySpan<byte> rom) { if (rom.Length != _rom.Length) throw new ArgumentException($"TRS-80 Model I ROM must be {_rom.Length} bytes.", nameof(rom)); rom.CopyTo(_rom); }
    public void Tick(int tStates) { Fdc?.Tick(tStates); Cassette?.Tick(tStates); }
    // CPU reset preserves RAM and inserted media on the real machine.
    public void Reset() { }
    public byte AcknowledgeInterrupt() => 0xFF;
}
