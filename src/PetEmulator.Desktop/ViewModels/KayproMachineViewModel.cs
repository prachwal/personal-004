using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PetEmulator.Core;
using PetEmulator.Desktop.Input;
using PetEmulator.Kaypro;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Kaypro II profile and terminal surface for the Avalonia shell.</summary>
public sealed partial class KayproMachineViewModel : ObservableObject, IMachineViewModel, IDiskDriveViewModel
{
    private const ulong InstructionsPerTick = 20_000;
    private readonly KayproMachine _machine;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiskIconBrush))]
    private bool _diskLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiskIconBrush))]
    private bool _diskBusy;

    [ObservableProperty]
    private string _windowTitle = "Kaypro II";

    [ObservableProperty]
    private string _statusText = "Kaypro II  Z80  PC=0x0000  Cycles=0 Instructions=0";

    [ObservableProperty]
    private IReadOnlyList<IDeviceStatus> _devices = [];

    public KayproMachineViewModel(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        var kayproRoot = Path.Combine(romsRoot, "kaypro");
        _machine = new KayproMachine();
        _machine.LoadMonitorRom(File.ReadAllBytes(Path.Combine(kayproRoot, "kaypro-81-149c.bin")));
        Font = KayproFont.Load(Path.Combine(kayproRoot, "kaypro-81-146.bin"));
        var diskPath = Path.Combine(kayproRoot, "cpm22-rom149.dsk");
        if (File.Exists(diskPath))
        {
            _machine.InsertDisk(0, LoadKayproDisk(diskPath));
            DiskLoaded = true;
        }

        FrameBuffer = new uint[PixelWidth * PixelHeight];
        Reset();
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    public KayproFont Font { get; }
    public IBrush DiskIconBrush => !DiskLoaded ? Brushes.Gray : DiskBusy ? Brushes.Red : Brushes.LimeGreen;
    public int PixelWidth => KayproVideo.PixelWidth;
    public int PixelHeight => KayproVideo.PixelHeight;
    // The 640x240 Kaypro raster uses a 2:1 vertical pixel correction to
    // reproduce the 4:3 CRT geometry of the 80x24, 8x10 character display.
    public (int Width, int Height) PixelAspect => (1, 2);
    public uint[] FrameBuffer { get; }
    public object? Extra => null;
    public event EventHandler? FrameReady;
    public event EventHandler? GeometryChanged;

    public void LoadDisk(string path)
    {
        _machine.InsertDisk(0, LoadKayproDisk(path));
        DiskLoaded = true;
    }

    public void Reset()
    {
        _machine.Reset();
        Render();
    }

    public void HandleKey(Key key, HostKeyEventKind kind)
    {
        if (kind != HostKeyEventKind.Press || !TryMapTerminalByte(key, out var value))
            return;
        _machine.FeedKeyboardByte(value);
    }

    public void Tick()
    {
        _machine.Run(InstructionsPerTick);
        Render();

        var registers = ((IDebuggableProcessor)_machine.Processor).GetRegisters();
        StatusText = $"Kaypro II  Z80  PC=0x{registers["PC"]:X4} SP=0x{registers["SP"]:X4} " +
                     $"Cycles={_machine.CycleCount} Instructions={_machine.Processor.InstructionCount} " +
                     $"Disk={(DiskLoaded ? "ready" : "none")}";
        FrameReady?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() { }

    private void Render() => _machine.Video.Render(FrameBuffer, Font);

    private static KayproDiskImage LoadKayproDisk(string path)
    {
        if (Path.GetExtension(path).Equals(".td0", StringComparison.OrdinalIgnoreCase))
        {
            using var stream = File.OpenRead(path);
            return KayproTd0Reader.Read(stream);
        }

        // The 81-149C CP/M disk image uses zero-based sector IDs.  This is
        // also the convention used by KayproTd0Reader and the binary boot test.
        return new KayproDiskImage(File.ReadAllBytes(path), firstSectorId: 0);
    }

    private static bool TryMapTerminalByte(Key key, out byte value)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            value = (byte)('A' + (key - Key.A));
            return true;
        }
        if (key is >= Key.D0 and <= Key.D9)
        {
            value = (byte)('0' + (key - Key.D0));
            return true;
        }

        value = key switch
        {
            Key.Space => 0x20,
            Key.Return or Key.Enter => 0x0D,
            Key.Back => 0x08,
            Key.Tab => 0x09,
            Key.Escape => 0x1B,
            Key.OemMinus => (byte)'-',
            Key.OemPlus => (byte)'=',
            Key.OemComma => (byte)',',
            Key.OemPeriod => (byte)'.',
            Key.OemQuestion => (byte)'/',
            Key.OemSemicolon => (byte)';',
            Key.OemQuotes => (byte)'\'',
            _ => 0
        };
        return value != 0;
    }
}
