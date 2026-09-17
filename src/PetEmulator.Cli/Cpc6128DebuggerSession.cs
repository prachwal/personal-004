using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Cpc6128;
using PetEmulator.Cpc464;
using PetEmulator.CpcFdc;
using PetEmulator.Debugger;

namespace PetEmulator.Cli;

/// <summary>Headless debugger session for an Amstrad CPC6128.</summary>
public sealed class Cpc6128DebuggerSession
{
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("CLI");
    private string? _romsRoot;
    private Cpc6128Machine? _machine;
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
                "disk" => LoadDisk(parts),
                "key" => Key(parts),
                "status" => Status(),
                _ => EnsureDebugger().Execute(commandLine)
            };
        }
        catch (Exception ex) { Log.LogError(ex, "cpc6128-debug '{Command}' failed.", commandLine); return $"error: {ex.Message}"; }
    }

    private string SetRoms(string path) { _romsRoot = path; _machine = null; _debugger = null; return $"roms set: {Path.GetFullPath(path)}"; }

    private string LoadTape(string path)
    {
        var image = Cpc464CdtImage.Parse(File.ReadAllBytes(path));
        EnsureMachine().Cassette.LoadPulses(image.PulseTicks);
        return $"tape loaded: {Path.GetFileName(path)}";
    }

    private string LoadDisk(string[] parts)
    {
        var drive = int.Parse(parts[1]);
        var image = DskDiskImage.Load(File.ReadAllBytes(parts[2]));
        EnsureMachine().LoadDisk(drive, image);
        return $"disk {drive} loaded: {Path.GetFileName(parts[2])}";
    }

    private string Key(string[] parts)
    {
        var row = byte.Parse(parts[1]);
        var column = byte.Parse(parts[2]);
        var down = parts[3].Equals("down", StringComparison.OrdinalIgnoreCase);
        EnsureMachine().Keyboard.SetKey(row, column, down);
        return $"key {row},{column} {parts[3]}";
    }

    private string Status()
    {
        var machine = EnsureMachine();
        return $"profile={machine.Name} cycles={machine.CycleCount} instructions={machine.Processor.InstructionCount} halted={machine.Processor.Halted}";
    }

    private Cpc6128Machine EnsureMachine() => _machine ??= CreateMachine();

    private Cpc6128Machine CreateMachine()
    {
        if (_romsRoot is null) throw new InvalidOperationException("need 'roms' before any command that touches the machine");
        var rom = File.ReadAllBytes(Path.Combine(_romsRoot, "cpc6128", "cpc6128.rom"));
        return new Cpc6128Machine(rom);
    }

    private MachineDebugger EnsureDebugger() => _debugger ??= new MachineDebugger(EnsureMachine());
    private static string Argument(string line, string command) => line[(command.Length + 1)..].Trim();
}
