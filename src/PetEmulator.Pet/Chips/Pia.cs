using PetEmulator.Core;

namespace PetEmulator.Pet.Chips;

/// <summary>Four-register Motorola 6520/6821 Peripheral Interface Adapter.</summary>
public sealed class Pia : IMemoryMappedDevice
{
    private const byte DataRegisterSelect = 0x04;
    private const byte WritableControlBits = 0x3F;
    private readonly ushort _baseAddress;
    private byte _ddra;
    private byte _ddrb;
    private byte _ora;
    private byte _orb;
    private byte _cra;
    private byte _crb;
    private bool _ca1;
    private bool _ca2;
    private bool _cb1;
    private bool _cb2;
    private bool _ca1Flag;
    private bool _ca2Flag;
    private bool _cb1Flag;
    private bool _cb2Flag;

    public Pia(string name = "PIA", ushort baseAddress = 0)
    {
        Name = name;
        _baseAddress = baseAddress;
    }

    public string Name { get; }

    public uint Length => 4;

    public Func<byte>? PortAInput { get; set; }

    public Func<byte>? PortBInput { get; set; }

    public Action<byte>? PortAWritten { get; set; }

    public Action<byte>? PortBWritten { get; set; }

    public Action<byte>? ControlAWritten { get; set; }

    public Action<byte>? ControlBWritten { get; set; }

    public Action? PortARead { get; set; }

    public Action? PortBRead { get; set; }

    public Action<bool>? Ca2OutputChanged { get; set; }

    public Action<bool>? Cb2OutputChanged { get; set; }

    /// <summary>
    /// Overrides which control-register values select the data register (vs. the direction
    /// register) on port A/B, in place of the standard "bit 2 set" rule. Leave null for standard
    /// PIA behavior.
    /// </summary>
    public Func<byte, bool>? SelectPortADataRegister { get; set; }

    public Func<byte, bool>? SelectPortBDataRegister { get; set; }

    /// <summary>Whether CB2 is currently configured as an output (vs. input) by the control
    /// register. A device driving CB2 as a real-world signal (e.g. cassette motor control) needs
    /// this alongside <see cref="CB2"/>'s value: right after <see cref="Reset"/>, CB2 defaults to
    /// input mode with an arbitrary level, which is not the same as "configured output, value
    /// false" - callers should treat "not yet in output mode" as inactive/unknown, not as
    /// whatever <see cref="CB2"/> happens to currently read.</summary>
    public bool IsCb2Output => IsControl2Output(_crb);

    public byte DDRA => _ddra;

    public byte DDRB => _ddrb;

    public byte ORA => _ora;

    public byte ORB => _orb;

    public bool CA1
    {
        get => _ca1;
        set => SetControlInput(ref _ca1, value, IsRisingEdge(_cra), ref _ca1Flag);
    }

    public bool CA2
    {
        get => _ca2;
        set
        {
            if (!IsControl2Output(_cra))
                SetControlInput(ref _ca2, value, IsRisingEdge2(_cra), ref _ca2Flag);
        }
    }

    public bool CB1
    {
        get => _cb1;
        set => SetControlInput(ref _cb1, value, IsRisingEdge(_crb), ref _cb1Flag);
    }

    public bool CB2
    {
        get => _cb2;
        set
        {
            if (!IsControl2Output(_crb))
                SetControlInput(ref _cb2, value, IsRisingEdge2(_crb), ref _cb2Flag);
        }
    }

    public bool IRQA => (_ca1Flag && (_cra & 0x01) != 0) || (_ca2Flag && IsControl2InputInterruptEnabled(_cra));

    public bool IRQB => (_cb1Flag && (_crb & 0x01) != 0) || (_cb2Flag && IsControl2InputInterruptEnabled(_crb));

    public bool IRQ => IRQA || IRQB;

    public bool HasInterrupt => IRQ;

