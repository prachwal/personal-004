using PetEmulator.Pet.Ieee488;

namespace PetEmulator.Vic20.Serial;

/// <summary>One protocol-level activity on a <see cref="Vic20SerialBus"/>: a line change
/// (Kind="line-change", e.g. Detail="ATN=assert") or a byte moved over DATA (Kind="byte", e.g.
/// Detail="0x24 written (command)"). Mirrors <see cref="PetIeeeBus.Activity"/>'s shape (same
/// observability need: a future debug surface, or - the reason this was added - correlating live
/// bus behavior with real KERNAL PC during real-CPU-timing bug hunts that idealized
/// Tick()-sequenced unit tests can't reproduce; see docs/vic20-disk.md's IEC debugging notes).</summary>
public readonly record struct Vic20SerialActivity(string Kind, string Detail);

/// <summary>
/// Device-agnostic CBM IEC bus. ATN, CLK, and DATA are active-low open-collector
/// lines: <see langword="false"/> asserts a line and <see langword="true"/> releases it.
/// Bytes are shifted LSB-first on CLK rising edges.
/// </summary>
public sealed class Vic20SerialBus
{
    private enum BusState { Idle, Command, DataOut, DataIn }

    private readonly List<IIeeeDevice> _devices = [];
    private BusState _state = BusState.Idle;
    private IIeeeDevice? _listenerDevice;
    private IIeeeDevice? _talkerDevice;
    private byte _listenerSecondary;
    private byte _talkerSecondary;
    private bool _hostAtnLow;
    private bool _hostClockLow;
    private bool _hostDataLow;
    private bool _deviceClockLow;
    private bool _deviceDataLow;
    private byte _incomingByte;
    private int _incomingBitCount;
    private bool _incomingAcknowledgePending;
    private int _incomingEoiProbeCycles;
    private int _incomingEoiAcknowledgeCycles;
    private byte _outgoingByte;
    private int _outgoingBitIndex = -1;
    private bool _outgoingAwaitingStart;
    private bool _outgoingAwaitingEoiAcknowledge;
    private int _outgoingPhaseCycles;
    private int _outgoingAcknowledgePulseCycles;
    private bool _outgoingFinalClockPulse;

    // 26 cycles ("23us on a PAL VIC") is SRSEND's own bit-hold time - the VIC's *outgoing*
    // (VIC-to-drive) transmit routine, a different direction from this constant's actual use
    // (DataIn/talker: the emulated *drive* sending bytes to the VIC, read by FACPTR's
    // LAB_EF58/LAB_EF66 loop). A real 1541's own bit-clocking rate is drive-firmware timed, not
    // bound to this VIC-side constant - reusing it here was a plausible-looking but unverified
    // guess, not a disassembly-sourced requirement for this direction.
    //
    // Root-caused by instruction-level + Activity-correlated tracing (see docs/vic20-disk.md):
    // with this at 26, a live LOAD"$",8 run desyncs mid-transfer - the CPU permanently parks in
    // LAB_EF66 (wait CLK low) on one byte while the bus's own byte-dequeue counter shows the
    // *device* has already raced ahead and popped the next byte(s), because each bit's CLK/DATA
    // transition fires on a blind fixed-cycle countdown with no feedback on whether the KERNAL's
    // real, jitter-prone polling loop (LAB_EF58/66, each pass variable-cost on real 6502 timing)
    // actually sampled it first. 26 cycles is barely 1-2 debounce passes of margin; occasional
    // real-CPU jitter is enough to lose a bit and never resynchronize. Widened for headroom.
    //
    // An earlier attempt to widen this (to 100) was tried and reverted as making "zero difference"
    // - that test predates both the FinalClockPulseCycles fix below and this byte-42/43 desync
    // being isolated, and only checked end-to-end pass/fail rather than the windowed
    // instruction-level trace that found the actual desync point. Re-verify with that technique
    // before changing this again.
    private const int BitPhaseCycles = 100;
    private const int EoiProbeCycles = 200;
    // The listener's EOI-acknowledge DATA-low pulse (real KERNAL LAB_EE60: "wait for serial data
    // to go low") was previously reusing BitPhaseCycles (26 cycles). The KERNAL's own polling loop
    // there (JSR SERGET; LSR; BCS loop) costs ~24 CPU cycles per iteration - a 26-cycle pulse is
    // barely one iteration wide and can fall entirely between two samples, so the KERNAL's own
    // polling never observes it and LAB_EE60 spins forever. Confirmed by instruction-level tracing
    // (see docs/vic20-disk.md's IEC debugging notes) - widened to comfortably outlast one polling
    // iteration with margin, independent of the unrelated bit-transmission timing constant above.
    private const int EoiAcknowledgePulseCycles = 100;
    // Real IEC protocol: right after ATN releases with a device addressed as talker, that device
    // pulses CLK low, unprompted, to acknowledge its address (KERNAL: LAB_EED3/LAB_EEDD, "wait for
    // bus end after send" - waits for CLK to go low once). This is distinct from and happens
    // before the later per-byte ready handshake FACPTR itself implements (LAB_EF21 onward, which
    // starts by releasing CLK again). Same polling-loop-width reasoning as EoiAcknowledgePulseCycles.
    private const int TalkerAcknowledgePulseCycles = 100;
    // Real KERNAL bug found by instruction-level tracing on a real CPU run (see docs/vic20-disk.md
    // IEC debugging notes): FACPTR's bit-receive loop is LAB_EF58 (wait CLK high, sample bit) then
    // unconditionally LAB_EF66 (wait CLK low) BEFORE checking "8 bits done yet" - this wait-for-low
    // happens after EVERY bit, including the 8th/last one. Releasing CLK after bit 8 and stopping
    // there (the previous behavior) leaves the KERNAL parked at LAB_EF66 forever, since it needs
    // one more falling edge it will never see. A real 1541 always sends this extra pulse; we must
    // too, before settling into "awaiting next byte" (where CLK correctly stays released for good).
    // Width: this is an ISOLATED, one-shot edge (not part of the already-synchronized repeating
    // bit train LAB_EF58/66 already tracks) - same aliasing risk bug 1's EOI pulse had against a
    // ~24-cycle debounce-read loop, so it needs the same wider, independent margin, not the tight
    // bit-period constant.
    private const int FinalClockPulseCycles = 100;

