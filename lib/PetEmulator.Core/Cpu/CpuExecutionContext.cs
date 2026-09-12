namespace PetEmulator.Core;

/// <summary>
/// Shared services available while an opcode is executed.
/// Family-specific bus observers remain outside this abstraction.
/// </summary>
public sealed class CpuExecutionContext
{
    public CpuExecutionContext(IMemoryBus memory, IClock clock, IPortBus? ports = null)
    {
        Memory = memory;
        Clock = clock;
        Ports = ports;
    }

    public IMemoryBus Memory { get; }

    public IClock Clock { get; }

    public IPortBus? Ports { get; }
}
