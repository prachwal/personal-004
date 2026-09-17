using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Audio;
using PetEmulator.Cpc464;
using PetEmulator.Cpc6128;
using PetEmulator.CpcFdc;
using PetEmulator.Core;
using PetEmulator.Desktop.Input;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class Cpc6128MachineViewModel : ObservableObject, IMachineViewModel, IDatasetteViewModel, IDiskDriveViewModel
{
    private const ulong InstructionsPerTick = 20_000;
    private readonly Cpc6128Machine _machine;
    private readonly IAudioOutput _audioOutput;
    private readonly ILogger _log = EmulatorLogging.CreateLogger("CPC6128");
    [ObservableProperty] private bool _tapeLoaded;
    [ObservableProperty] private bool _tapePlaying;
    [ObservableProperty] private bool _diskLoaded;
    [ObservableProperty] private bool _diskBusy;
    [ObservableProperty] private string? _diskName;
    [ObservableProperty]
    private IReadOnlyList<StatusField> _statusFields =
    [
        new("AF", "0x0000"), new("BC", "0x0000"), new("DE", "0x0000"), new("HL", "0x0000"),
        new("IX", "0x0000"), new("IY", "0x0000"), new("PC", "0x0000"), new("SP", "0xFFFF"),
        new("I", "0x00"), new("R", "0x00"), new("IFF1", "0"), new("IM", "0"),
        new("Cycles", "0"), new("Instructions", "0"), new("Disk", "none"),
    ];

    public Cpc6128MachineViewModel(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        var romPath = Path.Combine(romsRoot, "cpc6128", "cpc6128.rom");
        var expansionPath = Path.Combine(romsRoot, "cpc6128", "amsdos.rom");
        _log.LogInformation("Creating CPC6128 machine from '{Rom}' + '{Expansion}'.", romPath, expansionPath);
        try
        {
            _machine = new Cpc6128Machine(File.ReadAllBytes(romPath), _log);
            _machine.LoadExpansionRom(7, File.ReadAllBytes(expansionPath));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to create Cpc6128Machine from '{Rom}'.", romPath);
            throw;
        }

        _audioOutput = CreateAudioOutput();
        FrameBuffer = new uint[320 * 200];
        Reset();
    }

    public string WindowTitle => "Amstrad CPC6128";
    public string MachineSummary => $"Amstrad CPC6128 | {PixelWidth}×{PixelHeight} | Z80 | 128K";

    public KeyboardToggleViewModel? KeyboardToggle => null;

    public bool IsStatusEnabled { get; set; } = true;
    public IAudioOutput AudioOutput => _audioOutput;
    public int PixelWidth => 320;
    public int PixelHeight => 200;
    public (int Width, int Height) PixelAspect => (1, 1);
    public uint[] FrameBuffer { get; }
    public IReadOnlyList<IDeviceStatus> Devices => [];
    public object? Extra => null;
    public IBrush TapeIconBrush => !TapeLoaded ? Brushes.Gray : TapePlaying ? Brushes.LimeGreen : Brushes.LightGray;
    public IBrush DiskIconBrush => DiskLoaded ? Brushes.LimeGreen : Brushes.Gray;
    public event EventHandler? FrameReady;
    public event EventHandler? GeometryChanged;

    public void Tick()
    {
        _machine.Run(InstructionsPerTick);
        Render();
        if (IsStatusEnabled)
        {
            var r = ((IDebuggableProcessor)_machine.Processor).GetRegisters();
            StatusFields =
            [
                new("AF", $"0x{r["AF"]:X4}"), new("BC", $"0x{r["BC"]:X4}"),
                new("DE", $"0x{r["DE"]:X4}"), new("HL", $"0x{r["HL"]:X4}"),
                new("IX", $"0x{r["IX"]:X4}"), new("IY", $"0x{r["IY"]:X4}"),
                new("PC", $"0x{r["PC"]:X4}"), new("SP", $"0x{r["SP"]:X4}"),
                new("I", $"0x{r["I"]:X2}"), new("R", $"0x{r["R"]:X2}"),
                new("IFF1", $"{r["IFF1"]}"), new("IM", $"{r["IM"]}"),
                new("Cycles", $"{_machine.CycleCount}"),
                new("Instructions", $"{_machine.Processor.InstructionCount}"),
                new("Disk", DiskLoaded ? "ready" : "none"),
            ];
        }
        FrameReady?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        _machine.Reset();
        TapeLoaded = _machine.Cassette.HasTape;
        TapePlaying = false;
        Render();
        FrameReady?.Invoke(this, EventArgs.Empty);
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadTape(string path)
    {
        try
        {
            _log.LogInformation("Loading tape '{Path}'.", path);
            _machine.Cassette.LoadPulses(Cpc464CdtImage.Parse(File.ReadAllBytes(path), _log).PulseTicks);
            TapeLoaded = true;
            TapePlaying = false;
            _log.LogInformation("Tape loaded: '{Path}'.", path);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load tape '{Path}'.", path);
            throw;
        }
    }

    public void LoadDisk(string path)
    {
        try
        {
            _log.LogInformation("Loading disk '{Path}'.", path);
            _machine.LoadDisk(0, DskDiskImage.Load(File.ReadAllBytes(path)));
            DiskLoaded = true;
            DiskBusy = false;
            DiskName = Path.GetFileName(path);
            _log.LogInformation("Disk loaded: '{Path}'.", path);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load disk '{Path}'.", path);
            throw;
        }
    }

    [RelayCommand]
    private void EjectDisk()
    {
        try
        {
            _log.LogInformation("Ejecting disk '{Disk}'.", DiskName ?? "(none)");
            _machine.EjectDisk(0);
            DiskLoaded = false;
            DiskBusy = false;
            DiskName = null;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to eject disk.");
            throw;
        }
    }

    [RelayCommand] private void PlayTape() { if (_machine.Cassette.HasTape) { _machine.Cassette.PressPlay(); TapePlaying = true; } }
    [RelayCommand] private void StopTape() { _machine.Cassette.Stop(); TapePlaying = false; }
    [RelayCommand] private void EjectTape() { _machine.Cassette.Eject(); TapeLoaded = false; TapePlaying = false; }
    public void Dispose()
    {
        try
        {
            _audioOutput.Dispose();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Audio output dispose failed.");
        }
    }

    public void HandleKey(Key key, HostKeyEventKind kind)
    {
        if (Matrix.TryGetValue(key, out var cell)) _machine.Keyboard.SetKey(cell.Row, cell.Column, kind == HostKeyEventKind.Press);
    }

    private void Render()
    {
        _machine.GateArray.RenderFrame();
        var pixels = _machine.GateArray.Pixels;
        for (var index = 0; index < FrameBuffer.Length; index++)
            FrameBuffer[index] = PetEmulator.Cpc.CpcGateArray.HardwareColors[_machine.GateArray.GetInkColorIndex(pixels[index])];
    }

    private static readonly Dictionary<Key, (byte Row, byte Column)> Matrix = new()
    {
        [Key.Up] = (0, 0), [Key.Right] = (0, 1), [Key.Down] = (0, 2), [Key.Left] = (1, 0),
        [Key.Return] = (2, 2), [Key.Enter] = (2, 2), [Key.LeftShift] = (2, 5), [Key.RightShift] = (2, 5),
        [Key.P] = (3, 3), [Key.O] = (4, 2), [Key.I] = (4, 3), [Key.L] = (4, 4), [Key.K] = (4, 5), [Key.M] = (4, 6),
        [Key.U] = (5, 2), [Key.Y] = (5, 3), [Key.H] = (5, 4), [Key.J] = (5, 5), [Key.N] = (5, 6), [Key.Space] = (5, 7),
        [Key.R] = (6, 2), [Key.T] = (6, 3), [Key.G] = (6, 4), [Key.F] = (6, 5), [Key.B] = (6, 6), [Key.V] = (6, 7),
        [Key.E] = (7, 2), [Key.W] = (7, 3), [Key.S] = (7, 4), [Key.D] = (7, 5), [Key.C] = (7, 6), [Key.X] = (7, 7),
        [Key.Q] = (8, 3), [Key.A] = (8, 5), [Key.Z] = (8, 7), [Key.Back] = (9, 7), [Key.Delete] = (9, 7),
        [Key.D0] = (4, 0), [Key.D1] = (8, 0), [Key.D2] = (8, 1), [Key.D3] = (7, 1), [Key.D4] = (7, 0),
        [Key.D5] = (6, 1), [Key.D6] = (6, 0), [Key.D7] = (5, 1), [Key.D8] = (5, 0), [Key.D9] = (4, 1),
    };

    /// <summary>Platform audio backend with silent fallback - see
    /// <see cref="Vic20MachineViewModel"/> for why the fallback exists.</summary>
    private IAudioOutput CreateAudioOutput()
    {
        try
        {
            var output = AudioOutputFactory.CreateDefault();
            output.Start(_machine.Ay);
            _log.LogInformation("Audio backend started: {Backend}.", output.GetType().Name);
            return output;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "No platform audio backend ({ErrorType}: {Message}); continuing silent.", ex.GetType().Name, ex.Message);
            var silent = AudioOutputFactory.CreateNull();
            silent.Start(_machine.Ay);
            return silent;
        }
    }
}
