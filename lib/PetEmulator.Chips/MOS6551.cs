using PetEmulator.Core;
using PetEmulator.Core.Serial;

namespace PetEmulator.Chips;

/// <summary>
/// MOS 6551 ACIA used by the SuperPET RS-232 port. Register offsets are data,
/// status, command and control (0..3). Byte timing and the host terminal are
/// supplied through <see cref="ISerialTransport"/>.
/// </summary>
public sealed class MOS6551 : IMemoryMappedDevice
{
    public const byte ReceiveDataRegisterFull = 0x08;
    public const byte TransmitDataRegisterEmpty = 0x10;
    public const byte InterruptRequest = 0x80;
    public const ushort RegisterCount = 4;

    private readonly ushort _baseAddress;
    private readonly ISerialTransport _transport;
    private byte _command;
    private byte _control;
    private bool _interruptPending;

    public MOS6551(ISerialTransport transport, string name = "MOS6551 ACIA", ushort baseAddress = 0)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        if ((uint)baseAddress + RegisterCount > 0x1_0000)
            throw new ArgumentOutOfRangeException(nameof(baseAddress), "The ACIA must fit in the 16-bit address space.");

        Name = name;
        _baseAddress = baseAddress;
        _transport.ByteReceived += OnByteReceived;
        Reset();
    }

    public string Name { get; }

    public uint Length => RegisterCount;

    public bool Irq => _interruptPending;

    public byte Command => _command;

    public byte Control => _control;

    public Action<BusAccess>? Observer { get; set; }

    public void Reset()
    {
        _command = 0;
        _control = 0;
        _interruptPending = false;
    }

    public void Tick(ulong cycles)
    {
        // The transport owns wall-clock/baud timing.
    }

    public byte Read(ushort address)
    {
        var offset = GetOffset(address);
        var value = offset switch
        {
            0 => _transport.TryReadByte(out var received) ? received : (byte)0,
            1 => ReadStatus(),
            2 => _command,
            _ => _control
        };
        Observer?.Invoke(new BusAccess(IsWrite: false, address, value));
        return value;
    }

    public void Write(ushort address, byte value)
    {
        var offset = GetOffset(address);
        switch (offset)
        {
            case 0:
                _transport.WriteByte(value);
                break;
            case 1:
                // The 6551 programmed-reset write is intentionally not modeled yet.
                break;
            case 2:
                _command = value;
                break;
            default:
                _control = value;
                break;
        }
        Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
    }

    private byte ReadStatus()
    {
        var status = (byte)0;
        if (_transport.HasByteAvailable)
            status |= ReceiveDataRegisterFull;
        if (_transport.TransmitReady)
            status |= TransmitDataRegisterEmpty;
        if (_interruptPending)
            status |= InterruptRequest;

        _interruptPending = false;
        return status;
    }

    private void OnByteReceived()
    {
        if ((_command & 0x02) == 0)
            _interruptPending = true;
    }

    private ushort GetOffset(ushort address)
    {
        if (address < _baseAddress || address >= _baseAddress + Length)
            throw new ArgumentOutOfRangeException(nameof(address), address, "Address is outside the ACIA range.");

        return (ushort)(address - _baseAddress);
    }
}
