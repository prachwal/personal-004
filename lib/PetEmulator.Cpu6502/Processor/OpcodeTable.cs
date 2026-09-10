namespace PetEmulator.Cpu6502;

/// <summary>Configurable dispatch table for the 256-byte opcode space.</summary>
public sealed class OpcodeTable
{
    private readonly OpcodeDefinition?[] _entries = new OpcodeDefinition?[256];
    private bool _sealed;

    public OpcodeDefinition this[byte opcode] =>
        _entries[opcode] ?? throw new NotSupportedException($"Undefined opcode 0x{opcode:X2}.");

    public IEnumerable<OpcodeDefinition> Definitions => _entries.OfType<OpcodeDefinition>();

    public void Set(OpcodeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        EnsureMutable();
        _entries[definition.Opcode] = definition;
    }

    public void Remove(byte opcode)
    {
        EnsureMutable();
        _entries[opcode] = null;
    }

    public bool IsSealed => _sealed;

    public OpcodeTable Seal()
    {
        _sealed = true;
        return this;
    }

    public OpcodeTable Derive(Action<OpcodeTable> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var derived = new OpcodeTable();
        Array.Copy(_entries, derived._entries, _entries.Length);
        changes(derived);
        return derived.Seal();
    }

    private void EnsureMutable()
    {
        if (_sealed)
            throw new InvalidOperationException("Opcode table is sealed.");
    }
}
