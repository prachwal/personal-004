using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.Cpc;
using PetEmulator.CpcFdc;

namespace PetEmulator.Cpc6128;

/// <summary>Initial CPC6128 machine composition: Z80, 128 KB RAM, CPC I/O, video, AY and cassette.</summary>
public sealed class Cpc6128Machine : IMachine
{
    private readonly Z80Cpu _cpu;
    private readonly Cpc6128MemoryBus _bus;
    private readonly Cpc6128Ports _ports;
    private readonly I8272Chip _fdc;
    private int _deviceCycleRemainder;
    private ulong _frameCycles;

    public Cpc6128Machine(ReadOnlySpan<byte> rom)
    {
        CpcGateArray? gateArray = null;
        Crtc = new MT6545("CPC6128 CRTC", 0xBC00);
        _bus = new Cpc6128MemoryBus(rom, () => gateArray!.LowerRomEnabled,
            () => gateArray!.UpperRomEnabled, () => gateArray!.RamConfiguration);
        gateArray = new CpcGateArray(Crtc, _bus.ReadVideoRam);
        GateArray = gateArray;
        Ay = new Ay38910();
        Keyboard = new CpcKeyboard();
        Cassette = new CpcCassette();
        InterruptLines = new PetEmulator.CpuZ80.Interrupts.InterruptLines();
        _fdc = new I8272Chip();
        _ports = new Cpc6128Ports(GateArray, Crtc, Ay, Keyboard, Cassette, _fdc, _bus);
        _bus.AttachPorts(_ports);
        _cpu = new Z80Cpu(_bus, InterruptLines);
        Reset();
    }

    public string Name => "Amstrad CPC6128";
    public bool IsReady => true;
    public ulong CycleCount => _cpu.CycleCount;
    public IProcessor Processor => _cpu;
    public Z80Cpu Cpu => _cpu;
    public IMemoryBus Memory => _bus;
    public Cpc6128MemoryBus Bus => _bus;
    public Cpc6128Ports Ports => _ports;
    public CpcGateArray GateArray { get; }
    public MT6545 Crtc { get; }
    public Ay38910 Ay { get; }
    public CpcKeyboard Keyboard { get; }
    public CpcCassette Cassette { get; }
    public I8272Chip Fdc => _fdc;
    public PetEmulator.CpuZ80.Interrupts.InterruptLines InterruptLines { get; }
    public ulong FrameCount { get; private set; }

    public Cpc6128Snapshot CaptureState() => new()
    {
        Cpu = _cpu.CaptureSnapshot(), Memory = new() { PhysicalRam = _bus.CapturePhysicalRam(), UpperRomNumber = _bus.UpperRomNumber },
        FrameCount = FrameCount, DeviceCycleRemainder = _deviceCycleRemainder, FrameCycles = _frameCycles,
        GateArray = GateArray.CaptureState(), Crtc = Crtc.CaptureState(), Ay = Ay.CaptureState(), Keyboard = Keyboard.CaptureState(),
        Cassette = Cassette.CaptureState(), Ports = Ports.CaptureState(), Fdc = _fdc.CaptureState()
    };

    public void RestoreState(Cpc6128Snapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Version != 1) throw new InvalidDataException($"Unsupported CPC6128 snapshot version {state.Version}.");
        _bus.RestorePhysicalRam(state.Memory.PhysicalRam); _bus.SelectUpperRom(state.Memory.UpperRomNumber); _cpu.RestoreSnapshot(state.Cpu);
        GateArray.RestoreState(state.GateArray); Crtc.RestoreState(state.Crtc); Ay.RestoreState(state.Ay); Keyboard.RestoreState(state.Keyboard);
        Cassette.RestoreState(state.Cassette); Ports.RestoreState(state.Ports); _fdc.RestoreState(state.Fdc);
        _deviceCycleRemainder = state.DeviceCycleRemainder; _frameCycles = state.FrameCycles; FrameCount = state.FrameCount;
    }

    public void LoadExpansionRom(byte number, ReadOnlySpan<byte> rom) => _bus.LoadUpperRom(number, rom);

    public void LoadDisk(int drive, DskDiskImage image)
    {
        var floppy = new DskFloppyDrive(image) { MotorOn = true };
        if (drive == 0) _fdc.Drive0 = floppy;
        else if (drive == 1) _fdc.Drive1 = floppy;
        else throw new ArgumentOutOfRangeException(nameof(drive));
    }

    public void Reset()
    {
        _bus.Reset();
        _cpu.Reset();
        GateArray.Reset();
        Crtc.Reset();
        Ay.Reset();
        Keyboard.Reset();
        Cassette.Reset();
        _fdc.Reset();
        _ports.Reset();
        InterruptLines.SetInt(false);
        _deviceCycleRemainder = 0;
        _frameCycles = FrameCount = 0;
    }

    public void StepInstruction()
    {
        var before = _cpu.CycleCount;
        _cpu.StepInstruction();
        var cycles = checked((int)(_cpu.CycleCount - before));
        InterruptLines.SetInt(GateArray.InterruptPending);
        _deviceCycleRemainder += cycles;
        while (_deviceCycleRemainder >= 4)
        {
            _deviceCycleRemainder -= 4;
            GateArray.Tick();
            Cassette.Tick();
        }
        _fdc.Tick(cycles);
        _frameCycles += (uint)cycles;
        if (_frameCycles >= 80_000)
        {
            _frameCycles -= 80_000;
            FrameCount++;
            GateArray.RenderFrame();
        }
    }

    public void Run(ulong instructionCount)
    {
        for (var index = 0UL; index < instructionCount; index++) StepInstruction();
    }
}
