namespace PetEmulator.Cpu6800;

/// <summary>Motorola 6800 condition-code register.</summary>
public class M6800Flags
{
    public bool H { get; set; }
    public bool I { get; set; }
    public bool N { get; set; }
    public bool Z { get; set; }
    public bool V { get; set; }
    public bool C { get; set; }

    public byte ToByte()
    {
        byte value = 0;
        if (H) value |= 0x20;
        if (I) value |= 0x10;
        if (N) value |= 0x08;
        if (Z) value |= 0x04;
        if (V) value |= 0x02;
        if (C) value |= 0x01;
        return value;
    }

    public static M6800Flags FromByte(byte value) =>
        new()
        {
            H = (value & 0x20) != 0,
            I = (value & 0x10) != 0,
            N = (value & 0x08) != 0,
            Z = (value & 0x04) != 0,
            V = (value & 0x02) != 0,
            C = (value & 0x01) != 0,
        };

    public void Reset()
    {
        H = false;
        I = true;
        N = false;
        Z = false;
        V = false;
        C = false;
    }
}
