using Cpu6502.Variants;
using PetEmulator.Core;
using PetEmulator.Pet.Chips;
using PetEmulator.Vic20.Chips;
using PetEmulator.Vic20.Keyboard;
using PetEmulator.Vic20.Roms;

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

        // VIA1 port A: row-select (active-low, ORA & DDRA - real VIC-20 wiring, see
        // Vic20KeyboardMatrix's doc comment); port B: column readback for the selected row.
        _via1.PortAWritten = rowMask =>
        {
            _keyboard.SetRowSelect(rowMask);
            _via1.PortBInput = _keyboard.ReadColumns();
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

        _cpu.SetIRQ(_via1.IRQ || _via2.IRQ);
    }

    public void Run(ulong instructionCount)
    {
        for (var i = 0UL; i < instructionCount; i++)
            StepInstruction();
    }
}
