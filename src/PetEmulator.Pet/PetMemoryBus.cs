using PetEmulator.Core;
using PetEmulator.Chips;
using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet;

/// <summary>
/// Address decoder for a PET machine: RAM (video RAM is a subrange of the same array, per
/// <see cref="PetProfile.VideoRamStart"/>/<see cref="PetProfile.VideoRamLength"/> - not a
/// separate device), ROM images loaded per <see cref="PetProfile.RomManifest"/>, and the four
/// fixed chip base addresses (PIA1 $E810, PIA2 $E820, VIA $E840, CRTC $E880 when the profile
/// requires one - see personal-001's PetMachine for the confirmed real PET address map).
/// Unmapped addresses (and CRTC-range reads when the profile has no CRTC) read as open bus
/// ($FF) rather than throwing, so a debugger can dump the full 64K without crashing.
/// </summary>
public sealed class PetMemoryBus : IMemoryBus
{
    /// <summary>Fires for every real Read/Write this bus serves - the debugging "actions" pattern
    /// personal-002's Z80Cpu.BusCycleObserver uses for cycle-level visibility into what the CPU is
    /// actually touching, ported here as a bus-level hook instead of a CPU-core change (this
    /// repo's CLAUDE.md routes src/PetEmulator.Cpu6502/ changes through a dedicated subagent; a
    /// bus-level observer needs none of that, and "which chip/address did this instruction just
    /// touch" is exactly what it's for). Optional (nullable) - zero added cost on the hot Read/
    /// Write path when nobody's watching.</summary>
    public Action<BusAccess>? Observer { get; set; }

    public const ushort Pia1Base = 0xE810;
    public const ushort Pia2Base = 0xE820;
    public const ushort ViaBase = 0xE840;
    public const ushort CrtcBase = 0xE880;
    private const byte OpenBus = 0xFF;

    private readonly byte[] _ram;
    private readonly uint _ramSize;
    private readonly uint _videoRamStart;
    private readonly uint _videoRamEnd;
    private readonly IReadOnlyList<PetRomImage> _roms;
    private readonly MT6520 _pia1;
    private readonly MT6520 _pia2;
    private readonly MOS6522 _via;
    private readonly MT6545? _crtc;

    public PetMemoryBus(PetProfile profile, IReadOnlyList<PetRomImage> roms, MT6520 pia1, MT6520 pia2, MOS6522 via, MT6545? crtc)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _roms = roms ?? throw new ArgumentNullException(nameof(roms));
        _pia1 = pia1 ?? throw new ArgumentNullException(nameof(pia1));
        _pia2 = pia2 ?? throw new ArgumentNullException(nameof(pia2));
        _via = via ?? throw new ArgumentNullException(nameof(via));
        _crtc = crtc;

        _ramSize = profile.RamSize;
        _videoRamStart = profile.VideoRamStart;
        _videoRamEnd = profile.VideoRamStart + profile.VideoRamLength;
        // Sized to cover both regions: for 32K+/CBM profiles video RAM sits right after main RAM
        // (no gap); for the 8K profile it sits far above RAM, so the array simply extends out to
        // it - IsRam still gates the gap in between as open bus, it is not backed by zeros here.
        _ram = new byte[Math.Max(_ramSize, _videoRamEnd)];
    }

    public byte Read(ushort address)
    {
        var value = ReadCore(address);
        Observer?.Invoke(new BusAccess(IsWrite: false, address, value));
        return value;
    }

    private byte ReadCore(ushort address)
    {
        if (IsRam(address))
            return _ram[address];

        if (TryFindRom(address, out var rom))
            return rom.Data[address - rom.Requirement.Address];

        if (InRange(address, Pia1Base, _pia1.Length))
            return _pia1.Read(address);
        if (InRange(address, Pia2Base, _pia2.Length))
            return _pia2.Read(address);
        if (InRange(address, ViaBase, _via.Length))
            return _via.Read(address);
        if (_crtc is not null && InRange(address, CrtcBase, _crtc.Length))
            return _crtc.Read(address);

        return OpenBus;
    }

    public void Write(ushort address, byte value)
    {
        WriteCore(address, value);
        Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
    }

    private void WriteCore(ushort address, byte value)
    {
        if (IsRam(address))
        {
            _ram[address] = value;
            return;
        }

        // ROM is read-only: writes are silently dropped, matching real hardware.
        if (TryFindRom(address, out _))
            return;

        if (InRange(address, Pia1Base, _pia1.Length))
            _pia1.Write(address, value);
        else if (InRange(address, Pia2Base, _pia2.Length))
            _pia2.Write(address, value);
        else if (InRange(address, ViaBase, _via.Length))
            _via.Write(address, value);
        else if (_crtc is not null && InRange(address, CrtcBase, _crtc.Length))
            _crtc.Write(address, value);
        // else: unmapped - write has no effect (open bus).
    }

    /// <summary>Zeroes RAM, including the video-RAM subrange.</summary>
    public void ClearRam() => Array.Clear(_ram);

    private bool IsRam(uint address) => address < _ramSize || (address >= _videoRamStart && address < _videoRamEnd);

    private bool TryFindRom(ushort address, out PetRomImage rom)
    {
        foreach (var image in _roms)
        {
            if (address >= image.Requirement.Address && address < image.Requirement.Address + image.Requirement.Length)
            {
                rom = image;
                return true;
            }
        }

        rom = null!;
        return false;
    }

    private static bool InRange(ushort address, ushort baseAddress, uint length) => address >= baseAddress && address < baseAddress + length;
}
