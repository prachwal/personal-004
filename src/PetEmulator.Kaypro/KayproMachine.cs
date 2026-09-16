using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;

namespace PetEmulator.Kaypro;

/// <summary>Kaypro II machine composition using the shared Z80, Core and FD1793 contracts.</summary>
public sealed class KayproMachine : IMachine, IMachineStateStore<KayproSnapshot>
{
    public KayproMachine(IBusCycleObserver? cycleObserver = null)
    {
        Bus = new KayproBus();
        Processor = new Z80Cpu(Bus, Bus.InterruptLines, cycleObserver: cycleObserver);
    }

    public string Name => "Kaypro II";
    public bool IsReady { get; private set; }
    public ulong CycleCount => Processor.CycleCount;
    public IProcessor Processor { get; }
    public IMemoryBus Memory => Bus;
    public KayproBus Bus { get; }
    public KayproVideo Video => Bus.Video;
    public Z80Cpu Cpu => (Z80Cpu)Processor;

    public void LoadMonitorRom(ReadOnlySpan<byte> rom)
    {
        Bus.LoadRom(rom);
        IsReady = true;
    }

    public void InsertDisk(int drive, KayproDiskImage? disk) => Bus.InsertDisk(drive, disk);

    public void FeedKeyboardByte(byte value) => Bus.Sio.EnqueueKeyboardByte(value);

    public void Reset()
    {
        Bus.Reset();
        Processor.Reset();
    }

    public void StepInstruction()
    {
        var before = Processor.CycleCount;
        Processor.StepInstruction();
        Bus.Tick(checked((int)(Processor.CycleCount - before)), Cpu.Halted);
    }

    public void Run(ulong instructionCount)
    {
        for (ulong index = 0; index < instructionCount; index++)
            StepInstruction();
    }

    public KayproSnapshot CaptureState()
    {
        var state = Bus.CaptureState();
        state.Cpu = Cpu.CaptureSnapshot();
        return state;
    }

    public void RestoreState(KayproSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Version != 1)
            throw new InvalidDataException($"Unsupported Kaypro snapshot version {state.Version}.");
        Bus.RestoreState(state);
        Cpu.RestoreSnapshot(state.Cpu);
    }
}
