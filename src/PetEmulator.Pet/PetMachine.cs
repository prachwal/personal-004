using Cpu6502.Variants;
using PetEmulator.Core;
using PetEmulator.Pet.CbmDos;
using PetEmulator.Pet.Chips;
using PetEmulator.Pet.Devices;
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
    private readonly List<PetIeeeDriveStatus> _mountedDrives = [];
    private byte _keyboardSelectedRow;
    private bool _diskActivityPending;
    private long _ieeeByteCount;

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
        _ieeeBus = new PetIeeeBus();
        // Latches true on any real byte transfer (LISTEN/TALK addressing, filename, or file data -
        // all real bus traffic, not just payload) for a GUI's disk-activity LED - see
        // PollDiskActivity. Line-change events (ATN/DAV/NRFD/NDAC) don't count; they fire on
        // nearly every bus cycle even when idle and would make the LED look permanently lit.
        _ieeeBus.Activity += activity =>
        {
            if (activity.Kind == "byte")
            {
                _diskActivityPending = true;
                _ieeeByteCount++;
            }
        };

        // PIA1 port A: writing selects the keyboard row (low nibble); reading it back echoes
        // that row plus PA4 (cassette #1 sense, active low - see PetDatasette.Sense) and PA6
        // (IEEE EOI, active low - see PetIeeeBus.EOI), ported from personal-001's
        // PetPia1Binding.ReadPortA. PA5 (cassette #2 sense) is left high - no cassette #2 on any
        // profile this repo models. Port B reads back which columns are held down in the
        // currently selected row.
        Keyboard = keyboard ?? new PetKeyboardMatrix();
        _pia1.PortAWritten = value => _keyboardSelectedRow = (byte)(value & 0x0F);
        _pia1.PortAInput = () =>
        {
            var value = (byte)(0xF0 | _keyboardSelectedRow);
            if (_datasette.Sense) value &= 0xEF;
            if (_ieeeBus.EOI) value &= 0xBF;
            return value;
        };
        _pia1.PortBInput = () => Keyboard.ReadColumns(_keyboardSelectedRow);

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

    /// <summary>Fires for every real bus access (RAM/ROM/chip read or write) the CPU makes - see
    /// <see cref="BusAccess"/>'s doc comment. Optional; zero added cost on the hot path when
    /// unset.</summary>
    public Action<BusAccess>? BusObserver
    {
        get => _memoryBus.Observer;
        set => _memoryBus.Observer = value;
    }

    /// <summary>The cassette #1 datasette - a caller (GUI menu, debugger script) loads a tape
    /// through this directly.</summary>
    public PetDatasette Datasette => _datasette;

    /// <summary>Every peripheral currently attached and worth a GUI status icon for - see
    /// <see cref="IPetDeviceStatus"/>'s doc comment for why this is a dynamic list rather than a
    /// fixed set of properties. Rebuilt on each access (cheap: a handful of entries), so it always
    /// reflects the latest <see cref="MountDisk"/>/<see cref="Datasette"/> state.</summary>
    public IReadOnlyList<IPetDeviceStatus> Devices =>
        [new PetDatasetteStatus(_datasette), .. _mountedDrives];

    /// <summary>Mounts a D64 disk image on the IEEE-488 bus at <paramref name="deviceNumber"/>
    /// (8 is the PET/CBM DOS convention for the first drive). Replaces whatever was already
    /// mounted at that device number rather than attaching a second device under the same
    /// address.</summary>
    public void MountDisk(string path, int deviceNumber = 8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var image = D64Image.Load(path);
        var drive = new PetIeeeDiskDrive(deviceNumber);
        drive.Engine.AttachImage(image);
        _ieeeBus.AttachDevice(drive);
        _mountedDrives.RemoveAll(d => d.Id == $"ieee488:{deviceNumber}");
        _mountedDrives.Add(new PetIeeeDriveStatus(deviceNumber, Path.GetFileName(path)));
    }

    /// <summary>Whether a disk is currently mounted at <paramref name="deviceNumber"/> - for a
    /// GUI's single dedicated disk-drive icon (device 8 is the PET/CBM DOS convention for "the"
    /// drive; see <see cref="MountDisk"/>'s default), as opposed to <see cref="Devices"/>'s full
    /// dynamic list.</summary>
    public bool HasDisk(int deviceNumber = 8) => _mountedDrives.Any(d => d.Id == $"ieee488:{deviceNumber}");

    /// <summary>Returns whether any real byte crossed the IEEE-488 bus since the last call, then
    /// clears the flag - a GUI polls this once per render tick to drive a brief activity-LED
    /// blink without needing its own event subscription.</summary>
    public bool PollDiskActivity()
    {
        var pending = _diskActivityPending;
        _diskActivityPending = false;
        return pending;
    }

    /// <summary>Every "byte" activity (LISTEN/TALK addressing, filename, or file data - real bus
    /// traffic, never a line-change) this machine's IEEE-488 bus has ever processed, cumulative
    /// (unlike <see cref="PollDiskActivity"/>, which latches and clears). A natural progress
    /// signal for <see cref="MachineExtensions.RunUntilOrStalled"/> when diagnosing a LOAD/SAVE that isn't
    /// finishing - if this stops advancing, the transfer, not just the CPU, is stuck.</summary>
    public long IeeeByteTransferCount => _ieeeByteCount;

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

    // RunUntil/RunUntilOrStalled moved to PetEmulator.Core.MachineExtensions (pure IMachine
    // extension methods, unchanged call syntax) once VIC-20 needed the identical logic - see
    // docs/vic20-migration-plan.md step 8.
}
