using PetEmulator.Core;
using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6809;

public partial class M6809Cpu
{
protected override ushort ResolveDirectAddress(byte offset) => (ushort)((State.DP << 8) | offset);
    protected override M6800Flags ConditionCodes => State.Flags;

    public ushort FetchIndexed(out int extraCycles)
    {
        return ResolveIndexed(out extraCycles);
    }

    public byte LdIndexed(out int extraCycles)
    {
        ushort addr = FetchIndexed(out extraCycles);
        return Mmu.Read(addr);
    }

    public ushort Ld16Indexed(out int extraCycles)
    {
        ushort addr = FetchIndexed(out extraCycles);
        return Read16(addr);
    }

    /// <summary>
    /// Phase D1: Resolve indexed addressing modes per the postbyte table.
    /// Returns the effective address and sets extraCycles (0-5 beyond the base).
    /// </summary>
    private ushort ResolveIndexed(out int extraCycles)
    {
        byte postbyte = Fetch();
        extraCycles = 0;
        // Bit 4 is the sign bit for the compact 5-bit form (0RRnnnnn).
        // It becomes the indirect selector only in the extended 1RR mode.
        bool indirect = (postbyte & 0x80) != 0 && (postbyte & 0x10) != 0;
        int rr = (postbyte & 0x60) >> 5;
        ushort baseReg = GetIndexReg(rr);

        if ((postbyte & 0x80) == 0)
        {
            // 0RRnnnnn: 5-bit signed offset
            int offset5 = postbyte & 0x1F;
            if ((offset5 & 0x10) != 0) offset5 = offset5 - 32;  // sign extend to -16..-1
            ushort addr = (ushort)(baseReg + offset5);
            extraCycles = 1;
            return indirect ? Read16(addr) : addr;
        }

        // 1RR mode
        int mode = postbyte & 0x0F;
        ushort ea;

        switch (mode)
        {
            case 0x00: // ,R+
                ea = baseReg;
                SetIndexReg(rr, (ushort)(baseReg + 1));
                extraCycles = 2;
                break;

            case 0x01: // ,R++
                ea = baseReg;
                SetIndexReg(rr, (ushort)(baseReg + 2));
                extraCycles = 3;
                break;

            case 0x02: // ,−R
                SetIndexReg(rr, (ushort)(baseReg - 1));
                ea = (ushort)(baseReg - 1);
                extraCycles = 2;
                break;

            case 0x03: // ,−−R
                SetIndexReg(rr, (ushort)(baseReg - 2));
                ea = (ushort)(baseReg - 2);
                extraCycles = 3;
                break;

            case 0x04: // ,R
                ea = baseReg;
                extraCycles = 0;
                break;

            case 0x05: // B,R
                ea = (ushort)(baseReg + (sbyte)State.B);
                extraCycles = 1;
                break;

            case 0x06: // A,R
                ea = (ushort)(baseReg + (sbyte)State.A);
                extraCycles = 1;
                break;

            case 0x08: // n8,R
                {
                    sbyte n8 = (sbyte)Fetch();
                    ea = (ushort)(baseReg + n8);
                    extraCycles = 1;
                }
                break;

            case 0x09: // n16,R
                {
                    ushort n16 = Fetch16();
                    ea = (ushort)(baseReg + (short)n16);
                    extraCycles = 4;
                }
                break;

            case 0x0B: // D,R
                ea = (ushort)(baseReg + (short)State.D);
                extraCycles = 4;
                break;

            case 0x0C: // n8,PCR
                {
                    sbyte n8 = (sbyte)Fetch();
                    ea = (ushort)(State.PC + n8);
                    extraCycles = 1;
                }
                break;

            case 0x0D: // n16,PCR
                {
                    ushort n16 = Fetch16();
                    ea = (ushort)(State.PC + (short)n16);
                    extraCycles = 5;
                }
                break;

            case 0x0F: // [n16]
                ea = Fetch16();
                extraCycles = 4;
                break;

            default:
                // ponytail: invalid postbyte mode = no-op addressing
                ea = 0;
                extraCycles = 1;
                break;
        }

        // Add indirect cost if indirect bit is set (except for ,R+ and ,−R forms)
        if (indirect && mode != 0x00 && mode != 0x02)
        {
            ushort addrRef = ea;
            ea = Read16(addrRef);
            extraCycles += 3;
        }

        return ea;
    }

    private ushort GetIndexReg(int rr)
    {
        return rr switch
        {
            0 => State.X,
            1 => State.Y,
            2 => State.U,
            3 => State.S,
            _ => 0,
        };
    }

    private void SetIndexReg(int rr, ushort value)
    {
        switch (rr)
        {
            case 0:
                State.X = value;
                break;
            case 1:
                State.Y = value;
                break;
            case 2:
                State.U = value;
                break;
            case 3:
                State.S = value;
                _nmiArmed = true;  // NMI armed on S write
                break;
        }
    }

    // Special indexed addressing for extended indirect [n16]
    private ushort FetchExtendedIndirect()
    {
        byte postbyte = Fetch();
        if (postbyte == 0x9F)
        {
            // [n16] extended indirect
            ushort addrRef = Fetch16();
            return Read16(addrRef);
        }
        return 0;  // ponytail: error case
    }

    public byte FetchSigned()
    {
        return Fetch();
    }
}
