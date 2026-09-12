namespace PetEmulator.CpuZ80.Bus;

public enum MemoryAccessKind
{
    Read,
    Write,
}
/// <summary>A condition-checked action for one watched address - see <see cref="MemoryWatch"/>.</summary>
public sealed class MemoryHook(Func<MemoryAccessKind, ushort, byte, bool> condition, Action<MemoryAccessKind, ushort, byte> action)
{
    public void RunIfMatched(MemoryAccessKind kind, ushort address, byte value)
    {
        if (condition(kind, address, value))
            action(kind, address, value);
    }
}