    /// <summary>Resolved ATN line level.</summary>
    public bool ATN => !_hostAtnLow;

    /// <summary>Resolved CLK line level.</summary>
    public bool CLK => !(_hostClockLow || _deviceClockLow);

    /// <summary>Resolved DATA line level.</summary>
    public bool DATA => !(_hostDataLow || _deviceDataLow);

    /// <summary>True while the final byte from the current talker is being shifted out.</summary>
    public bool EOI { get; private set; }

    /// <summary>Fires for every control-line change, decoded command byte, and data byte this bus
    /// processes. Optional (nullable multicast delegate) - zero-cost and behavior-neutral when
    /// nobody subscribes.</summary>
    public event Action<Vic20SerialActivity>? Activity;

    /// <summary>Attaches a device, replacing an existing device at the same primary address.</summary>
    public void AttachDevice(IIeeeDevice device)
    {
        _devices.RemoveAll(existing => existing.PrimaryAddress == device.PrimaryAddress);
        _devices.Add(device);
    }

    /// <summary>Drives or releases the computer's ATN output. <see langword="false"/> asserts it.</summary>
    public void SetHostAtn(bool released)
    {
        if (ATN == released)
            return;

        _hostAtnLow = !released;
        RaiseActivity("line-change", $"ATN={(released ? "release" : "assert")}");
        AbortTransfer();
        _state = released ? SelectDataMode() : BusState.Command;
        RaiseActivity("state", $"{_state}");
        if (_state is BusState.Command or BusState.DataOut)
            ArmIncomingAcknowledge();
    }

    /// <summary>Drives or releases the computer's CLK output. <see langword="false"/> asserts it.</summary>
    public void SetHostClock(bool released)
    {
        bool wasHigh = CLK;
        _hostClockLow = !released;
        if (CLK != wasHigh)
            RaiseActivity("line-change", $"CLK={(CLK ? "release" : "assert")}");
        if (_state is BusState.Command or BusState.DataOut)
        {
            if (!released)
                _incomingEoiProbeCycles = 0;
            else if (!wasHigh && CLK)
            {
                if (_incomingAcknowledgePending)
                {
                    _incomingAcknowledgePending = false;
                    _deviceDataLow = false;
                    _incomingEoiProbeCycles = EoiProbeCycles;
                }
                else
                {
                    ShiftIncomingBit(DATA);
                }
            }
        }
    }

