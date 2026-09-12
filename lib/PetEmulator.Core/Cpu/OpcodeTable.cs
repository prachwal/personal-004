namespace PetEmulator.Core;

/// <summary>
/// Instance opcode registry that can be extended by derived processor classes.
/// </summary>
public sealed class OpcodeTable<TState>
{
    private readonly Dictionary<OpcodeKey, OpcodeDefinition<TState>> _entries = new();
    private bool _sealed;

    public IReadOnlyCollection<OpcodeDefinition<TState>> Entries => _entries.Values;

    public bool IsSealed => _sealed;

    public void Add(OpcodeDefinition<TState> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        EnsureMutable();

        if (!_entries.TryAdd(definition.Key, definition))
            throw new InvalidOperationException($"Opcode {definition.Key} is already registered.");
    }

    public void Replace(OpcodeDefinition<TState> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        EnsureMutable();
        _entries[definition.Key] = definition;
    }

    public void Set(OpcodeDefinition<TState> definition) => Replace(definition);

    public void Remove(OpcodeKey key)
    {
        EnsureMutable();
        _entries.Remove(key);
    }

    public OpcodeTable<TState> Seal()
    {
        _sealed = true;
        return this;
    }

    public OpcodeTable<TState> Derive(Action<OpcodeTable<TState>> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var derived = new OpcodeTable<TState>();
        foreach (var definition in _entries.Values)
            derived._entries.Add(definition.Key, definition);
        changes(derived);
        return derived.Seal();
    }

    public bool TryGet(OpcodeKey key, out OpcodeDefinition<TState>? definition)
        => _entries.TryGetValue(key, out definition);

    public OpcodeDefinition<TState> Get(OpcodeKey key)
        => _entries.TryGetValue(key, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Opcode {key} is not registered.");

    private void EnsureMutable()
    {
        if (_sealed)
            throw new InvalidOperationException("Opcode table is sealed.");
    }
}
