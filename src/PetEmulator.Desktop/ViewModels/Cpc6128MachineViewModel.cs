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
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class Cpc6128MachineViewModel : ObservableObject, IMachineViewModel, IDatasetteViewModel, IDiskDriveViewModel
{
    private const ulong InstructionsPerTick = 20_000;
    private readonly Cpc6128Machine _machine;
    private readonly IAudioOutput _audioOutput;
    [ObservableProperty] private bool _tapeLoaded;
    [ObservableProperty] private bool _tapePlaying;
    [ObservableProperty] private bool _diskLoaded;
    [ObservableProperty] private bool _diskBusy;
    [ObservableProperty] private string _statusText = "Amstrad CPC6128  Z80  PC=0x0000";

    public Cpc6128MachineViewModel(string romsRoot)
    {
        _machine = new Cpc6128Machine(File.ReadAllBytes(Path.Combine(romsRoot, "cpc6128", "cpc6128.rom")));
        _machine.LoadExpansionRom(7, File.ReadAllBytes(Path.Combine(romsRoot, "cpc6128", "amsdos.rom")));
        _audioOutput = AudioOutputFactory.CreateDefault();
        _audioOutput.Start(_machine.Ay);
        FrameBuffer = new uint[320 * 200];
        Reset();
    }

    public string WindowTitle => "Amstrad CPC6128";
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
        StatusText = $"Amstrad CPC6128  Z80  PC=0x{_machine.Cpu.Registers.PC:X4}  Cycles={_machine.CycleCount}";
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
        _machine.Cassette.LoadPulses(Cpc464CdtImage.Parse(File.ReadAllBytes(path)).PulseTicks);
        TapeLoaded = true;
        TapePlaying = false;
    }

    public void LoadDisk(string path)
    {
        _machine.LoadDisk(0, DskDiskImage.Load(File.ReadAllBytes(path)));
        DiskLoaded = true;
        DiskBusy = false;
    }

    [RelayCommand] private void PlayTape() { if (_machine.Cassette.HasTape) { _machine.Cassette.PressPlay(); TapePlaying = true; } }
    [RelayCommand] private void StopTape() { _machine.Cassette.Stop(); TapePlaying = false; }
    [RelayCommand] private void EjectTape() { _machine.Cassette.Eject(); TapeLoaded = false; TapePlaying = false; }
    public void Dispose() => _audioOutput.Dispose();

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
}
