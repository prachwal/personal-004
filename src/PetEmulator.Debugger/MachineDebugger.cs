using System.Globalization;
using System.Text;
using PetEmulator.Core;

namespace PetEmulator.Debugger;

/// <summary>
/// A pure, testable command dispatcher for stepping and inspecting an <see cref="IMachine"/>.
/// Ported from personal-003's Retro.Debugger REPL, adapted to this repo's CPU-agnostic
/// <see cref="IMachine"/>/<see cref="IProcessor"/>/<see cref="IMemoryBus"/> contracts.
/// </summary>
/// <remarks>
/// Dropped from the source tool, and why:
/// <list type="bullet">
/// <item><c>screen</c>, <c>screen-txt</c>, <c>vram-dump</c> — the source rendered through a
/// real VideoController/TextRasterizer and read VDP VRAM directly. This repo's <see cref="IMachine"/>
/// has no display/VRAM contract yet (no PetMachine, no frame buffer). TODO: revisit once a
/// display abstraction exists.</item>
/// <item><c>trace</c> prints step/cycle/instruction counters instead of a disassembly+register
/// line — <see cref="IProcessor"/> is intentionally CPU-agnostic and exposes no PC or registers.</item>
/// <item><c>break-pc</c> becomes <c>break-cycle</c>/<c>break-instruction-count</c> — with no PC on
/// <see cref="IProcessor"/>, a breakpoint can only key off the two counters it does expose.</item>
/// <item>The source's separate <c>run</c> command (advance + report watch hits, no per-step print)
/// is folded into <c>trace</c> since this contract has only one stepping primitive
/// (<see cref="IMachine.StepInstruction"/>) and nothing extra to print per step for <c>run</c> to skip.</item>
/// </list>
/// </remarks>
public sealed class MachineDebugger
{
    private readonly IMachine _machine;
    private readonly List<ushort> _watch = [];
    private readonly Dictionary<ushort, byte> _lastWatched = [];
    private ulong? _breakCycle;
    private ulong? _breakInstructionCount;

    public MachineDebugger(IMachine machine) => _machine = machine;

    /// <summary>Parses and runs one command line, returning what a REPL would have printed.</summary>
    public string Execute(string commandLine)
    {
        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return string.Empty;

        try
        {
            return parts[0] switch
            {
                "trace" => Trace(int.Parse(parts[1], CultureInfo.InvariantCulture)),
                "break-cycle" => BreakCycle(ulong.Parse(parts[1], CultureInfo.InvariantCulture)),
                "break-instruction-count" => BreakInstructionCount(ulong.Parse(parts[1], CultureInfo.InvariantCulture)),
                "watch" => Watch(parts[1..]),
                "watch-range" => WatchRange(parts[1], parts[2]),
                "unwatch" => Unwatch(),
                "dump" => Dump(parts[1], parts[2]),
                var command => $"error: unknown command '{command}'",
            };
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }
    }

    // Single stepping primitive: advances the machine, printing counters (no PC/registers -
    // see class remarks), reporting watch changes, and stopping early on Halted or a break-*
    // target.
    private string Trace(int count)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            if (_machine.Processor.Halted)
            {
                sb.AppendLine($"[{i}] halted before step");
                break;
            }

            _machine.StepInstruction();
            var cycles = _machine.Processor.CycleCount;
            var instructions = _machine.Processor.InstructionCount;
            var halted = _machine.Processor.Halted;
            sb.AppendLine($"[{i}] cycles={cycles} instructions={instructions} halted={halted}");
            ReportWatchChanges(sb, i);

            if (halted)
            {
                sb.AppendLine($"[{i}] halted");
                break;
            }

            if (_breakCycle is { } targetCycle && cycles >= targetCycle)
            {
                sb.AppendLine($"[{i}] break-cycle hit: cycles={cycles} >= {targetCycle}");
                break;
            }

            if (_breakInstructionCount is { } targetInstructions && instructions >= targetInstructions)
            {
                sb.AppendLine($"[{i}] break-instruction-count hit: instructions={instructions} >= {targetInstructions}");
                break;
            }
        }

        return sb.ToString();
    }

    private string BreakCycle(ulong target)
    {
        _breakCycle = target;
        return $"break-cycle set at {target}";
    }

    private string BreakInstructionCount(ulong target)
    {
        _breakInstructionCount = target;
        return $"break-instruction-count set at {target}";
    }

    private string Watch(string[] addresses)
    {
        if (addresses.Length == 0)
            throw new InvalidOperationException("watch requires at least one hex address");

        foreach (var a in addresses)
            _watch.Add(Convert.ToUInt16(a, 16));
        SeedWatch();
        return $"watching {_watch.Count} address(es)";
    }

    private string WatchRange(string startHex, string endHex)
    {
        for (ushort a = Convert.ToUInt16(startHex, 16), end = Convert.ToUInt16(endHex, 16); a < end; a++)
            _watch.Add(a);
        SeedWatch();
        return $"watching {_watch.Count} address(es)";
    }

    private string Unwatch()
    {
        _watch.Clear();
        _lastWatched.Clear();
        return "watch list cleared";
    }

    // Seeds lastWatched for any address not yet seeded so its first observed read is never
    // reported as a spurious "00h -> value" change.
    private void SeedWatch()
    {
        foreach (var addr in _watch)
            if (!_lastWatched.ContainsKey(addr))
                _lastWatched[addr] = _machine.Memory.Read(addr);
    }

    private void ReportWatchChanges(StringBuilder sb, int step)
    {
        foreach (var addr in _watch)
        {
            var value = _machine.Memory.Read(addr);
            if (!_lastWatched.TryGetValue(addr, out var previous) || previous != value)
            {
                sb.AppendLine($"[{step}] {addr:X4}h {previous:X2}h -> {value:X2}h");
                _lastWatched[addr] = value;
            }
        }
    }

    private string Dump(string startHex, string endHex)
    {
        var start = Convert.ToUInt16(startHex, 16);
        var end = Convert.ToUInt16(endHex, 16);
        var sb = new StringBuilder();
        for (var addr = start; addr < end; addr += 16)
        {
            sb.Append($"{addr:X4}: ");
            for (var i = 0; i < 16 && addr + i < end; i++)
                sb.Append($"{_machine.Memory.Read((ushort)(addr + i)):X2} ");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
