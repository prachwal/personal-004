namespace PetEmulator.Kaypro;

/// <summary>Kaypro's two-byte PIO/latch windows used for printer and drive signals.</summary>
public sealed class KayproPio
{
    public const ushort BasePort = 0x08;

    private byte _portA;
    private byte _portB;
    private byte _portAControl;
    private byte _portBControl;

    public event Action<byte>? PortAWritten;

    public byte Read(ushort port) => port switch
    {
        0x08 => _portA,
        0x09 => _portAControl,
        0x0A => _portB,
        0x0B => _portBControl,
        _ => 0xFF,
    };

    public void Write(ushort port, byte value)
    {
        switch (port)
        {
            case 0x08:
                _portA = value;
                PortAWritten?.Invoke(value);
                break;
            case 0x09:
                _portAControl = value;
                break;
            case 0x0A:
                _portB = value;
                break;
            case 0x0B:
                _portBControl = value;
                break;
        }
    }

    public void Reset()
    {
        _portA = 0;
        _portB = 0;
        _portAControl = 0;
        _portBControl = 0;
    }
}
