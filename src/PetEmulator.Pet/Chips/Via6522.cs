using PetEmulator.Core;

namespace PetEmulator.Pet.Chips;

/// <summary>Minimal MOS 6522 Versatile Interface Adapter, clocked once per PHI2 by <see cref="Update"/>.</summary>
public sealed class Via6522 : IMemoryMappedDevice
{
    public const ushort Orb = 0x00;
    public const ushort Ora = 0x01;
    public const ushort Ddrb = 0x02;
    public const ushort Ddra = 0x03;
    public const ushort T1CounterLow = 0x04;
    public const ushort T1CounterHigh = 0x05;
    public const ushort T1LatchLow = 0x06;
    public const ushort T1LatchHigh = 0x07;
    public const ushort T2CounterLow = 0x08;
    public const ushort T2CounterHigh = 0x09;
    public const ushort ShiftRegister = 0x0A;
    public const ushort AuxiliaryControl = 0x0B;
    public const ushort PeripheralControl = 0x0C;
    public const ushort InterruptFlag = 0x0D;
    public const ushort InterruptEnable = 0x0E;
    public const ushort OraWithoutHandshake = 0x0F;

    public const byte Ca2Interrupt = 0x01;
    public const byte Ca1Interrupt = 0x02;
    public const byte ShiftRegisterInterrupt = 0x04;
    public const byte Cb2Interrupt = 0x08;
    public const byte Cb1Interrupt = 0x10;
    public const byte Timer2Interrupt = 0x20;
    public const byte Timer1Interrupt = 0x40;
    public const byte AnyInterrupt = 0x80;

    private readonly ushort _baseAddress;
    private byte _orb;
    private byte _ora;
    private byte _ddrb;
    private byte _ddra;
    private ushort _t1Counter;
    private ushort _t1Latch;
    private ushort _t2Counter;
    private ushort _t2Latch;
    private byte _shiftRegister;
    private byte _acr;
    private byte _pcr;
    private byte _ifr;
    private byte _ier;
    private byte _latchedPortA;
    private byte _latchedPortB;
    private bool _ca1;
    private bool _ca2;
    private bool _cb1;
    private bool _cb2;
    private bool _previousCa1;
    private bool _previousCa2;
    private bool _previousCb1;
    private bool _previousCb2;
    private bool _t1Running;
    private bool _t2Running;
    private bool _t1OneShotArmed;
    private bool _t2OneShotArmed;
    private bool _t1Pb7;
    private int _shiftCount;

    public Via6522(string name = "VIA", ushort baseAddress = 0)
    {
        Name = name;
        _baseAddress = baseAddress;
        Reset();
    }

    public string Name { get; }

    public uint Length => 16;

    public byte PortAInput { get; set; }


    public byte PortBInput { get; set; }

    public Action<byte>? PortAWritten { get; set; }

    public Action<byte>? PortBWritten { get; set; }

    public byte ORA => _ora;

    public byte ORB => _orb;

    public byte DDRA => _ddra;

    public byte DDRB => _ddrb;

    public byte PortAOutput => (byte)(_ora & _ddra);

    public byte PortBOutput => (_acr & 0x80) != 0
        ? (byte)((_orb & _ddrb & 0x7F) | (_t1Pb7 ? 0x80 : 0))
        : (byte)(_orb & _ddrb);

    public ushort Timer1Counter => _t1Counter;

    public ushort Timer1Latch => _t1Latch;

    public ushort Timer2Counter => _t2Counter;

    public byte SR => _shiftRegister;

    public byte ACR => _acr;

    public byte PCR => _pcr;

    public byte IFR => ReadInterruptFlags();

    public byte IER => _ier;

    // Detects the CA1 edge synchronously on assignment, not only when a later Update() call
    // happens to sample it - see UpdateControlInputs' identical (_previousCa1, _ca1) comparison,
    // which still runs too but becomes a no-op here since _previousCa1 is kept in sync below.
    // Needed for a datasette-style caller that raises then immediately lowers-then-raises CA1
    // again within one Tick() (a real edge-sensitive input reacts the instant the level changes,
    // not only at whatever cadence the caller happens to invoke Update()) - see
    // Vic20Machine/Vic20Datasette's doc comments, and Pia.CA1 (PET's equivalent chip), whose
    // setter already worked this way.
    public bool CA1
    {
        get => _ca1;
        set
        {
            if (IsActiveEdge(_ca1, value, (_pcr & 0x01) != 0))
            {
                if ((_acr & 0x01) != 0)
                    _latchedPortA = PortAInput;
                SetInterrupt(Ca1Interrupt);
                ReleasePortAHandshake();
            }
            _previousCa1 = value;
            _ca1 = value;
        }
    }

