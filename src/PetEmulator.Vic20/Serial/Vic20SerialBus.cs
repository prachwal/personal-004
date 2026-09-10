using PetEmulator.Pet.Ieee488;

namespace PetEmulator.Vic20.Serial;

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
    private byte _outgoingByte;
    private int _outgoingBitIndex = -1;

    /// <summary>Resolved ATN line level.</summary>
    public bool ATN => !_hostAtnLow;

    /// <summary>Resolved CLK line level.</summary>
    public bool CLK => !(_hostClockLow || _deviceClockLow);

    /// <summary>Resolved DATA line level.</summary>
    public bool DATA => !(_hostDataLow || _deviceDataLow);

    /// <summary>True while the final byte from the current talker is being shifted out.</summary>
    public bool EOI { get; private set; }

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
        AbortTransfer();
        _state = released ? SelectDataMode() : BusState.Command;
    }

    /// <summary>Drives or releases the computer's CLK output. <see langword="false"/> asserts it.</summary>
    public void SetHostClock(bool released)
    {
        bool wasHigh = CLK;
        _hostClockLow = !released;
        if (!wasHigh && CLK && _state is BusState.Command or BusState.DataOut)
            ShiftIncomingBit(DATA);
    }

    /// <summary>Drives or releases the computer's DATA output. <see langword="false"/> asserts it.</summary>
    public void SetHostData(bool released) => _hostDataLow = !released;

    /// <summary>
    /// Advances device-originated TALK traffic by one clock phase. The listener side is edge-driven
    /// by <see cref="SetHostClock"/>.
    /// </summary>
    public void Tick()
    {
        if (_state != BusState.DataIn || _talkerDevice is null)
            return;

        if (_outgoingBitIndex < 0)
        {
            _deviceDataLow = false;
            EOI = false;
            if (!_talkerDevice.TryRead(out _outgoingByte))
                return;

            _outgoingBitIndex = 0;
            EOI = !_talkerDevice.DataAvailable;
            DriveOutgoingBit();
            _deviceClockLow = true;
            return;
        }

        if (_deviceClockLow)
        {
            _deviceClockLow = false;
            if (CLK)
                AdvanceOutgoingBit();
            return;
        }

        DriveOutgoingBit();
        _deviceClockLow = true;
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
        _outgoingByte = 0;
        _outgoingBitIndex = -1;
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
        _outgoingBitIndex = -1;
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
            ProcessCommandByte(data);
        else
            _listenerDevice?.Write(data);
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

    private void DriveOutgoingBit() => _deviceDataLow = (_outgoingByte & (1 << _outgoingBitIndex)) == 0;

    private void AdvanceOutgoingBit()
    {
        if (++_outgoingBitIndex == 8)
            _outgoingBitIndex = -1;
    }

    private IIeeeDevice? FindDevice(int primaryAddress) =>
        _devices.FirstOrDefault(device => device.PrimaryAddress == primaryAddress);
}
