namespace PetEmulator.Cpu6809;

/// <summary>6809 CPU state: registers, flags, and execution counters.</summary>
public class M6809State
{
    public byte A, B;          // accumulators
    public byte DP;            // direct page register
    public ushort X, Y, U, S;  // index registers and stack pointers (S=hardware, U=user)
    public ushort PC;          // program counter
    public M6809Flags Flags = new();
    public bool Halted;        // set by SYNC/CWAI while waiting
    public long Cycles;

    /// <summary>D register as A:B (A is high byte, B is low byte).</summary>
    public ushort D
    {
        get => (ushort)((A << 8) | B);
        set
        {
            A = (byte)(value >> 8);
            B = (byte)value;
        }
    }

    /// <summary>Reset to power-on state: A=B=0, DP=0, X=Y=U=S=PC=0, Flags: I=true, F=true, others false; Halted=false.</summary>
    public virtual void Reset()
    {
        A = 0;
        B = 0;
        DP = 0;
        X = 0;
        Y = 0;
        U = 0;
        S = 0;
        PC = 0;
        Flags = new M6809Flags { I = true, F = true };
        Halted = false;
        Cycles = 0;
    }
}
