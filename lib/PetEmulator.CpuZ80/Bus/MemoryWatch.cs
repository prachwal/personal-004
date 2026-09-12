namespace PetEmulator.CpuZ80.Bus;

/// <summary>
/// Address-keyed watchpoints - a sibling to <c>Z80Cpu.Hooks</c> for memory
/// instead of instructions. Backed by a struct array (index = address),
/// not a Dictionary(address -&gt; hooks): once allocated, checking an
/// address is one array index, not a hash + bucket walk, and the array
/// holding the hooks by value (not boxed per-cell objects) keeps it one
/// contiguous, cache-friendly block. Lives at the SystemBus level (not
/// pushed down into RamMemory) specifically so it covers the whole 64K
/// bus - ROM, keyboard, FDC registers - the same as every underlying
/// IMemoryBus region, not just RAM.
///
/// The array (64K x (byte + object reference), ~1MB) is allocated lazily
/// on the first <see cref="Add"/> - SystemBus is constructed by nearly
/// every test in this project, and paying that upfront for a feature
/// almost nothing uses would be exactly the kind of "cost even when
/// nobody's watching" this design is meant to avoid.
/// </summary>
public sealed class MemoryWatch
{
    private struct Cell
    {
        public byte LastValue;
        public List<MemoryHook>? Hooks;
    }

    private Cell[]? cells;

    /// <summary>Lets SystemBus skip calling <see cref="OnRead"/>/<see cref="OnWrite"/> entirely when nothing is watched.</summary>
    public bool HasAny => cells is not null;

    public void Add(ushort address, MemoryHook hook)
    {
        cells ??= new Cell[ushort.MaxValue + 1];
        ref var cell = ref cells[address];
        (cell.Hooks ??= []).Add(hook);
    }

    internal void OnRead(ushort address, byte value)
    {
        if (cells is null)
            return;

        ref var cell = ref cells[address];
        cell.LastValue = value; // keeps a later write's change-check correct even if nothing wrote here first
        if (cell.Hooks is { Count: > 0 } hooks)
            for (var i = 0; i < hooks.Count; i++)
                hooks[i].RunIfMatched(MemoryAccessKind.Read, address, value);
    }

    /// <summary>Only fires when this address has hooks AND the value actually changed - a same-value rewrite is a no-op for watchers.</summary>
    internal void OnWrite(ushort address, byte value)
    {
        if (cells is null)
            return;

        ref var cell = ref cells[address];
        var changed = cell.LastValue != value;
        cell.LastValue = value;
        if (changed && cell.Hooks is { Count: > 0 } hooks)
            for (var i = 0; i < hooks.Count; i++)
                hooks[i].RunIfMatched(MemoryAccessKind.Write, address, value);
    }
}
