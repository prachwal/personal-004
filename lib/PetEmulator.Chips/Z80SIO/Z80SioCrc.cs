namespace PetEmulator.Chips;

/// <summary>CRC-16/X-25 used by the SDLC framing model.</summary>
public static class Z80SioCrc
{
    public const ushort InitialValue = 0xFFFF;
    public const ushort Polynomial = 0x8408;
    public const ushort FinalXor = 0xFFFF;

    public static ushort ComputeSdlc(IEnumerable<byte> bytes)
    {
        var crc = InitialValue;
        foreach (var value in bytes)
            crc = UpdateByte(crc, value);
        return (ushort)(crc ^ FinalXor);
    }

    public static ushort UpdateByte(ushort crc, byte value)
    {
        var current = crc;
        for (var bit = 0; bit < 8; bit++)
        {
            var mix = (current ^ value) & 1;
            current >>= 1;
            if (mix != 0) current ^= Polynomial;
            value >>= 1;
        }
        return current;
    }
}
