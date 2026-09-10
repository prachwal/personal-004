using PetEmulator.Chips;

namespace PetEmulator.Vic20.Serial;

/// <summary>
/// Connects the VIC-20's IEC pins to its VIAs: VIA1 PA7 drives ATN, VIA1 PA0/PA1 read CLK/DATA,
/// and VIA2 CA2/CB2 drive CLK/DATA. IEC lines are active-low open-collector.
/// </summary>
public sealed class Vic20SerialBusBinding
{
    private const byte ClockInput = 0x01;
    private const byte DataInput = 0x02;
    private const byte AtnOutput = 0x80;

    private readonly MOS6522 _via1;
    private readonly MOS6522 _via2;
    private readonly Vic20SerialBus _bus;
    private bool? _lastAtn;
    private bool? _lastClockOutput;
    private bool? _lastDataOutput;
    private bool? _lastClockInput;
    private bool? _lastDataInput;

    public Vic20SerialBusBinding(MOS6522 via1, MOS6522 via2, Vic20SerialBus bus)
    {
        _via1 = via1 ?? throw new ArgumentNullException(nameof(via1));
        _via2 = via2 ?? throw new ArgumentNullException(nameof(via2));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        Sync();
    }

    /// <summary>Polls VIA outputs, advances the bus, then refreshes VIA1's IEC input pins.</summary>
    public void Tick()
    {
        SyncHostOutputs();
        _bus.Tick();
        SyncInputs();
    }

    public void Reset()
    {
        _bus.Reset();
        _lastAtn = _lastClockOutput = _lastDataOutput = _lastClockInput = _lastDataInput = null;
        Sync();
    }

    private void Sync()
    {
        SyncHostOutputs();
        SyncInputs();
    }

    private void SyncHostOutputs()
    {
        // An input-configured VIA pin releases an open-collector IEC line.
        SetIfChanged(ref _lastAtn,
            (_via1.DDRA & AtnOutput) == 0 || (_via1.ORA & AtnOutput) != 0,
            _bus.SetHostAtn);
        SetIfChanged(ref _lastClockOutput,
            !IsCa2Output() || _via2.CA2Output,
            _bus.SetHostClock);
        SetIfChanged(ref _lastDataOutput,
            !IsCb2Output() || _via2.CB2Output,
            _bus.SetHostData);
    }

    private void SyncInputs()
    {
        SetIfChanged(ref _lastClockInput, _bus.CLK, clock => SetPortAInput(ClockInput, clock));
        SetIfChanged(ref _lastDataInput, _bus.DATA, data => SetPortAInput(DataInput, data));
    }

    private void SetPortAInput(byte pin, bool released)
    {
        _via1.PortAInput = released
            ? (byte)(_via1.PortAInput | pin)
            : (byte)(_via1.PortAInput & ~pin);
    }

    private bool IsCa2Output() => (_via2.PCR & 0x0E) >= 0x08;

    private bool IsCb2Output() => (_via2.PCR & 0xE0) >= 0x80;

    private static void SetIfChanged(ref bool? previous, bool current, Action<bool> set)
    {
        if (previous == current)
            return;

        previous = current;
        set(current);
    }
}
