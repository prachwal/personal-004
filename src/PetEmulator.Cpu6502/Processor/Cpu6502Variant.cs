namespace Cpu6502;

[Flags]
public enum CpuQuirk
{
    None = 0,
    DecimalArithmetic = 1,
    JmpIndirectPageWrap = 2,
    CmosBcdExtraCycle = 4,
    RockwellBitOps = 8
}

public sealed class Cpu6502Variant
{
    public Cpu6502Variant(
        string name,
        OpcodeTable opcodeTable,
        CpuQuirk quirks)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Variant name is required.", nameof(name)) : name;
        OpcodeTable = opcodeTable ?? throw new ArgumentNullException(nameof(opcodeTable));
        Quirks = quirks;
    }

    public string Name { get; }
    public OpcodeTable OpcodeTable { get; }
    public CpuQuirk Quirks { get; }
}
