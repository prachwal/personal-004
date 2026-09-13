using PetEmulator.Core;

namespace PetEmulator.Cpu8080;

/// <summary>Architectural state of an Intel 8080 processor.</summary>
public sealed class Cpu8080State : CpuState
{
    public byte A { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    public ushort PC { get; set; }
    public ushort SP { get; set; }

    /// <summary>8080 flags: S, Z, AC, P and CY. Unused bits are kept clear.</summary>
    public byte Flags { get; set; }

    public bool InterruptsEnabled { get; set; }

    public Cpu8080Registers Registers => new(A, B, C, D, E, H, L, PC, SP, Flags);

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
        base.Reset();
        A = B = C = D = E = H = L = 0;
        PC = 0;
        SP = 0;
        Flags = 0;
        InterruptsEnabled = false;
    }

    public override IReadOnlyDictionary<string, ulong> GetRegisters() => new Dictionary<string, ulong>
    {
        ["A"] = A,
        ["B"] = B,
        ["C"] = C,
        ["D"] = D,
        ["E"] = E,
        ["H"] = H,
        ["L"] = L,
        ["BC"] = BC,
        ["DE"] = DE,
        ["HL"] = HL,
        ["PC"] = PC,
        ["SP"] = SP,
        ["Flags"] = Flags,
        ["INTE"] = InterruptsEnabled ? 1UL : 0UL,
    };

    public override CpuStateSnapshot CaptureSnapshot()
        => new(GetRegisters(), Halted);

    public override void RestoreSnapshot(CpuStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        A = ReadByte(snapshot, "A");
        B = ReadByte(snapshot, "B");
        C = ReadByte(snapshot, "C");
        D = ReadByte(snapshot, "D");
        E = ReadByte(snapshot, "E");
        H = ReadByte(snapshot, "H");
        L = ReadByte(snapshot, "L");
        PC = ReadUShort(snapshot, "PC");
        SP = ReadUShort(snapshot, "SP");
        Flags = ReadByte(snapshot, "Flags");
        InterruptsEnabled = snapshot.Registers.TryGetValue("INTE", out var inte) && inte != 0;
        Halted = snapshot.Halted;
    }

    private static byte ReadByte(CpuStateSnapshot snapshot, string name)
        => checked((byte)snapshot.Registers[name]);

    private static ushort ReadUShort(CpuStateSnapshot snapshot, string name)
        => checked((ushort)snapshot.Registers[name]);
}
