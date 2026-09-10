using PetEmulator.Core;
using PetEmulator.Chips;
using PetEmulator.Vic20.Roms;

namespace PetEmulator.Vic20;

/// <summary>
/// Address decoder for an unexpanded VIC-20 (see <see cref="Vic20MemoryMap"/>): built-in RAM,
/// char/BASIC/KERNAL ROM, VIC/VIA1/VIA2/color-RAM chip registers. Mirrors
/// PetEmulator.Pet.PetMemoryBus's shape exactly (same <see cref="Observer"/> hook via
/// <see cref="PetEmulator.Core.BusAccess"/>, same ReadCore/WriteCore split, same open-bus
/// fallback) - not shared code (different address map, different chip set), but the same
/// structure so tooling built against one (InstructionTracer, MachineDebugger) works unchanged
/// against the other.
/// </summary>
public sealed class Vic20MemoryBus : IMemoryBus
{
    public Action<BusAccess>? Observer { get; set; }

    private const byte OpenBus = 0xFF;

    private readonly byte[] _zeroPageRam = new byte[Vic20MemoryMap.ZeroPageRamSize];
    private readonly byte[] _builtinRam = new byte[Vic20MemoryMap.BuiltinRamSize];
    private readonly byte[]? _block0;
    private readonly byte[]? _block1;
    private readonly byte[]? _block2;
    private readonly byte[]? _block3;
    private readonly byte[]? _cartridgeRam;
    private readonly byte[] _charRom;
    private readonly byte[] _basicRom;
    private readonly byte[] _kernalRom;
    private readonly MOS6560 _vic;
    private readonly MOS6522 _via1;
    private readonly MOS6522 _via2;
    private readonly MOS2114 _colorRam;

    public Vic20MemoryBus(
        IReadOnlyList<Vic20RomImage> roms,
        MOS6560 vic,
        MOS6522 via1,
        MOS6522 via2,
        MOS2114 colorRam,
        Vic20ExpansionPreset expansionPreset = Vic20ExpansionPreset.Unexpanded)
    {
        ArgumentNullException.ThrowIfNull(roms);
        _vic = vic ?? throw new ArgumentNullException(nameof(vic));
        _via1 = via1 ?? throw new ArgumentNullException(nameof(via1));
        _via2 = via2 ?? throw new ArgumentNullException(nameof(via2));
        _colorRam = colorRam ?? throw new ArgumentNullException(nameof(colorRam));

        _charRom = Find(roms, Vic20MemoryMap.CharRomStart);
        _basicRom = Find(roms, Vic20MemoryMap.BasicRomStart);
        _kernalRom = Find(roms, Vic20MemoryMap.KernalRomStart);

        _block0 = IsEnabled(expansionPreset, 0) ? new byte[Vic20MemoryMap.Block0Size] : null;
        _block1 = IsEnabled(expansionPreset, 1) ? new byte[Vic20MemoryMap.Block1Size] : null;
        _block2 = IsEnabled(expansionPreset, 2) ? new byte[Vic20MemoryMap.Block2Size] : null;
        _block3 = IsEnabled(expansionPreset, 3) ? new byte[Vic20MemoryMap.Block3Size] : null;
        _cartridgeRam = IsEnabled(expansionPreset, 5) ? new byte[Vic20MemoryMap.CartridgeSize] : null;
    }

    public byte Read(ushort address)
    {
        var value = ReadCore(address);
        Observer?.Invoke(new BusAccess(IsWrite: false, address, value));
        return value;
    }

    private byte ReadCore(ushort address)
    {
        if (address < Vic20MemoryMap.ZeroPageRamSize)
            return _zeroPageRam[address];
        if (_block0 is not null && InRange(address, Vic20MemoryMap.Block0Start, (uint)_block0.Length))
            return _block0[address - Vic20MemoryMap.Block0Start];
        if (InRange(address, Vic20MemoryMap.BuiltinRamStart, (uint)_builtinRam.Length))
            return _builtinRam[address - Vic20MemoryMap.BuiltinRamStart];
        if (_block1 is not null && InRange(address, Vic20MemoryMap.Block1Start, (uint)_block1.Length))
            return _block1[address - Vic20MemoryMap.Block1Start];
        if (_block2 is not null && InRange(address, Vic20MemoryMap.Block2Start, (uint)_block2.Length))
            return _block2[address - Vic20MemoryMap.Block2Start];
        if (_block3 is not null && InRange(address, Vic20MemoryMap.Block3Start, (uint)_block3.Length))
            return _block3[address - Vic20MemoryMap.Block3Start];
        if (InRange(address, Vic20MemoryMap.CharRomStart, (uint)_charRom.Length))
            return _charRom[address - Vic20MemoryMap.CharRomStart];
        if (InRange(address, Vic20MemoryMap.VicBaseAddress, _vic.Length))
            return _vic.Read(address);
        if (InRange(address, Vic20MemoryMap.Via1BaseAddress, _via1.Length))
            return _via1.Read(address);
        if (InRange(address, Vic20MemoryMap.Via2BaseAddress, _via2.Length))
            return _via2.Read(address);
        if (InRange(address, Vic20MemoryMap.ColorRamStart, _colorRam.Length))
            return _colorRam.Read(address);
        if (_cartridgeRam is not null && InRange(address, Vic20MemoryMap.CartridgeStart, (uint)_cartridgeRam.Length))
            return _cartridgeRam[address - Vic20MemoryMap.CartridgeStart];
        if (InRange(address, Vic20MemoryMap.BasicRomStart, (uint)_basicRom.Length))
            return _basicRom[address - Vic20MemoryMap.BasicRomStart];
        if (InRange(address, Vic20MemoryMap.KernalRomStart, (uint)_kernalRom.Length))
            return _kernalRom[address - Vic20MemoryMap.KernalRomStart];

        return OpenBus;
    }

