namespace PetEmulator.CpuZ80.Cpu;

/// <summary>
/// A condition-checked action run once before every completed instruction
/// (including the NMI/INT/HALT paths, before <see cref="Z80Cpu.Registers"/>
/// changes for that step) - inject ad-hoc diagnosis (stop and log when PC
/// hits an address, dump registers on a specific opcode, count how often
/// a routine runs) via <see cref="Z80Cpu.Hooks"/> instead of hand-rolling a
/// throwaway trace harness. Etap 7/12's boot-hang diagnosis kept
/// re-inventing exactly this ("watch for this PC, tell me the registers")
/// as one-off scratch scripts; this is the reusable form of that.
///
/// Deliberately just two delegates, not a condition-expression language or
/// a persistent breakpoint registry with pause/resume semantics - the
/// condition is arbitrary C#, which already covers everything a small
/// expression DSL would, and Hooks defaults to empty so this costs nothing
/// on the hot Step() path when nobody has added one.
/// </summary>
public sealed class CpuHook(Func<Z80Cpu, bool> condition, Action<Z80Cpu> action)
{
    /// <summary>No condition to check - the action runs on every Step().</summary>
    public CpuHook(Action<Z80Cpu> action) : this(_ => true, action)
    {
    }

    public void RunIfMatched(Z80Cpu cpu)
    {
        if (condition(cpu))
            action(cpu);
    }
}
