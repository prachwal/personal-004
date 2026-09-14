using System.Globalization;
using System.Text;
using PetEmulator.Core;
using PetEmulator.Debugger;
using PetEmulator.Pet;
using PetEmulator.Pet.Diagnostics;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Cli;

/// <summary>
/// Scripted, headless command session for a <see cref="PetMachine"/> - the PET-specific analog of
/// personal-003's Retro.Debugger REPL: point a script (or a `debug` subcommand line) at a profile,
/// a ROM directory, a tape file, a disk image, by path, instead of clicking through a GUI. Useful
/// for reproducing a bug or driving a boot sequence deterministically instead of hand-writing a
/// throwaway test file for every question.
/// </summary>
/// <remarks>
/// Construction commands (<c>profile</c>/<c>roms</c>/<c>keymap</c>/<c>tape</c>/<c>disk</c>/
/// <c>play</c>/<c>stop</c>/<c>eject</c>/<c>key</c>/<c>type</c>/<c>devices</c>/<c>status</c>/
/// <c>trace-log</c>/<c>superpet-diagnose</c>/<c>superpet-boot-checkpoints</c>/<c>disk-stall-check</c>/
/// <c>via-irq-check</c>) live
/// here, PET-specific (<c>trace-log</c>/<c>disk-stall-check</c> wrap <see cref="InstructionTracer"/>/
/// <see cref="MachineExtensions.RunUntilOrStalled"/> - see docs/pet/debug-tools.md). Everything else
/// (<c>trace</c>/<c>watch</c>/<c>watch-range</c>/<c>unwatch</c>/<c>break-cycle</c>/
/// <c>break-instruction-count</c>/<c>dump</c>) is CPU-agnostic and already implemented once in
/// <see cref="MachineDebugger"/> - this class delegates to it rather than duplicating it, once the
/// machine exists. Like <see cref="MachineDebugger"/>, a command never throws out of
/// <see cref="Execute"/> - failures come back as an <c>"error: ..."</c> string, so one bad line in
/// a script doesn't abort the rest of it.
/// </remarks>
public sealed class PetDebuggerSession
{
    private string? _profileId;
    private string? _romsRoot;
    private IPetKeyboardMap _keymap = new Pet2001GraphicsKeyboardMap();
    private PetMachine? _machine;
    private MachineDebugger? _debugger;

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
                "profile" => SetProfile(parts[1]),
                "roms" => SetRoms(Argument(commandLine, parts[0])),
                "keymap" => SetKeymap(parts[1]),
                "tape" => LoadTape(Argument(commandLine, parts[0])),
                "disk" => LoadDisk(parts),
                "new-disk" => NewDisk(parts),
                "play" => PlayTape(),
                "stop" => StopTape(),
                "eject" => EjectTape(),
                "key" => Key(parts),
                "type" => Type(commandLine[(parts[0].Length + 1)..]),
                "devices" => Devices(),
                "status" => Status(),
                "trace-log" => TraceLog(int.Parse(parts[1], CultureInfo.InvariantCulture), parts[2]),
                "superpet-diagnose" => SuperPetDiagnose(parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 16),
                "superpet-boot-checkpoints" => SuperPetBootCheckpointsCommand(
                    ulong.Parse(parts[1], CultureInfo.InvariantCulture),
                    parts.Length > 2 ? parts[2] : null),
                "via-irq-check" => ViaIrqCheck(ulong.Parse(parts[1], CultureInfo.InvariantCulture)),
                "disk-stall-check" => DiskStallCheck(
                    ulong.Parse(parts[1], CultureInfo.InvariantCulture),
                    parts.Length > 2 ? ulong.Parse(parts[2], CultureInfo.InvariantCulture) : 50_000),
                _ => EnsureDebugger().Execute(commandLine),
            };
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }
    }

    private string SetProfile(string id)
    {
        _profileId = id;
        _machine = null;
        _debugger = null;
        return $"profile set: {id}";
    }

    private string SetRoms(string dir)
    {
        _romsRoot = dir;
        _machine = null;
        _debugger = null;
        return $"roms set: {Path.GetFullPath(dir)}";
    }

    private string SetKeymap(string id)
    {
        _keymap = id switch
        {
            "pet-2001-graphics" => new Pet2001GraphicsKeyboardMap(),
            "pet-cbm-4032" => new Cbm4032KeyboardMap(),
            "pet-cbm-8032" => new Cbm8032KeyboardMap(),
            _ => throw new InvalidOperationException(
                $"unknown keymap '{id}' (pet-2001-graphics, pet-cbm-4032, pet-cbm-8032)"),
        };
        return $"keymap set: {id}";
    }

    private string LoadTape(string path)
    {
        var machine = EnsureMachine();
        var tap = PetTapFile.Parse(File.ReadAllBytes(path));
        machine.Datasette.LoadTape(tap.PulseCycles, Path.GetFileName(path));
        return $"tape loaded: {Path.GetFileName(path)} ({tap.PulseCycles.Count} pulses)";
    }

    /// <summary>Presses the (emulated) PLAY button - the real-hardware step the KERNAL's
    /// "PRESS PLAY ON TAPE #1" prompt is actually waiting for, distinct from just attaching a
    /// tape file. See <see cref="PetDatasette"/>'s <c>PlayPressed</c>.</summary>
    private string PlayTape()
    {
        EnsureMachine().Datasette.PressPlay();
        return "play pressed";
    }

    private string StopTape()
    {
        EnsureMachine().Datasette.Stop();
        return "stopped";
    }

    private string EjectTape()
    {
        EnsureMachine().Datasette.Eject();
        return "tape ejected";
    }

    private string LoadDisk(string[] parts)
    {
        var machine = EnsureMachine();
        var device = parts.Length > 2 ? int.Parse(parts[2], CultureInfo.InvariantCulture) : 8;
        machine.MountDisk(parts[1], device);
        return $"disk mounted: {Path.GetFileName(parts[1])} on device {device}";
    }

    private string NewDisk(string[] parts)
    {
        var machine = EnsureMachine();
        var path = parts[1];
        var diskName = parts.Length > 2 ? parts[2] : Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
        var device = parts.Length > 3 ? int.Parse(parts[3], CultureInfo.InvariantCulture) : 8;
        machine.MountNewDisk(path, diskName, deviceNumber: device);
        return $"new disk created and mounted: {Path.GetFileName(path)} ({diskName}) on device {device}";
    }

    private string Key(string[] parts)
    {
        var machine = EnsureMachine();
        var row = int.Parse(parts[1], CultureInfo.InvariantCulture);
        var column = int.Parse(parts[2], CultureInfo.InvariantCulture);
        var down = parts[3].Equals("down", StringComparison.OrdinalIgnoreCase);
        if (down) machine.Keyboard.Press(row, column); else machine.Keyboard.Release(row, column);
        return $"key {row},{column} {(down ? "down" : "up")}";
    }

    private string Type(string text)
    {
        var machine = EnsureMachine();
        TextTyper.Type(machine, _keymap, text);
        return $"typed {text.Length} character(s)";
    }

    private string Devices()
    {
        var machine = EnsureMachine();
        var sb = new StringBuilder();
        foreach (var device in machine.Devices)
            sb.AppendLine($"{device.Icon} {device.DisplayName}: {device.StatusText}");
        return sb.ToString();
    }

    private string Status()
    {
        var machine = EnsureMachine();
        return $"profile={machine.Name} cycles={machine.CycleCount} " +
            $"instructions={machine.Processor.InstructionCount} halted={machine.Processor.Halted}";
    }

    /// <summary>Steps <paramref name="count"/> instructions recording PC + every real bus access
    /// per instruction (<see cref="InstructionTracer"/>), then writes the rendered trace to
    /// <paramref name="path"/> - the tool this session's manual read-procedure-trace.log
    /// investigation should have started as. Detaches the tracer afterward so it never lingers on
    /// the machine past this one command.</summary>
    private string TraceLog(int count, string path)
    {
        var machine = EnsureMachine();
        var tracer = new InstructionTracer(machine);
        try
        {
            tracer.Run((ulong)count);
            File.WriteAllText(path, tracer.Render());
        }
        finally
        {
            tracer.Detach();
        }

        return $"trace written: {Path.GetFullPath(path)} ({count} instructions)";
    }

    private string SuperPetDiagnose(int instructionCount) =>
        EnsureMachine().DiagnoseSuperPet6809Startup(instructionCount).Render();

    /// <summary>Runs a SuperPET 6809 boot up to <paramref name="maxInstructions"/> steps and reports
    /// which named boot stages (<see cref="SuperPetBootCheckpoints.WellKnown"/>) were reached, with a
    /// full register + watched-memory snapshot each time, and which were never reached at all -
    /// bounded by construction, unlike <c>superpet-diagnose</c>/<c>trace</c> which both OOM well
    /// before the instruction counts a real boot needs (see docs/pet/superpet-6809-boot-hang.md's
    /// "Tooling gap" section). When <paramref name="path"/> is given the full report is also written
    /// there so a run doesn't need to be repeated to look at it again.</summary>
    private string SuperPetBootCheckpointsCommand(ulong maxInstructions, string? path)
    {
        var report = SuperPetBootCheckpoints.Run(EnsureMachine(), maxInstructions);
        var rendered = report.Render();
        if (path is not null)
            File.WriteAllText(path, rendered);

        return path is null
            ? rendered
            : $"boot-checkpoints written: {Path.GetFullPath(path)} " +
              $"({report.Hits.Count} hits, {report.NeverReached.Count} never reached)";
    }

    /// <summary>Steps <paramref name="steps"/> instructions sampling <see cref="PetMachine.Via"/>'s,
    /// <see cref="PetMachine.Pia1"/>'s and <see cref="PetMachine.Acia"/>'s IRQ lines after each one -
    /// answers "is anything actually interrupting" without writing a one-off instrumented test every
    /// time that question comes up (see docs/pet/superpet-6809-boot-hang.md, which needed exactly
    /// this and had none available).</summary>
    private string ViaIrqCheck(ulong steps)
    {
        var machine = EnsureMachine();
        var viaTrueCount = 0UL;
        var lastViaTrueAt = -1L;
        var pia1TrueCount = 0UL;
        var lastPia1TrueAt = -1L;
        var aciaTrueCount = 0UL;
        var lastAciaTrueAt = -1L;
        for (var i = 0UL; i < steps; i++)
        {
            machine.StepInstruction();
            if (machine.Via.IRQ)
            {
                viaTrueCount++;
                lastViaTrueAt = (long)i;
            }
            if (machine.Pia1.IRQ)
            {
                pia1TrueCount++;
                lastPia1TrueAt = (long)i;
            }
            if (machine.Acia?.Irq == true)
            {
                aciaTrueCount++;
                lastAciaTrueAt = (long)i;
            }
        }

        return $"VIA.IRQ true on {viaTrueCount}/{steps} (last {lastViaTrueAt}); " +
            $"PIA1.IRQ true on {pia1TrueCount}/{steps} (last {lastPia1TrueAt}); " +
            $"ACIA.Irq true on {aciaTrueCount}/{steps} (last {lastAciaTrueAt})";
    }

    /// <summary>Runs up to <paramref name="maxInstructions"/> instructions watching
    /// <see cref="PetMachine.IeeeByteTransferCount"/> for a plateau (<see cref="MachineExtensions.RunUntilOrStalled"/>)
    /// - the fast, scripted way to answer "is a LOAD/SAVE actually stuck, or just slow" instead of
    /// picking an instruction budget by hand and rerunning.</summary>
    private string DiskStallCheck(ulong maxInstructions, ulong stallWindow)
    {
        var machine = EnsureMachine();
        var result = machine.RunUntilOrStalled(_ => false, () => machine.IeeeByteTransferCount, maxInstructions, stallWindow);
        return result.Stalled
            ? $"stalled: no IEEE-488 byte transfer for {stallWindow} instructions (ran {result.InstructionsRun} of {maxInstructions})"
            : $"no stall detected (ran {result.InstructionsRun}, byte transfer count={machine.IeeeByteTransferCount})";
    }

    private PetMachine EnsureMachine()
    {
        if (_machine is not null)
            return _machine;
        if (_profileId is null || _romsRoot is null)
            throw new InvalidOperationException("need 'profile' and 'roms' before any command that touches the machine");

        _machine = new PetMachine(PetProfileCatalog.Find(_profileId), _romsRoot);
        return _machine;
    }

    private MachineDebugger EnsureDebugger() => _debugger ??= new MachineDebugger(EnsureMachine());

    private static string Argument(string line, string command) => line[(command.Length + 1)..].Trim();
}
