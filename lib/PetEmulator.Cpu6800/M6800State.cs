namespace PetEmulator.Cpu6800;

/// <summary>Common architectural state shared by the 6800 family.</summary>
public class M6800State
{
    public byte A { get; set; }
    public byte B { get; set; }
    public ushort X { get; set; }
    public virtual ushort StackPointer { get; set; }
    public ushort PC { get; set; }
    public virtual M6800Flags Flags { get; set; } = new();
    public bool Halted { get; set; }
    public long Cycles { get; set; }

    public virtual void Reset()
    {
        A = 0;
        B = 0;
        X = 0;
        StackPointer = 0;
        PC = 0;
        Flags = new M6800Flags();
        Flags.Reset();
        Halted = false;
        Cycles = 0;
    }
}
