using Cpu6502.Variants;
using PetEmulator.Core;
using PetEmulator.Pet.Chips;
using PetEmulator.Pet.Ieee488;
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

    public PetMachine(PetProfile profile, string romsRoot)
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

        // TODO: PetKeyboardMatrix is imported but not wired to PIA1 here - the real PET key->row/
        // column map (PetKeyboardMap) was explicitly not ported (depends on a framework this repo
        // doesn't have), so a wired matrix would have no meaningful key data behind it yet.
        _ieeeBus = new PetIeeeBus();
        _ieeeBusBinding = new PetIeeeBusBinding(_pia2, _via, _ieeeBus);

        Reset();
    }

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
        _cpu.Reset();
    }

    public void StepInstruction()
    {
        var cyclesBefore = _cpu.CycleCount;
        _cpu.StepInstruction();
        var cycles = _cpu.CycleCount - cyclesBefore;

        _pia1.Tick(cycles);
        _pia2.Tick(cycles);
        _via.Tick(cycles);
        _crtc?.Tick(cycles);
        for (ulong i = 0; i < cycles; i++)
        {
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
}
