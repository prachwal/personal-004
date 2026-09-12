namespace PetEmulator.CpuZ80.Bus;

public enum BusCycleKind
{
    OpcodeFetch,
    MemoryRead,
    MemoryWrite,
    IoRead,
    IoWrite,
    InterruptAcknowledge,
    Refresh
}
public readonly record struct BusCycle(
    BusCycleKind Kind,
    ushort Address,
    byte Value,
    int MachineCycle,
    int TState,
    int TStates)
{
    public BusCycle(BusCycleKind kind, ushort address, byte value)
        : this(kind, address, value, 0, 0, 0) { }
}

public interface IBusCycleObserver
{
    void Observe(BusCycle cycle);
}
