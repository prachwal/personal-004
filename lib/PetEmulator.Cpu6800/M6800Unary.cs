namespace PetEmulator.Cpu6800;

public partial class M6800Cpu
{
    protected int Neg(ushort address) => MemoryUnary(address, UnaryOperation.Neg);
    protected int Com(ushort address) => MemoryUnary(address, UnaryOperation.Com);
    protected int Lsr(ushort address) => MemoryUnary(address, UnaryOperation.Lsr);
    protected int Ror(ushort address) => MemoryUnary(address, UnaryOperation.Ror);
    protected int Asr(ushort address) => MemoryUnary(address, UnaryOperation.Asr);
    protected int Asl(ushort address) => MemoryUnary(address, UnaryOperation.Asl);
    protected int Rol(ushort address) => MemoryUnary(address, UnaryOperation.Rol);
    protected int Dec(ushort address) => MemoryUnary(address, UnaryOperation.Dec);
    protected int Inc(ushort address) => MemoryUnary(address, UnaryOperation.Inc);
    protected int Tst(ushort address) => MemoryUnary(address, UnaryOperation.Tst);
    protected int Clr(ushort address) => MemoryUnary(address, UnaryOperation.Clr);

    protected int NegA() => RegisterUnary(true, UnaryOperation.Neg);
    protected int ComA() => RegisterUnary(true, UnaryOperation.Com);
    protected int LsrA() => RegisterUnary(true, UnaryOperation.Lsr);
    protected int RorA() => RegisterUnary(true, UnaryOperation.Ror);
    protected int AsrA() => RegisterUnary(true, UnaryOperation.Asr);
    protected int AslA() => RegisterUnary(true, UnaryOperation.Asl);
    protected int RolA() => RegisterUnary(true, UnaryOperation.Rol);
    protected int DecA() => RegisterUnary(true, UnaryOperation.Dec);
    protected int IncA() => RegisterUnary(true, UnaryOperation.Inc);
    protected int TstA() => RegisterUnary(true, UnaryOperation.Tst);
    protected int ClrA() => RegisterUnary(true, UnaryOperation.Clr);

    protected int NegB() => RegisterUnary(false, UnaryOperation.Neg);
    protected int ComB() => RegisterUnary(false, UnaryOperation.Com);
    protected int LsrB() => RegisterUnary(false, UnaryOperation.Lsr);
    protected int RorB() => RegisterUnary(false, UnaryOperation.Ror);
    protected int AsrB() => RegisterUnary(false, UnaryOperation.Asr);
    protected int AslB() => RegisterUnary(false, UnaryOperation.Asl);
    protected int RolB() => RegisterUnary(false, UnaryOperation.Rol);
    protected int DecB() => RegisterUnary(false, UnaryOperation.Dec);
    protected int IncB() => RegisterUnary(false, UnaryOperation.Inc);
    protected int TstB() => RegisterUnary(false, UnaryOperation.Tst);
    protected int ClrB() => RegisterUnary(false, UnaryOperation.Clr);

    private int MemoryUnary(ushort address, UnaryOperation operation)
    {
        byte input = Mmu.Read(address);
        byte result = ApplyUnary(input, operation, out bool writeResult);
        if (writeResult)
            Mmu.Write(address, result);
        return 6;
    }

    private int RegisterUnary(bool registerA, UnaryOperation operation)
    {
        byte input = registerA ? State.A : State.B;
        byte result = ApplyUnary(input, operation, out bool writeResult);
        if (writeResult)
        {
            if (registerA)
                State.A = result;
            else
                State.B = result;
        }
        return 2;
    }

    private byte ApplyUnary(byte input, UnaryOperation operation, out bool writeResult)
    {
        byte result = input;
        bool carryIn = ConditionCodes.C;
        writeResult = operation != UnaryOperation.Tst;

        switch (operation)
        {
            case UnaryOperation.Neg:
                result = (byte)-input;
                ConditionCodes.V = result == 0x80;
                ConditionCodes.C = result != 0;
                break;
            case UnaryOperation.Com:
                result = (byte)~input;
                ConditionCodes.V = false;
                ConditionCodes.C = true;
                break;
            case UnaryOperation.Lsr:
                result = (byte)(input >> 1);
                ConditionCodes.N = false;
                ConditionCodes.C = (input & 1) != 0;
                break;
            case UnaryOperation.Ror:
                result = (byte)((input >> 1) | (carryIn ? 0x80 : 0));
                ConditionCodes.C = (input & 1) != 0;
                break;
            case UnaryOperation.Asr:
                result = (byte)((input >> 1) | (input & 0x80));
                ConditionCodes.C = (input & 1) != 0;
                break;
            case UnaryOperation.Asl:
                result = (byte)(input << 1);
                ConditionCodes.V = ((input ^ result) & 0x80) != 0;
                ConditionCodes.C = (input & 0x80) != 0;
                break;
            case UnaryOperation.Rol:
                result = (byte)((input << 1) | (carryIn ? 1 : 0));
                ConditionCodes.V = ((input ^ result) & 0x80) != 0;
                ConditionCodes.C = (input & 0x80) != 0;
                break;
            case UnaryOperation.Dec:
                result = (byte)(input - 1);
                ConditionCodes.V = result == 0x7F;
                break;
            case UnaryOperation.Inc:
                result = (byte)(input + 1);
                ConditionCodes.V = result == 0x80;
                break;
            case UnaryOperation.Tst:
                ConditionCodes.V = false;
                break;
            case UnaryOperation.Clr:
                result = 0;
                ConditionCodes.V = false;
                ConditionCodes.C = false;
                break;
        }

        ConditionCodes.N = (result & 0x80) != 0;
        ConditionCodes.Z = result == 0;
        return result;
    }

    private enum UnaryOperation
    {
        Neg, Com, Lsr, Ror, Asr, Asl, Rol, Dec, Inc, Tst, Clr
    }
}
