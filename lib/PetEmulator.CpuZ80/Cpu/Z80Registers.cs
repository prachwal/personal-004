namespace PetEmulator.CpuZ80.Cpu;

public sealed class Z80Registers
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

    public void Reset()
    {
        A = F = B = C = D = E = H = L = I = R = 0;
        PC = 0;
        SP = 0xFFFF;
        IX = IY = AlternateAF = AlternateBC = AlternateDE = AlternateHL = 0;
    }
}