    /// <summary>Drives or releases the computer's DATA output. <see langword="false"/> asserts it.</summary>
    public void SetHostData(bool released)
    {
        bool wasHigh = DATA;
        _hostDataLow = !released;
        if (DATA != wasHigh)
            RaiseActivity("line-change", $"DATA={(DATA ? "release" : "assert")} (host)");
    }

    /// <summary>
    /// Advances device-originated TALK traffic and the listener's EOI probe. The listener side is
    /// otherwise edge-driven by <see cref="SetHostClock"/>.
    /// </summary>
    public void Tick()
    {
        bool clkWasHigh = CLK, dataWasHigh = DATA;
        try
        {
            TickCore();
        }
        finally
        {
            if (CLK != clkWasHigh)
                RaiseActivity("line-change", $"CLK={(CLK ? "release" : "assert")} (device)");
            if (DATA != dataWasHigh)
                RaiseActivity("line-change", $"DATA={(DATA ? "release" : "assert")} (device)");
        }
    }

    private void TickCore()
    {
        if (_state == BusState.DataOut)
        {
            TickIncomingEoiProbe();
            return;
        }

        if (_state != BusState.DataIn || _talkerDevice is null)
            return;

        if (_outgoingAcknowledgePulseCycles > 0)
        {
            if (--_outgoingAcknowledgePulseCycles == 0)
            {
                _deviceClockLow = false;
                _outgoingAwaitingStart = true;
            }
            return;
        }

        if (_outgoingAwaitingStart)
        {
            if (!_hostDataLow && CLK)
            {
                if (_outgoingBitIndex < 0)
                    BeginOutgoingByte();
                else
                    StartOutgoingBits();
            }
            return;
        }

        if (_outgoingAwaitingEoiAcknowledge)
        {
            if (_hostDataLow)
            {
                _outgoingAwaitingEoiAcknowledge = false;
                _outgoingAwaitingStart = true;
            }
            return;
        }

        if (--_outgoingPhaseCycles > 0)
            return;

        if (_outgoingFinalClockPulse)
        {
            _outgoingFinalClockPulse = false;
            _deviceClockLow = false;
            _outgoingAwaitingStart = true;
            return;
        }

        if (_deviceClockLow)
        {
            _deviceClockLow = false;
            _outgoingPhaseCycles = BitPhaseCycles;
            return;
        }

        if (++_outgoingBitIndex == 8)
        {
            _outgoingBitIndex = -1;
            _deviceDataLow = false;
            EOI = false;
            _deviceClockLow = true;
            _outgoingPhaseCycles = FinalClockPulseCycles;
            _outgoingFinalClockPulse = true;
            return;
        }

        StartOutgoingBits();
    }

    public void Reset()
    {
        _listenerDevice?.Close();
        if (_talkerDevice != _listenerDevice)
            _talkerDevice?.Close();
        _state = BusState.Idle;
        _listenerDevice = null;
        _talkerDevice = null;
        _listenerSecondary = 0;
        _talkerSecondary = 0;
        _hostAtnLow = false;
        _hostClockLow = false;
        _hostDataLow = false;
        _deviceClockLow = false;
        _deviceDataLow = false;
        _incomingByte = 0;
        _incomingBitCount = 0;
        _incomingAcknowledgePending = false;
        _incomingEoiProbeCycles = 0;
        _incomingEoiAcknowledgeCycles = 0;
        _outgoingByte = 0;
        _outgoingBitIndex = -1;
        _outgoingAwaitingStart = false;
        _outgoingAwaitingEoiAcknowledge = false;
        _outgoingPhaseCycles = 0;
        _outgoingAcknowledgePulseCycles = 0;
        _outgoingFinalClockPulse = false;
        EOI = false;
    }

    private BusState SelectDataMode()
    {
        if (_listenerDevice is not null)
        {
            _listenerDevice.OpenForRead(_listenerSecondary);
            return BusState.DataOut;
        }

        if (_talkerDevice is not null)
        {
            _talkerDevice.OpenForWrite(_talkerSecondary);
            _deviceClockLow = true;
            _outgoingAcknowledgePulseCycles = TalkerAcknowledgePulseCycles;
            return BusState.DataIn;
        }

        return BusState.Idle;
    }

