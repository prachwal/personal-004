using PetEmulator.Core;
using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6809;

public partial class M6809Cpu
{
private int Jmp(ushort addr)
    {
        State.PC = addr;
        return 3;
    }

    // Single-byte inherent
    private int Sync()
    {
        State.Halted = true;
        return 4;
    }

    private int Daa()
    {
        byte a = State.A;
        byte result = a;
        var carry = State.Flags.C || a > 0x99;
        if (State.Flags.H || (a & 0x0F) > 0x09)
            result = (byte)(a + 0x06);
        if (carry)
            result = (byte)(result + 0x60);
        State.Flags.N = (result & 0x80) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = false;
        State.Flags.C = carry;
        State.A = result;
        return 2;
    }

    private int Orcc(byte imm)
    {
        var flags = M6809Flags.FromByte(State.Flags.ToByte());
        flags = M6809Flags.FromByte((byte)(flags.ToByte() | imm));
        State.Flags = flags;
        return 3;
    }

    private int Andcc(byte imm)
    {
        var flags = M6809Flags.FromByte(State.Flags.ToByte());
        flags = M6809Flags.FromByte((byte)(flags.ToByte() & imm));
        State.Flags = flags;
        return 3;
    }

    private int Sex()
    {
        if ((State.B & 0x80) != 0)
            State.A = 0xFF;
        else
            State.A = 0x00;
        State.Flags.N = (State.A & 0x80) != 0;
        // SEX affects N/Z from the resulting 16-bit D register, not A alone.
        State.Flags.Z = State.D == 0;
        return 2;
    }

    private int Exg(byte postbyte)
    {
        byte src = (byte)(postbyte >> 4);
        byte dst = (byte)(postbyte & 0x0F);
        GetReg(src, out ushort srcVal, out bool src16);
        GetReg(dst, out ushort dstVal, out bool dst16);
        SetReg(src, dstVal, src16);
        SetReg(dst, srcVal, dst16);
        return 8;
    }

    private int Tfr(byte postbyte)
    {
        byte src = (byte)(postbyte >> 4);
        byte dst = (byte)(postbyte & 0x0F);
        GetReg(src, out ushort srcVal, out bool src16);
        SetReg(dst, srcVal, src16);
        return 6;
    }

    protected virtual void GetReg(byte code, out ushort value, out bool is16)
    {
        is16 = true;
        value = code switch
        {
            0x0 => State.D,
            0x1 => State.X,
            0x2 => State.Y,
            0x3 => State.U,
            0x4 => State.S,
            0x5 => State.PC,
            0x8 => (ushort)State.A,
            0x9 => (ushort)State.B,
            0xA => (ushort)State.Flags.ToByte(),
            0xB => (ushort)State.DP,
            _ => 0,
        };
        is16 = code is not (0x8 or 0x9 or 0xA or 0xB);
    }

    protected virtual void SetReg(byte code, ushort value, bool is16)
    {
        switch (code)
        {
            case 0x0:
                State.D = value;
                break;
            case 0x1:
                State.X = value;
                break;
            case 0x2:
                State.Y = value;
                break;
            case 0x3:
                State.U = value;
                break;
            case 0x4:
                State.S = value;
                _nmiArmed = true;
                break;
            case 0x5:
                State.PC = value;
                break;
            case 0x8:
                State.A = (byte)value;
                break;
            case 0x9:
                State.B = (byte)value;
                break;
            case 0xA:
                State.Flags = M6809Flags.FromByte((byte)value);
                break;
            case 0xB:
                State.DP = (byte)value;
                break;
        }
    }

    // Long branches (16-bit offset)
    private int Lbra(ushort offset)
    {
        State.PC = (ushort)(State.PC + (short)offset);
        return 5;
    }

    private int Lbsr(ushort offset)
    {
        // BSR/LBSR use the hardware stack; the U stack is reserved for PSHU/PULU.
        PushS16(State.PC);
        State.PC = (ushort)(State.PC + (short)offset);
        return 9;
    }

    private int Lbrn(ushort offset)
    {
        return 5;
    }

