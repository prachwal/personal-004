using System.Collections;

namespace PetEmulator.CpuZ80.Cpu;

/// <summary>
/// The collection <see cref="Z80Cpu.Hooks"/> exposes. Tracks whether it's
/// non-empty via a plain bool flag (set on <see cref="Add"/>, cleared the
/// moment the last hook is removed) instead of Step() checking Count each
/// time - a field read instead of a property getter on the hot path.
/// </summary>
public sealed class CpuHookCollection : IReadOnlyList<CpuHook>
{
    private readonly List<CpuHook> hooks = [];

    /// <summary>True from the first <see cref="Add"/> until the collection is emptied again.</summary>
    public bool HasAny { get; private set; }

    public int Count => hooks.Count;
    public CpuHook this[int index] => hooks[index];

    public void Add(CpuHook hook)
    {
        hooks.Add(hook);
        HasAny = true;
    }

    public bool Remove(CpuHook hook)
    {
        var removed = hooks.Remove(hook);
        if (removed && hooks.Count == 0)
            HasAny = false;
        return removed;
    }

    public void Clear()
    {
        hooks.Clear();
        HasAny = false;
    }

    public IEnumerator<CpuHook> GetEnumerator() => hooks.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
