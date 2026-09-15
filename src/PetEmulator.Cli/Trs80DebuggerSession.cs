using System.Globalization;
using PetEmulator.Debugger;
using PetEmulator.Trs80;

namespace PetEmulator.Cli;

public sealed class Trs80DebuggerSession
{
    private string? _romsRoot;
    private Trs80Machine? _machine;
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
                "disk" => LoadDisk(parts[1]),
                "tape" => LoadTape(parts[1]),
                "key" => Key(parts),
                "status" => Status(),
                _ => EnsureDebugger().Execute(commandLine)
            };
        }
        catch (Exception ex) { return $"error: {ex.Message}"; }
    }

    private string SetRoms(string path) { _romsRoot = path; _machine = null; _debugger = null; return $"roms set: {Path.GetFullPath(path)}"; }
    private string LoadDisk(string path) { EnsureMachine().InsertDisk(LoadDiskImage(path)); return $"disk mounted: {Path.GetFileName(path)}"; }
    private string LoadTape(string path) { EnsureMachine().LoadTape(File.ReadAllBytes(path)); return $"tape loaded: {Path.GetFileName(path)}"; }
    private string Key(string[] parts) { var key = Enum.Parse<Trs80Key>(parts[1], true); EnsureMachine().Keyboard.SetKeyDown(key, parts[2].Equals("down", StringComparison.OrdinalIgnoreCase)); return $"key {key} {parts[2]}"; }
    private string Status() { var machine = EnsureMachine(); return $"profile={machine.Name} cycles={machine.CycleCount} instructions={machine.Processor.InstructionCount} halted={machine.Processor.Halted}"; }
    private Trs80Machine EnsureMachine() => _machine ??= CreateMachine();
    private Trs80Machine CreateMachine(PetEmulator.Chips.IFD1791DiskImage? disk = null, byte[]? tape = null)
    {
        if (_romsRoot is null) throw new InvalidOperationException("need 'roms' before any command that touches the machine");
        var root = Path.Combine(_romsRoot, "trs80");
        var rom = File.ReadAllBytes(Path.Combine(root, "model1-level2-v1.4.bin"));
        var fontPath = Path.Combine(root, "character_set_8s.bin");
        return new Trs80Machine(rom, disk: disk, tape: tape, font: File.Exists(fontPath) ? new Trs80CharacterFont(File.ReadAllBytes(fontPath)) : null);
    }
    private static PetEmulator.Chips.IFD1791DiskImage LoadDiskImage(string path) =>
        Path.GetExtension(path).Equals(".dmk", StringComparison.OrdinalIgnoreCase)
            ? new Trs80DmkDiskImageAdapter(DmkDiskImage.Load(path))
            : new Trs80DiskImageAdapter(Jv1DiskImage.Load(path));
    private MachineDebugger EnsureDebugger() => _debugger ??= new MachineDebugger(EnsureMachine());
    private static string Argument(string line, string command) => line[(command.Length + 1)..].Trim();
}
