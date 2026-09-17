using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Audio;
using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.Desktop.Input;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Desktop.Views.Controls;
using PetEmulator.Kaypro;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Kaypro II profile and terminal surface for the Avalonia shell.</summary>
public sealed partial class KayproMachineViewModel : ObservableObject, IMachineViewModel, IDiskDriveViewModel
{
    private const ulong InstructionsPerTick = 20_000;
    private static readonly TimeSpan DiskActivityLinger = TimeSpan.FromMilliseconds(200);
    private readonly KayproMachine _machine;
    private readonly IAudioOutput _audioOutput;
    private readonly ILogger _log = EmulatorLogging.CreateLogger("KAYPRO-VM");
    private DateTime _diskActivityUntilUtc = DateTime.MinValue;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiskIconBrush))]
    private bool _diskLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiskIconBrush))]
    private bool _diskBusy;

    [ObservableProperty]
    private string? _diskName;

    [ObservableProperty]
    private string _windowTitle = "Kaypro II";

    [ObservableProperty]
    private IReadOnlyList<StatusField> _statusFields =
    [
        new("AF", "0x0000"), new("BC", "0x0000"), new("DE", "0x0000"), new("HL", "0x0000"),
        new("IX", "0x0000"), new("IY", "0x0000"), new("PC", "0x0000"), new("SP", "0xFFFF"),
        new("I", "0x00"), new("R", "0x00"), new("IFF1", "0"), new("IM", "0"),
        new("Cycles", "0"), new("Instructions", "0"), new("Disk", "none"),
    ];

    [ObservableProperty]
    private IReadOnlyList<IDeviceStatus> _devices = [];

    public KayproMachineViewModel(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        var kayproRoot = Path.Combine(romsRoot, "kaypro");
        _log.LogInformation("Creating Kaypro II machine from '{Directory}'.", kayproRoot);
        try
        {
            _machine = new KayproMachine(logger: _log);
            _audioOutput = AudioOutputFactory.CreateNull();
            _machine.Bus.FdcWiring.ActivityChanged += OnFdcActivityChanged;
            _machine.LoadMonitorRom(File.ReadAllBytes(Path.Combine(kayproRoot, "kaypro-81-149c.bin")));
            Font = KayproFont.Load(Path.Combine(kayproRoot, "kaypro-81-146.bin"));
            var diskPath = Path.Combine(kayproRoot, "cpm22-rom149.dsk");
            if (File.Exists(diskPath))
            {
                _machine.InsertDisk(0, LoadKayproDisk(diskPath));
                DiskLoaded = true;
                DiskName = Path.GetFileName(diskPath);
                _log.LogInformation("Boot disk auto-mounted: '{Path}'.", diskPath);
            }
            else
            {
                _log.LogInformation("No boot disk at '{Path}'; starting diskless.", diskPath);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to create Kaypro II machine from '{Directory}'.", kayproRoot);
            throw;
        }

        FrameBuffer = new uint[PixelWidth * PixelHeight];
        Reset();
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    public KayproFont Font { get; }
    public string MachineSummary => $"Kaypro II | {PixelWidth}×{PixelHeight} | Z80 | CP/M | ENTER sends CR";

    /// <summary>The single on-screen keyboard toggle, bound both by the header bar's ⌨ button
    /// and the keyboard zone visibility - one source of truth, no sync code.</summary>
    public KeyboardToggleViewModel KeyboardToggle { get; } = new();

    public bool IsStatusEnabled { get; set; } = true;
    public IBrush DiskIconBrush => !DiskLoaded ? Brushes.Gray : DiskBusy ? Brushes.Red : Brushes.LimeGreen;
    public int PixelWidth => KayproVideo.PixelWidth;
    public IAudioOutput AudioOutput => _audioOutput;
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
        try
        {
            _log.LogInformation("Mounting disk '{Path}'.", path);
            _machine.InsertDisk(0, LoadKayproDisk(path));
            DiskLoaded = true;
            DiskName = Path.GetFileName(path);
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
            _machine.InsertDisk(0, null);
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

    /// <summary>Feeds one byte straight to the keyboard SIO line, bypassing host-key translation -
    /// for the on-screen keyboard (KayproKeyboardLayoutFactory), whose keys already carry their
    /// resolved byte as their signal.</summary>
    public void SendKeyboardByte(byte value) => _machine.FeedKeyboardByte(value);

    private int _shiftHeld;
    private bool _capsLock;

    /// <summary>On-screen keyboard entry point: resolves raw key signals (see
    /// <see cref="KayproKeyboardLayoutFactory"/>) into bytes for the SIO line. SHIFT (either key)
    /// is a held modifier and CAPS LOCK a press-toggle - while either is active, a-z letters go
    /// out uppercased (digits/symbols pass through untouched: click their stacked shift halves
    /// instead, exactly like the layout's own doc comment describes).</summary>
    public void SendKeyboardSignal(string signal, bool pressed)
    {
        if (signal.Equals(KayproKeyboardLayoutFactory.ShiftSignal, StringComparison.Ordinal))
        {
            if (pressed)
                _shiftHeld++;
            else
                _shiftHeld = Math.Max(0, _shiftHeld - 1);
            return;
        }

        if (signal.Equals(KayproKeyboardLayoutFactory.CapsSignal, StringComparison.Ordinal))
        {
            if (pressed)
            {
                _capsLock = !_capsLock;
                _log.LogInformation("Caps Lock {State}.", _capsLock ? "on" : "off");
            }

            return;
        }

        if (signal.Equals(KayproKeyboardLayoutFactory.UnknownSignal, StringComparison.Ordinal))
            return;

        if (!pressed)
            return;

        if (!KayproKeyboardLayoutFactory.TryParseSignal(signal, out var value))
        {
            _log.LogWarning("Ignoring unknown keyboard signal '{Signal}'.", signal);
            return;
        }

        if ((_shiftHeld > 0 || _capsLock) && value is >= (byte)'a' and <= (byte)'z')
            value = (byte)(value - 0x20);
        _machine.FeedKeyboardByte(value);
    }

    public void Tick()
    {
        try
        {
            _machine.Run(InstructionsPerTick);
            Render();
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
            DiskBusy = DateTime.UtcNow < _diskActivityUntilUtc;

            var registers = ((IDebuggableProcessor)_machine.Processor).GetRegisters();
            StatusFields =
            [
                new("AF", $"0x{registers["AF"]:X4}"), new("BC", $"0x{registers["BC"]:X4}"),
                new("DE", $"0x{registers["DE"]:X4}"), new("HL", $"0x{registers["HL"]:X4}"),
                new("IX", $"0x{registers["IX"]:X4}"), new("IY", $"0x{registers["IY"]:X4}"),
                new("PC", $"0x{registers["PC"]:X4}"), new("SP", $"0x{registers["SP"]:X4}"),
                new("I", $"0x{registers["I"]:X2}"), new("R", $"0x{registers["R"]:X2}"),
                new("IFF1", $"{registers["IFF1"]}"), new("IM", $"{registers["IM"]}"),
                new("Cycles", $"{_machine.CycleCount}"),
                new("Instructions", $"{_machine.Processor.InstructionCount}"),
                new("Disk", DiskLoaded ? "ready" : "none"),
            ];
        }

        FrameReady?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _machine.Bus.FdcWiring.ActivityChanged -= OnFdcActivityChanged;
        _audioOutput.Dispose();
    }

    private void Render() => _machine.Video.Render(FrameBuffer, Font);

    private void OnFdcActivityChanged(object? sender, FD1791ActivityEventArgs e)
    {
        if (e.Kind is FD1791ActivityKind.CommandStarted or
            FD1791ActivityKind.DataRequested or
            FD1791ActivityKind.DataTransferred or
            FD1791ActivityKind.CommandCompleted or
            FD1791ActivityKind.Error)
        {
            _diskActivityUntilUtc = DateTime.UtcNow + DiskActivityLinger;
            DiskBusy = true;
        }
    }

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