    public byte Read(ushort address)
    {
        int offset = address - _baseAddress;
        return offset switch
        {
            0 => ReadPortAOrDirection(),
            1 => ReadControl(_cra, _ca1Flag, _ca2Flag),
            2 => ReadPortBOrDirection(),
            3 => ReadControl(_crb, _cb1Flag, _cb2Flag),
            _ => throw new ArgumentOutOfRangeException(nameof(address))
        };
    }

    public void Write(ushort address, byte value)
    {
        int offset = address - _baseAddress;
        switch (offset)
        {
            case 0 when !IsPortADataSelected():
                _ddra = value;
                break;
            case 0:
                _ora = value;
                PortAWritten?.Invoke(value);
                break;
            case 1:
                _cra = (byte)(value & WritableControlBits);
                SetControl2Output(ref _ca2, _cra, Ca2OutputChanged);
                ControlAWritten?.Invoke(_cra);
                break;
            case 2 when !IsPortBDataSelected():
                _ddrb = value;
                break;
            case 2:
                _orb = value;
                PortBWritten?.Invoke(value);
                break;
            case 3:
                _crb = (byte)(value & WritableControlBits);
                SetControl2Output(ref _cb2, _crb, Cb2OutputChanged);
                ControlBWritten?.Invoke(_crb);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    public void Reset()
    {
        _ddra = _ddrb = _ora = _orb = _cra = _crb = 0;
        _ca1 = _ca2 = _cb1 = _cb2 = false;
        _ca1Flag = _ca2Flag = _cb1Flag = _cb2Flag = false;
    }

    /// <summary>Pia has no internal cycle-driven timing; all state changes happen on
    /// bus access or explicit control-line assignment.</summary>
    public void Tick(ulong cycles)
    {
    }

    private bool IsPortADataSelected() => SelectPortADataRegister?.Invoke(_cra) ?? (_cra & DataRegisterSelect) != 0;

    private bool IsPortBDataSelected() => SelectPortBDataRegister?.Invoke(_crb) ?? (_crb & DataRegisterSelect) != 0;

    private byte ReadPortAOrDirection()
    {
        if (!IsPortADataSelected())
            return _ddra;

        var value = CombinePort(_ora, _ddra, PortAInput?.Invoke() ?? 0);
        _ca1Flag = _ca2Flag = false;
        PortARead?.Invoke();
        return value;
    }

    private byte ReadPortBOrDirection()
    {
        if (!IsPortBDataSelected())
            return _ddrb;

        var value = CombinePort(_orb, _ddrb, PortBInput?.Invoke() ?? 0);
        _cb1Flag = _cb2Flag = false;
        PortBRead?.Invoke();
        return value;
    }

    private static byte ReadControl(byte control, bool control1Flag, bool control2Flag)
        => (byte)(control | (control1Flag ? 0x80 : 0) | (control2Flag ? 0x40 : 0));

    private static void SetControlInput(ref bool line, bool value, bool risingEdge, ref bool interruptFlag)
    {
        if (line == value)
            return;

        var isActiveEdge = value == risingEdge;
        line = value;
        if (isActiveEdge)
            interruptFlag = true;
    }

    private static void SetControl2Output(ref bool line, byte control, Action<bool>? outputChanged)
    {
        if (!IsControl2Output(control))
            return;

        var value = (control & 0x08) != 0;
        if (line == value)
            return;

        line = value;
        outputChanged?.Invoke(value);
    }

    private static bool IsRisingEdge(byte control) => (control & 0x02) != 0;

    private static bool IsRisingEdge2(byte control) => (control & 0x10) != 0;

    private static bool IsControl2Output(byte control) => (control & 0x20) != 0;

    private static bool IsControl2InputInterruptEnabled(byte control)
        => !IsControl2Output(control) && (control & 0x08) != 0;

    private static byte CombinePort(byte outputLatch, byte dataDirection, byte input)
        => (byte)((outputLatch & dataDirection) | (input & ~dataDirection));
}
