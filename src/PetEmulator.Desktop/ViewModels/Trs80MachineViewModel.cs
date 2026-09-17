using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Audio;
using PetEmulator.Core;
using PetEmulator.Desktop.Input;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Trs80;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class Trs80MachineViewModel : ObservableObject, IMachineViewModel, IDiskDriveViewModel, ITapeViewModel
{
    private const ulong InstructionsPerTick = 20_000;
    private readonly string _romsRoot;
    private Trs80Machine _machine;
    private readonly IAudioOutput _audioOutput;
    private readonly ILogger _log = EmulatorLogging.CreateLogger("TRS80-VM");
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DiskIconBrush))] private bool _diskLoaded;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DiskIconBrush))] private bool _diskBusy;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TapeIconBrush))] private bool _tapeLoaded;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TapeIconBrush))] private bool _tapePlaying;
    [ObservableProperty] private string? _diskName;
    [ObservableProperty]
    private IReadOnlyList<StatusField> _statusFields =
    [
        new("AF", "0x0000"), new("BC", "0x0000"), new("DE", "0x0000"), new("HL", "0x0000"),
        new("IX", "0x0000"), new("IY", "0x0000"), new("PC", "0x0000"), new("SP", "0xFFFF"),
        new("I", "0x00"), new("R", "0x00"), new("IFF1", "0"), new("IM", "0"),
        new("Cycles", "0"), new("Instructions", "0"), new("Disk", "none"), new("Tape", "none"),
    ];
    public Trs80MachineViewModel(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        _romsRoot = romsRoot;
        _log.LogInformation("Creating TRS-80 Model I machine from '{Directory}'.", Path.Combine(romsRoot, "trs80"));
        try
        {
            _machine = CreateMachine();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to create TRS-80 Model I machine from '{RomsRoot}'.", romsRoot);
            throw;
        }
        _audioOutput = AudioOutputFactory.CreateNull();
        FrameBuffer = new uint[PixelWidth * PixelHeight];
        Reset();
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }
    public string WindowTitle => "TRS-80 Model I";
    public string MachineSummary => $"TRS-80 Model I | {PixelWidth}×{PixelHeight} | Z80 | Level II";

    public KeyboardToggleViewModel? KeyboardToggle => null;

    public bool IsStatusEnabled { get; set; } = true;
    public int PixelWidth => PetEmulator.Trs80.Display.Trs80RasterDisplay.PixelWidth;
    public IAudioOutput AudioOutput => _audioOutput;
    public int PixelHeight => PetEmulator.Trs80.Display.Trs80RasterDisplay.PixelHeight;
    public (int Width, int Height) PixelAspect => (1, 1);
    public uint[] FrameBuffer { get; }
    public IReadOnlyList<IDeviceStatus> Devices => [];
    public object? Extra => null;
    public IBrush DiskIconBrush => !DiskLoaded ? Brushes.Gray : DiskBusy ? Brushes.Red : Brushes.LimeGreen;

    /// <summary>Read-only tape status: the TRS-80 exposes only its cassette port
    /// (<see cref="ITapeViewModel"/> deliberately has no transport controls - firmware owns the
    /// motor), so unlike <see cref="IDatasetteViewModel"/> machines there are no play/stop/eject
    /// buttons here, just the 📼 state the view binds.</summary>
    public IBrush TapeIconBrush => !TapeLoaded ? Brushes.Gray : TapePlaying ? Brushes.LimeGreen : Brushes.LightGray;
    public event EventHandler? FrameReady;
    public event EventHandler? GeometryChanged;
    public void Tick()
    {
        try
        {
            _machine.Run(InstructionsPerTick);
            _machine.Video.Render(FrameBuffer);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Tick failed at cycles={Cycles} instructions={Instructions}.",
                _machine.CycleCount, _machine.Processor.InstructionCount);
            throw;
        }

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
                new("Tape", _machine.Cassette is null ? "none" : _machine.Cassette.MotorOn ? "playing" : "loaded"),
            ];
            TapeLoaded = _machine.Cassette is not null;
            TapePlaying = _machine.Cassette?.MotorOn == true;
        }

        FrameReady?.Invoke(this, EventArgs.Empty);
    }
    public void Reset()
    {
        try
        {
            _machine.Reset();
            _machine.Video.Render(FrameBuffer);
            FrameReady?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Reset failed.");
            throw;
        }
    }
    public void Dispose() => _audioOutput.Dispose();
    public void LoadDisk(string path)
    {
        try
        {
            _log.LogInformation("Mounting disk '{Path}'.", path);
            _machine.InsertDisk(Path.GetExtension(path).Equals(".dmk", StringComparison.OrdinalIgnoreCase) ? new Trs80DmkDiskImageAdapter(DmkDiskImage.Load(path, _log)) : new Trs80DiskImageAdapter(Jv1DiskImage.Load(path, _log))); DiskLoaded = true; DiskName = Path.GetFileName(path);
            _log.LogInformation("Disk mounted: '{Path}'.", path);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to mount disk '{Path}'.", path);
            throw;
        }
    }
    [RelayCommand]
    private void EjectDisk()
    {
        try
        {
            _log.LogInformation("Ejecting disk '{Disk}'.", DiskName ?? "(none)");
            _machine.EjectDisk();
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
    public void LoadTape(string path)
    {
        try
        {
            _log.LogInformation("Loading tape '{Path}'.", path);
            _machine.LoadTape(File.ReadAllBytes(path));
            _log.LogInformation("Tape loaded: '{Path}'.", path);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load tape '{Path}'.", path);
            throw;
        }
    }
    public void HandleKey(Key key, HostKeyEventKind kind) { if (TryMap(key, out var trsKey)) _machine.Keyboard.SetKeyDown(trsKey, kind == HostKeyEventKind.Press); }
    private Trs80Machine CreateMachine(PetEmulator.Chips.IFD1791DiskImage? disk = null) { var root = Path.Combine(_romsRoot, "trs80"); var font = new Trs80CharacterFont(File.ReadAllBytes(Path.Combine(root, "character_set_8s.bin"))); return new Trs80Machine(File.ReadAllBytes(Path.Combine(root, "model1-level2-v1.4.bin")), disk: disk, font: font, logger: _log); }
    private static bool TryMap(Key key, out Trs80Key value)
    {
        if (key is >= Key.A and <= Key.Z) { value = (Trs80Key)((int)Trs80Key.A + key - Key.A); return true; }
        if (key is >= Key.D0 and <= Key.D9) { value = (Trs80Key)((int)Trs80Key.D0 + key - Key.D0); return true; }
        value = key switch { Key.Enter or Key.Return => Trs80Key.Enter, Key.Space => Trs80Key.Space, Key.LeftShift or Key.RightShift => Trs80Key.Shift, Key.Up => Trs80Key.Up, Key.Down => Trs80Key.Down, Key.Left => Trs80Key.Left, Key.Right => Trs80Key.Right, Key.OemMinus => Trs80Key.Minus, Key.OemComma => Trs80Key.Comma, Key.OemPeriod => Trs80Key.Period, Key.OemQuestion => Trs80Key.Slash, _ => (Trs80Key)(-1) };
        return (int)value >= 0;
    }
}
