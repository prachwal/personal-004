using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6809;

/// <summary>6809 condition code register (CC): E F H I N Z V C (bits 7..0).</summary>
public class M6809Flags : M6800Flags
{
    public bool E { get; set; }  // bit 7 — entire frame (affects RTI)
    public bool F { get; set; }  // bit 6 — FIRQ mask

    /// <summary>Pack all 8 flag bits into a byte: E F H I N Z V C (bits 7..0).</summary>
    public override byte ToByte()
    {
        byte value = 0;
        if (E) value |= 0x80;
        if (F) value |= 0x40;
        return (byte)(value | base.ToByte());
    }

    /// <summary>Unpack 8 bits from a byte into flag fields.</summary>
    public new static M6809Flags FromByte(byte value)
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

    public override void Reset()
    {
        base.Reset();
        E = false;
        F = true;
    }
}
