namespace PetEmulator.Cpu6800;

public abstract partial class M6800Cpu
{
    protected virtual int ShortBranchCycles => 4;
    protected virtual int BranchSubroutineCycles => 8;

    protected int Bra(byte offset) => Branch(offset, true);
    protected int Brn(byte offset) => ShortBranchCycles;
    protected int Bhi(byte offset) => Branch(offset, !ConditionCodes.C && !ConditionCodes.Z);
    protected int Bls(byte offset) => Branch(offset, ConditionCodes.C || ConditionCodes.Z);
    protected int Bcc(byte offset) => Branch(offset, !ConditionCodes.C);
    protected int Bcs(byte offset) => Branch(offset, ConditionCodes.C);
    protected int Bne(byte offset) => Branch(offset, !ConditionCodes.Z);
    protected int Beq(byte offset) => Branch(offset, ConditionCodes.Z);
    protected int Bvc(byte offset) => Branch(offset, !ConditionCodes.V);
    protected int Bvs(byte offset) => Branch(offset, ConditionCodes.V);
    protected int Bpl(byte offset) => Branch(offset, !ConditionCodes.N);
    protected int Bmi(byte offset) => Branch(offset, ConditionCodes.N);
    protected int Bge(byte offset) => Branch(offset, ConditionCodes.N == ConditionCodes.V);
    protected int Blt(byte offset) => Branch(offset, ConditionCodes.N != ConditionCodes.V);
    protected int Bgt(byte offset) => Branch(offset, !ConditionCodes.Z && ConditionCodes.N == ConditionCodes.V);
    protected int Ble(byte offset) => Branch(offset, ConditionCodes.Z || ConditionCodes.N != ConditionCodes.V);

    protected int Bsr(byte offset)
    {
        PushStack16(State.PC);
        State.PC = (ushort)(State.PC + (sbyte)offset);
        return BranchSubroutineCycles;
    }

    protected int Rts()
    {
        State.PC = PopStack16();
        return 5;
    }

    protected void PushStack8(byte value)
    {
        State.StackPointer--;
        Mmu.Write(State.StackPointer, value);
    }

    protected void PushStack16(ushort value)
    {
        PushStack8((byte)value);
        PushStack8((byte)(value >> 8));
    }

    protected byte PopStack8()
    {
        byte value = Mmu.Read(State.StackPointer);
        State.StackPointer++;
        return value;
    }

    protected ushort PopStack16()
    {
        byte high = PopStack8();
        byte low = PopStack8();
        return (ushort)((high << 8) | low);
    }

    private int Branch(byte offset, bool taken)
    {
        if (taken)
            State.PC = (ushort)(State.PC + (sbyte)offset);
        return ShortBranchCycles;
    }
}
