using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Core;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Vic20;
using PetEmulator.Vic20.Display;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Desktop;

/// <summary>
/// Owns a running <see cref="Vic20Machine"/>, its render loop, and keyboard translation - the
/// VIC-20 implementation of <see cref="IMachineViewModel"/>, mirroring
/// <see cref="PetMachineViewModel"/>'s shape exactly. No devices modeled yet (v1 scope cut - see
/// docs/vic20-migration-plan.md), so <see cref="Devices"/> is always empty.
/// </summary>
public sealed partial class Vic20MachineViewModel : ObservableObject, IMachineViewModel
{
    // Same budget as PetMachineViewModel - see that class's identical constant for why.
    private const ulong InstructionsPerTick = 20_000;

    private readonly Vic20Machine _machine;
    private readonly Vic20RasterDisplay _display;
    private readonly Vic20KeyboardMap _keyboardMap = new();

    [ObservableProperty]
    private string _windowTitle = "VIC-20 Emulator";

    [ObservableProperty]
    private string _statusText = "PC=0x0000 A=0x00 X=0x00 Y=0x00 SP=0x00 P=0x00 Cycles=0 Instructions=0";

    public Vic20MachineViewModel(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);

        _machine = new Vic20Machine(romsRoot);
        _display = new Vic20RasterDisplay(_machine.Memory, _machine.Vic);
        FrameBuffer = new uint[_display.PixelWidth * _display.PixelHeight];

        // See PetMachineViewModel's constructor for why this fires here (CS0067 + documents
        // geometry is fixed for this instance's lifetime).
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    public int PixelWidth => _display.PixelWidth;

    public int PixelHeight => _display.PixelHeight;

    /// <summary>Square pixels - the VIC-I doesn't have PET's non-square pixel-aspect quirk (or
    /// this port doesn't model it yet; real VIC-20 NTSC pixels are close enough to square that
    /// getting this wrong wouldn't be obviously visible - a documented simplification, not a
    /// verified fact).</summary>
    public (int Width, int Height) PixelAspect => (1, 1);

    public event EventHandler? FrameReady;

    public event EventHandler? GeometryChanged;

    public object? Extra => null;

    /// <summary>Always empty - no devices modeled for VIC-20 yet (v1 scope cut).</summary>
    public IReadOnlyList<IDeviceStatus> Devices => [];

    public uint[] FrameBuffer { get; }

    public void Reset() => _machine.Reset();

    public void HandleKey(Key key, HostKeyEventKind kind)
    {
        var hostKey = KeyMapping.ToHostKey(key);
        if (hostKey is null)
            return;

        var cell = _keyboardMap.Translate(hostKey);
        if (cell is not { } c)
            return;

        if (kind == HostKeyEventKind.Press)
            _machine.Keyboard.Press(c.Row, c.Col);
        else
            _machine.Keyboard.Release(c.Row, c.Col);
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

        FrameReady?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() { }
}
