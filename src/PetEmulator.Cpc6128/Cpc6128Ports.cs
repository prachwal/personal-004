using PetEmulator.Chips;
using PetEmulator.Cpc464;
using PetEmulator.CpcFdc;

namespace PetEmulator.Cpc6128;

/// <summary>CPC6128 port decoder. FDC ports remain intentionally unimplemented until M5.</summary>
public sealed class Cpc6128Ports(Cpc464GateArray gateArray, MT6545 crtc, Ay38910 ay,
    Cpc464Keyboard keyboard, Cpc464Cassette cassette, I8272Chip fdc, Cpc6128MemoryBus memory)
{
    private byte _portA, _portB, _portC, _ppiControl = 0x9B;

    public byte Read(byte port) => Read((ushort)port);
    public byte Read(ushort port)
    {
        if (port == 0xFB7E) return fdc.ReadMainStatus();
        if (port == 0xFB7F) return fdc.ReadDataRegister();
        if ((port & 0xFF00) == 0xDF00) return memory.UpperRomNumber;
        return (byte)(port >> 8) switch
        {
            0xBC => crtc.Read(0xBC00),
            0xBD => crtc.Read(0xBC01),
            0xF4 => PortAInput ? ReadPsg() : _portA,
            0xF5 => PortBInput ? (byte)(0x1E | (crtc.VSync ? 1 : 0) | (cassette.ReadSignal() ? 0x80 : 0)) : _portB,
            0xF6 => ReadPortC(),
            _ => 0xFF,
        };
    }

    public void Write(byte port, byte value) => Write((ushort)port, value);
    public void Write(ushort port, byte value)
    {
        if (port == 0xFB7F) { fdc.WriteDataRegister(value); return; }
        if (port == 0xFA7E)
        {
            if (fdc.Drive0 is DskFloppyDrive drive0) drive0.MotorOn = (value & 1) != 0;
            if (fdc.Drive1 is DskFloppyDrive drive1) drive1.MotorOn = (value & 1) != 0;
            return;
        }
        if ((port & 0xFF00) == 0xDF00) { memory.SelectUpperRom(value); return; }
        switch ((byte)(port >> 8))
        {
            case 0x7F: gateArray.WritePort(port, value); break;
            case 0xBC: crtc.Write(0xBC00, value); break;
            case 0xBD: crtc.Write(0xBC01, value); break;
            case 0xF4 when !PortAInput: _portA = value; break;
            case 0xF5 when !PortBInput: _portB = value; break;
            case 0xF6: WritePortC(value); break;
            case 0xF7: WriteControl(value); break;
        }
    }

    public byte AcknowledgeInterrupt() { gateArray.AcknowledgeInterrupt(); return 0xFF; }

    public void Reset() => (_portA, _portB, _portC, _ppiControl) = (0, 0, 0, 0x9B);

    private bool PortAInput => (_ppiControl & 0x10) != 0;
    private bool PortBInput => (_ppiControl & 0x02) != 0;
    private bool PortCUpperInput => (_ppiControl & 0x08) != 0;
    private bool PortCLowerInput => (_ppiControl & 0x01) != 0;

    private void WriteControl(byte value)
    {
        if ((value & 0x80) != 0) { _ppiControl = value; return; }
        var bit = (value >> 1) & 7;
        if ((value & 1) != 0) _portC |= (byte)(1 << bit); else _portC &= (byte)~(1 << bit);
        cassette.SetMotor((_portC & 0x10) != 0);
        ApplyPsg();
    }

    private void WritePortC(byte value)
    {
        var writable = (byte)((PortCUpperInput ? 0 : 0xF0) | (PortCLowerInput ? 0 : 0x0F));
        _portC = (byte)((_portC & ~writable) | (value & writable));
        cassette.SetMotor((_portC & 0x10) != 0);
        ApplyPsg();
    }

    private byte ReadPortC()
    {
        var input = (byte)((PortCUpperInput ? 0xF0 : 0) | (PortCLowerInput ? 0x0F : 0));
        return (byte)((_portC & ~input) | input);
    }

    private void ApplyPsg()
    {
        if (PortCUpperInput) return;
        switch ((_portC >> 6) & 3)
        {
            case 2: ay.WritePort(0xA1, _portA); break;
            case 3: ay.WritePort(0xA0, _portA); break;
        }
    }

    private byte ReadPsg() => ((_portC >> 6) & 3) != 1
        ? (byte)0xFF : _portA == 14 ? keyboard.ReadRow((byte)(_portC & 0x0F)) : ay.ReadSelected();
}