    public bool CA2 { get => _ca2; set => _ca2 = value; }

    public bool CB1 { get => _cb1; set => _cb1 = value; }

    public bool CB2 { get => _cb2; set => _cb2 = value; }

    public bool CA2Output { get; private set; }

    public bool CB2Output { get; private set; }

    public bool IRQ { get; private set; }

    public bool HasInterrupt => IRQ;

    public byte Read(ushort address)
    {
        uint offset = (uint)(address - _baseAddress);
        return offset switch
        {
            Orb => ReadPortB(),
            Ora or OraWithoutHandshake => ReadPortA(),
            Ddrb => _ddrb,
            Ddra => _ddra,
            T1CounterLow => ReadTimer1Low(),
            T1CounterHigh => (byte)(_t1Counter >> 8),
            T1LatchLow => (byte)_t1Latch,
            T1LatchHigh => (byte)(_t1Latch >> 8),
            T2CounterLow => ReadTimer2Low(),
            T2CounterHigh => (byte)(_t2Counter >> 8),
            ShiftRegister => ReadShiftRegister(),
            AuxiliaryControl => _acr,
            PeripheralControl => _pcr,
            InterruptFlag => ReadInterruptFlags(),
            InterruptEnable => (byte)(_ier | AnyInterrupt),
            _ => 0xFF
        };
    }

    public void Write(ushort address, byte value)
    {
        uint offset = (uint)(address - _baseAddress);
        switch (offset)
        {
            case Orb:
                _orb = value;
                PortBWritten?.Invoke(PortBOutput);
                break;
            case Ora:
                _ora = value;
                PortAWritten?.Invoke(PortAOutput);
                SetPortAHandshakeLow();
                break;
            case OraWithoutHandshake:
                _ora = value;
                PortAWritten?.Invoke(PortAOutput);
                break;
            case Ddrb:
                _ddrb = value;
                PortBWritten?.Invoke(PortBOutput);
                break;
            case Ddra:
                _ddra = value;
                PortAWritten?.Invoke(PortAOutput);
                break;
            case T1CounterLow:
            case T1LatchLow:
                _t1Latch = (ushort)((_t1Latch & 0xFF00) | value);
                break;
            case T1CounterHigh:
                _t1Latch = (ushort)((_t1Latch & 0x00FF) | (value << 8));
                _t1Counter = _t1Latch;
                _t1Running = true;
                _t1OneShotArmed = true;
                _t1Pb7 = false;
                ClearInterrupt(Timer1Interrupt);
                break;
            case T1LatchHigh:
                _t1Latch = (ushort)((_t1Latch & 0x00FF) | (value << 8));
                break;
            case T2CounterLow:
                _t2Latch = (ushort)((_t2Latch & 0xFF00) | value);
                break;
            case T2CounterHigh:
                _t2Latch = (ushort)((_t2Latch & 0x00FF) | (value << 8));
                _t2Counter = _t2Latch;
                _t2Running = true;
                _t2OneShotArmed = true;
                ClearInterrupt(Timer2Interrupt);
                break;
            case ShiftRegister:
                _shiftRegister = value;
                _shiftCount = 0;
                ClearInterrupt(ShiftRegisterInterrupt);
                break;
            case AuxiliaryControl:
                _acr = value;
                break;
            case PeripheralControl:
                _pcr = value;
                UpdateControlOutputs();
                break;
            case InterruptFlag:
                ClearInterrupt((byte)(value & ~AnyInterrupt));
                break;
            case InterruptEnable:
                if ((value & AnyInterrupt) != 0)
                    _ier |= (byte)(value & ~AnyInterrupt);
                else
                    _ier &= (byte)~value;
                UpdateIrq();
                break;
        }
    }

    public void Reset()
    {
        _orb = _ora = _ddrb = _ddra = _shiftRegister = _acr = _pcr = _ifr = _ier = 0;
        _t1Counter = _t1Latch = _t2Counter = _t2Latch = 0;
        _latchedPortA = _latchedPortB = 0;
        _ca1 = _ca2 = _cb1 = _cb2 = _previousCa1 = _previousCa2 = _previousCb1 = _previousCb2 = false;
        _t1Running = _t2Running = _t1OneShotArmed = _t2OneShotArmed = _t1Pb7 = false;
        _shiftCount = 0;
        CA2Output = CB2Output = IRQ = false;
        PortAInput = PortBInput = 0;
    }

