namespace PetEmulator.Pet.Ieee488;

/// <summary>One protocol-level activity on a <see cref="PetIeeeBus"/>: a control-line change
/// (Kind="line-change", e.g. Detail="ATN=true") or a byte moved over DIO (Kind="byte", e.g.
/// Detail="0x41 written"). For a future debug surface to observe *why* bus state changed, not
/// just that a memory-mapped line flipped.</summary>
public readonly record struct IeeeBusActivity(string Kind, string Detail);

/// <summary>
/// LISTEN/TALK/UNLISTEN/UNTALK/OPEN/CLOSE command decode and DAV/NRFD/NDAC handshake state
/// machine, addressable-device-agnostic (any <see cref="IIeeeDevice"/>).
///
/// Wired into a PET machine's Pia2/Via by <see cref="PetIeeeBusBinding"/> (edge/CA2/CB2 driven,
/// not just raw pin forwarding).
/// </summary>
public sealed class PetIeeeBus
{
    private enum BusState { Idle, Command, DataOut, DataIn }

    private readonly List<IIeeeDevice> _devices = [];
    private BusState _state = BusState.Idle;
    private int _listenerAddr = -1;
    private int _talkerAddr = -1;
    private byte _listenerSec;
    private byte _talkerSec;
    private IIeeeDevice? _listenerDevice;
    private IIeeeDevice? _talkerDevice;
    private byte _lastDio;
    private byte _cachedInput;
    private bool _hasCachedInput;
    private bool _dataInRead;
    private int _dataInFetchDelay;
    private int _writeAckDelay;
    private bool _lastAtn;
    private bool _pendingCommand;

    /// <summary>Fires for every control-line change and byte transfer this bus processes. Optional
    /// (nullable multicast delegate) - zero-cost and behavior-neutral when nobody subscribes.</summary>
    public event Action<IeeeBusActivity>? Activity;

    public byte LastDio => _lastDio;
    public bool DAV { get; private set; }
    public bool NRFD { get; private set; }
    public bool NDAC { get; private set; }

    /// <summary>End Or Identify: asserted by the talker device with the last byte of a transfer
    /// (PET reads this on PIA1 PA6). Cleared whenever ATN is asserted or a new talker transfer
    /// starts.</summary>
    public bool EOI { get; private set; }

    public bool IsIdle => _state == BusState.Idle;

    public byte GetCurrentDio() => _hasCachedInput ? _cachedInput : (byte)0xFF;

    /// <summary>Attaches a device, replacing whatever was already at the same
    /// <see cref="IIeeeDevice.PrimaryAddress"/> - two devices sharing an address would otherwise
    /// both sit in <see cref="_devices"/> with the older one still winning every lookup (it's a
    /// <c>FirstOrDefault</c>), silently ignoring a re-mount (e.g. swapping the disk in drive 8).</summary>
    public void AttachDevice(IIeeeDevice device)
    {
        _devices.RemoveAll(d => d.PrimaryAddress == device.PrimaryAddress);
        _devices.Add(device);
    }

    public void OnATNWrite(bool atn)
    {
        if (atn == _lastAtn) return;
        _lastAtn = atn;
        RaiseActivity("line-change", $"ATN={(atn ? "true" : "false")}");

        if (atn)
        {
            _pendingCommand = true;
            DAV = false;
            NRFD = false;
            NDAC = true;
            EOI = false;
            _cachedInput = 0xFF;
            _hasCachedInput = false;

            switch (_state)
            {
                case BusState.DataOut:
                    _listenerDevice?.Close();
                    break;
                case BusState.DataIn:
                    _talkerDevice?.Close();
                    break;
            }
            _state = BusState.Command;
        }
        else
        {
            if (_listenerDevice != null)
            {
                _state = BusState.DataOut;
                _listenerDevice.OpenForRead(_listenerSec);
            }
            else if (_talkerDevice != null)
            {
                _state = BusState.DataIn;
                _talkerDevice.OpenForWrite(_talkerSec);
                // A write-ack release armed by the command bytes that just addressed this talker
                // (see ArmWriteAck) can still be ticking down - if it fires after Tick()'s DataIn
                // branch has already asserted DAV for a freshly cached read byte, it stomps the
                // line back to false and the KERNAL's read loop hangs waiting for a DAV it will
                // never see rise again. The read side owns DAV/NRFD/NDAC from here.
                _writeAckDelay = 0;
            }
            else
            {
                _state = BusState.Idle;
            }
        }
    }

    public void MarkPendingCommand() => _pendingCommand = true;

