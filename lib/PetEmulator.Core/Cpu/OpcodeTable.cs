namespace PetEmulator.Core;

/// <summary>
/// Instance opcode registry that can be extended by derived processor classes.
/// </summary>
public sealed class OpcodeTable<TState>
{
    private readonly Dictionary<OpcodeKey, OpcodeDefinition<TState>> _entries = new();

    public IReadOnlyCollection<OpcodeDefinition<TState>> Entries => _entries.Values;

    public void Add(OpcodeDefinition<TState> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!_entries.TryAdd(definition.Key, definition))
            throw new InvalidOperationException($"Opcode {definition.Key} is already registered.");
    }

    public void Replace(OpcodeDefinition<TState> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _entries[definition.Key] = definition;
    }

    public bool TryGet(OpcodeKey key, out OpcodeDefinition<TState>? definition)
        => _entries.TryGetValue(key, out definition);

    public OpcodeDefinition<TState> Get(OpcodeKey key)
        => _entries.TryGetValue(key, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Opcode {key} is not registered.");
}
