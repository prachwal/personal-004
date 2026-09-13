using PetEmulator.CpuZ80.Bus;

namespace PetEmulator.CpuZ80.Cpu;

/// <summary>Hardware-specific capabilities consumed by the shared CPU lifecycle.</summary>
public interface IZ80CoreCapabilities
{
    bool WaitAsserted { get; }
    bool IntAsserted { get; }
    bool NmiAsserted { get; }
    byte AcknowledgeInterrupt();
    void Observe(BusCycle cycle);
}
