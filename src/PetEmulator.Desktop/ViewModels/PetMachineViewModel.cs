using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Desktop.Input;
using PetEmulator.Core;
using PetEmulator.Core.Keyboard;
using PetEmulator.Pet;
using PetEmulator.Pet.Display;
using PetEmulator.Pet.Fonts;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>
/// Owns a running <see cref="PetMachine"/>, its render loop, and keyboard translation - the
/// PET implementation of <see cref="IMachineViewModel"/>. One instance per profile: switching
/// profile (<see cref="MainWindowViewModel"/>'s Machine menu) constructs a new instance rather
/// than mutating this one in place, so <see cref="MainWindowViewModel.CurrentModule"/> swaps
/// wholesale and any stale event subscription dies with the old instance.
/// </summary>
public sealed partial class PetMachineViewModel : ObservableObject, IMachineViewModel, IDatasetteViewModel, IDiskDriveViewModel
{
    // ponytail: fixed per-tick instruction budget, no adaptive pacing to a wall-clock cycle
    // rate. Good enough for a display GUI; revisit if playback speed needs to match real hardware.
    private const ulong InstructionsPerTick = 20_000;

    // How long the disk-activity LED stays visibly red after a byte transfer - PollDiskActivity
    // is checked once per (20ms) tick, and gaps between individual bus bytes mid-transfer would
    // otherwise make it flicker frame-to-frame instead of reading as a steady blink.
    private static readonly TimeSpan DiskActivityLinger = TimeSpan.FromMilliseconds(200);

    private readonly PetMachine _machine;
    private readonly PetRasterDisplay _display;
    private readonly IPetKeyboardMap _keyboardMap;
    private readonly PetProfile _profile;
    private DateTime _diskActivityUntilUtc = DateTime.MinValue;

    [ObservableProperty]
    private string _windowTitle;

    [ObservableProperty]
    private string _statusText = "PC=0x0000 A=0x00 X=0x00 Y=0x00 SP=0x00 P=0x00 Cycles=0 Instructions=0";

