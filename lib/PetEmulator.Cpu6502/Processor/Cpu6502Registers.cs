namespace PetEmulator.Cpu6502;

/// <summary>Live architectural registers exposed as one debugger-friendly object.</summary>
public sealed class Cpu6502Registers
{
    private readonly Cpu6502 _cpu;

    internal Cpu6502Registers(Cpu6502 cpu) => _cpu = cpu;

    public byte A { get => _cpu.A; set => _cpu.A = value; }
    public byte X { get => _cpu.X; set => _cpu.X = value; }
    public byte Y { get => _cpu.Y; set => _cpu.Y = value; }
    public byte SP { get => _cpu.SP; set => _cpu.SP = value; }
    public byte P { get => _cpu.P; set => _cpu.P = value; }
    public ushort PC { get => _cpu.PC; set => _cpu.PC = value; }

    public ushort Get(string name) => name.ToUpperInvariant() switch
    {
        "A" => A,
        "X" => X,
        "Y" => Y,
        "SP" => SP,
        "P" or "SR" => P,
        "PC" => PC,
        _ => throw new ArgumentException($"Unknown register: {name}", nameof(name))
    };

    public void Set(string name, ushort value)
    {
        switch (name.ToUpperInvariant())
        {
            case "A": A = (byte)value; break;
            case "X": X = (byte)value; break;
            case "Y": Y = (byte)value; break;
            case "SP": SP = (byte)value; break;
            case "P":
            case "SR": P = (byte)value; break;
            case "PC": PC = value; break;
            default: throw new ArgumentException($"Unknown register: {name}", nameof(name));
        }
    }

    public IReadOnlyList<string> Names { get; } = ["A", "X", "Y", "SP", "P", "PC"];
}
