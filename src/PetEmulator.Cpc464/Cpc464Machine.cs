using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;

namespace PetEmulator.Cpc464;

public sealed class Cpc464Machine : IMachine
{
    private readonly Z80Cpu _cpu;
    public Cpc464Machine(ReadOnlySpan<byte> rom)
    {
        Bus = new Cpc464Bus(rom);
        _cpu = new Z80Cpu(Bus, Bus.InterruptLines);
        Processor = _cpu;
        IsReady = true;
        Reset();
    }
    public string Name => "Amstrad CPC464";
    public bool IsReady { get; }
    public ulong CycleCount => _cpu.CycleCount;
    public IProcessor Processor { get; }
    public IMemoryBus Memory => Bus;
    public Cpc464Bus Bus { get; }
    public Z80Cpu Cpu => _cpu;
    public Cpc464Cassette Cassette => Bus.Cassette;
    public int PixelWidth => Bus.GateArray.Width;
    public int PixelHeight => Bus.GateArray.Height;

    public Cpc464Snapshot CaptureState() => new()
    {
        Cpu = _cpu.CaptureSnapshot(), Memory = Bus.CaptureMemoryState(), Ports = Bus.CapturePortsState(),
        GateArray = Bus.GateArray.CaptureState(), Crtc = Bus.Crtc.CaptureState(), Ay = Bus.Ay.CaptureState(),
        Keyboard = Bus.Keyboard.CaptureState(), Cassette = Bus.Cassette.CaptureState()
    };

    public void RestoreState(Cpc464Snapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Version != 1) throw new InvalidDataException($"Unsupported CPC464 snapshot version {state.Version}.");
        Bus.RestoreMemoryState(state.Memory); _cpu.RestoreSnapshot(state.Cpu); Bus.GateArray.RestoreState(state.GateArray);
        Bus.Crtc.RestoreState(state.Crtc); Bus.Ay.RestoreState(state.Ay); Bus.Keyboard.RestoreState(state.Keyboard);
        Bus.Cassette.RestoreState(state.Cassette); Bus.RestorePortsState(state.Ports);
    }
    public void Reset() { Bus.Reset(); _cpu.Reset(); Bus.GateArray.RenderFrame(); }
    public void StepInstruction() { var before = _cpu.CycleCount; _cpu.StepInstruction(); Bus.Tick(checked((int)(_cpu.CycleCount - before))); }
    public void Run(ulong instructionCount) { for (ulong i = 0; i < instructionCount; i++) StepInstruction(); }
}
