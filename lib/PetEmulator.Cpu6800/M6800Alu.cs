namespace PetEmulator.Cpu6800;

public abstract partial class M6800Cpu
{
    protected int SubA(byte value) => SubByte(value, true);
    protected int CmpA(byte value) => CompareByte(State.A, value);
    protected int SbcA(byte value) => SubtractWithCarry(value, true);
    protected int AndA(byte value) => AndByte(value, true);
    protected int BitA(byte value) => TestByte(State.A, value);
    protected int LdaI(byte value) => LoadByte(value, true);
    protected int EorA(byte value) => XorByte(value, true);
    protected int AdcA(byte value) => AddWithCarry(value, true);
    protected int OraA(byte value) => OrByte(value, true);
    protected int AddA(byte value) => AddByte(value, true);

    protected int SubB(byte value) => SubByte(value, false);
    protected int CmpB(byte value) => CompareByte(State.B, value);
    protected int SbcB(byte value) => SubtractWithCarry(value, false);
    protected int AndB(byte value) => AndByte(value, false);
    protected int BitB(byte value) => TestByte(State.B, value);
    protected int LdbI(byte value) => LoadByte(value, false);
    protected int EorB(byte value) => XorByte(value, false);
    protected int AdcB(byte value) => AddWithCarry(value, false);
    protected int OraB(byte value) => OrByte(value, false);
    protected int AddB(byte value) => AddByte(value, false);
    protected int LdB(byte value) => LoadByte(value, false);

    private int SubByte(byte value, bool registerA)
    {
        byte register = registerA ? State.A : State.B;
        byte before = register;
        byte result = (byte)(before - value);
        ConditionCodes.N = (result & 0x80) != 0;
        ConditionCodes.Z = result == 0;
        ConditionCodes.V = ((before ^ value) & (before ^ result) & 0x80) != 0;
        ConditionCodes.C = before < value;
        SetRegister(registerA, result);
        return 2;
    }

    private int CompareByte(byte register, byte value)
    {
        byte result = (byte)(register - value);
        ConditionCodes.N = (result & 0x80) != 0;
        ConditionCodes.Z = result == 0;
        ConditionCodes.V = ((register ^ value) & (register ^ result) & 0x80) != 0;
        ConditionCodes.C = register < value;
        return 2;
    }

    private int SubtractWithCarry(byte value, bool registerA)
    {
        byte register = registerA ? State.A : State.B;
        byte before = register;
        int borrow = ConditionCodes.C ? 1 : 0;
        byte result = (byte)(before - value - borrow);
        ConditionCodes.N = (result & 0x80) != 0;
        ConditionCodes.Z = result == 0;
        ConditionCodes.V = ((before ^ value) & (before ^ result) & 0x80) != 0;
        ConditionCodes.C = before < value || (before == value && borrow != 0);
        SetRegister(registerA, result);
        return 2;
    }

    private int AndByte(byte value, bool registerA)
    {
        byte register = registerA ? State.A : State.B;
        register &= value;
        SetRegister(registerA, register);
        SetLogicalFlags(register);
        return 2;
    }

    private int TestByte(byte register, byte value)
    {
        SetLogicalFlags((byte)(register & value));
        return 2;
    }

    private int LoadByte(byte value, bool registerA)
    {
        SetRegister(registerA, value);
        SetLogicalFlags(value);
        return 2;
    }

    private int XorByte(byte value, bool registerA)
    {
        byte register = registerA ? State.A : State.B;
        register ^= value;
        SetRegister(registerA, register);
        SetLogicalFlags(register);
        return 2;
    }

    private int AddWithCarry(byte value, bool registerA)
    {
        byte register = registerA ? State.A : State.B;
        byte before = register;
        int carry = ConditionCodes.C ? 1 : 0;
        int result = before + value + carry;
        ConditionCodes.H = ((before ^ value ^ (byte)result) & 0x10) != 0;
        ConditionCodes.N = (result & 0x80) != 0;
        ConditionCodes.Z = (result & 0xFF) == 0;
        ConditionCodes.V = ((before ^ value ^ 0x80) & (before ^ (byte)result) & 0x80) != 0;
        ConditionCodes.C = result > 0xFF;
        SetRegister(registerA, (byte)result);
        return 2;
    }

    private int OrByte(byte value, bool registerA)
    {
        byte register = registerA ? State.A : State.B;
        register |= value;
        SetRegister(registerA, register);
        SetLogicalFlags(register);
        return 2;
    }

    private int AddByte(byte value, bool registerA)
    {
        byte register = registerA ? State.A : State.B;
        byte before = register;
        int result = before + value;
        ConditionCodes.H = ((before ^ value ^ (byte)result) & 0x10) != 0;
        ConditionCodes.N = (result & 0x80) != 0;
        ConditionCodes.Z = (result & 0xFF) == 0;
        ConditionCodes.V = ((before ^ value ^ 0x80) & (before ^ (byte)result) & 0x80) != 0;
        ConditionCodes.C = result > 0xFF;
        SetRegister(registerA, (byte)result);
        return 2;
    }

    private void SetLogicalFlags(byte value)
    {
        ConditionCodes.N = (value & 0x80) != 0;
        ConditionCodes.Z = value == 0;
        ConditionCodes.V = false;
    }

    private void SetRegister(bool registerA, byte value)
    {
        if (registerA)
            State.A = value;
        else
            State.B = value;
    }
}