    public void OnDioWrite(byte data)
    {
        _lastDio = data;
        RaiseActivity("byte", $"0x{data:X2} written");

        if (_pendingCommand)
        {
            _pendingCommand = false;
            if (IsUnaddressedChannelByte(data))
                return;
            ProcessCommandByte(data);
            AcceptHandshake();
            ArmWriteAck();
            return;
        }

        switch (_state)
        {
            case BusState.Command:
                if (IsUnaddressedChannelByte(data))
                    return;
                ProcessCommandByte(data);
                ArmWriteAck();
                break;
            case BusState.DataOut:
                _listenerDevice?.Write(data);
                AcceptHandshake();
                ArmWriteAck();
                break;
        }
    }

    /// <summary>Schedules <see cref="CompleteHandshake"/> a few cycles after any byte this bus
    /// just finished processing (LISTEN/TALK/SECONDARY under ATN, or a filename/data byte to the
    /// listener) - every one of those paths leaves NRFD asserted (busy) via
    /// <see cref="AcceptHandshake"/> or <see cref="CommandHandshake"/> with nothing to release it
    /// again. Real listener firmware would release it almost immediately once done; our
    /// processing already finished synchronously by the time this is called, so a short settle
    /// delay (mirroring <see cref="Tick"/>'s own <c>_dataInFetchDelay</c> for the read side) is
    /// all that's needed - without it, the KERNAL's own "wait for the listener to be ready before
    /// sending the next byte" poll loop (real IEEE-488 behavior, not a bug in the ROM) never sees
    /// NRFD go ready and hangs forever, e.g. stuck printing "SEARCHING FOR ..." on a real LOAD.
    /// Uses the same <see cref="SettleDelayCycles"/> as the read side for the identical reason -
    /// see that constant's doc comment.</summary>
    private void ArmWriteAck() => _writeAckDelay = SettleDelayCycles;

    /// <summary>Cycles a talker/listener settle delay ticks down before releasing a handshake line
    /// or pre-fetching the next byte (see <see cref="ArmWriteAck"/> and <see cref="OnDioRead"/>/
    /// <see cref="Tick"/>). Was 32 - too short: traced with <see cref="PetEmulator.Pet.PetMachine.BusObserver"/>
    /// against a real multi-hundred-byte LOAD (see docs/pet/disk-testing-strategy.md), the
    /// periodic ~60Hz keyboard-scan IRQ takes ~550-600 instructions (>1,000 cycles) to run, comfortably
    /// outlasting a 32-cycle window; this let the read side silently pre-fetch and re-assert DAV
    /// for the *next* byte while the KERNAL's read-wait loop was still parked inside that ISR,
    /// permanently hiding the DAV release the resumed KERNAL code was waiting to see. 4,000 clears
    /// that ISR with margin while staying well under the ~16,667-cycle jiffy period, so it doesn't
    /// perceptibly slow real transfers (disassembly of the real ACPTR routine at $F18C confirms it
    /// has no SEI/CLI guard either - real hardware only avoids this race because a real drive's own
    /// firmware is far slower than either delay).</summary>
    private const int SettleDelayCycles = 4_000;

    public byte OnDioRead()
    {
        if (_hasCachedInput)
        {
            _hasCachedInput = false;
            _lastDio = _cachedInput;
            _dataInRead = true;
            DAV = false;
            _dataInFetchDelay = SettleDelayCycles;
            ProvideHandshake();
            RaiseActivity("byte", $"0x{_cachedInput:X2} read");
            return _cachedInput;
        }
        _lastDio = 0xFF;
        return 0xFF;
    }

    public byte GetViaPortBInput()
    {
        byte result = 0;
        if (!NDAC) result |= 0x01;
        if (!NRFD) result |= 0x40;
        if (!DAV) result |= 0x80;
        return result;
    }

    public void CompleteHandshake()
    {
        NRFD = false;
        NDAC = true;
        RaiseActivity("line-change", "NRFD=false NDAC=true");
    }

    public void SetDAVState(bool asserted)
    {
        DAV = asserted;
        RaiseActivity("line-change", $"DAV={(asserted ? "true" : "false")}");
    }

    public void SetNdacAccepted(bool accepted)
    {
        if (!accepted)
            return;

        NDAC = true;
        RaiseActivity("line-change", "NDAC=true");
        if (_state == BusState.DataIn && _dataInRead)
        {
            DAV = false;
            _dataInRead = false;
            _dataInFetchDelay = SettleDelayCycles;
            RaiseActivity("line-change", "DAV=false");
        }
    }

