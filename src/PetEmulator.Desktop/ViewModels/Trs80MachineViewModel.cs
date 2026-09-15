using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PetEmulator.Core;
using PetEmulator.Desktop.Input;
using PetEmulator.Trs80;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class Trs80MachineViewModel : ObservableObject, IMachineViewModel, IDiskDriveViewModel
{
    private const ulong InstructionsPerTick = 20_000;
    private readonly string _romsRoot;
    private Trs80Machine _machine;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DiskIconBrush))] private bool _diskLoaded;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DiskIconBrush))] private bool _diskBusy;
    [ObservableProperty] private string _statusText = "TRS-80 Model I  Z80  PC=0x0000  Cycles=0";
    public Trs80MachineViewModel(string romsRoot)
    {
        _romsRoot = romsRoot;
        _machine = CreateMachine();
        FrameBuffer = new uint[PixelWidth * PixelHeight];
        Reset();
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }
    public string WindowTitle => "TRS-80 Model I";
    public int PixelWidth => PetEmulator.Trs80.Display.Trs80RasterDisplay.PixelWidth;
    public int PixelHeight => PetEmulator.Trs80.Display.Trs80RasterDisplay.PixelHeight;
    public (int Width, int Height) PixelAspect => (1, 1);
    public uint[] FrameBuffer { get; }
    public IReadOnlyList<IDeviceStatus> Devices => [];
    public object? Extra => null;
    public IBrush DiskIconBrush => !DiskLoaded ? Brushes.Gray : DiskBusy ? Brushes.Red : Brushes.LimeGreen;
    public event EventHandler? FrameReady;
    public event EventHandler? GeometryChanged;
    public void Tick() { _machine.Run(InstructionsPerTick); _machine.Video.Render(FrameBuffer); var r = ((IDebuggableProcessor)_machine.Processor).GetRegisters(); StatusText = $"TRS-80 Model I  Z80  PC=0x{r["PC"]:X4} SP=0x{r["SP"]:X4} Cycles={_machine.CycleCount}"; FrameReady?.Invoke(this, EventArgs.Empty); }
    public void Reset() { _machine.Reset(); _machine.Video.Render(FrameBuffer); FrameReady?.Invoke(this, EventArgs.Empty); }
    public void Dispose() { }
    public void LoadDisk(string path) { _machine.InsertDisk(Path.GetExtension(path).Equals(".dmk", StringComparison.OrdinalIgnoreCase) ? new Trs80DmkDiskImageAdapter(DmkDiskImage.Load(path)) : new Trs80DiskImageAdapter(Jv1DiskImage.Load(path))); DiskLoaded = true; }
    public void LoadTape(string path) => _machine.LoadTape(File.ReadAllBytes(path));
    public void HandleKey(Key key, HostKeyEventKind kind) { if (TryMap(key, out var trsKey)) _machine.Keyboard.SetKeyDown(trsKey, kind == HostKeyEventKind.Press); }
    private Trs80Machine CreateMachine(PetEmulator.Chips.IFD1791DiskImage? disk = null) { var root = Path.Combine(_romsRoot, "trs80"); var font = new Trs80CharacterFont(File.ReadAllBytes(Path.Combine(root, "character_set_8s.bin"))); return new Trs80Machine(File.ReadAllBytes(Path.Combine(root, "model1-level2-v1.4.bin")), disk: disk, font: font); }
    private static bool TryMap(Key key, out Trs80Key value)
    {
        if (key is >= Key.A and <= Key.Z) { value = (Trs80Key)((int)Trs80Key.A + key - Key.A); return true; }
        if (key is >= Key.D0 and <= Key.D9) { value = (Trs80Key)((int)Trs80Key.D0 + key - Key.D0); return true; }
        value = key switch { Key.Enter or Key.Return => Trs80Key.Enter, Key.Space => Trs80Key.Space, Key.LeftShift or Key.RightShift => Trs80Key.Shift, Key.Up => Trs80Key.Up, Key.Down => Trs80Key.Down, Key.Left => Trs80Key.Left, Key.Right => Trs80Key.Right, Key.OemMinus => Trs80Key.Minus, Key.OemComma => Trs80Key.Comma, Key.OemPeriod => Trs80Key.Period, Key.OemQuestion => Trs80Key.Slash, _ => (Trs80Key)(-1) };
        return (int)value >= 0;
    }
}
