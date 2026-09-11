using PetEmulator.Cpu6502.Variants;
using PetEmulator.Core;
using PetEmulator.Pet.CbmDos;
using PetEmulator.Pet.Devices;
using PetEmulator.Pet.Tape;
using PetEmulator.Chips;
using PetEmulator.Vic20.Devices;
using PetEmulator.Vic20.Keyboard;
using PetEmulator.Vic20.Roms;
using PetEmulator.Vic20.Serial;
using PetEmulator.Vic20.Tape;

namespace PetEmulator.Vic20;

/// <summary>
/// Orchestrates a complete unexpanded VIC-20: a stock NMOS 6502 (<see cref="Cpu6502Classic"/>),
/// its address-decoded bus (<see cref="Vic20MemoryBus"/>), and the VIC/VIA1/VIA2/color-RAM chips
/// that hang off it. Mirrors PetEmulator.Pet.PetMachine's shape exactly (same StepInstruction/
/// Reset/BusObserver pattern) - see docs/vic20-migration-plan.md step 8.
///
/// Also watches for a real SAVE dispatch to build <see cref="Datasette"/>'s content - see
/// <see cref="CaptureSaveIfDispatched"/>'s doc comment and docs/vic20-tape.md's "Write (SAVE)"
/// section for the full story (a real, labeled KERNAL disassembly, not a guess).
/// </summary>
public sealed class Vic20Machine : IMachine
{
    private readonly Vic20MemoryBus _memoryBus;
    private readonly Cpu6502Classic _cpu;
    private readonly MOS6560 _vic;
    private readonly MOS6522 _via1;
    private readonly MOS6522 _via2;
    private readonly MOS2114 _colorRam;
    private readonly Vic20KeyboardMatrix _keyboard = new();
    private readonly Vic20Datasette _datasette;
    private readonly Vic20SerialBus _serialBus;
    private readonly Vic20SerialBusBinding _serialBusBinding;
    private readonly List<PetIeeeDriveStatus> _mountedDrives = [];
    private bool _diskActivityPending;

    // The real KERNAL's IRQ vector ($0314/$0315, "CINV") while idle - both LOAD and SAVE
    // temporarily redirect it to their own tape ISR for the duration of the operation, then
    // restore it (see TAPE's own restore-check loop in the real disassembly). Confirmed
    // empirically (BusObserver-free: just reading $0314/$0315 across a real boot, LOAD, and
    // SAVE) and cross-checked against the real disassembly's IRQVCTRS table (WRTZ is entry $08,
    // "write tape leader IRQ routine" - the vector SAVE's TAPE dispatcher installs first).
    private const ushort DefaultIrqVector = 0xEABF;
    private const ushort SaveDispatchVector = 0xFCA8; // WRTZ
    private ushort _lastIrqVector = DefaultIrqVector;

    public Vic20Machine(
        string romsRoot,
        Vic20DisplayConfig? displayConfig = null,
        Vic20ExpansionPreset expansionPreset = Vic20ExpansionPreset.Unexpanded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);

        DisplayConfig = displayConfig ?? Vic20DisplayConfig.Ntsc;
        var roms = Vic20RomLoader.Load(romsRoot, Vic20RomManifest.Ntsc);

        _vic = new MOS6560("VIC", Vic20MemoryMap.VicBaseAddress);
        _via1 = new MOS6522("VIA1", Vic20MemoryMap.Via1BaseAddress);
        _via2 = new MOS6522("VIA2", Vic20MemoryMap.Via2BaseAddress);
        _colorRam = new MOS2114("Color RAM", Vic20MemoryMap.ColorRamStart, Vic20MemoryMap.ColorRamSize);

        _memoryBus = new Vic20MemoryBus(roms, _vic, _via1, _via2, _colorRam, expansionPreset);
        _cpu = new Cpu6502Classic(_memoryBus);
        _datasette = new Vic20Datasette(_via1, _via2);
        _serialBus = new Vic20SerialBus();
        _serialBus.Activity += activity =>
        {
            if (activity.Kind == "byte")
                _diskActivityPending = true;
        };
        _serialBusBinding = new Vic20SerialBusBinding(_via1, _via2, _serialBus);

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

    public MOS6560 Vic => _vic;

    public MOS6522 Via1 => _via1;

    public MOS6522 Via2 => _via2;

    /// <summary>The cassette datasette - a caller (GUI menu, debugger script) loads a tape
    /// through this directly.</summary>
    public Vic20Datasette Datasette => _datasette;

    /// <summary>Every peripheral currently attached and worth a GUI status icon for - see
    /// <see cref="IDeviceStatus"/>'s doc comment. Rebuilt on each access so it reflects the
    /// latest <see cref="MountDisk"/>/<see cref="Datasette"/> state.</summary>
    public IReadOnlyList<IDeviceStatus> Devices => [new Vic20DatasetteStatus(_datasette), .. _mountedDrives];

