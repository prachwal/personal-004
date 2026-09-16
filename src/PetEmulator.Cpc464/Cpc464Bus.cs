using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Interrupts;
using PetEmulator.Cpc;

namespace PetEmulator.Cpc464;

public sealed class Cpc464Bus : IBus, IMemoryBus
{
    public const int RomSize = 0x8000;
    private readonly byte[] _ram = new byte[0x10000];
    private readonly byte[] _rom;
    public Cpc464Bus(ReadOnlySpan<byte> rom)
    {
        if (rom.Length != RomSize) throw new ArgumentException("CPC464 ROM must be exactly 32 KB.", nameof(rom));
        _rom = rom.ToArray();
        Crtc = new MT6545("CPC CRTC", 0xBC00);
        GateArray = new CpcGateArray(Crtc, ReadRam);
        Ay = new Ay38910();
        Keyboard = new Cpc464Keyboard();
        Cassette = new Cpc464Cassette();
        InterruptLines = new InterruptLines();
    }
    public MT6545 Crtc { get; }
    public CpcGateArray GateArray { get; }
    public Ay38910 Ay { get; }
    public Cpc464Keyboard Keyboard { get; }
    public Cpc464Cassette Cassette { get; }
    public InterruptLines InterruptLines { get; }

    public Cpc464MemorySnapshot CaptureMemoryState() => new() { Ram = _ram.ToArray() };
    public void RestoreMemoryState(Cpc464MemorySnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Ram.Length != _ram.Length) throw new ArgumentException("CPC464 RAM snapshot has an invalid size.", nameof(state));
        state.Ram.AsSpan().CopyTo(_ram);
    }

    public Cpc464PortsSnapshot CapturePortsState() => new() { PortA = _portA, PortB = _portB, PortC = _portC, PpiControl = _ppiControl };
    public void RestorePortsState(Cpc464PortsSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state); _portA = state.PortA; _portB = state.PortB; _portC = state.PortC; _ppiControl = state.PpiControl;
    }
    private byte _portA, _portB, _portC, _ppiControl = 0x9B;
    private bool PortAInput => (_ppiControl & 0x10) != 0;
    private bool PortBInput => (_ppiControl & 2) != 0;
    private bool PortCUpperInput => (_ppiControl & 8) != 0;
    private bool PortCLowerInput => (_ppiControl & 1) != 0;
    public byte Read(ushort address) => ReadMemory(address);
    public void Write(ushort address, byte value) => WriteMemory(address, value);
    public byte ReadMemory(ushort address) => address < 0x4000 && GateArray.LowerRomEnabled ? _rom[address] : address >= 0xC000 && GateArray.UpperRomEnabled ? _rom[0x4000 + address - 0xC000] : _ram[address];
    public byte ReadRam(ushort address) => _ram[address];
    public void WriteMemory(ushort address, byte value) => _ram[address] = value;
    public byte ReadPort(byte port) => ReadPort((ushort)port);
    public byte ReadPort(ushort port) => (byte)(port >> 8) switch
    {
        0xBC => Crtc.Read(0xBC00), 0xBD => Crtc.Read(0xBC01),
        0xF4 => PortAInput ? ReadPsg() : _portA,
        0xF5 => PortBInput ? (byte)(0x1E | (Crtc.VSync ? 1 : 0) | (Cassette.ReadSignal() ? 0x80 : 0)) : _portB,
        0xF6 => (byte)((_portC & ~(byte)((PortCUpperInput ? 0xF0 : 0) | (PortCLowerInput ? 0x0F : 0))) | ((PortCUpperInput ? 0xF0 : 0) | (PortCLowerInput ? 0x0F : 0))),
        _ => 0xFF
    };
    public void WritePort(byte port, byte value) => WritePort((ushort)port, value);
    public void WritePort(ushort port, byte value)
    {
        switch ((byte)(port >> 8))
        {
            case 0x7F: GateArray.WritePort(port, value); break;
            case 0xBC: Crtc.Write(0xBC00, value); break;
            case 0xBD: Crtc.Write(0xBC01, value); break;
            case 0xF4 when !PortAInput: _portA = value; break;
            case 0xF5 when !PortBInput: _portB = value; break;
            case 0xF6: WritePortC(value); break;
            case 0xF7: WriteControl(value); break;
        }
    }
    public byte AcknowledgeInterrupt() { GateArray.AcknowledgeInterrupt(); return 0xFF; }
    public void Tick(int cycles)
    {
        for (var i = 0; i < cycles / 4; i++) { GateArray.Tick(); Cassette.Tick(); }
        InterruptLines.SetInt(GateArray.InterruptPending);
    }
    public void Reset() { Array.Clear(_ram); _portA = _portB = _portC = 0; _ppiControl = 0x9B; GateArray.Reset(); Crtc.Reset(); Ay.Reset(); Keyboard.Reset(); Cassette.Reset(); InterruptLines.Clear(); }
    private void WriteControl(byte value) { if ((value & 0x80) != 0) { _ppiControl = value; return; } var bit = (value >> 1) & 7; if ((value & 1) != 0) _portC |= (byte)(1 << bit); else _portC &= (byte)~(1 << bit); ApplyCassetteControl(); ApplyPsg(); }
    private void WritePortC(byte value) { var writable = (byte)((PortCUpperInput ? 0 : 0xF0) | (PortCLowerInput ? 0 : 0x0F)); _portC = (byte)((_portC & ~writable) | (value & writable)); ApplyCassetteControl(); ApplyPsg(); }
    private void ApplyCassetteControl() { Cassette.SetMotor((_portC & 0x10) != 0); Cassette.WriteData((_portC & 0x20) != 0); }
    private void ApplyPsg() { if (PortCUpperInput) return; switch ((_portC >> 6) & 3) { case 2: Ay.WritePort(0xA1, _portA); break; case 3: Ay.WritePort(0xA0, _portA); break; } }
    private byte ReadPsg() => ((_portC >> 6) & 3) == 1 ? (_portA == 14 ? Keyboard.ReadRow((byte)(_portC & 0x0F)) : Ay.ReadSelected()) : (byte)0xFF;
}
