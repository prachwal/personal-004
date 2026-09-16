using PetEmulator.Core;
using PetEmulator.Chips;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.Pet.Fonts;
using PetEmulator.CpuZ80.Interrupts;
using PetEmulator.Trs80.Display;

namespace PetEmulator.Trs80;

public sealed class Trs80Machine : IMachine, IMachineStateStore<Trs80Snapshot>
{
    public const int TStatesPerSecond = 1_774_000;
    public Trs80Machine(ReadOnlySpan<byte> rom, Jv1DiskImage? disk = null, byte[]? tape = null, IGlyphFont? font = null, IBusCycleObserver? cycleObserver = null)
        : this(rom, disk is null ? null : new Trs80DiskImageAdapter(disk), tape, font, cycleObserver) { }

    public Trs80Machine(ReadOnlySpan<byte> rom, IFD1791DiskImage? disk, byte[]? tape = null, IGlyphFont? font = null, IBusCycleObserver? cycleObserver = null)
    {
        Keyboard = new Trs80KeyboardMatrix();
        Printer = new Trs80Printer();
        Fdc = disk is null ? null : new Trs80FdcWiring(disk);
        Cassette = tape is null ? null : new Trs80CassettePlayer(tape);
        Bus = new Trs80MemoryBus(Keyboard, Printer, Fdc, Cassette);
        Bus.LoadRom(rom);
        InterruptLines = new InterruptLines();
        Cpu = new Z80Cpu(Bus, InterruptLines, cycleObserver: cycleObserver);
        Processor = Cpu;
        Video = new Trs80RasterDisplay(Bus, font ?? new Trs80CharacterFont(new byte[2048]));
        IsReady = true;
    }
    public string Name => "TRS-80 Model I";
    public bool IsReady { get; }
    public ulong CycleCount => Cpu.CycleCount;
    public IProcessor Processor { get; }
    public IMemoryBus Memory => Bus;
    public Trs80MemoryBus Bus { get; }
    public Trs80KeyboardMatrix Keyboard { get; }
    public Trs80Printer Printer { get; }
    public Trs80FdcWiring? Fdc { get; private set; }
    public Trs80CassettePlayer? Cassette { get; private set; }
    public InterruptLines InterruptLines { get; }
    public Z80Cpu Cpu { get; }
    public Trs80RasterDisplay Video { get; }

    /// <summary>Hot-swaps a disk into the running machine (RAM/CPU state untouched) - the
    /// controller is created lazily on first insert for a machine that booted with none (see the
    /// constructor's own doc comment on why "no disk at boot" deliberately means "no FDC at all",
    /// not "an FDC with nothing in the drive"). Works for both JV1 and DMK images since
    /// Trs80DiskImageAdapter/Trs80DmkDiskImageAdapter both implement IFD1791DiskImage.</summary>
    public void InsertDisk(IFD1791DiskImage disk)
    {
        ArgumentNullException.ThrowIfNull(disk);
        if (Fdc is null)
        {
            Fdc = new Trs80FdcWiring(disk);
            Bus.Fdc = Fdc;
        }
        else
        {
            Fdc.Controller.InsertDisk(0, disk);
        }
    }

    public void InsertDisk(Jv1DiskImage disk) => InsertDisk(new Trs80DiskImageAdapter(disk));

    /// <summary>Hot-swaps a cassette into the running machine (RAM/CPU state untouched) - unlike
    /// the FDC, cassette hardware has no "hangs forever probing a drive that isn't there" hazard,
    /// so it's safe to always (re)attach one instead of requiring one to already exist.</summary>
    public void LoadTape(byte[] tape)
    {
        ArgumentNullException.ThrowIfNull(tape);
        Cassette = new Trs80CassettePlayer(tape);
        Bus.Cassette = Cassette;
    }

    public void Reset() { Bus.Reset(); InterruptLines.Clear(); Cpu.Reset(); }
    public void StepInstruction() { var before = Cpu.CycleCount; Cpu.StepInstruction(); var elapsed = checked((int)(Cpu.CycleCount - before)); Bus.Tick(elapsed); Video.Tick(elapsed); }
    public void Run(ulong instructionCount) { for (ulong i = 0; i < instructionCount; i++) StepInstruction(); }

    public Trs80Snapshot CaptureState() => new()
    {
        Cpu = Cpu.CaptureSnapshot(), Memory = Bus.CaptureState(), Keyboard = Keyboard.CaptureState(),
        Fdc = Fdc?.CaptureState(), Cassette = Cassette?.CaptureState()
    };

    public void RestoreState(Trs80Snapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Version != 1)
            throw new InvalidDataException($"Unsupported TRS-80 snapshot version {state.Version}.");
        if ((state.Fdc is null) != (Fdc is null))
            throw new InvalidDataException("TRS-80 snapshot FDC configuration does not match the machine.");
        if ((state.Cassette is null) != (Cassette is null))
            throw new InvalidDataException("TRS-80 snapshot cassette configuration does not match the machine.");
        Bus.RestoreState(state.Memory);
        Cpu.RestoreSnapshot(state.Cpu);
        Keyboard.RestoreState(state.Keyboard);
        if (state.Fdc is not null) Fdc!.RestoreState(state.Fdc);
        if (state.Cassette is not null) Cassette!.RestoreState(state.Cassette);
    }
}
