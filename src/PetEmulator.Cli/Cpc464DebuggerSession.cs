using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Debugger;
using PetEmulator.Cpc464;

namespace PetEmulator.Cli;

/// <summary>Mirrors <see cref="Trs80DebuggerSession"/> for the Amstrad CPC464: no FDC (CPC464 has
/// no disk drive), so this only adds "tape" (a .cdt image, played back through the same
/// Cpc464Cassette.LoadPulses path the Desktop ViewModel uses) and "key" (raw 10x8 matrix position -
/// CPC464 has no named key enum like Trs80Key, so the script gives row/column directly, matching
/// Cpc464Keyboard.SetKey's own parameters).</summary>
public sealed class Cpc464DebuggerSession
{
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("CLI");
    private string? _romsRoot;
    private Cpc464Machine? _machine;
    private MachineDebugger? _debugger;

    public string Execute(string commandLine)
    {
        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return string.Empty;
        try
        {
            return parts[0] switch
            {
                "roms" => SetRoms(Argument(commandLine, parts[0])),
                "tape" => LoadTape(parts[1]),
                "key" => Key(parts),
                "status" => Status(),
                _ => EnsureDebugger().Execute(commandLine)
            };
        }
        catch (Exception ex) { Log.LogError(ex, "cpc464-debug '{Command}' failed.", commandLine); return $"error: {ex.Message}"; }
    }

    private string SetRoms(string path) { _romsRoot = path; _machine = null; _debugger = null; return $"roms set: {Path.GetFullPath(path)}"; }
    private string LoadTape(string path)
    {
        var image = Cpc464CdtImage.Parse(File.ReadAllBytes(path));
        EnsureMachine().Bus.Cassette.LoadPulses(image.PulseTicks);
        return $"tape loaded: {Path.GetFileName(path)}";
    }
    private string Key(string[] parts)
    {
        var row = byte.Parse(parts[1]);
        var column = byte.Parse(parts[2]);
        var down = parts[3].Equals("down", StringComparison.OrdinalIgnoreCase);
        EnsureMachine().Bus.Keyboard.SetKey(row, column, down);
        return $"key {row},{column} {parts[3]}";
    }
    private string Status() { var machine = EnsureMachine(); return $"profile={machine.Name} cycles={machine.CycleCount} instructions={machine.Processor.InstructionCount} halted={machine.Processor.Halted}"; }
    private Cpc464Machine EnsureMachine() => _machine ??= CreateMachine();
    private Cpc464Machine CreateMachine()
    {
        if (_romsRoot is null) throw new InvalidOperationException("need 'roms' before any command that touches the machine");
        var rom = File.ReadAllBytes(Path.Combine(_romsRoot, "cpc464", "cpc464.rom"));
        return new Cpc464Machine(rom);
    }
    private MachineDebugger EnsureDebugger() => _debugger ??= new MachineDebugger(EnsureMachine());
    private static string Argument(string line, string command) => line[(command.Length + 1)..].Trim();
}
