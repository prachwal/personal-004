using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Core;
using PetEmulator.Pet;
using PetEmulator.Pet.Devices;
using PetEmulator.Pet.Display;
using PetEmulator.Pet.Fonts;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Desktop;

/// <summary>
/// Owns the running <see cref="PetMachine"/>, its render loop, and keyboard translation - the
/// View (<see cref="MainWindow"/>) only binds to this and forwards Avalonia-specific input
/// events/frame uploads it can't otherwise express as a binding (raw pixel buffer, Key enum).
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    // ponytail: fixed per-tick instruction budget, no adaptive pacing to a wall-clock cycle
    // rate. Good enough for a display GUI; revisit if playback speed needs to match real hardware.
    private const ulong InstructionsPerTick = 20_000;

    private readonly string _romsRoot;
    private readonly DispatcherTimer _timer;
    private PetMachine _machine = null!;
    private PetRasterDisplay _display = null!;
    private IPetKeyboardMap _keyboardMap = null!;
    private PetProfile _currentProfile = null!;

    [ObservableProperty]
    private string _windowTitle = "PET Emulator";

    [ObservableProperty]
    private string _statusText = "PC=0x0000 A=0x00 X=0x00 Y=0x00 SP=0x00 P=0x00 Cycles=0 Instructions=0";

    /// <summary>Refreshed every <see cref="Tick"/> from <see cref="PetMachine.Devices"/> - the
    /// status bar's ItemsControl binds directly to this, so a device attached/replaced mid-session
    /// (a tape loaded, a disk swapped) shows up within one tick with no extra event wiring.</summary>
    [ObservableProperty]
    private IReadOnlyList<IPetDeviceStatus> _devices = [];

    public MainWindowViewModel()
    {
        _romsRoot = RomsRootLocator.Find();
        LoadProfile(PetProfileCatalog.Pet2001_32);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    /// <summary>Every selectable machine configuration, for the Machine menu.</summary>
    public IReadOnlyList<PetProfile> Profiles => PetProfileCatalog.All;

    public int PixelWidth => _display.PixelWidth;

    public int PixelHeight => _display.PixelHeight;

    /// <summary>Physical width:height ratio of one source pixel - see <see cref="PetProfile.PixelAspect"/>.</summary>
    public (int Width, int Height) PixelAspect => _currentProfile.PixelAspect;

    /// <summary>Raised after each render tick, once <see cref="FrameBuffer"/> holds the new frame -
    /// the View reacts by pushing it into its screen control (not a bindable property: mutating
    /// a uint[] in place wouldn't raise per-element change notifications anyway).</summary>
    public event EventHandler? FrameReady;

    /// <summary>Raised when a profile switch changes the native resolution - the View reacts by
    /// resizing its screen control before the next <see cref="FrameReady"/>.</summary>
    public event EventHandler? GeometryChanged;

    /// <summary>Raised by <see cref="ExitCommand"/> - the View closes the window.</summary>
    public event EventHandler? CloseRequested;

    public uint[] FrameBuffer { get; private set; } = [];

    [RelayCommand]
    private void Reset() => _machine.Reset();

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void LoadProfile(PetProfile profile)
    {
        var font = new PetCharacterRomLoader().Load(
            Path.Combine(_romsRoot, profile.RomDirectory, profile.CharacterRomPath));

        _currentProfile = profile;
        _machine = new PetMachine(profile, _romsRoot);
        _display = new PetRasterDisplay(profile, _machine.Memory, font, _machine.Crtc);
        FrameBuffer = new uint[_display.PixelWidth * _display.PixelHeight];

        _keyboardMap = profile.BasicVersion != "BASIC 4"
            ? new Pet2001GraphicsKeyboardMap()
            : profile.Id == PetProfileCatalog.Cbm8032.Id
                ? new Cbm8032KeyboardMap()
                : new Cbm4032KeyboardMap();

        WindowTitle = $"PET Emulator — {profile.Name}";
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

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

    /// <summary>Translates one Avalonia key event into matrix presses/releases on the running
    /// machine's keyboard. The View owns the Avalonia <see cref="Key"/> -&gt; host-key-string
    /// mapping (<see cref="KeyMapping"/>) since that's purely an Avalonia input concern.</summary>
    public void HandleKey(Key key, HostKeyEventKind kind)
    {
        var hostKey = KeyMapping.ToHostKey(key);
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

    private void Tick()
    {
        _machine.Run(InstructionsPerTick);
        _display.Tick();
        _display.Render(FrameBuffer);

        var regs = ((IDebuggableProcessor)_machine.Processor).GetRegisters();
        StatusText =
            $"PC=0x{regs["PC"]:X4} A=0x{regs["A"]:X2} X=0x{regs["X"]:X2} Y=0x{regs["Y"]:X2} " +
            $"SP=0x{regs["SP"]:X2} P=0x{regs["P"]:X2} " +
            $"Cycles={_machine.Processor.CycleCount} Instructions={_machine.Processor.InstructionCount}";

        Devices = _machine.Devices;
        FrameReady?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => _timer.Stop();
}
