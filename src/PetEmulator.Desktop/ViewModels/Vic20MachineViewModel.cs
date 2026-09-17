using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Desktop.Input;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Audio;
using PetEmulator.Core;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Core.Keyboard;
using PetEmulator.Pet.Tape;
using PetEmulator.Vic20;
using PetEmulator.Vic20.Display;
using PetEmulator.Vic20.Keyboard;
using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>
/// Owns a running <see cref="Vic20Machine"/>, its render loop, and keyboard translation - the
/// VIC-20 implementation of <see cref="IMachineViewModel"/>, mirroring
/// <see cref="PetMachineViewModel"/>'s shape exactly, tape widget included (see
/// <see cref="Vic20Machine.Datasette"/>, added by docs/vic20/tape.md). <see cref="Devices"/>
/// excludes the datasette and primary disk drive (both have dedicated widgets), while any other
/// attached device is exposed through the generic status bar like PET's.
/// </summary>
public sealed partial class Vic20MachineViewModel : ObservableObject, IMachineViewModel, IDatasetteViewModel, IDiskDriveViewModel, ICartridgeViewModel, INewDiskViewModel, INewTapeViewModel
{
    // Same budget as PetMachineViewModel - see that class's identical constant for why.
    private const ulong InstructionsPerTick = 20_000;