    public void AcceptHandshake()
    {
        NRFD = true;
        NDAC = false;
        RaiseActivity("line-change", "NRFD=true NDAC=false");
    }

    /// <summary>Advances the talker-side data fetch delay and the write-ack settle delay (see
    /// <see cref="ArmWriteAck"/>); call once per bus/device tick.</summary>
    public void Tick()
    {
        if (_writeAckDelay > 0 && --_writeAckDelay == 0)
        {
            CompleteHandshake();
            // CommandHandshake() (every command byte) and AcceptHandshake() both leave DAV
            // asserted - real protocol only releases it once the talker (the PET, driving DAV
            // itself via PIA2 CB2) sees NDAC go ready and lets go. The auto-processed bypass path
            // never does that CB2 toggle, so nothing else ever clears it; left stuck asserted,
            // the KERNAL's own "wait for DAV to settle before the next byte" check hangs forever.
            SetDAVState(false);
        }

        if (_hasCachedInput || _state != BusState.DataIn || _talkerDevice is not { } device)
            return;

        if (_dataInFetchDelay > 0)
        {
            _dataInFetchDelay--;
            return;
        }

        if (device.TryRead(out byte data))
        {
            _cachedInput = data;
            _hasCachedInput = true;
            _dataInRead = false;
            DAV = true;
            NRFD = false;
            NDAC = false;
            EOI = !device.DataAvailable;
        }
    }

    public void Reset()
    {
        _listenerDevice?.Close();
        _talkerDevice?.Close();
        _state = BusState.Idle;
        _listenerAddr = -1;
        _talkerAddr = -1;
        _listenerDevice = null;
        _talkerDevice = null;
        _lastDio = 0;
        DAV = false;
        NRFD = true;
        NDAC = true;
        EOI = false;
        _listenerSec = 0;
        _talkerSec = 0;
        _cachedInput = 0;
        _hasCachedInput = false;
        _dataInRead = false;
        _dataInFetchDelay = 0;
        _writeAckDelay = 0;
    }

    private void ProcessCommandByte(byte cmd)
    {
        if (cmd is >= 0x20 and <= 0x3E)
        {
            int dev = cmd & 0x1F;
            _listenerAddr = dev;
            _listenerDevice = FindDevice(_listenerAddr);
            CommandHandshake();
        }
        else if (cmd == 0x3F)
        {
            _listenerDevice?.Close();
            _listenerAddr = -1;
            _listenerDevice = null;
            CommandHandshake();
        }
        else if (cmd is >= 0x40 and <= 0x5E)
        {
            int dev = cmd & 0x1F;
            _talkerAddr = dev;
            _talkerDevice = FindDevice(_talkerAddr);
            CommandHandshake();
        }
        else if (cmd == 0x5F)
        {
            _talkerDevice?.Close();
            _talkerAddr = -1;
            _talkerDevice = null;
            CommandHandshake();
        }
        else if (cmd is >= 0x60 and <= 0x7F)
        {
            byte sec = (byte)(cmd & 0x1F);
            if (_listenerDevice != null) _listenerSec = sec;
            if (_talkerDevice != null) _talkerSec = sec;
            CommandHandshake();
        }
        else if (cmd is >= 0xE0 and <= 0xEF)
        {
            // KERNAL 4 uses CLOSE SA (not UNLISTEN) to end the filename/data phase.
            _listenerDevice?.Close();
            _listenerAddr = -1;
            _listenerDevice = null;
            CommandHandshake();
        }
        else if (cmd is >= 0xF0 and <= 0xFF)
        {
            byte sec = (byte)(cmd & 0x0F);
            if (_listenerDevice != null) _listenerSec = sec;
            CommandHandshake();
        }
        else
        {
            CommandHandshake();
        }
    }

    private bool IsUnaddressedChannelByte(byte cmd) =>
        _listenerDevice == null && _talkerDevice == null && cmd is >= 0x60 and <= 0xFF;

    private void CommandHandshake()
    {
        DAV = true;
        NRFD = true;
        NDAC = true;
        RaiseActivity("line-change", "DAV=true NRFD=true NDAC=true");
    }

    private void ProvideHandshake()
    {
        NRFD = false;
        NDAC = true;
        RaiseActivity("line-change", "NRFD=false NDAC=true");
    }

    private IIeeeDevice? FindDevice(int primaryAddr) =>
        _devices.FirstOrDefault(device => device.PrimaryAddress == primaryAddr);

    private void RaiseActivity(string kind, string detail) => Activity?.Invoke(new IeeeBusActivity(kind, detail));
}
