using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;

namespace PetEmulator.Kaypro;

/// <summary>Kaypro II machine composition using the shared Z80, Core and FD1793 contracts.</summary>
public sealed class KayproMachine : IMachine, IMachineStateStore<KayproSnapshot>
{
    private readonly ILogger _log;

    public KayproMachine(IBusCycleObserver? cycleObserver = null, ILogger? logger = null)
    {
        _log = logger ?? NullLogger.Instance;
        _log.LogInformation("Constructing KayproMachine.");
        Bus = new KayproBus();
        Processor = new Z80Cpu(Bus, Bus.InterruptLines, cycleObserver: cycleObserver);
        _log.LogInformation("KayproMachine constructed (monitor ROM still required via LoadMonitorRom).");
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
        _log.LogInformation("Loading monitor ROM ({Size} B).", rom.Length);
        Bus.LoadRom(rom);
        IsReady = true;
    }

    public void InsertDisk(int drive, KayproDiskImage? disk)
    {
        _log.LogInformation("InsertDisk drive={Drive} disk={Disk}.", drive, disk is null ? "ejected" : $"{KayproDiskImage.ImageSize} B");
        Bus.InsertDisk(drive, disk);
    }

    public void FeedKeyboardByte(byte value) => Bus.Sio.EnqueueKeyboardByte(value);

    public void Reset()
    {
        _log.LogDebug("Reset.");
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
