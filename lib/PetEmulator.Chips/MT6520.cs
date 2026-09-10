using PetEmulator.Core;

namespace PetEmulator.Chips;

/// <summary>Four-register Motorola 6520/6821 Peripheral Interface Adapter.</summary>
public sealed class MT6520 : IMemoryMappedDevice
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
    private byte _ca2PulseCyclesRemaining;
    private byte _cb2PulseCyclesRemaining;

    public MT6520(string name = "PIA", ushort baseAddress = 0)
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
        set
        {
            if (SetControlInput(ref _ca1, value, IsRisingEdge(_cra), ref _ca1Flag))
                RestoreCa2Handshake();
        }
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
        set
        {
            if (SetControlInput(ref _cb1, value, IsRisingEdge(_crb), ref _cb1Flag))
                RestoreCb2Handshake();
        }
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
                _ca2PulseCyclesRemaining = 0;
                ApplyControl2OutputMode(_cra, SetCa2Output);
                ControlAWritten?.Invoke(_cra);
                break;
            case 2 when !IsPortBDataSelected():
                _ddrb = value;
                break;
            case 2:
                _orb = value;
                PortBWritten?.Invoke(value);
                TriggerCb2Handshake();
                break;
            case 3:
                _crb = (byte)(value & WritableControlBits);
                _cb2PulseCyclesRemaining = 0;
                ApplyControl2OutputMode(_crb, SetCb2Output);
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
        _ca2PulseCyclesRemaining = _cb2PulseCyclesRemaining = 0;
    }

    /// <summary>Auto-restores CA2/CB2 after a "pulse" output (CRA/CRB bit4=0, bit3=1): the line
    /// goes low for exactly one PHI2 cycle after the triggering Port A read / Port B write, then
    /// comes back high on its own - unlike "handshake" mode (bit3=0), which instead waits for the
    /// next active CA1/CB1 edge. Everything else on this chip is bus/control-line-driven with no
    /// cycle-accurate timing of its own, so this is the one place Tick actually does something.</summary>
    public void Tick(ulong cycles)
    {
        if (_ca2PulseCyclesRemaining > 0)
        {
            _ca2PulseCyclesRemaining -= cycles >= _ca2PulseCyclesRemaining ? _ca2PulseCyclesRemaining : (byte)cycles;
            if (_ca2PulseCyclesRemaining == 0)
                SetCa2Output(true);
        }

        if (_cb2PulseCyclesRemaining > 0)
        {
            _cb2PulseCyclesRemaining -= cycles >= _cb2PulseCyclesRemaining ? _cb2PulseCyclesRemaining : (byte)cycles;
            if (_cb2PulseCyclesRemaining == 0)
                SetCb2Output(true);
        }
    }

    private bool IsPortADataSelected() => SelectPortADataRegister?.Invoke(_cra) ?? (_cra & DataRegisterSelect) != 0;

    private bool IsPortBDataSelected() => SelectPortBDataRegister?.Invoke(_crb) ?? (_crb & DataRegisterSelect) != 0;

    private byte ReadPortAOrDirection()
    {
        if (!IsPortADataSelected())
            return _ddra;

        var value = CombinePort(_ora, _ddra, PortAInput?.Invoke() ?? 0);
        _ca1Flag = _ca2Flag = false;
        TriggerCa2Handshake();
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

    /// <summary>Returns whether the transition was the control register's configured active edge
    /// (the same edge that also drives the C1 interrupt flag).</summary>
    private static bool SetControlInput(ref bool line, bool value, bool risingEdge, ref bool interruptFlag)
    {
        if (line == value)
            return false;

        var isActiveEdge = value == risingEdge;
        line = value;
        if (isActiveEdge)
            interruptFlag = true;
        return isActiveEdge;
    }

    /// <summary>Applies the level a CRA/CRB write establishes for CA2/CB2 when configured as an
    /// output: manual mode (bit4=1) sets it directly from bit3; handshake/pulse mode (bit4=0)
    /// idles high (real 6520 behavior - it only pulses low when the mode's actual trigger fires:
    /// a Port A read for CA2, a Port B write for CB2 - see <see cref="TriggerCa2Handshake"/>/
    /// <see cref="TriggerCb2Handshake"/>), not whatever the line happened to read before this
    /// control-register write. Leaves input mode (bit5=0) alone entirely.</summary>
    private static void ApplyControl2OutputMode(byte control, Action<bool> setOutput)
    {
        if (!IsControl2Output(control))
            return;

        setOutput(IsControl2Manual(control) ? (control & 0x08) != 0 : true);
    }

    /// <summary>Fires on every Port A data-register read: in handshake/pulse output mode
    /// (CRA bit5=1, bit4=0), CA2 drops low. Pulse mode (bit3=1) also arms a one-cycle
    /// auto-restore; handshake mode (bit3=0) instead waits for the next active CA1 edge
    /// (<see cref="RestoreCa2Handshake"/>).</summary>
    private void TriggerCa2Handshake()
    {
        if (!IsControl2Output(_cra) || IsControl2Manual(_cra))
            return;

        SetCa2Output(false);
        if (IsControl2Pulse(_cra))
            _ca2PulseCyclesRemaining = 1;
    }

    private void RestoreCa2Handshake()
    {
        if (!IsControl2Output(_cra) || IsControl2Manual(_cra) || IsControl2Pulse(_cra))
            return;

        SetCa2Output(true);
    }

    /// <summary>Fires on every Port B write: the write-side mirror of
    /// <see cref="TriggerCa2Handshake"/> (CB2 instead of CA2, triggered by writing ORB instead of
    /// reading Port A - real hardware ties CA2's automatic modes to the read side and CB2's to the
    /// write side).</summary>
    private void TriggerCb2Handshake()
    {
        if (!IsControl2Output(_crb) || IsControl2Manual(_crb))
            return;

        SetCb2Output(false);
        if (IsControl2Pulse(_crb))
            _cb2PulseCyclesRemaining = 1;
    }

    private void RestoreCb2Handshake()
    {
        if (!IsControl2Output(_crb) || IsControl2Manual(_crb) || IsControl2Pulse(_crb))
            return;

        SetCb2Output(true);
    }

    private void SetCa2Output(bool value)
    {
        if (_ca2 == value)
            return;
        _ca2 = value;
        Ca2OutputChanged?.Invoke(value);
    }

    private void SetCb2Output(bool value)
    {
        if (_cb2 == value)
            return;
        _cb2 = value;
        Cb2OutputChanged?.Invoke(value);
    }

    private static bool IsRisingEdge(byte control) => (control & 0x02) != 0;

    private static bool IsRisingEdge2(byte control) => (control & 0x10) != 0;

    private static bool IsControl2Output(byte control) => (control & 0x20) != 0;

    /// <summary>CRA/CRB bit4: 1 = manual output (bit3 sets the level directly), 0 = automatic
    /// handshake/pulse (see <see cref="IsControl2Pulse"/> for which of the two).</summary>
    private static bool IsControl2Manual(byte control) => (control & 0x10) != 0;

    /// <summary>Only meaningful when output and not manual: CRA/CRB bit3, 0 = handshake
    /// (restores on the next active C1 edge), 1 = pulse (restores automatically after one cycle).</summary>
    private static bool IsControl2Pulse(byte control) => (control & 0x08) != 0;

    private static bool IsControl2InputInterruptEnabled(byte control)
        => !IsControl2Output(control) && (control & 0x08) != 0;

    private static byte CombinePort(byte outputLatch, byte dataDirection, byte input)
        => (byte)((outputLatch & dataDirection) | (input & ~dataDirection));
}