    private int Lbhi(ushort offset)
    {
        if (!State.Flags.C && !State.Flags.Z)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbls(ushort offset)
    {
        if (State.Flags.C || State.Flags.Z)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbcc(ushort offset)
    {
        if (!State.Flags.C)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbcs(ushort offset)
    {
        if (State.Flags.C)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbne(ushort offset)
    {
        if (!State.Flags.Z)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbeq(ushort offset)
    {
        if (State.Flags.Z)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbvc(ushort offset)
    {
        if (!State.Flags.V)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbvs(ushort offset)
    {
        if (State.Flags.V)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbpl(ushort offset)
    {
        if (!State.Flags.N)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbmi(ushort offset)
    {
        if (State.Flags.N)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbge(ushort offset)
    {
        if ((State.Flags.N && State.Flags.V) || (!State.Flags.N && !State.Flags.V))
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lblt(ushort offset)
    {
        if ((State.Flags.N && !State.Flags.V) || (!State.Flags.N && State.Flags.V))
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbgt(ushort offset)
    {
        if (!State.Flags.Z && ((State.Flags.N && State.Flags.V) || (!State.Flags.N && !State.Flags.V)))
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lble(ushort offset)
    {
        if (State.Flags.Z || ((State.Flags.N && !State.Flags.V) || (!State.Flags.N && State.Flags.V)))
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    // LEA instructions
    private int Leax(ushort addr)
    {
        State.X = addr;
        State.Flags.Z = addr == 0;
        return 4;
    }

    private int Leay(ushort addr)
    {
        State.Y = addr;
        State.Flags.Z = addr == 0;
        return 4;
    }

    private int Leas(ushort addr)
    {
        State.S = addr;
        _nmiArmed = true;
        return 4;
    }

    private int Leau(ushort addr)
    {
        State.U = addr;
        return 4;
    }

    // Stack operations
    private int Pshs(byte mask)
    {
        int cycles = 5;
        if ((mask & 0x80) != 0) { PushS16(State.PC); cycles++; }
        if ((mask & 0x40) != 0) { PushS16(State.U); cycles++; }
        if ((mask & 0x20) != 0) { PushS16(State.Y); cycles++; }
        if ((mask & 0x10) != 0) { PushS16(State.X); cycles++; }
        if ((mask & 0x08) != 0) { PushS8(State.DP); cycles++; }
        if ((mask & 0x04) != 0) { PushS8(State.B); cycles++; }
        if ((mask & 0x02) != 0) { PushS8(State.A); cycles++; }
        if ((mask & 0x01) != 0) { PushS8(State.Flags.ToByte()); cycles++; }
        return cycles;
    }

    private int Puls(byte mask)
    {
        int cycles = 5;
        if ((mask & 0x01) != 0) { State.Flags = M6809Flags.FromByte(PopS8()); cycles++; }
        if ((mask & 0x02) != 0) { State.A = PopS8(); cycles++; }
        if ((mask & 0x04) != 0) { State.B = PopS8(); cycles++; }
        if ((mask & 0x08) != 0) { State.DP = PopS8(); cycles++; }
        if ((mask & 0x10) != 0) { State.X = PopS16(); cycles += 2; }
        if ((mask & 0x20) != 0) { State.Y = PopS16(); cycles += 2; }
        if ((mask & 0x40) != 0) { State.U = PopS16(); cycles += 2; }
        if ((mask & 0x80) != 0) { State.PC = PopS16(); cycles += 2; }
        return cycles;
    }

    private int Pshu(byte mask)
    {
        int cycles = 5;
        if ((mask & 0x80) != 0) { PushU16(State.PC); cycles++; }
        if ((mask & 0x40) != 0) { PushU16(State.S); cycles++; }
        if ((mask & 0x20) != 0) { PushU16(State.Y); cycles++; }
        if ((mask & 0x10) != 0) { PushU16(State.X); cycles++; }
        if ((mask & 0x08) != 0) { PushU8(State.DP); cycles++; }
        if ((mask & 0x04) != 0) { PushU8(State.B); cycles++; }
        if ((mask & 0x02) != 0) { PushU8(State.A); cycles++; }
        if ((mask & 0x01) != 0) { PushU8(State.Flags.ToByte()); cycles++; }
        return cycles;
    }

    private int Pulu(byte mask)
    {
        int cycles = 5;
        if ((mask & 0x01) != 0) { State.Flags = M6809Flags.FromByte(PopU8()); cycles++; }
        if ((mask & 0x02) != 0) { State.A = PopU8(); cycles++; }
        if ((mask & 0x04) != 0) { State.B = PopU8(); cycles++; }
        if ((mask & 0x08) != 0) { State.DP = PopU8(); cycles++; }
        if ((mask & 0x10) != 0) { State.X = PopU16(); cycles += 2; }
        if ((mask & 0x20) != 0) { State.Y = PopU16(); cycles += 2; }
        if ((mask & 0x40) != 0) { State.S = PopU16(); cycles += 2; _nmiArmed = true; }
        if ((mask & 0x80) != 0) { State.PC = PopU16(); cycles += 2; }
        return cycles;
    }

    // Return/jump
    private int Abx()
    {
        State.X = (ushort)(State.X + State.B);
        return 3;
    }

    private int Rti()
    {
        State.Flags = M6809Flags.FromByte(PopS8());
        if (State.Flags.E)
        {
            State.A = PopS8();
            State.B = PopS8();
            State.DP = PopS8();
            State.X = PopS16();
            State.Y = PopS16();
            State.U = PopS16();
            State.PC = PopS16();
            return 15;
        }
        else
        {
            State.PC = PopS16();
            return 6;
        }
    }

    private int Cwai(byte mask)
    {
        State.Flags = M6809Flags.FromByte((byte)(State.Flags.ToByte() & mask));
        State.Flags.E = true;
        PushFullFrame();
        State.Halted = true;
        return 20;
    }

    private int Mul()
    {
        ushort result = (ushort)(State.A * State.B);
        State.D = result;
        State.Flags.Z = result == 0;
        State.Flags.C = (State.B & 0x80) != 0;
        return 11;
    }

    private int Swi()
    {
        PushFullFrame();
        State.Flags.I = true;
        State.Flags.F = true;
        State.PC = Read16(0xFFFA);
        return 19;
    }

    private int Swi2()
    {
        PushFullFrame();
        State.PC = Read16(0xFFF4);
        return 20;
    }

    private int Swi3()
    {
        PushFullFrame();
        State.PC = Read16(0xFFF2);
        return 20;
    }

    private int SubD(ushort val)
    {
        ushort result = (ushort)(State.D - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.D ^ val) & (State.D ^ result) & 0x8000) != 0;
        State.Flags.C = State.D < val;
        State.D = result;
        return 4;
    }

    private int CmpD(ushort val)
    {
        ushort result = (ushort)(State.D - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.D ^ val) & (State.D ^ result) & 0x8000) != 0;
        State.Flags.C = State.D < val;
        return 5;
    }

    private int CmpY(ushort val)
    {
        ushort result = (ushort)(State.Y - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.Y ^ val) & (State.Y ^ result) & 0x8000) != 0;
        State.Flags.C = State.Y < val;
        return 5;
    }

    private int CmpU(ushort val)
    {
        ushort result = (ushort)(State.U - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.U ^ val) & (State.U ^ result) & 0x8000) != 0;
        State.Flags.C = State.U < val;
        return 5;
    }

    private int CmpS(ushort val)
    {
        ushort result = (ushort)(State.S - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.S ^ val) & (State.S ^ result) & 0x8000) != 0;
        State.Flags.C = State.S < val;
        return 5;
    }

    private int JsrD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        PushS16(State.PC);
        State.PC = addr;
        return 7;
    }

    private int JsrIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        PushS16(State.PC);
        State.PC = addr;
        return 7 + ex;
    }

    private int JsrExt()
    {
        ushort addr = FetchExtended();
        PushS16(State.PC);
        State.PC = addr;
        return 8;
    }

    private int AddD(ushort val)
    {
        ushort result = (ushort)(State.D + val);
        State.Flags.H = ((State.D ^ val ^ result) & 0x100) != 0;
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.D ^ val ^ 0x8000) & (State.D ^ result) & 0x8000) != 0;
        State.Flags.C = result < State.D;
        State.D = result;
        return 4;
    }

    private int LddI(ushort val)
    {
        State.D = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 3;
    }

    private int LduI(ushort val)
    {
        State.U = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 3;
    }

    private int LddD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        State.D = Read16(addr);
        State.Flags.N = (State.D & 0x8000) != 0;
        State.Flags.Z = State.D == 0;
        State.Flags.V = false;
        return 5;
    }

    private int StdD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        Write16(addr, State.D);
        State.Flags.N = (State.D & 0x8000) != 0;
        State.Flags.Z = State.D == 0;
        State.Flags.V = false;
        return 5;
    }

    private int LduD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        State.U = Read16(addr);
        State.Flags.N = (State.U & 0x8000) != 0;
        State.Flags.Z = State.U == 0;
        State.Flags.V = false;
        return 5;
    }

    private int StuD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        Write16(addr, State.U);
        State.Flags.N = (State.U & 0x8000) != 0;
        State.Flags.Z = State.U == 0;
        State.Flags.V = false;
        return 5;
    }

    private int LddIdx()
    {
        ushort val = Ld16Indexed(out int ex);
        State.D = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int StdIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        Write16(addr, State.D);
        State.Flags.N = (State.D & 0x8000) != 0;
        State.Flags.Z = State.D == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int LduIdx()
    {
        ushort val = Ld16Indexed(out int ex);
        State.U = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int StuIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        Write16(addr, State.U);
        State.Flags.N = (State.U & 0x8000) != 0;
        State.Flags.Z = State.U == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int LddExt()
    {
        ushort val = Ld16Extended();
        State.D = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 6;
    }

    private int StdExt()
    {
        ushort addr = FetchExtended();
        Write16(addr, State.D);
        State.Flags.N = (State.D & 0x8000) != 0;
        State.Flags.Z = State.D == 0;
        State.Flags.V = false;
        return 6;
    }

    private int LduExt()
    {
        ushort val = Ld16Extended();
        State.U = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 6;
    }

    private int StuExt()
    {
        ushort addr = FetchExtended();
        Write16(addr, State.U);
        State.Flags.N = (State.U & 0x8000) != 0;
        State.Flags.Z = State.U == 0;
        State.Flags.V = false;
        return 6;
    }

    // Page 0x10 specific instructions
    private int LdyI(ushort val)
    {
        State.Y = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 4;
    }

    private int LdyD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        State.Y = Read16(addr);
        State.Flags.N = (State.Y & 0x8000) != 0;
        State.Flags.Z = State.Y == 0;
        State.Flags.V = false;
        return 6;
    }

    private int LdyIdx()
    {
        ushort val = Ld16Indexed(out int ex);
        State.Y = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 6 + ex;
    }

    private int LdyExt()
    {
        ushort val = Ld16Extended();
        State.Y = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 7;
    }

    private int StyD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        Write16(addr, State.Y);
        State.Flags.N = (State.Y & 0x8000) != 0;
        State.Flags.Z = State.Y == 0;
        State.Flags.V = false;
        return 5;
    }

    private int StyIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        Write16(addr, State.Y);
        State.Flags.N = (State.Y & 0x8000) != 0;
        State.Flags.Z = State.Y == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int StyExt()
    {
        ushort addr = FetchExtended();
        Write16(addr, State.Y);
        State.Flags.N = (State.Y & 0x8000) != 0;
        State.Flags.Z = State.Y == 0;
        State.Flags.V = false;
        return 6;
    }

    private int LdsI(ushort val)
    {
        State.S = val;
        _nmiArmed = true;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 4;
    }

    private int LdsD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        State.S = Read16(addr);
        _nmiArmed = true;
        State.Flags.N = (State.S & 0x8000) != 0;
        State.Flags.Z = State.S == 0;
        State.Flags.V = false;
        return 6;
    }

