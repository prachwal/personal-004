namespace PetEmulator.Core;

/// <summary>Result of <see cref="MachineExtensions.RunUntilOrStalled"/>: whether <c>condition</c>
/// was met, whether a stall (no progress for the configured window) was detected instead, and how
/// many instructions actually ran.</summary>
public readonly record struct StallCheckResult(bool ConditionMet, bool Stalled, ulong InstructionsRun);

/// <summary>Stepping helpers built purely on <see cref="IMachine"/> - moved here from
/// PetEmulator.Pet.PetMachine once a second machine (VIC-20) needed the identical logic instead
/// of a copy-pasted duplicate. Extension-method call syntax (<c>machine.RunUntil(...)</c>) is
/// unchanged for existing callers - only the implementation moved.</summary>
public static class MachineExtensions
{
    /// <summary>Steps until <paramref name="condition"/> is true (checked against the machine's
    /// memory bus after every instruction) or <paramref name="maxInstructions"/> is reached.
    /// Returns whether the condition was met. Ported from personal-001's PetMachine.RunUntil -
    /// waiting for a real condition (e.g. "READY." bytes appearing in video RAM) instead of a
    /// fixed instruction count is what its own working keyboard integration tests rely on.</summary>
    public static bool RunUntil(this IMachine machine, Func<IMemoryBus, bool> condition, ulong maxInstructions)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(condition);
        for (var i = 0UL; i < maxInstructions; i++)
        {
            if (condition(machine.Memory))
                return true;
            machine.StepInstruction();
        }

        return condition(machine.Memory);
    }

    /// <summary>Like <see cref="RunUntil"/>, but also watches <paramref name="progress"/> (e.g. a
    /// cumulative byte-transfer counter) for a plateau: if it hasn't changed for
    /// <paramref name="stallWindow"/> instructions, stops early with
    /// <see cref="StallCheckResult.Stalled"/> set instead of running blind all the way to
    /// <paramref name="maxInstructions"/> before reporting failure. Built to replace manual
    /// instruction-budget bisection (halve the budget, rerun, re-read a trace by hand) - see
    /// docs/pet/disk-testing-strategy.md and docs/pet/debug-tools.md.</summary>
    public static StallCheckResult RunUntilOrStalled(
        this IMachine machine, Func<IMemoryBus, bool> condition, Func<long> progress, ulong maxInstructions, ulong stallWindow = 50_000)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(progress);

        var lastProgress = progress();
        var lastProgressAt = 0UL;
        for (var i = 0UL; i < maxInstructions; i++)
        {
            if (condition(machine.Memory))
                return new StallCheckResult(true, false, i);

            machine.StepInstruction();

            var current = progress();
            if (current != lastProgress)
            {
                lastProgress = current;
                lastProgressAt = i + 1;
            }
            else if (i + 1 - lastProgressAt >= stallWindow)
            {
                return new StallCheckResult(condition(machine.Memory), true, i + 1);
            }
        }

        return new StallCheckResult(condition(machine.Memory), false, maxInstructions);
    }
}
