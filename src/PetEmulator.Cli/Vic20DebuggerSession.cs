using System.Globalization;
using System.Text;
using PetEmulator.Debugger;
using PetEmulator.Pet.Tape;
using PetEmulator.Vic20;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Cli;

/// <summary>
/// Scripted, headless command session for a <see cref="Vic20Machine"/> - the VIC-20 analog of
/// <see cref="PetDebuggerSession"/> (see docs/vic20-migration-plan.md step 10). No <c>profile</c>
/// command (v1 is NTSC-unexpanded only, see that plan's scope cuts) - just <c>roms</c>, then
/// machine-touching commands. Everything not listed here (<c>trace</c>/<c>watch</c>/
/// <c>break-cycle</c>/<c>break-pc</c>/<c>dump</c>/...) delegates to <see cref="MachineDebugger"/>,
/// which needs zero VIC-20-specific code to work (built purely against IMachine).
/// </summary>
public sealed class Vic20DebuggerSession
{
    private string? _romsRoot;
    private Vic20Machine? _machine;
    private MachineDebugger? _debugger;

    public string Execute(string commandLine)
    {
        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return string.Empty;

        try
        {
            return parts[0] switch
            {
                "roms" => SetRoms(Argument(commandLine, parts[0])),
                "tape" => LoadTape(Argument(commandLine, parts[0])),
                "new-tape" => NewTape(Argument(commandLine, parts[0])),
                "play" => PlayTape(),
                "stop" => StopTape(),
                "eject" => EjectTape(),
                "key" => Key(parts),
                "type" => Type(commandLine[(parts[0].Length + 1)..]),
                "devices" => Devices(),
                "status" => Status(),
                _ => EnsureDebugger().Execute(commandLine),
            };
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }
    }

    private string SetRoms(string dir)
    {
        _romsRoot = dir;
        _machine = null;
        _debugger = null;
        return $"roms set: {Path.GetFullPath(dir)}";
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
        Vic20TextTyper.Type(machine, text);
        return $"typed {text.Length} character(s)";
    }

    private string LoadTape(string path)
    {
        var machine = EnsureMachine();
        var tap = PetTapFile.Parse(File.ReadAllBytes(path));
        machine.Datasette.LoadTape(tap.PulseCycles, Path.GetFileName(path));
        return $"tape loaded: {Path.GetFileName(path)} ({tap.PulseCycles.Count} pulses)";
    }

    private string NewTape(string name)
    {
        EnsureMachine().Datasette.NewBlankTape(string.IsNullOrWhiteSpace(name) ? "New Tape" : name);
        return $"new blank tape: {(string.IsNullOrWhiteSpace(name) ? "New Tape" : name)}";
    }

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

    private Vic20Machine EnsureMachine()
    {
        if (_machine is not null)
            return _machine;
        if (_romsRoot is null)
            throw new InvalidOperationException("need 'roms' before any command that touches the machine");

        _machine = new Vic20Machine(_romsRoot);
        return _machine;
    }

    private MachineDebugger EnsureDebugger() => _debugger ??= new MachineDebugger(EnsureMachine());

    private static string Argument(string line, string command) => line[(command.Length + 1)..].Trim();
}
