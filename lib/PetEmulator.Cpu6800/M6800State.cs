using PetEmulator.Core;

namespace PetEmulator.Cpu6800;

/// <summary>Common architectural state shared by the 6800 family.</summary>
public class M6800State : CpuState
{
    public byte A { get; set; }
    public byte B { get; set; }
    public ushort X { get; set; }
    public virtual ushort StackPointer { get; set; }
    public ushort PC { get; set; }
    public virtual M6800Flags Flags { get; set; } = new();
    public long Cycles { get; set; }

    public override void Reset()
    {
        base.Reset();
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

    public override IReadOnlyDictionary<string, ulong> GetRegisters() => new Dictionary<string, ulong>
    {
        ["PC"] = PC,
        ["A"] = A,
        ["B"] = B,
        ["X"] = X,
        ["SP"] = StackPointer,
        ["P"] = Flags.ToByte(),
    };

    public override void RestoreSnapshot(CpuStateSnapshot snapshot)
    {
        A = (byte)snapshot.Registers["A"];
        B = (byte)snapshot.Registers["B"];
        X = (ushort)snapshot.Registers["X"];
        StackPointer = (ushort)snapshot.Registers["SP"];
        PC = (ushort)snapshot.Registers["PC"];
        Flags = M6800Flags.FromByte((byte)snapshot.Registers["P"]);
        Halted = snapshot.Halted;
    }
}
