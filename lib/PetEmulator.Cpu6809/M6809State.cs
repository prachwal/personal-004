using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6809;

/// <summary>6809 CPU state: registers, flags, and execution counters.</summary>
public class M6809State : M6800State
{
    public byte DP;            // direct page register
    public ushort Y, U, S;     // index registers and stack pointers (S=hardware, U=user)
    public override ushort StackPointer
    {
        get => S;
        set => S = value;
    }

    public new M6809Flags Flags { get; set; } = new();

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
    public override void Reset()
    {
        base.Reset();
        DP = 0;
        Y = 0;
        U = 0;
        S = 0;
        Flags = new M6809Flags { I = true, F = true };
    }
}
