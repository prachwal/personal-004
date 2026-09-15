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
/// <item><c>trace</c> prints step/cycle/instruction counters, plus a register line
/// (<c>PC=... A=... X=... Y=... SP=... P=...</c>) whenever <see cref="Processor"/> implements
/// the optional <see cref="IDebuggableProcessor"/> - omitted entirely for a plain
/// <see cref="IProcessor"/> that doesn't (this contract is intentionally CPU-agnostic).</item>
/// <item><c>break-pc</c> is supported only when <see cref="IDebuggableProcessor"/> is available
/// (checked at the point the command runs, not at construction - a future CPU-agnostic caller
/// gets a clear error instead of a silent no-op); <c>break-cycle</c>/<c>break-instruction-count</c>
/// stay as the two counters every <see cref="IProcessor"/> exposes regardless.</item>
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
    private ushort? _breakPc;
    private static readonly string[] DefaultEightBitRegisterNames = ["A", "X", "Y", "SP", "P"];

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
                "break-pc" => BreakPc(Convert.ToUInt16(parts[1], 16)),
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
            ushort? pc = null;
            if (_machine.Processor is IDebuggableProcessor dbg)
            {
                var regs = dbg.GetRegisters();
                if (regs.TryGetValue("PC", out var pcValue))
                    pc = (ushort)pcValue;
                var preferred = new[] { "PC", "A", "X", "Y", "SP", "P" };
                var orderedKeys = preferred.Where(regs.ContainsKey).Concat(regs.Keys.Except(preferred).OrderBy(key => key));
                // Width must come from the register's own identity, not its current value - a
                // 16-bit register (PC, or Z80's HL/BC/DE/AF/IX/IY) that happens to hold a small
                // value is still 16-bit and must print 4 digits, or it's indistinguishable from a
                // genuine 8-bit register in the same line. EightBitRegisterNames is empty for CPUs
                // that don't override it (6502/6800/6809/8080), so DefaultEightBitRegisterNames
                // reproduces exactly their original hardcoded PC=X4/A,X,Y,SP,P=X2 formatting.
                var eightBit = dbg.EightBitRegisterNames;
                if (eightBit.Count == 0)
                    eightBit = DefaultEightBitRegisterNames;
                var registerText = string.Join(" ", orderedKeys.Select(key =>
                    $"{key}={(eightBit.Contains(key) ? regs[key].ToString("X2") : regs[key].ToString("X4"))}"));
                sb.AppendLine($"[{i}]   {registerText}");
            }
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

            if (_breakPc is { } targetPc && pc == targetPc)
            {
                sb.AppendLine($"[{i}] break-pc hit: PC={pc:X4} == {targetPc:X4}");
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

    private string BreakPc(ushort target)
    {
        if (_machine.Processor is not IDebuggableProcessor)
            return "error: break-pc requires an IDebuggableProcessor (this IProcessor exposes no PC)";
        _breakPc = target;
        return $"break-pc set at {target:X4}";
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
