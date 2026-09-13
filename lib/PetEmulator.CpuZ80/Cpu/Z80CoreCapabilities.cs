using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Cpu;

/// <summary>Adapts Z80 bus, interrupt and cycle-observer capabilities to one contract.</summary>
public sealed class Z80CoreCapabilities(
    IBus bus,
    IInterruptLines interruptLines,
    IBusCycleObserver? cycleObserver = null) : IZ80CoreCapabilities
{
    public bool WaitAsserted => interruptLines.WaitAsserted;
    public bool IntAsserted => interruptLines.IntAsserted;
    public bool NmiAsserted => interruptLines.NmiAsserted;
    public byte AcknowledgeInterrupt() => bus.AcknowledgeInterrupt();
    public void Observe(BusCycle cycle) => cycleObserver?.Observe(cycle);
}
