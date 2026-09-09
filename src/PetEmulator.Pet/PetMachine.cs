using Cpu6502.Variants;
using PetEmulator.Core;
using PetEmulator.Pet.Chips;
using PetEmulator.Pet.Ieee488;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Roms;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet;

/// <summary>
/// Orchestrates a complete Commodore PET/CBM: a stock NMOS 6502 (<see cref="Cpu6502Classic"/>),
/// its address-decoded bus (<see cref="PetMemoryBus"/>), and the PIA/VIA/CRTC chips and
/// tape/IEEE-488 bindings that hang off it, per <paramref name="profile"/>.
/// </summary>
public sealed class PetMachine : IMachine
{
    private readonly PetProfile _profile;
    private readonly PetMemoryBus _memoryBus;
    private readonly Cpu6502Classic _cpu;
    private readonly Pia _pia1;
    private readonly Pia _pia2;
    private readonly Via6522 _via;
    private readonly Crtc6545? _crtc;
    private readonly PetDatasette _datasette;
    private readonly PetIeeeBus _ieeeBus;
    private readonly PetIeeeBusBinding _ieeeBusBinding;
    private byte _keyboardSelectedRow;

    // PIA1's CB1 line (not VIA CA1 - a prior version of this wiring targeted the wrong chip
    // entirely, confirmed against personal-001's PetMachine.ClockPia1Cb1) carries the ~60Hz video
    // blanking pulse on real PET hardware - that edge is what drives the periodic jiffy-clock/
    // keyboard-scan IRQ. Every profile gets this same synthetic square wave (personal-001 doesn't
    // derive it from the CRTC either, even on CRTC-equipped profiles - it's unconditional).
    private const int Pia1Cb1PulsePeriodCycles = 16_667; // ~1MHz PET clock / 60Hz
    private int _pia1Cb1Phase;

    public PetMachine(PetProfile profile, string romsRoot, PetKeyboardMatrix? keyboard = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);

        _profile = profile;
        var roms = PetRomLoader.Load(Path.Combine(romsRoot, profile.RomDirectory), profile.RomManifest);

        _pia1 = new Pia("PIA1", PetMemoryBus.Pia1Base);
        _pia2 = new Pia("PIA2", PetMemoryBus.Pia2Base);
        _via = new Via6522("VIA", PetMemoryBus.ViaBase);
        _crtc = profile.RequiresCrtc ? new Crtc6545("CRTC", PetMemoryBus.CrtcBase) : null;

        _memoryBus = new PetMemoryBus(profile, roms, _pia1, _pia2, _via, _crtc);
        _cpu = new Cpu6502Classic(_memoryBus);

        _datasette = new PetDatasette(_pia1);

        // PIA1 port A: writing selects the keyboard row (low nibble); reading it back echoes
        // that row plus sense bits this repo doesn't model yet (cassette/IEEE EOI - see
        // personal-001's PetPia1Binding, which this wiring mirrors). Port B reads back which
        // columns are held down in the currently selected row.
        Keyboard = keyboard ?? new PetKeyboardMatrix();
        _pia1.PortAWritten = value => _keyboardSelectedRow = (byte)(value & 0x0F);
        _pia1.PortAInput = () => (byte)(0xF0 | _keyboardSelectedRow);
        _pia1.PortBInput = () => Keyboard.ReadColumns(_keyboardSelectedRow);

        _ieeeBus = new PetIeeeBus();
        _ieeeBusBinding = new PetIeeeBusBinding(_pia2, _via, _ieeeBus);

        Reset();
    }

    /// <summary>The keyboard matrix PIA1 scans. A caller (e.g. a GUI's key handler, via
    /// <see cref="Keyboard.IPetKeyboardMap"/>) presses/releases cells on this directly.</summary>
    public PetKeyboardMatrix Keyboard { get; }

    /// <summary>The CRTC, when this profile has one (<see cref="PetProfile.RequiresCrtc"/>) - null
    /// otherwise. A caller (e.g. a display renderer locating the text cursor) reads
    /// <see cref="Crtc6545.CursorAddress"/>/<see cref="Crtc6545.DisplayStartAddress"/> from this.</summary>
    public Crtc6545? Crtc => _crtc;

    /// <summary>The VIA - exposed for debug tooling (timer/IRQ state).</summary>
    public Via6522 Via => _via;

    public static PetMachine Create(PetProfile profile, string romsRoot) => new(profile, romsRoot);

    public string Name => _profile.Name;

    public bool IsReady => true;

    public ulong CycleCount => Processor.CycleCount;

    public IProcessor Processor => _cpu;

    public IMemoryBus Memory => _memoryBus;

    public void Reset()
    {
        // Real hardware powers up with indeterminate RAM; this zeroes it instead for
        // deterministic, reproducible boots/tests.
        _memoryBus.ClearRam();
        _pia1.Reset();
        _pia2.Reset();
        _via.Reset();
        _crtc?.Reset();
        _datasette.Reset();
        _ieeeBusBinding.Reset();
        Keyboard.Reset();
        _keyboardSelectedRow = 0;
        _pia1Cb1Phase = 0;
        _cpu.Reset();
    }

    /// <summary>Ported verbatim from personal-001's PetMachine.ClockPia1Cb1: a plain square wave,
    /// high for the first half of the period and low for the second.</summary>
    private void ClockPia1Cb1()
    {
        if (++_pia1Cb1Phase == Pia1Cb1PulsePeriodCycles / 2)
        {
            _pia1.CB1 = true;
        }
        else if (_pia1Cb1Phase == Pia1Cb1PulsePeriodCycles)
        {
            _pia1Cb1Phase = 0;
            _pia1.CB1 = false;
        }
    }

    public void StepInstruction()
    {
        var cyclesBefore = _cpu.CycleCount;
        _cpu.StepInstruction();
        var cycles = _cpu.CycleCount - cyclesBefore;

        _pia1.Tick(cycles);
        _pia2.Tick(cycles);
        _crtc?.Tick(cycles);
        _via.Tick(cycles);

        for (ulong i = 0; i < cycles; i++)
        {
            ClockPia1Cb1();
            _datasette.Tick();
            _ieeeBusBinding.Tick();
        }

        _cpu.SetIRQ(_pia1.IRQ || _pia2.IRQ || _via.IRQ);
    }

    public void Run(ulong instructionCount)
    {
        for (ulong i = 0; i < instructionCount; i++)
            StepInstruction();
    }

    /// <summary>Steps until <paramref name="condition"/> is true (checked against this machine's
    /// memory bus after every instruction) or <paramref name="maxInstructions"/> is reached.
    /// Returns whether the condition was met. Ported from personal-001's PetMachine.RunUntil -
    /// waiting for a real condition (e.g. "READY." bytes appearing in video RAM) instead of a
    /// fixed instruction count is what its own working keyboard integration tests rely on.</summary>
    public bool RunUntil(Func<IMemoryBus, bool> condition, ulong maxInstructions)
    {
        ArgumentNullException.ThrowIfNull(condition);
        for (ulong i = 0; i < maxInstructions; i++)
        {
            if (condition(Memory))
                return true;
            StepInstruction();
        }

        return condition(Memory);
    }
}
