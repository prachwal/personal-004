using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20;

/// <summary>Maps equal-sized CRT banks and selects the visible bank through I/O2 $9800.</summary>
internal sealed class Vic20BankedCrt : IVic20CartridgeIoDevice
{
    private const ushort BankRegisterAddress = Vic20MemoryMap.Io2Start;
    private readonly IReadOnlyDictionary<ushort, byte[]> _banks;
    private readonly ushort _romLength;
    private ushort _activeBank;

    public Vic20BankedCrt(IReadOnlyList<Vic20CrtChip> chips)
    {
        ArgumentNullException.ThrowIfNull(chips);
        if (chips.Count < 2)
            throw new InvalidDataException("A banked CRT must contain at least two CHIP packets.");
        if (chips.Any(chip => chip.Data.Count == 0 || chip.Data.Count > Vic20MemoryMap.CartridgeSize))
            throw new InvalidDataException("Banked CRT data must contain 1..8192 bytes per bank.");

        var first = chips[0];
        if (first.Bank != 0 || !FitsCartridge(first.LoadAddress, first.Data.Count))
            throw new InvalidDataException("Banked CRT bank 0 must fit in $A000-$BFFF.");
        if (chips.Any(chip => chip.Bank > byte.MaxValue))
            throw new InvalidDataException("Banked CRT bank numbers must fit in the one-byte bank register.");
        if (chips.Any(chip => chip.LoadAddress != first.LoadAddress || chip.Data.Count != first.Data.Count))
            throw new InvalidDataException("All banked CRT CHIP packets must use the same load address and size.");

        _banks = chips.ToDictionary(chip => chip.Bank, chip => chip.Data.ToArray());
        _romLength = (ushort)first.Data.Count;
        RomStartAddress = first.LoadAddress;
        Resource = new Vic20CartridgeResource(
            "CRT bank register",
            BankRegisterAddress,
            1,
            Vic20CartridgeResourceKind.Io,
            Vic20CartridgeResourceAccess.ReadWrite);
        RomResource = new Vic20CartridgeResource(
            "CRT banked ROM",
            RomStartAddress,
            _romLength,
            Vic20CartridgeResourceKind.Rom,
            Vic20CartridgeResourceAccess.Read);
        Resources = [RomResource, Resource];
        Vic20CartridgeResourceValidator.ThrowIfConflicting(Resources);
    }

    public ushort RomStartAddress { get; }

    public ushort RomLength => _romLength;

    public ushort StartAddress => BankRegisterAddress;

    public ushort Length => 1;

    public Vic20CartridgeResource RomResource { get; }

    public Vic20CartridgeResource Resource { get; }

    public IReadOnlyList<Vic20CartridgeResource> Resources { get; }

    public byte Read(ushort address) => (byte)_activeBank;

    public void Write(ushort address, byte value)
    {
        if (_banks.ContainsKey(value))
            _activeBank = value;
    }

    public bool TryRead(ushort address, out byte value)
    {
        if (address != BankRegisterAddress)
        {
            value = 0;
            return false;
        }

        value = (byte)_activeBank;
        return true;
    }

    public bool TryReadRom(ushort address, out byte value)
    {
        if (!InRange(address, RomStartAddress, _romLength))
        {
            value = 0;
            return false;
        }

        value = _banks[_activeBank][address - RomStartAddress];
        return true;
    }

    public bool TryWrite(ushort address, byte value)
    {
        if (address != BankRegisterAddress)
            return false;

        Write(address, value);
        return true;
    }

    public bool ConsumesRomWrite(ushort address) => InRange(address, RomStartAddress, _romLength);

    public void Reset() => _activeBank = 0;

    public void Tick(ulong cycles)
    {
    }

    private static bool FitsCartridge(ushort startAddress, int length) =>
        startAddress >= Vic20MemoryMap.CartridgeStart
        && startAddress + length <= Vic20MemoryMap.CartridgeStart + Vic20MemoryMap.CartridgeSize;

    private static bool InRange(ushort address, ushort start, ushort length) =>
        address >= start && address < start + length;
}