    private void AbortTransfer()
    {
        if (_state == BusState.DataOut)
            _listenerDevice?.Close();
        else if (_state == BusState.DataIn)
            _talkerDevice?.Close();

        _incomingByte = 0;
        _incomingBitCount = 0;
        _incomingAcknowledgePending = false;
        _incomingEoiProbeCycles = 0;
        _incomingEoiAcknowledgeCycles = 0;
        _outgoingBitIndex = -1;
        _outgoingAwaitingStart = false;
        _outgoingAwaitingEoiAcknowledge = false;
        _outgoingPhaseCycles = 0;
        _outgoingAcknowledgePulseCycles = 0;
        _outgoingFinalClockPulse = false;
        _deviceClockLow = false;
        _deviceDataLow = false;
        EOI = false;
    }

    private void ShiftIncomingBit(bool released)
    {
        if (released)
            _incomingByte |= (byte)(1 << _incomingBitCount);

        if (++_incomingBitCount != 8)
            return;

        byte data = _incomingByte;
        _incomingByte = 0;
        _incomingBitCount = 0;

        if (_state == BusState.Command)
        {
            RaiseActivity("byte", $"0x{data:X2} command");
            ProcessCommandByte(data);
        }
        else
        {
            RaiseActivity("byte", $"0x{data:X2} written (listen)");
            _listenerDevice?.Write(data);
        }

        ArmIncomingAcknowledge();
    }

    private void ProcessCommandByte(byte command)
    {
        if (command is >= 0x20 and <= 0x3E)
        {
            _listenerDevice = FindDevice(command & 0x1F);
        }
        else if (command == 0x3F)
        {
            _listenerDevice?.Close();
            _listenerDevice = null;
        }
        else if (command is >= 0x40 and <= 0x5E)
        {
            _talkerDevice = FindDevice(command & 0x1F);
        }
        else if (command == 0x5F)
        {
            _talkerDevice?.Close();
            _talkerDevice = null;
        }
        else if (command is >= 0x60 and <= 0x7F)
        {
            byte secondary = (byte)(command & 0x1F);
            if (_listenerDevice is not null) _listenerSecondary = secondary;
            if (_talkerDevice is not null) _talkerSecondary = secondary;
        }
        else if (command is >= 0xE0 and <= 0xEF)
        {
            _listenerDevice?.Close();
            _listenerDevice = null;
        }
        else if (command is >= 0xF0 and <= 0xFF && _listenerDevice is not null)
        {
            _listenerSecondary = (byte)(command & 0x0F);
        }
    }

    private void ArmIncomingAcknowledge()
    {
        if (_devices.Count == 0)
            return;

        _incomingAcknowledgePending = true;
        _deviceDataLow = true;
    }

    private void TickIncomingEoiProbe()
    {
        if (_incomingEoiAcknowledgeCycles > 0)
        {
            if (--_incomingEoiAcknowledgeCycles == 0)
                _deviceDataLow = false;
            return;
        }

        if (_incomingEoiProbeCycles > 0 && --_incomingEoiProbeCycles == 0 && CLK)
        {
            _deviceDataLow = true;
            _incomingEoiAcknowledgeCycles = EoiAcknowledgePulseCycles;
        }
    }

    private void BeginOutgoingByte()
    {
        if (!_talkerDevice!.TryRead(out _outgoingByte))
            return;

        _outgoingAwaitingStart = false;
        _outgoingBitIndex = 0;
        EOI = !_talkerDevice.DataAvailable;
        RaiseActivity("byte", $"0x{_outgoingByte:X2} read (talk){(EOI ? " EOI" : "")}");
        if (EOI)
        {
            _outgoingAwaitingEoiAcknowledge = true;
            return;
        }

        StartOutgoingBits();
    }

    private void StartOutgoingBits()
    {
        _outgoingAwaitingStart = false;
        DriveOutgoingBit();
        _deviceClockLow = true;
        _outgoingPhaseCycles = BitPhaseCycles;
    }

    private void DriveOutgoingBit() => _deviceDataLow = (_outgoingByte & (1 << _outgoingBitIndex)) == 0;

    private IIeeeDevice? FindDevice(int primaryAddress) =>
        _devices.FirstOrDefault(device => device.PrimaryAddress == primaryAddress);

    private void RaiseActivity(string kind, string detail) => Activity?.Invoke(new Vic20SerialActivity(kind, detail));
}
