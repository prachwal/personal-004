namespace PetEmulator.Cpu6800;

public partial class M6800Cpu
{
    protected int LdxI(ushort value)
    {
        State.X = value;
        SetWordLogicalFlags(value);
        return 3;
    }

    protected int CmpX(ushort value)
    {
        ushort before = State.X;
        ushort result = (ushort)(before - value);
        ConditionCodes.N = (result & 0x8000) != 0;
        ConditionCodes.Z = result == 0;
        ConditionCodes.V = ((before ^ value) & (before ^ result) & 0x8000) != 0;
        ConditionCodes.C = before < value;
        return 4;
    }

    protected int LoadX(ushort address, int cycles)
    {
        State.X = Read16(address);
        SetWordLogicalFlags(State.X);
        return cycles;
    }

    protected int StoreX(ushort address, int cycles)
    {
        Write16(address, State.X);
        SetWordLogicalFlags(State.X);
        return cycles;
    }

    private void SetWordLogicalFlags(ushort value)
    {
        ConditionCodes.N = (value & 0x8000) != 0;
        ConditionCodes.Z = value == 0;
        ConditionCodes.V = false;
    }
}
