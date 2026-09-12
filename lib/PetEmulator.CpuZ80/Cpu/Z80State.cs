using PetEmulator.Core;

namespace PetEmulator.CpuZ80.Cpu;

/// <summary>
/// Architectural and lifecycle state shared by the Z80 processor implementation.
/// <see cref="Z80Registers"/> remains the public register view for compatibility.
/// </summary>
public class Z80State : CpuState
{
    public byte A { get; set; }
    public byte F { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    public byte I { get; set; }
    public byte R { get; set; }
    public ushort PC { get; set; }
    public ushort SP { get; set; }
    public ushort IX { get; set; }
    public ushort IY { get; set; }
    public ushort AlternateAF { get; set; }
    public ushort AlternateBC { get; set; }
    public ushort AlternateDE { get; set; }
    public ushort AlternateHL { get; set; }

    public bool Iff1 { get; set; }
    public bool Iff2 { get; set; }
    public byte InterruptMode { get; set; }
    public int InterruptDelay { get; set; }
    public bool PreviousNmi { get; set; }

    public ushort AF
    {
        get => (ushort)((A << 8) | F);
        set
        {
            A = (byte)(value >> 8);
            F = (byte)value;
        }
    }

    public ushort BC
    {
        get => (ushort)((B << 8) | C);
        set
        {
            B = (byte)(value >> 8);
            C = (byte)value;
        }
    }

    public ushort DE
    {
        get => (ushort)((D << 8) | E);
        set
        {
            D = (byte)(value >> 8);
            E = (byte)value;
        }
    }

    public ushort HL
    {
        get => (ushort)((H << 8) | L);
        set
        {
            H = (byte)(value >> 8);
            L = (byte)value;
        }
    }

    public override void Reset()
    {
        A = F = B = C = D = E = H = L = I = R = 0;
        PC = 0;
        SP = 0xFFFF;
        IX = IY = AlternateAF = AlternateBC = AlternateDE = AlternateHL = 0;
        Iff1 = false;
        Iff2 = false;
        InterruptMode = 0;
        InterruptDelay = 0;
        PreviousNmi = false;
        base.Reset();
    }

    public override IReadOnlyDictionary<string, ulong> GetRegisters() => new Dictionary<string, ulong>
    {
        ["A"] = A, ["F"] = F,
        ["B"] = B, ["C"] = C,
        ["D"] = D, ["E"] = E,
        ["H"] = H, ["L"] = L,
        ["I"] = I, ["R"] = R,
        ["PC"] = PC, ["SP"] = SP,
        ["IX"] = IX, ["IY"] = IY,
        ["AF"] = AF, ["BC"] = BC,
        ["DE"] = DE, ["HL"] = HL,
        ["AF'"] = AlternateAF, ["BC'"] = AlternateBC,
        ["DE'"] = AlternateDE, ["HL'"] = AlternateHL,
        ["IFF1"] = Iff1 ? 1UL : 0UL,
        ["IFF2"] = Iff2 ? 1UL : 0UL,
        ["IM"] = InterruptMode,
        ["InterruptDelay"] = checked((ulong)InterruptDelay),
        ["PreviousNmi"] = PreviousNmi ? 1UL : 0UL
    };

    public override void RestoreSnapshot(CpuStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var registers = snapshot.Registers;
        A = Byte(registers, "A");
        F = Byte(registers, "F");
        B = Byte(registers, "B");
        C = Byte(registers, "C");
        D = Byte(registers, "D");
        E = Byte(registers, "E");
        H = Byte(registers, "H");
        L = Byte(registers, "L");
        I = Byte(registers, "I");
        R = Byte(registers, "R");
        PC = Word(registers, "PC");
        SP = Word(registers, "SP");
        IX = Word(registers, "IX");
        IY = Word(registers, "IY");
        AlternateAF = Word(registers, "AF'");
        AlternateBC = Word(registers, "BC'");
        AlternateDE = Word(registers, "DE'");
        AlternateHL = Word(registers, "HL'");
        Iff1 = Flag(registers, "IFF1");
        Iff2 = Flag(registers, "IFF2");
        InterruptMode = Byte(registers, "IM");
        InterruptDelay = checked((int)registers["InterruptDelay"]);
        PreviousNmi = Flag(registers, "PreviousNmi");
        Halted = snapshot.Halted;
    }

    private static byte Byte(IReadOnlyDictionary<string, ulong> registers, string name)
        => checked((byte)registers[name]);

    private static ushort Word(IReadOnlyDictionary<string, ulong> registers, string name)
        => checked((ushort)registers[name]);

    private static bool Flag(IReadOnlyDictionary<string, ulong> registers, string name)
        => registers[name] != 0;
}