    /// <summary>Refreshed every <see cref="Tick"/> from <see cref="PetMachine.Devices"/>, minus
    /// the datasette and the primary (device 8) disk drive - both get their own dedicated icon
    /// widget (<see cref="TapeIconBrush"/>/<see cref="PlayTapeCommand"/>,
    /// <see cref="DiskIconBrush"/>) instead of a generic read-only status chip. The status bar's
    /// ItemsControl binds directly to this, so any OTHER device attached/replaced mid-session (a
    /// second disk drive at a different device number) shows up within one tick with no extra
    /// event wiring.</summary>
    [ObservableProperty]
    private IReadOnlyList<IDeviceStatus> _devices = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TapeIconBrush))]
    private bool _tapeLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TapeIconBrush))]
    private bool _tapePlaying;

    /// <summary>Drives the tape icon's color: gray with nothing loaded, green once PLAY is
    /// actually engaged (see <see cref="PetDatasette.PlayPressed"/>), the default foreground
    /// otherwise (loaded but stopped) - the real feedback loop this widget exists for, since
    /// without it a real PET sits stuck forever on "PRESS PLAY ON TAPE #1" with no way to
    /// answer it.</summary>
    public IBrush TapeIconBrush => !TapeLoaded ? Brushes.Gray : TapePlaying ? Brushes.LimeGreen : Brushes.LightGray;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiskIconBrush))]
    private bool _diskLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiskIconBrush))]
    private bool _diskBusy;

    /// <summary>Drives the disk icon's color: gray with nothing mounted, green once a disk is
    /// mounted, briefly red (see <see cref="DiskActivityLinger"/>) while a real byte is crossing
    /// the IEEE-488 bus - see <see cref="PetMachine.PollDiskActivity"/>.</summary>
    public IBrush DiskIconBrush => !DiskLoaded ? Brushes.Gray : DiskBusy ? Brushes.Red : Brushes.LimeGreen;

    public PetMachineViewModel(PetProfile profile, string romsRoot)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);

        _profile = profile;
        var font = new PetCharacterRomLoader().Load(
            Path.Combine(romsRoot, profile.RomDirectory, profile.CharacterRomPath));

        _machine = new PetMachine(profile, romsRoot);
        _display = new PetRasterDisplay(profile, _machine.Memory, font);
        FrameBuffer = new uint[_display.PixelWidth * _display.PixelHeight];

        _keyboardMap = profile.KeyboardLayout switch
        {
            PetKeyboardLayout.Pet2001Graphics => new Pet2001GraphicsKeyboardMap(),
            PetKeyboardLayout.Cbm4032 => new Cbm4032KeyboardMap(),
            PetKeyboardLayout.Cbm8032 => new Cbm8032KeyboardMap(),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile.KeyboardLayout, null)
        };

        _windowTitle = $"PET Emulator — {profile.Name}";

        // Nobody has subscribed yet at construction time (this instance doesn't exist for a View
        // to bind to until the constructor returns) - this exists only to give GeometryChanged a
        // real invocation site (CS0067 otherwise) and to document that geometry is fixed for a
        // PET's lifetime: the View establishes initial sizing unconditionally on DataContext
        // change instead of waiting for this event (see PetMachineView.axaml.cs).
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    public int PixelWidth => _display.PixelWidth;

    public int PixelHeight => _display.PixelHeight;

    public (int Width, int Height) PixelAspect => _profile.PixelAspect;

    public event EventHandler? FrameReady;

    /// <summary>Never actually raised - a PET's resolution is fixed for the profile's lifetime
    /// (unlike VIC-20's chip-register-driven geometry). Present only to satisfy
    /// <see cref="IMachineViewModel"/>; a fresh instance per profile switch already gets its own
    /// initial <c>SetScreenSize</c> call from <see cref="MainWindow"/>.</summary>
    public event EventHandler? GeometryChanged;

    public object? Extra => null;

    public uint[] FrameBuffer { get; }

    public void Reset() => _machine.Reset();

    /// <summary>Loads a VICE-style .tap file into the running machine's datasette - the file-picker
    /// dialog itself is Avalonia-specific glue that lives in <see cref="MainWindow"/>'s code-behind
    /// (needs a <c>TopLevel</c>), which calls straight through to this.</summary>
    public void LoadTape(string path)
    {
        var tap = PetTapFile.Parse(File.ReadAllBytes(path));
        _machine.Datasette.LoadTape(tap.PulseCycles, Path.GetFileName(path));
    }

    /// <inheritdoc cref="LoadTape"/>
    public void LoadDisk(string path) => _machine.MountDisk(path);

    /// <summary>Creates a fresh, formatted, writable D64 at <paramref name="path"/> and mounts it
    /// - see <see cref="PetMachine.MountNewDisk"/>. Unlike <see cref="LoadTape"/>/
    /// <see cref="LoadDisk"/> this WRITES the file (a real image needs bytes on disk before
    /// <c>MountDisk</c> can read it back), so <see cref="MainWindow"/>'s code-behind uses a save,
    /// not an open, file picker for this one.</summary>
    public void NewDisk(string path) => _machine.MountNewDisk(path, Path.GetFileNameWithoutExtension(path).ToUpperInvariant());

    [RelayCommand]
    private void PlayTape() => _machine.Datasette.PressPlay();

    [RelayCommand]
    private void StopTape() => _machine.Datasette.Stop();

    [RelayCommand]
    private void EjectTape() => _machine.Datasette.Eject();

    public void HandleKey(Key key, HostKeyEventKind kind)
    {
        var atKey = KeyMapping.ToAtKeyboardKey(key);
        var hostKey = atKey is { } physicalKey ? KeyMapping.ToHostKey(physicalKey) : null;
        if (hostKey is null)
            return;

        foreach (var action in _keyboardMap.Translate(hostKey, kind))
        {
            if (action.Pressed)
                _machine.Keyboard.Press(action.Row, action.Column);
            else
                _machine.Keyboard.Release(action.Row, action.Column);
        }
    }

    public void Tick()
    {
        _machine.Run(InstructionsPerTick);
        _display.Tick();
        _display.Render(FrameBuffer);

        var regs = ((IDebuggableProcessor)_machine.Processor).GetRegisters();
        StatusText =
            $"PC=0x{regs["PC"]:X4} A=0x{regs["A"]:X2} X=0x{regs["X"]:X2} Y=0x{regs["Y"]:X2} " +
            $"SP=0x{regs["SP"]:X2} P=0x{regs["P"]:X2} " +
            $"Cycles={_machine.Processor.CycleCount} Instructions={_machine.Processor.InstructionCount}";

        Devices = _machine.Devices.Where(d => d.Id is not ("datasette" or "ieee488:8")).ToList();
        TapeLoaded = _machine.Datasette.HasTape;
        TapePlaying = _machine.Datasette.PlayPressed && _machine.Datasette.MotorOn;

        DiskLoaded = _machine.HasDisk();
        if (_machine.PollDiskActivity())
            _diskActivityUntilUtc = DateTime.UtcNow + DiskActivityLinger;
        DiskBusy = DateTime.UtcNow < _diskActivityUntilUtc;

        FrameReady?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() { }
}