    public void Write(ushort address, byte value)
    {
        WriteCore(address, value);
        Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
    }

    private void WriteCore(ushort address, byte value)
    {
        if (address < Vic20MemoryMap.ZeroPageRamSize)
        {
            _zeroPageRam[address] = value;
            return;
        }
        if (_block0 is not null && InRange(address, Vic20MemoryMap.Block0Start, (uint)_block0.Length))
        {
            _block0[address - Vic20MemoryMap.Block0Start] = value;
            return;
        }
        if (InRange(address, Vic20MemoryMap.BuiltinRamStart, (uint)_builtinRam.Length))
        {
            _builtinRam[address - Vic20MemoryMap.BuiltinRamStart] = value;
            return;
        }
        if (_block1 is not null && InRange(address, Vic20MemoryMap.Block1Start, (uint)_block1.Length))
        {
            _block1[address - Vic20MemoryMap.Block1Start] = value;
            return;
        }
        if (_block2 is not null && InRange(address, Vic20MemoryMap.Block2Start, (uint)_block2.Length))
        {
            _block2[address - Vic20MemoryMap.Block2Start] = value;
            return;
        }
        if (_block3 is not null && InRange(address, Vic20MemoryMap.Block3Start, (uint)_block3.Length))
        {
            _block3[address - Vic20MemoryMap.Block3Start] = value;
            return;
        }
        // ROM is read-only: writes silently dropped, matching real hardware.
        if (InRange(address, Vic20MemoryMap.CharRomStart, (uint)_charRom.Length)
            || InRange(address, Vic20MemoryMap.BasicRomStart, (uint)_basicRom.Length)
            || InRange(address, Vic20MemoryMap.KernalRomStart, (uint)_kernalRom.Length))
            return;

        if (_cartridgeRam is not null && InRange(address, Vic20MemoryMap.CartridgeStart, (uint)_cartridgeRam.Length))
        {
            _cartridgeRam[address - Vic20MemoryMap.CartridgeStart] = value;
            return;
        }

        if (InRange(address, Vic20MemoryMap.VicBaseAddress, _vic.Length))
            _vic.Write(address, value);
        else if (InRange(address, Vic20MemoryMap.Via1BaseAddress, _via1.Length))
            _via1.Write(address, value);
        else if (InRange(address, Vic20MemoryMap.Via2BaseAddress, _via2.Length))
            _via2.Write(address, value);
        else if (InRange(address, Vic20MemoryMap.ColorRamStart, _colorRam.Length))
            _colorRam.Write(address, value);
        // else: unmapped - write has no effect (open bus), including the $A000-$BFFF cartridge
        // window and unexpanded blocks $2000-$7FFF/$A000-$BFFF - no cartridge support in v1.
    }

    /// <summary>Zeroes both RAM regions.</summary>
    public void ClearRam()
    {
        Array.Clear(_zeroPageRam);
        Array.Clear(_builtinRam);
        if (_block0 is not null) Array.Clear(_block0);
        if (_block1 is not null) Array.Clear(_block1);
        if (_block2 is not null) Array.Clear(_block2);
        if (_block3 is not null) Array.Clear(_block3);
        if (_cartridgeRam is not null) Array.Clear(_cartridgeRam);
    }

    private static bool IsEnabled(Vic20ExpansionPreset preset, int block) => preset switch
    {
        Vic20ExpansionPreset.ThreeK => block == 0,
        Vic20ExpansionPreset.EightK => block == 1,
        Vic20ExpansionPreset.SixteenK => block is 1 or 2,
        Vic20ExpansionPreset.TwentyFourK => block is 1 or 2 or 3,
        Vic20ExpansionPreset.All => true,
        _ => false,
    };

    private static byte[] Find(IReadOnlyList<Vic20RomImage> roms, ushort address)
    {
        foreach (var rom in roms)
            if (rom.Requirement.Address == address)
                return rom.Data;
        throw new InvalidOperationException($"No ROM image loaded for ${address:X4}.");
    }

    private static bool InRange(ushort address, ushort baseAddress, uint length) => address >= baseAddress && address < baseAddress + length;
}
