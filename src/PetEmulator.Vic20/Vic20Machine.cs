using Cpu6502.Variants;
using PetEmulator.Core;
using PetEmulator.Pet.Chips;
using PetEmulator.Vic20.Chips;
using PetEmulator.Vic20.Devices;
using PetEmulator.Vic20.Keyboard;
using PetEmulator.Vic20.Roms;
using PetEmulator.Vic20.Tape;

namespace PetEmulator.Vic20;

/// <summary>
/// Orchestrates a complete unexpanded VIC-20: a stock NMOS 6502 (<see cref="Cpu6502Classic"/>),
/// its address-decoded bus (<see cref="Vic20MemoryBus"/>), and the VIC/VIA1/VIA2/color-RAM chips
/// that hang off it. Mirrors PetEmulator.Pet.PetMachine's shape exactly (same StepInstruction/
/// Reset/BusObserver pattern) - see docs/vic20-migration-plan.md step 8.
/// </summary>
public sealed class Vic20Machine : IMachine
{
    private readonly Vic20MemoryBus _memoryBus;
    private readonly Cpu6502Classic _cpu;
    private readonly Vic6560 _vic;
    private readonly Via6522 _via1;
    private readonly Via6522 _via2;
    private readonly Vic20ColorRam _colorRam;
    private readonly Vic20KeyboardMatrix _keyboard = new();
    private readonly Vic20Datasette _datasette;

    public Vic20Machine(string romsRoot, Vic20DisplayConfig? displayConfig = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);

        DisplayConfig = displayConfig ?? Vic20DisplayConfig.Ntsc;
        var roms = Vic20RomLoader.Load(romsRoot, Vic20RomManifest.Ntsc);

        _vic = new Vic6560("VIC", Vic20MemoryMap.VicBaseAddress);
        _via1 = new Via6522("VIA1", Vic20MemoryMap.Via1BaseAddress);
        _via2 = new Via6522("VIA2", Vic20MemoryMap.Via2BaseAddress);
        _colorRam = new Vic20ColorRam("Color RAM", Vic20MemoryMap.ColorRamStart, Vic20MemoryMap.ColorRamSize);

        _memoryBus = new Vic20MemoryBus(roms, _vic, _via1, _via2, _colorRam);
        _cpu = new Cpu6502Classic(_memoryBus);
        _datasette = new Vic20Datasette(_via1, _via2);

        // VIA2 port B ($9120): row-select (active-low, ORB & DDRB); VIA2 port A ($9121): column
        // readback for the selected row. Confirmed against the real KERNAL disassembly
        // (docs/vic20-disassembly/kernal.asm ~line 1685: "sta $9120" writes row-select,
        // "lda $9121"/"lda $9121" debounce-reads columns) and the real boot-time DDR writes
        // ($9122=DDRB=$FF all-output, $9123=DDRA=$00 all-input) - NOT VIA1, and NOT port A for
        // output/port B for input as an earlier version of this wiring (and the reference project
        // it was ported from) assumed. Getting this backwards doesn't corrupt anything visibly -
        // it just means every real keypress silently vanishes, since the KERNAL's own scan reads
        // a VIA/port pair nothing ever writes to.
        _via2.PortBWritten = rowMask =>
        {
            _keyboard.SetRowSelect(rowMask);
            _via2.PortAInput = _keyboard.ReadColumns();
        };

        Reset();
    }

    /// <summary>The keyboard matrix VIA1 scans. A caller (e.g. a GUI's key handler) presses/
    /// releases cells on this directly.</summary>
    public Vic20KeyboardMatrix Keyboard => _keyboard;

    /// <summary>Display geometry this machine was constructed with - see
    /// <see cref="Vic20DisplayConfig"/>'s doc comment for why this is a passed-in config rather
    /// than hardcoded downstream (e.g. in a Desktop ViewModel).</summary>
    public Vic20DisplayConfig DisplayConfig { get; }

    public Vic6560 Vic => _vic;

    public Via6522 Via1 => _via1;

    public Via6522 Via2 => _via2;

    /// <summary>The cassette datasette - a caller (GUI menu, debugger script) loads a tape
    /// through this directly.</summary>
    public Vic20Datasette Datasette => _datasette;

    /// <summary>Every peripheral currently attached and worth a GUI status icon for - see
    /// <see cref="IDeviceStatus"/>'s doc comment. Mirrors <c>PetMachine.Devices</c>'s shape;
    /// just the datasette for now (no disk drive on an unexpanded VIC-20's IEEE-488... it has
    /// none - see docs/vic20-migration-plan.md's scope cuts).</summary>
    public IReadOnlyList<IDeviceStatus> Devices => [new Vic20DatasetteStatus(_datasette)];

    /// <summary>Fires for every real bus access (RAM/ROM/chip read or write) the CPU makes - see
    /// <see cref="BusAccess"/>'s doc comment. Optional; zero added cost on the hot path when
    /// unset.</summary>
    public Action<BusAccess>? BusObserver
    {
        get => _memoryBus.Observer;
        set => _memoryBus.Observer = value;
    }

    public static Vic20Machine Create(string romsRoot) => new(romsRoot);

    public string Name => "VIC-20 NTSC (unexpanded)";

    public bool IsReady => true;

    public ulong CycleCount => Processor.CycleCount;

    public IProcessor Processor => _cpu;

    public IMemoryBus Memory => _memoryBus;

    public void Reset()
    {
        // Real hardware powers up with indeterminate RAM; this zeroes it instead for
        // deterministic, reproducible boots/tests.
        _memoryBus.ClearRam();
        _vic.Reset();
        _via1.Reset();
        _via2.Reset();
        _colorRam.Reset();
        _keyboard.Reset();
        _datasette.Reset();
        _cpu.Reset();
    }

    public void StepInstruction()
    {
        var cyclesBefore = _cpu.CycleCount;
        _cpu.StepInstruction();
        var cycles = _cpu.CycleCount - cyclesBefore;

        _vic.Tick(cycles);
        _via1.Tick(cycles);
        _via2.Tick(cycles);

        for (var i = 0UL; i < cycles; i++)
            _datasette.Tick();

        _cpu.SetIRQ(_via1.IRQ || _via2.IRQ);
    }

    public void Run(ulong instructionCount)
    {
        for (var i = 0UL; i < instructionCount; i++)
            StepInstruction();
    }
}
