namespace PetEmulator.Cpu6809;

/// <summary>6809 condition code register (CC): E F H I N Z V C (bits 7..0).</summary>
public sealed class M6809Flags
{
    public bool E;  // bit 7 — entire frame (affects RTI)
    public bool F;  // bit 6 — FIRQ mask
    public bool H;  // bit 5 — half carry (ADD/ADC only)
    public bool I;  // bit 4 — IRQ mask
    public bool N;  // bit 3 — negative
    public bool Z;  // bit 2 — zero
    public bool V;  // bit 1 — two's-complement overflow
    public bool C;  // bit 0 — carry/borrow

    /// <summary>Pack all 8 flag bits into a byte: E F H I N Z V C (bits 7..0).</summary>
    public byte ToByte()
    {
        byte value = 0;
        if (E) value |= 0x80;
        if (F) value |= 0x40;
        if (H) value |= 0x20;
        if (I) value |= 0x10;
        if (N) value |= 0x08;
        if (Z) value |= 0x04;
        if (V) value |= 0x02;
        if (C) value |= 0x01;
        return value;
    }

    /// <summary>Unpack 8 bits from a byte into flag fields.</summary>
    public static M6809Flags FromByte(byte value)
    {
        return new M6809Flags
        {
            E = (value & 0x80) != 0,
            F = (value & 0x40) != 0,
            H = (value & 0x20) != 0,
            I = (value & 0x10) != 0,
            N = (value & 0x08) != 0,
            Z = (value & 0x04) != 0,
            V = (value & 0x02) != 0,
            C = (value & 0x01) != 0,
        };
    }
}