    private readonly Vic20Machine _machine;
    private readonly string _romsRoot;
    private readonly Vic20RasterDisplay _display;
    private readonly IAudioOutput _audioOutput;
    private readonly ILogger _log = EmulatorLogging.CreateLogger("VIC20");
    private readonly Vic20KeyboardMap _keyboardMap = new();
    private static readonly TimeSpan DiskActivityLinger = TimeSpan.FromMilliseconds(200);
    private DateTime _diskActivityUntilUtc = DateTime.MinValue;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiskIconBrush))]
    private bool _diskLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiskIconBrush))]
    private bool _diskBusy;

    public IBrush DiskIconBrush => !DiskLoaded ? Brushes.Gray : DiskBusy ? Brushes.Red : Brushes.LimeGreen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CartridgeIconBrush))]
    private bool _cartridgeLoaded;

    [ObservableProperty]
    private string _cartridgeName = "No cartridge";

    [ObservableProperty]
    private bool _canEjectCartridge;

    [ObservableProperty]
    private IReadOnlyList<CartridgeResourceRow> _cartridgeResources = [];

    [ObservableProperty]
    private IReadOnlyList<LoadedCartridgeRow> _loadedCartridges = [];

    public IBrush CartridgeIconBrush => CartridgeLoaded ? Brushes.LimeGreen : Brushes.Gray;

    public Vic20MachineViewModel(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);

        _romsRoot = romsRoot;
        _log.LogInformation("Creating VIC-20 machine with ROMs directory '{RomsRoot}'.", romsRoot);
        try
        {
            _machine = new Vic20Machine(romsRoot, logger: _log);
            _log.LogInformation("Vic20Machine created: {Details}.", Vic20MachineDetails());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to create Vic20Machine from '{RomsRoot}'.", romsRoot);
            throw;
        }

        ProgramProfileSelector = new Vic20ProgramProfileSelectorViewModel();
        _display = new Vic20RasterDisplay(_machine.Memory, _machine.Vic);
        _audioOutput = CreateAudioOutput();
        FrameBuffer = new uint[_display.PixelWidth * _display.PixelHeight];
        UpdateCartridgeState();

        // See PetMachineViewModel's constructor for why this fires here (CS0067 + documents
        // geometry is fixed for this instance's lifetime).
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    public int PixelWidth => _display.PixelWidth;

    public IAudioOutput AudioOutput => _audioOutput;

    public Vic20ProgramProfileSelectorViewModel ProgramProfileSelector { get; }

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

    public void Reset()
    {
        try
        {
            _machine.Reset();
            _log.LogDebug( "Machine reset.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Machine reset failed.");
            throw;
        }
    }

    /// <summary>Loads a VICE-style .tap file into the running machine's datasette - see
    /// <see cref="PetMachineViewModel.LoadTape"/>'s identical doc comment.</summary>
    public void LoadTape(string path)
    {
        try
        {
            _log.LogInformation( $"Loading tape '{path}' ({DescribeFile(path)}).");
            var tap = PetTapFile.Parse(File.ReadAllBytes(path), _log);
            _machine.Datasette.LoadTape(tap.PulseCycles, Path.GetFileName(path));
            _log.LogInformation( $"Tape loaded: '{path}' ({tap.PulseCycles.Count} pulses).");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Failed to load tape '{path}'.");
            throw;
        }
    }

    public void LoadDisk(string path)
    {
        try
        {
            _log.LogInformation( $"Mounting disk '{path}' ({DescribeFile(path)}).");
            _machine.MountDisk(path);
            _log.LogInformation( $"Disk mounted: '{path}'.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Failed to mount disk '{path}'.");
            throw;
        }
    }

    public void LoadCartridge(string path)
    {
        try
        {
            _log.LogInformation( $"Mounting cartridge '{path}' ({DescribeFile(path)}).");
            _machine.MountCartridge(path);
            UpdateCartridgeState();
            _log.LogInformation( $"Cartridge mounted: '{path}'.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Failed to mount cartridge '{path}'.");
            throw;
        }
    }

    public void LoadCartridgePlugin(string pluginPath, string imagePath)
    {
        try
        {
            _log.LogInformation( $"Mounting cartridge plugin '{pluginPath}' with image '{imagePath}' ({DescribeFile(imagePath)}).");
            _machine.MountCartridgePlugin(pluginPath, imagePath);
            UpdateCartridgeState();
            _log.LogInformation( $"Cartridge plugin mounted: '{pluginPath}' + '{imagePath}'.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Failed to mount cartridge plugin '{pluginPath}' with image '{imagePath}'.");
            throw;
        }
    }

    public void LoadProgramProfile(Vic20ProgramProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _log.LogInformation( $"Loading program profile '{profile.Name}' ({profile.Cartridges.Count} cartridges).");
        _machine.EjectCartridge();

        if (profile.IsEmpty)
        {
            UpdateCartridgeState();
            return;
        }

        var cartridgeDirectory = Path.Combine(_romsRoot, "cartridges");
        try
        {
            foreach (var cartridge in profile.Cartridges)
            {
                var imagePath = Path.Combine(cartridgeDirectory, cartridge.FileName);
                if (cartridge.PluginId is { Length: > 0 } pluginId)
                    _machine.MountCartridgePlugin(ResolvePluginPath(pluginId), imagePath);
                else
                    _machine.MountCartridge(imagePath);
            }

            _machine.Reset();
            UpdateCartridgeState();
            _log.LogInformation( $"Program profile '{profile.Name}' loaded and machine reset.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, $"Failed to load program profile '{profile.Name}'.");
            throw;
        }
    }

    [RelayCommand]
    private void EjectCartridge()
    {
        _machine.EjectCartridge();
        UpdateCartridgeState();
    }

    public void EjectCartridge(string path)
    {
        _machine.EjectCartridge(path);
        UpdateCartridgeState();
    }

    public void EjectAllCartridges()
    {
        _machine.EjectCartridge();
        UpdateCartridgeState();
    }

    public IReadOnlyList<CartridgeResourceRow> GetCartridgeResources() => CartridgeResources;

    private void UpdateCartridgeState()
    {
        CartridgeLoaded = _machine.MountedCartridges.Count > 0;
        CartridgeName = _machine.CartridgePath is not null
            ? Path.GetFileName(_machine.CartridgePath)
            : "No cartridge";
        CanEjectCartridge = _machine.Cartridge is not null;
        CartridgeResources = _machine.CartridgeResources
            .Select(resource => new CartridgeResourceRow(
                resource.Name,
                $"${resource.StartAddress:X4}-${resource.EndAddress:X4}",
                resource.Kind.ToString().ToUpperInvariant(),
                resource.Access.ToString()))
            .ToArray();
        LoadedCartridges = _machine.MountedCartridges
            .Select(mounted => new LoadedCartridgeRow(
                Path.GetFileName(mounted.Path),
                mounted.Path,
                mounted.Cartridge.Resources.Select(resource => new CartridgeResourceRow(
                    resource.Name,
                    $"${resource.StartAddress:X4}-${resource.EndAddress:X4}",
                    resource.Kind.ToString().ToUpperInvariant(),
                    resource.Access.ToString())).ToArray()))
            .ToArray();
    }

    private static string ResolvePluginPath(string pluginId) => pluginId switch
    {
        "vic20-ram-3k" => PluginAssembly("PetEmulator.Vic20.Cartridge.Ram.3K.dll"),
        "vic20-ram-8k" => PluginAssembly("PetEmulator.Vic20.Cartridge.Ram.8K.dll"),
        "vic20-ram-16k" => PluginAssembly("PetEmulator.Vic20.Cartridge.Ram.16K.dll"),
        "vic20-ram-24k" => PluginAssembly("PetEmulator.Vic20.Cartridge.Ram.24K.dll"),
        "vic20-ram-35k" => PluginAssembly("PetEmulator.Vic20.Cartridge.Ram.35K.dll"),
        "vic20-mc146818-rtc" => typeof(PetEmulator.Vic20.Cartridge.Rtc.RtcCartridgePlugin).Assembly.Location,
        _ => throw new InvalidOperationException($"Unknown VIC-20 cartridge plugin '{pluginId}'.")
    };

    private static string PluginAssembly(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(path)
            ? path
            : throw new FileNotFoundException($"VIC-20 cartridge plugin '{fileName}' was not deployed.", path);
    }

    public void NewDisk(string path) => _machine.MountNewDisk(path, Path.GetFileNameWithoutExtension(path).ToUpperInvariant());

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
        if (Vic20KeyboardJoystickAdapter.TryApply(
                key, kind == HostKeyEventKind.Press, _machine.Joystick))
        {
            return;
        }

        var atKey = KeyMapping.ToAtKeyboardKey(key);
        var hostKey = atKey is { } physicalKey ? KeyMapping.ToHostKey(physicalKey) : null;
        if (hostKey is not null)
        {
            foreach (var action in _keyboardMap.Translate(hostKey, kind))
                ApplyKeyAction(action);
            return;
        }

        if (atKey is not { } unsupportedHostKey)
            return;

        // KeyMapping intentionally exposes only the common text-key vocabulary. Preserve the
        // VIC-20's existing direct mapping for controls (Escape, function keys, Ctrl, Home, ...).
        var cell = _keyboardMap.Translate(unsupportedHostKey);
        if (cell is { } position)
            ApplyKeyAction(new MatrixAction(position.Row, position.Column, kind == HostKeyEventKind.Press));
    }

    public void SetJoystickInput(Vic20JoystickInput input, bool pressed) =>
        _machine.Joystick.Set(input, pressed);

    private void ApplyKeyAction(MatrixAction action)
    {
        if (action.Pressed)
            _machine.Keyboard.Press(action.Row, action.Column);
        else
            _machine.Keyboard.Release(action.Row, action.Column);
    }

    public void Tick()
    {
        try
        {
            _machine.Run(InstructionsPerTick);
            _display.Tick();
            _display.Render(FrameBuffer);
        }
        catch (Exception ex)
        {
            // The shell timer logs the failure too, but only this frame has the machine
            // context (cycles/instructions) - keep it, then rethrow for the shell handler.
            _log.LogError(ex,
                $"Tick failed at cycles={_machine.Processor.CycleCount} instructions={_machine.Processor.InstructionCount}.");
            throw;
        }

        var regs = ((IDebuggableProcessor)_machine.Processor).GetRegisters();
        StatusText =
            $"PC=0x{regs["PC"]:X4} A=0x{regs["A"]:X2} X=0x{regs["X"]:X2} Y=0x{regs["Y"]:X2} " +
            $"SP=0x{regs["SP"]:X2} P=0x{regs["P"]:X2} " +
            $"Cycles={_machine.Processor.CycleCount} Instructions={_machine.Processor.InstructionCount}";

        Devices = _machine.Devices.Where(d => d.Id is not ("datasette" or "ieee488:8")).ToList();
        DiskLoaded = _machine.HasDisk();
        if (_machine.PollDiskActivity())
            _diskActivityUntilUtc = DateTime.UtcNow + DiskActivityLinger;
        DiskBusy = DateTime.UtcNow < _diskActivityUntilUtc;
        UpdateCartridgeState();
        // TapeName (not HasTape) - a freshly created blank tape has zero pulses but is still "in
        // the deck", same reasoning as Vic20DatasetteStatus's own switch.
        TapeLoaded = _machine.Datasette.TapeName is not null;
        TapePlaying = _machine.Datasette.PlayPressed && _machine.Datasette.MotorOn;

        FrameReady?.Invoke(this, EventArgs.Empty);
    }

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

    /// <summary>Creates the platform audio backend, falling back to silent output when the
    /// OS has no backend yet (Windows/macOS - see <see cref="AudioOutputFactory"/>). The
    /// fallback used to be a <c>PlatformNotSupportedException</c> out of this constructor,
    /// which killed VIC-20 creation on Windows with no error anywhere.</summary>
    private IAudioOutput CreateAudioOutput()
    {
        try
        {
            var output = AudioOutputFactory.CreateDefault();
            output.Start(_machine.Vic);
            _log.LogInformation( $"Audio backend started: {output.GetType().Name}.");
            return output;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                $"No platform audio backend ({ex.GetType().Name}: {ex.Message}); continuing silent. " +
                "Implement a backend in AudioOutputFactory.CreateDefault to enable sound.");
            var silent = AudioOutputFactory.CreateNull();
            silent.Start(_machine.Vic);
            return silent;
        }
    }

    private string Vic20MachineDetails() =>
        $"name={_machine.Name} display={_machine.DisplayConfig.VideoStandard} " +
        $"cartridges={_machine.MountedCartridges.Count}";

    private static string DescribeFile(string path)
    {
        try
        {
            return File.Exists(path) ? $"{new FileInfo(path).Length} B" : "(file does not exist)";
        }
        catch (Exception ex)
        {
            return $"(stat failed: {ex.Message})";
        }
    }
}