    /// <summary>Advances the VIA by <paramref name="cycles"/> PHI2 cycles.</summary>
    public void Tick(ulong cycles)
    {
        for (ulong i = 0; i < cycles; i++)
            Update();
    }

    /// <summary>Advances the VIA by one PHI2 cycle.</summary>
    public void Update()
    {
        UpdateControlInputs();
        UpdateTimers();
        UpdateShiftRegister();
        _previousCa1 = _ca1;
        _previousCa2 = _ca2;
        _previousCb1 = _cb1;
        _previousCb2 = _cb2;
        UpdateIrq();
    }

    /// <summary>Supplies one PB6 pulse when ACR selects timer-2 pulse counting.</summary>
    public void ClockTimer2()
    {
        if (_t2Running && (_acr & 0x20) != 0)
            DecrementTimer2();
    }

    private byte ReadPortA()
    {
        // PCR "independent interrupt" input sub-modes (0x02/0x06) mean CA2's flag is NOT
        // acknowledged by a Port A data read - only an explicit IFR write clears it.
        var mask = IsCa2InterruptIndependent(_pcr) ? Ca1Interrupt : (byte)(Ca1Interrupt | Ca2Interrupt);
        ClearInterrupt(mask);
        SetPortAHandshakeLow();
        return CombinePort(_ora, _ddra, (_acr & 0x01) != 0 ? _latchedPortA : PortAInput);
    }

    private byte ReadPortB()
    {
        var mask = IsCb2InterruptIndependent(_pcr) ? Cb1Interrupt : (byte)(Cb1Interrupt | Cb2Interrupt);
        ClearInterrupt(mask);
        var input = (_acr & 0x02) != 0 ? _latchedPortB : PortBInput;
        var value = CombinePort(_orb, _ddrb, input);
        return (_acr & 0x80) != 0 ? (byte)((value & 0x7F) | (_t1Pb7 ? 0x80 : 0)) : value;
    }

    private byte ReadTimer1Low()
    {
        ClearInterrupt(Timer1Interrupt);
        return (byte)_t1Counter;
    }

    private byte ReadTimer2Low()
    {
        ClearInterrupt(Timer2Interrupt);
        return (byte)_t2Counter;
    }

    private byte ReadShiftRegister()
    {
        ClearInterrupt(ShiftRegisterInterrupt);
        return _shiftRegister;
    }

    private void UpdateControlInputs()
    {
        if (IsActiveEdge(_previousCa1, _ca1, (_pcr & 0x01) != 0))
        {
            if ((_acr & 0x01) != 0)
                _latchedPortA = PortAInput;
            SetInterrupt(Ca1Interrupt);
            ReleasePortAHandshake();
        }

        if (IsActiveEdge(_previousCb1, _cb1, (_pcr & 0x10) != 0))
        {
            if ((_acr & 0x02) != 0)
                _latchedPortB = PortBInput;
            SetInterrupt(Cb1Interrupt);
            ClockShiftRegisterFromCb1();
        }

        if (IsCa2Input((byte)(_pcr & 0x0E)) && IsActiveEdge(_previousCa2, _ca2, (_pcr & 0x04) != 0))
            SetInterrupt(Ca2Interrupt);
        if (IsCb2Input((byte)(_pcr & 0xE0)) && IsActiveEdge(_previousCb2, _cb2, (_pcr & 0x40) != 0))
            SetInterrupt(Cb2Interrupt);
    }

    private void UpdateTimers()
    {
        // T1 keeps decrementing/wrapping forever once started, in both modes - real 6522 hardware
        // never stops the counter itself; one-shot mode (ACR6=0) only stops firing REPEAT
        // interrupts (and doesn't reload from the latch) until T1CH is written again, tracked by
        // _t1OneShotArmed. Free-run mode (ACR6=1) always fires and always reloads.
        if (_t1Running && --_t1Counter == ushort.MaxValue)
        {
            _t1Pb7 = !_t1Pb7;
            if ((_acr & 0x40) != 0)
            {
                SetInterrupt(Timer1Interrupt);
                _t1Counter = _t1Latch;
            }
            else if (_t1OneShotArmed)
            {
                SetInterrupt(Timer1Interrupt);
                _t1OneShotArmed = false;
            }
        }

        if (_t2Running && (_acr & 0x20) == 0)
            DecrementTimer2();
    }

