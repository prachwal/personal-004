using PetEmulator.Core;
using PetEmulator.Chips;
using PetEmulator.Vic20.Roms;
using PetEmulator.Vic20.Cartridge.Abstractions;

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
    private readonly byte[] _charRom;
    private readonly byte[] _basicRom;
    private readonly byte[] _kernalRom;
    private readonly MOS6560 _vic;
    private readonly MOS6522 _via1;
    private readonly MOS6522 _via2;
    private readonly MOS2114 _colorRam;
    private readonly Vic20ExpansionDeviceRegistry _expansions;

    public Vic20MemoryBus(
        IReadOnlyList<Vic20RomImage> roms,
        MOS6560 vic,
        MOS6522 via1,
        MOS6522 via2,
        MOS2114 colorRam,
        Vic20Cartridge? cartridge = null)
    {
        ArgumentNullException.ThrowIfNull(roms);
        _vic = vic ?? throw new ArgumentNullException(nameof(vic));
        _via1 = via1 ?? throw new ArgumentNullException(nameof(via1));
        _via2 = via2 ?? throw new ArgumentNullException(nameof(via2));
        _colorRam = colorRam ?? throw new ArgumentNullException(nameof(colorRam));

        _charRom = Find(roms, Vic20MemoryMap.CharRomStart);
        _basicRom = Find(roms, Vic20MemoryMap.BasicRomStart);
        _kernalRom = Find(roms, Vic20MemoryMap.KernalRomStart);

        _expansions = new Vic20ExpansionDeviceRegistry([]);
        if (cartridge is not null)
            _expansions.Insert(cartridge);
    }

    public Vic20Cartridge? Cartridge => _expansions.Devices.OfType<Vic20Cartridge>().FirstOrDefault();

    public IReadOnlyList<Vic20Cartridge> Cartridges => _expansions.Devices.OfType<Vic20Cartridge>().ToArray();

    public IReadOnlyList<IVic20ExpansionDevice> ExpansionDevices => _expansions.Devices;

    public bool ExpansionIrq => _expansions.Irq;

    public void InsertCartridge(Vic20Cartridge cartridge)
    {
        ArgumentNullException.ThrowIfNull(cartridge);
        _expansions.Insert(cartridge);
    }

    public void InsertExpansionDevice(IVic20ExpansionDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _expansions.Insert(device);
    }

    public void ResetCartridges()
    {
        _expansions.Reset();
    }

    public void TickCartridges(ulong cycles)
    {
        _expansions.Tick(cycles);
    }

    public void EjectCartridge() => _expansions.EjectAll();

    public void EjectCartridge(Vic20Cartridge cartridge) => _expansions.Eject(cartridge);

    public void EjectExpansionDevice(IVic20ExpansionDevice device) => _expansions.Eject(device);

    public byte Read(ushort address)
    {
        var value = ReadCore(address);
        Observer?.Invoke(new BusAccess(IsWrite: false, address, value));
        return value;
    }

    private byte ReadCore(ushort address)
    {
        if (_expansions.TryRead(address, out var cartridgeValue))
            return cartridgeValue;
        if (address < Vic20MemoryMap.ZeroPageRamSize)
            return _zeroPageRam[address];
        if (InRange(address, Vic20MemoryMap.BuiltinRamStart, (uint)_builtinRam.Length))
            return _builtinRam[address - Vic20MemoryMap.BuiltinRamStart];
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
        if (_expansions.TryWrite(address, value))
            return;
        if (address < Vic20MemoryMap.ZeroPageRamSize)
        {
            _zeroPageRam[address] = value;
            return;
        }
        if (InRange(address, Vic20MemoryMap.BuiltinRamStart, (uint)_builtinRam.Length))
        {
            _builtinRam[address - Vic20MemoryMap.BuiltinRamStart] = value;
            return;
        }
        // ROM is read-only: writes silently dropped, matching real hardware.
        if (InRange(address, Vic20MemoryMap.CharRomStart, (uint)_charRom.Length)
            || InRange(address, Vic20MemoryMap.BasicRomStart, (uint)_basicRom.Length)
            || InRange(address, Vic20MemoryMap.KernalRomStart, (uint)_kernalRom.Length))
            return;

        if (InRange(address, Vic20MemoryMap.VicBaseAddress, _vic.Length))
            _vic.Write(address, value);
        else if (InRange(address, Vic20MemoryMap.Via1BaseAddress, _via1.Length))
            _via1.Write(address, value);
        else if (InRange(address, Vic20MemoryMap.Via2BaseAddress, _via2.Length))
            _via2.Write(address, value);
        else if (InRange(address, Vic20MemoryMap.ColorRamStart, _colorRam.Length))
            _colorRam.Write(address, value);
        // else: unmapped - write has no effect (open bus).
    }

    /// <summary>Zeroes both RAM regions.</summary>
    public void ClearRam()
    {
        Array.Clear(_zeroPageRam);
        Array.Clear(_builtinRam);
    }

    public Vic20MemorySnapshot CaptureState() => new()
    {
        ZeroPageRam = _zeroPageRam.ToArray(),
        BuiltinRam = _builtinRam.ToArray()
    };

    public void RestoreState(Vic20MemorySnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.ZeroPageRam.Length != _zeroPageRam.Length || state.BuiltinRam.Length != _builtinRam.Length)
            throw new ArgumentException("Invalid VIC-20 memory snapshot dimensions.", nameof(state));
        state.ZeroPageRam.CopyTo(_zeroPageRam, 0);
        state.BuiltinRam.CopyTo(_builtinRam, 0);
    }

    private static byte[] Find(IReadOnlyList<Vic20RomImage> roms, ushort address)
    {
        foreach (var rom in roms)
            if (rom.Requirement.Address == address)
                return rom.Data;
        throw new InvalidOperationException($"No ROM image loaded for ${address:X4}.");
    }

    private static bool InRange(ushort address, ushort baseAddress, uint length) => address >= baseAddress && address < baseAddress + length;
}
