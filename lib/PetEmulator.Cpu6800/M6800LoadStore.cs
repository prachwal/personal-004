namespace PetEmulator.Cpu6800;

public partial class M6800Cpu
{
    protected int LoadA(ushort address, int cycles)
    {
        State.A = Mmu.Read(address);
        SetLogicalFlags(State.A);
        return cycles;
    }

    protected int StoreA(ushort address, int cycles)
    {
        Mmu.Write(address, State.A);
        SetLogicalFlags(State.A);
        return cycles;
    }

    protected int LoadB(ushort address, int cycles)
    {
        State.B = Mmu.Read(address);
        SetLogicalFlags(State.B);
        return cycles;
    }

    protected int StoreB(ushort address, int cycles)
    {
        Mmu.Write(address, State.B);
        SetLogicalFlags(State.B);
        return cycles;
    }
}