    private int LdsIdx()
    {
        ushort val = Ld16Indexed(out int ex);
        State.S = val;
        _nmiArmed = true;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 6 + ex;
    }

    private int LdsExt()
    {
        ushort val = Ld16Extended();
        State.S = val;
        _nmiArmed = true;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 7;
    }

    private int StsD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        Write16(addr, State.S);
        State.Flags.N = (State.S & 0x8000) != 0;
        State.Flags.Z = State.S == 0;
        State.Flags.V = false;
        return 5;
    }

    private int StsIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        Write16(addr, State.S);
        State.Flags.N = (State.S & 0x8000) != 0;
        State.Flags.Z = State.S == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int StsExt()
    {
        ushort addr = FetchExtended();
        Write16(addr, State.S);
        State.Flags.N = (State.S & 0x8000) != 0;
        State.Flags.Z = State.S == 0;
        State.Flags.V = false;
        return 6;
    }

    protected void PushS8(byte value)
    {
        State.S--;
        Mmu.Write(State.S, value);
    }

    protected void PushS16(ushort value)
    {
        PushS8((byte)(value & 0xFF));
        PushS8((byte)(value >> 8));
    }

    protected byte PopS8()
    {
        byte value = Mmu.Read(State.S);
        State.S++;
        return value;
    }

    protected ushort PopS16()
    {
        byte hi = PopS8();
        byte lo = PopS8();
        return (ushort)((hi << 8) | lo);
    }

    protected void PushU8(byte value)
    {
        State.U--;
        Mmu.Write(State.U, value);
    }

    protected void PushU16(ushort value)
    {
        PushU8((byte)(value & 0xFF));
        PushU8((byte)(value >> 8));
    }

    protected byte PopU8()
    {
        byte value = Mmu.Read(State.U);
        State.U++;
        return value;
    }

    protected ushort PopU16()
    {
        byte hi = PopU8();
        byte lo = PopU8();
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>Pushes the full 12-byte 6809 interrupt frame (NMI/IRQ/SWI/SWI2/SWI3/CWAI). Real
    /// hardware pushes PC first and CC last, so that after the sequence completes CC sits at the
    /// final (lowest) stack address and PC at the highest - <see cref="Rti"/> pops in the matching
    /// [CC,A,B,DP,X,Y,U,PC] order, which only lines up if the push order here is the exact reverse.
    /// Pushing CC first (as an earlier version of this method did) put PC's own bytes where CC/A
    /// belonged and silently swapped X/Y - RTI would then resume at a garbage address instead of
    /// the interrupted PC. See docs/pet/superpet-6809-boot-hang.md for how this was found (a real
    /// SuperPET boot corrupting its return address the first time IRQ ever fired) and
    /// M6809CpuInterruptFrameTests for the regression pin.</summary>
    protected void PushFullFrame()
    {
        State.Flags.E = true;
        PushS16(State.PC);
        PushS16(State.U);
        PushS16(State.Y);
        PushS16(State.X);
        PushS8(State.DP);
        PushS8(State.B);
        PushS8(State.A);
        PushS8(State.Flags.ToByte());
    }

    /// <summary>Pushes the 3-byte FIRQ frame (PC, CC only) - see <see cref="PushFullFrame"/>'s doc
    /// comment for why PC must be pushed before CC, not after.</summary>
    protected void PushFastFrame()
    {
        State.Flags.E = false;
        PushS16(State.PC);
        PushS8(State.Flags.ToByte());
    }

    public override void SetIRQ(bool active) => _irqPending = active;

    public override void SetNMI(bool active)
    {
        if (active)
            _nmiPending = true;
    }

    public new byte Fetch()
    {
        byte value = Mmu.Read(State.PC);
        State.PC++;
        return value;
    }

    public new ushort Fetch16()
    {
        byte hi = Fetch();
        byte lo = Fetch();
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>Read a 16-bit big-endian value from memory (HIGH byte first).</summary>
    protected new ushort Read16(ushort address)
    {
        byte hi = Mmu.Read(address);
        byte lo = Mmu.Read((ushort)(address + 1));
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>Write a 16-bit big-endian value to memory (HIGH byte first).</summary>
    protected new void Write16(ushort address, ushort value)
    {
        Mmu.Write(address, (byte)(value >> 8));
        Mmu.Write((ushort)(address + 1), (byte)value);
    }

    private void SetNZ(byte r)
    {
        State.Flags.N = (r & 0x80) != 0;
        State.Flags.Z = r == 0;
    }

    private void SetNZ16(ushort r)
    {
        State.Flags.N = (r & 0x8000) != 0;
        State.Flags.Z = r == 0;
    }
}
