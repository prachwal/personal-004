using PetEmulator.Core;
using PetEmulator.CpuZ80.Cpu;

namespace PetEmulator.Kaypro;

/// <summary>Kaypro II machine composition using the shared Z80, Core and FD1793 contracts.</summary>
public sealed class KayproMachine : IMachine
{
    public KayproMachine()
    {
        Bus = new KayproBus();
        Processor = new Z80Cpu(Bus, Bus.InterruptLines);
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
        Bus.Tick(checked((int)(Processor.CycleCount - before)));
    }

    public void Run(ulong instructionCount)
    {
        for (ulong index = 0; index < instructionCount; index++)
            StepInstruction();
    }
}
