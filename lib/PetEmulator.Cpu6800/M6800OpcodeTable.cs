namespace PetEmulator.Cpu6800;

public sealed class M6800OpcodeTable
{
    private readonly M6800OpcodeDefinition?[] _entries = new M6800OpcodeDefinition?[256];

    public M6800OpcodeDefinition this[byte opcode] =>
        _entries[opcode] ?? throw new InvalidOperationException($"Opcode 0x{opcode:X2} is not defined.");

    public IEnumerable<M6800OpcodeDefinition> Definitions => _entries.OfType<M6800OpcodeDefinition>();

    public void Set(M6800OpcodeDefinition definition) => _entries[definition.Opcode] = definition;

    public M6800OpcodeTable Derive(Action<M6800OpcodeTable> configure)
    {
        var derived = new M6800OpcodeTable();
        foreach (var definition in Definitions)
            derived.Set(definition);
        configure(derived);
        return derived;
    }
}