    /// <summary>Mounts a D64 disk image on the IEC serial bus at <paramref name="deviceNumber"/>.
    /// Replaces any drive already attached at that address.</summary>
    public void MountDisk(string path, int deviceNumber = 8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var image = D64Image.Load(path);
        var drive = new PetIeeeDiskDrive(deviceNumber);
        drive.Engine.AttachImage(image);
        _serialBus.AttachDevice(drive);
        _mountedDrives.RemoveAll(d => d.Id == $"ieee488:{deviceNumber}");
        _mountedDrives.Add(new PetIeeeDriveStatus(deviceNumber, Path.GetFileName(path)));
    }

    /// <summary>Creates a formatted, writable D64 at <paramref name="path"/> and mounts it.</summary>
    public void MountNewDisk(string path, string diskName, string diskId = "00", int deviceNumber = 8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllBytes(path, D64Image.CreateFormatted(diskName, diskId));
        MountDisk(path, deviceNumber);
    }

    /// <summary>Whether a disk is currently mounted at <paramref name="deviceNumber"/>.</summary>
    public bool HasDisk(int deviceNumber = 8) => _mountedDrives.Any(d => d.Id == $"ieee488:{deviceNumber}");

    /// <summary>Returns whether a real IEC byte crossed the bus since the last call, then clears
    /// the latch. Line changes are intentionally excluded so a Desktop LED reflects data traffic,
    /// not the bus's continuously changing handshake lines.</summary>
    public bool PollDiskActivity()
    {
        var pending = _diskActivityPending;
        _diskActivityPending = false;
        return pending;
    }

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
        _serialBusBinding.Reset();
        _diskActivityPending = false;
        _lastIrqVector = 0; // RAM is cleared too - matches $0314/5 reading 0 until the KERNAL re-inits it
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
        {
            _datasette.Tick();
            _serialBusBinding.Tick();
        }

        CaptureSaveIfDispatched();

        _cpu.SetIRQ(_via1.IRQ || _via2.IRQ);
    }

    /// <summary>
    /// Builds a real tape from a real SAVE - see this class's own doc comment and
    /// docs/vic20-tape.md's "Write (SAVE)" section.
    ///
    /// Detects the moment SAVE's <c>TAPE</c> dispatcher redirects $0314/$0315 to <c>WRTZ</c> (the
    /// real KERNAL's own "start of a write" signal - see <see cref="SaveDispatchVector"/>'s doc
    /// comment). At exactly that moment, the real KERNAL has already built a genuine 192-byte
    /// tape header block in RAM (type/start-address/end-address/filename, at the buffer
    /// <c>TAPE1</c> - zero page $B2/$B3 - points at) and hasn't touched the program bytes it's
    /// about to save at all yet, so both are safe to read directly, no waiting required. The
    /// bytes are re-encoded through <see cref="Vic20TapeEncoder"/> (the exact same pulse format
    /// <see cref="Vic20Datasette"/>'s LOAD side already decodes reliably) and handed straight to
    /// <see cref="Datasette"/> - so a real, unmodified SAVE followed by a real, unmodified LOAD
    /// genuinely round-trips, without this class replicating the real ROM's own analog write
    /// timing bit-for-bit (see <see cref="Vic20Datasette"/>'s doc comment for why that path was
    /// tried and dropped).
    /// </summary>
    private void CaptureSaveIfDispatched()
    {
        var vector = (ushort)(_memoryBus.Read(0x0314) | (_memoryBus.Read(0x0315) << 8));
        var justDispatched = vector == SaveDispatchVector && _lastIrqVector != SaveDispatchVector;
        _lastIrqVector = vector;
        if (!justDispatched)
            return;

        var tape1 = (ushort)(_memoryBus.Read(0x00B2) | (_memoryBus.Read(0x00B3) << 8));
        var headerBytes = new byte[PetTapeHeaderBlock.Length];
        for (var i = 0; i < headerBytes.Length; i++)
            headerBytes[i] = _memoryBus.Read((ushort)(tape1 + i));

        PetTapeHeaderBlock header;
        try
        {
            header = PetTapeHeaderBlock.Parse(headerBytes);
        }
        catch (FormatException)
        {
            return; // buffer wasn't really a header (shouldn't happen, but never corrupt the deck over it)
        }

        if (header.EndAddress <= header.StartAddress)
            return; // nothing real to save

        var payload = new byte[header.EndAddress - header.StartAddress];
        for (var i = 0; i < payload.Length; i++)
            payload[i] = _memoryBus.Read((ushort)(header.StartAddress + i));

        var pulses = Vic20TapeEncoder.EncodeTape(headerBytes, payload);
        var name = string.IsNullOrEmpty(header.FileName) ? _datasette.TapeName ?? "saved" : header.FileName;
        _datasette.ReplaceContentAfterSave(pulses, name);
    }

    public void Run(ulong instructionCount)
    {
        for (var i = 0UL; i < instructionCount; i++)
            StepInstruction();
    }
}
