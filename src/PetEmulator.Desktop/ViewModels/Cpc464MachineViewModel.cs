using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using PetEmulator.Core;
using PetEmulator.Desktop.Input;
using PetEmulator.Cpc464;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class Cpc464MachineViewModel : ObservableObject, IMachineViewModel
{
    private const ulong InstructionsPerTick = 20_000;
    private readonly Cpc464Machine _machine;
    private static readonly uint[] Palette = [0xFF000000, 0xFFFFFFFF, 0xFF0000AA, 0xFFAA0000];
    [ObservableProperty] private string _statusText = "Amstrad CPC464  Z80  PC=0x0000";
    public Cpc464MachineViewModel(string romsRoot)
    {
        var romPath = Path.Combine(romsRoot, "cpc464", "cpc464.rom");
        _machine = new Cpc464Machine(File.ReadAllBytes(romPath));
        FrameBuffer = new uint[320 * 200];
        Reset();
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }
    public string WindowTitle => "Amstrad CPC464";
    public int PixelWidth => 320;
    public int PixelHeight => 200;
    public (int Width, int Height) PixelAspect => (1, 1);
    public uint[] FrameBuffer { get; }
    public IReadOnlyList<IDeviceStatus> Devices => [];
    public object? Extra => null;
    public event EventHandler? FrameReady;
    public event EventHandler? GeometryChanged;
    public void Tick() { _machine.Run(InstructionsPerTick); Render(); StatusText = $"Amstrad CPC464  Z80  PC=0x{_machine.Cpu.Registers.PC:X4}  Cycles={_machine.CycleCount}"; FrameReady?.Invoke(this, EventArgs.Empty); }
    public void Reset() { _machine.Reset(); Render(); FrameReady?.Invoke(this, EventArgs.Empty); }
    public void Dispose() { }
    public void HandleKey(Key key, HostKeyEventKind kind) { if (TryMap(key, out var row, out var column)) _machine.Bus.Keyboard.SetKey(row, column, kind == HostKeyEventKind.Press); }
    private void Render() { _machine.Bus.GateArray.RenderFrame(); var pixels = _machine.Bus.GateArray.Pixels; for (var i = 0; i < FrameBuffer.Length; i++) FrameBuffer[i] = Palette[pixels[i] & 3]; }
    private static bool TryMap(Key key, out byte row, out byte column)
    {
        var code = key is >= Key.A and <= Key.Z ? 0x85 + (key - Key.A) : key switch { Key.Space => 0x57, Key.Enter or Key.Return => 0x06, Key.LeftShift or Key.RightShift => 0x25, Key.Up => 0x00, Key.Down => 0x02, Key.Left => 0x10, Key.Right => 0x01, _ => -1 };
        row = column = 0; if (code < 0) return false; row = (byte)(code >> 4); column = (byte)(code & 7); return true;
    }
}