    private void DecrementTimer2()
    {
        // Same "keeps counting, one-shot only suppresses repeat interrupts" fix as T1 - see
        // UpdateTimers. T2 has no free-run mode of its own, so there's no reload branch here.
        if (--_t2Counter == ushort.MaxValue)
        {
            if (_t2OneShotArmed)
            {
                SetInterrupt(Timer2Interrupt);
                _t2OneShotArmed = false;
            }

            // ponytail: SR modes clocked by T2 (0x04 in, 0x10 out-free-run, 0x14 out-once) all
            // shift one bit per T2 underflow here - real 0x10 additionally free-runs T2 itself
            // (reloading from the latch forever, ignoring one-shot) to keep generating a
            // continuous shift clock; that reload isn't implemented, so 0x10 clocks the same
            // single bit as 0x14 then goes quiet like any other one-shot T2. Upgrade path: give
            // 0x10 its own always-reload branch here if a real shift-out-free-running user shows up.
            var srMode = (byte)(_acr & 0x1C);
            if (srMode is 0x04 or 0x10 or 0x14)
                ClockShiftRegister();
        }
    }

    private void UpdateShiftRegister()
    {
        var mode = (byte)(_acr & 0x1C);
        if (mode is 0x08 or 0x18)
            ClockShiftRegister();
    }

    private void ClockShiftRegisterFromCb1()
    {
        var mode = (byte)(_acr & 0x1C);
        if (mode is 0x0C or 0x1C)
            ClockShiftRegister();
    }

    private void ClockShiftRegister()
    {
        var input = (_acr & 0x1C) is 0x04 or 0x08 or 0x0C;
        _shiftRegister = input
            ? (byte)((_shiftRegister >> 1) | (_cb2 ? 0x80 : 0))
            : (byte)(_shiftRegister << 1);
        if (++_shiftCount == 8)
        {
            _shiftCount = 0;
            SetInterrupt(ShiftRegisterInterrupt);
        }
    }

    private void UpdateControlOutputs()
    {
        var ca2Mode = (byte)(_pcr & 0x0E);
        CA2Output = ca2Mode switch { 0x0C => false, 0x0E => true, >= 0x08 => true, _ => false };
        var cb2Mode = (byte)(_pcr & 0xE0);
        CB2Output = cb2Mode switch { 0xC0 => false, 0xE0 => true, >= 0x80 => true, _ => false };
    }

    private void SetPortAHandshakeLow()
    {
        if ((_pcr & 0x0E) is 0x08 or 0x0A)
            CA2Output = false;
    }

    private void ReleasePortAHandshake()
    {
        if ((_pcr & 0x0E) is 0x08 or 0x0A)
            CA2Output = true;
    }

    private void SetInterrupt(byte mask)
    {
        _ifr |= mask;
        UpdateIrq();
    }

    private void ClearInterrupt(byte mask)
    {
        _ifr &= (byte)~mask;
        UpdateIrq();
    }

    private byte ReadInterruptFlags() => (byte)(_ifr | (IRQ ? AnyInterrupt : 0));

    private void UpdateIrq() => IRQ = (_ifr & _ier & ~AnyInterrupt) != 0;

    private static bool IsActiveEdge(bool previous, bool current, bool rising) => previous != current && current == rising;

    private static bool IsCa2Input(byte mode) => mode <= 0x06;

    private static bool IsCb2Input(byte mode) => mode <= 0x60;

    /// <summary>PCR "independent interrupt" input sub-modes (raw CA2 field 0x02/0x06): the CA2
    /// interrupt flag is NOT cleared by a Port A data read, only by an explicit IFR write - unlike
    /// the non-independent input modes (0x00/0x04), which <see cref="ReadPortA"/> normally clears.</summary>
    private static bool IsCa2InterruptIndependent(byte pcr) => (pcr & 0x0E) is 0x02 or 0x06;

    private static bool IsCb2InterruptIndependent(byte pcr) => (pcr & 0xE0) is 0x20 or 0x60;

    private static byte CombinePort(byte outputLatch, byte direction, byte input)
        => (byte)((outputLatch & direction) | (input & ~direction));
}
