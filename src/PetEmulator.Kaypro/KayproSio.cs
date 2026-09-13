namespace PetEmulator.Kaypro;

/// <summary>Polling subset of the Kaypro Z80 SIO used by the monitor console.</summary>
public sealed class KayproSio
{
    public const ushort BasePort = 0x04;
    public const ushort ChannelAData = 0x04;
    public const ushort ChannelBData = 0x05;
    public const ushort ChannelAControl = 0x06;
    public const ushort ChannelBControl = 0x07;

    private readonly Queue<byte> _keyboard = new();

    public event Action<byte>? Transmitted;

    public byte Read(ushort port) => port switch
    {
        ChannelBData => _keyboard.Count == 0 ? (byte)0 : _keyboard.Dequeue(),
        ChannelAData => 0,
        ChannelAControl or ChannelBControl => (byte)(0x04 | (_keyboard.Count == 0 ? 0 : 0x01)),
        _ => 0xFF,
    };

    public void Write(ushort port, byte value)
    {
        if (port is ChannelAData or ChannelBData)
            Transmitted?.Invoke(value);
    }

    public void EnqueueKeyboardByte(byte value) => _keyboard.Enqueue(value);

    public void Reset() => _keyboard.Clear();
}
