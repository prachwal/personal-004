using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Audio;
using PetEmulator.Core;
using PetEmulator.Desktop.Input;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Cpc464;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class Cpc464MachineViewModel : ObservableObject, IMachineViewModel, IDatasetteViewModel
{
    private const ulong InstructionsPerTick = 20_000;
    private readonly Cpc464Machine _machine;
    private readonly IAudioOutput _audioOutput;
    private readonly ILogger _log = EmulatorLogging.CreateLogger("CPC464");
    [ObservableProperty] private bool _tapeLoaded;
    [ObservableProperty] private bool _tapePlaying;

    public IBrush TapeIconBrush => !TapeLoaded ? Brushes.Gray : TapePlaying ? Brushes.LimeGreen : Brushes.LightGray;
    [ObservableProperty] private string _statusText = "Amstrad CPC464  Z80  PC=0x0000";
    public Cpc464MachineViewModel(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        var romPath = Path.Combine(romsRoot, "cpc464", "cpc464.rom");
        _log.LogInformation("Creating CPC464 machine from '{Path}'.", romPath);
        try
        {
            _machine = new Cpc464Machine(File.ReadAllBytes(romPath), _log);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to create Cpc464Machine from '{Path}'.", romPath);
            throw;
        }

        _audioOutput = CreateAudioOutput();
        FrameBuffer = new uint[320 * 200];
        Reset();
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }
    public string WindowTitle => "Amstrad CPC464";
    public int PixelWidth => 320;
    public IAudioOutput AudioOutput => _audioOutput;
    public int PixelHeight => 200;
    public (int Width, int Height) PixelAspect => (1, 1);
    public uint[] FrameBuffer { get; }
    public IReadOnlyList<IDeviceStatus> Devices => [];
    public object? Extra => null;
    public event EventHandler? FrameReady;
    public event EventHandler? GeometryChanged;
    public void Tick() { _machine.Run(InstructionsPerTick); Render(); StatusText = $"Amstrad CPC464  Z80  PC=0x{_machine.Cpu.Registers.PC:X4}  Cycles={_machine.CycleCount}"; FrameReady?.Invoke(this, EventArgs.Empty); }
    public void Reset()
    {
        _machine.Reset();
        TapeLoaded = _machine.Cassette.HasTape;
        TapePlaying = false;
        Render();
        FrameReady?.Invoke(this, EventArgs.Empty);
    }
    public void LoadTape(string path)
    {
        try
        {
            _log.LogInformation("Loading tape '{Path}'.", path);
            var cdt = Cpc464CdtImage.Parse(File.ReadAllBytes(path), _log);
            _machine.Bus.Cassette.LoadPulses(cdt.PulseTicks);
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

    [RelayCommand]
    private void PlayTape()
    {
        if (!_machine.Cassette.HasTape)
            return;

        _machine.Bus.Cassette.PressPlay();
        TapePlaying = true;
    }

    [RelayCommand]
    private void StopTape() { _machine.Bus.Cassette.Stop(); TapePlaying = false; }

    [RelayCommand]
    private void EjectTape() { _machine.Bus.Cassette.Eject(); TapeLoaded = false; TapePlaying = false; }
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
    public void HandleKey(Key key, HostKeyEventKind kind) { if (TryMap(key, out var row, out var column)) _machine.Bus.Keyboard.SetKey(row, column, kind == HostKeyEventKind.Press); }
    private void Render()
    {
        var gateArray = _machine.Bus.GateArray;
        gateArray.RenderFrame();
        var pixels = gateArray.Pixels;
        for (var i = 0; i < FrameBuffer.Length; i++)
            FrameBuffer[i] = PetEmulator.Cpc.CpcGateArray.HardwareColors[gateArray.GetInkColorIndex(pixels[i])];
    }
    /// <summary>Real Amstrad CPC 10x8 keyboard matrix positions - NOT a linear A-Z formula (the
    /// physical PCB scan lines interleave letters, digits and punctuation with no alphabetic
    /// order; a prior "0x85 + (key - A)" shortcut here produced garbage for every letter past A).
    /// Verified against real firmware behaviour, not just a reference key-map label: digits and
    /// letters were confirmed by decoding the actual rendered glyphs after boot, and '+' was found
    /// by an exhaustive matrix sweep against a "PRINT 1&lt;key&gt;1" oracle - the bare key at (3,4)
    /// renders something else; only Shift+(3,4) parses as addition (see
    /// Cpc464BootTests.RealFirmwareBootsAndExecutesPrintOnePlusOne's own doc comment). The host's
    /// own Shift key (already mapped below) supplies that modifier naturally when the operator
    /// actually presses Shift+= for '+'. <see cref="Key.OemQuotes"/> at (8,1) - the same cell as
    /// D2, again relying on the host's own Shift for the quote glyph - was confirmed the same way:
    /// a fresh-boot "PRINT &quot;A&quot;" typed through this exact matrix position rendered a bare
    /// 'A' (not a syntax error) and "Ready" reappeared, ruling out the earlier "glyph not
    /// conclusive" hedge.</summary>
    private static readonly Dictionary<Key, (byte Row, byte Column)> Matrix = new()
    {
        [Key.Up] = (0, 0), [Key.Right] = (0, 1), [Key.Down] = (0, 2),
        [Key.Left] = (1, 0),
        [Key.Return] = (2, 2), [Key.Enter] = (2, 2), [Key.LeftShift] = (2, 5), [Key.RightShift] = (2, 5),
        [Key.OemMinus] = (3, 1), [Key.P] = (3, 3), [Key.OemPlus] = (3, 4), [Key.OemQuestion] = (3, 6), [Key.OemComma] = (3, 7),
        [Key.OemSemicolon] = (3, 4), [Key.Multiply] = (3, 5), [Key.OemOpenBrackets] = (5, 0), [Key.OemCloseBrackets] = (4, 1), [Key.OemQuotes] = (8, 1),
        [Key.D0] = (4, 0), [Key.D9] = (4, 1), [Key.O] = (4, 2), [Key.I] = (4, 3), [Key.L] = (4, 4), [Key.K] = (4, 5), [Key.M] = (4, 6), [Key.OemPeriod] = (4, 7),
        [Key.D8] = (5, 0), [Key.D7] = (5, 1), [Key.U] = (5, 2), [Key.Y] = (5, 3), [Key.H] = (5, 4), [Key.J] = (5, 5), [Key.N] = (5, 6), [Key.Space] = (5, 7),
        [Key.D6] = (6, 0), [Key.D5] = (6, 1), [Key.R] = (6, 2), [Key.T] = (6, 3), [Key.G] = (6, 4), [Key.F] = (6, 5), [Key.B] = (6, 6), [Key.V] = (6, 7),
        [Key.D4] = (7, 0), [Key.D3] = (7, 1), [Key.E] = (7, 2), [Key.W] = (7, 3), [Key.S] = (7, 4), [Key.D] = (7, 5), [Key.C] = (7, 6), [Key.X] = (7, 7),
        [Key.D1] = (8, 0), [Key.D2] = (8, 1), [Key.Escape] = (8, 2), [Key.Q] = (8, 3), [Key.Tab] = (8, 4), [Key.A] = (8, 5), [Key.CapsLock] = (8, 6), [Key.Z] = (8, 7),
        [Key.Back] = (9, 7), [Key.Delete] = (9, 7),
    };

    private static bool TryMap(Key key, out byte row, out byte column)
    {
        if (Matrix.TryGetValue(key, out var cell)) { row = cell.Row; column = cell.Column; return true; }
        row = column = 0;
        return false;
    }

    /// <summary>Platform audio backend with silent fallback - see
    /// <see cref="Vic20MachineViewModel"/> for why the fallback exists.</summary>
    private IAudioOutput CreateAudioOutput()
    {
        try
        {
            var output = AudioOutputFactory.CreateDefault();
            output.Start(_machine.Bus.Ay);
            _log.LogInformation("Audio backend started: {Backend}.", output.GetType().Name);
            return output;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "No platform audio backend ({ErrorType}: {Message}); continuing silent.", ex.GetType().Name, ex.Message);
            var silent = AudioOutputFactory.CreateNull();
            silent.Start(_machine.Bus.Ay);
            return silent;
        }
    }
}
