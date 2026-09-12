namespace PetEmulator.Core;

/// <summary>Optional memory-bus capability used by CPU watchpoint integrations.</summary>
public interface IMemoryAccessObservable
{
    event Action<BusAccess> Accessed;
}
