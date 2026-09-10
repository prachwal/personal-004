using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Desktop.Input;
using PetEmulator.Audio;
using PetEmulator.Core;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Core.Keyboard;
using PetEmulator.Pet.Tape;
using PetEmulator.Vic20;
using PetEmulator.Vic20.Display;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>
/// Owns a running <see cref="Vic20Machine"/>, its render loop, and keyboard translation - the
/// VIC-20 implementation of <see cref="IMachineViewModel"/>, mirroring
/// <see cref="PetMachineViewModel"/>'s shape exactly, tape widget included (see
/// <see cref="Vic20Machine.Datasette"/>, added by docs/vic20-tape.md). <see cref="Devices"/>
/// excludes the datasette itself (own dedicated widget) the same way PetMachineViewModel's does;
/// nothing else is modeled yet, so today it only ever yields the datasette's own entry filtered
/// back out - effectively always empty, but wired generically like PET's for whatever's next.
/// </summary>
public sealed partial class Vic20MachineViewModel : ObservableObject, IMachineViewModel, IDatasetteViewModel
{
    // Same budget as PetMachineViewModel - see that class's identical constant for why.
    private const ulong InstructionsPerTick = 20_000;

    private readonly Vic20Machine _machine;
    private readonly Vic20RasterDisplay _display;
    private readonly IAudioOutput _audioOutput;
    private readonly Vic20KeyboardMap _keyboardMap = new();

    [ObservableProperty]
    private string _windowTitle = "VIC-20 Emulator";

    [ObservableProperty]
    private string _statusText = "PC=0x0000 A=0x00 X=0x00 Y=0x00 SP=0x00 P=0x00 Cycles=0 Instructions=0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TapeIconBrush))]
    private bool _tapeLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TapeIconBrush))]
    private bool _tapePlaying;

    /// <summary>Same feedback loop as PetMachineViewModel.TapeIconBrush - see that property's doc
    /// comment.</summary>
    public IBrush TapeIconBrush => !TapeLoaded ? Brushes.Gray : TapePlaying ? Brushes.LimeGreen : Brushes.LightGray;

    public Vic20MachineViewModel(
        string romsRoot,
        Vic20ExpansionPreset expansionPreset = Vic20ExpansionPreset.Unexpanded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);

        _machine = new Vic20Machine(romsRoot, expansionPreset: expansionPreset);
        _display = new Vic20RasterDisplay(_machine.Memory, _machine.Vic);
        _audioOutput = AudioOutputFactory.CreateDefault();
        _audioOutput.Start(_machine.Vic);
        FrameBuffer = new uint[_display.PixelWidth * _display.PixelHeight];

        // See PetMachineViewModel's constructor for why this fires here (CS0067 + documents
        // geometry is fixed for this instance's lifetime).
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    public int PixelWidth => _display.PixelWidth;

    public int PixelHeight => _display.PixelHeight;

    public (int Width, int Height) PixelAspect => (_machine.DisplayConfig.PixelAspectWidth, _machine.DisplayConfig.PixelAspectHeight);

    public event EventHandler? FrameReady;

    public event EventHandler? GeometryChanged;

    public object? Extra => null;

    /// <summary>Refreshed every <see cref="Tick"/> from <see cref="Vic20Machine.Devices"/>, minus
    /// the datasette - see this class's own doc comment.</summary>
    [ObservableProperty]
    private IReadOnlyList<IDeviceStatus> _devices = [];

    public uint[] FrameBuffer { get; }

    public void Reset() => _machine.Reset();

    /// <summary>Loads a VICE-style .tap file into the running machine's datasette - see
    /// <see cref="PetMachineViewModel.LoadTape"/>'s identical doc comment.</summary>
    public void LoadTape(string path)
    {
        var tap = PetTapFile.Parse(File.ReadAllBytes(path));
        _machine.Datasette.LoadTape(tap.PulseCycles, Path.GetFileName(path));
    }

    [RelayCommand]
    private void PlayTape() => _machine.Datasette.PressPlay();

    [RelayCommand]
    private void StopTape() => _machine.Datasette.Stop();

    [RelayCommand]
    private void EjectTape() => _machine.Datasette.Eject();

    /// <summary>Puts a fresh, empty, writable tape in the datasette - ready for a real SAVE (see
    /// <see cref="Vic20Machine"/>'s doc comment), then LOAD straight back. Real deck: the user
    /// still needs to press play once - <see cref="PlayTape"/> - before typing SAVE.</summary>
    public void NewTape() => _machine.Datasette.NewBlankTape("New Tape");

    public void HandleKey(Key key, HostKeyEventKind kind)
    {
        var atKey = KeyMapping.ToAtKeyboardKey(key);
        if (atKey is not { } physicalKey)
            return;

        var cell = _keyboardMap.Translate(physicalKey);
        if (cell is not { } c)
            return;

        if (kind == HostKeyEventKind.Press)
            _machine.Keyboard.Press(c.Row, c.Column);
        else
            _machine.Keyboard.Release(c.Row, c.Column);
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

        Devices = _machine.Devices.Where(d => d.Id is not "datasette").ToList();
        // TapeName (not HasTape) - a freshly created blank tape has zero pulses but is still "in
        // the deck", same reasoning as Vic20DatasetteStatus's own switch.
        TapeLoaded = _machine.Datasette.TapeName is not null;
        TapePlaying = _machine.Datasette.PlayPressed && _machine.Datasette.MotorOn;

        FrameReady?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => _audioOutput.Dispose();
}
