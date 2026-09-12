using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet;

/// <summary>
/// SuperPET's 6809 view of memory: Waterloo ROM overlays the PET bus at its
/// declared addresses and the existing MOS6551 remains visible at $EFF0-$EFF3.
/// All other accesses fall through to the normal PET bus.
/// </summary>
public sealed class SuperPet6809MemoryBus : IMemoryBus
{
    private const int ExpansionRamSize = SuperPetMemoryMap.BankCount * SuperPetMemoryMap.ExpansionRamWindowLength;
    private readonly IMemoryBus _petBus;
    private readonly IReadOnlyList<PetRomImage> _firmware;
    private readonly MOS6551 _acia;
    private readonly ushort _aciaBaseAddress;
    private readonly MOS6702 _protectionDongle;
    private readonly byte[] _expansionRam = new byte[ExpansionRamSize];
    private byte _selectedBank;

    /// <summary>Optional observer used by startup diagnostics and debugger tooling.</summary>
    public Action<BusAccess>? Observer { get; set; }

    public SuperPet6809MemoryBus(IMemoryBus petBus, IReadOnlyList<PetRomImage> firmware, MOS6551 acia, ushort aciaBaseAddress, MOS6702 protectionDongle)
    {
        _petBus = petBus ?? throw new ArgumentNullException(nameof(petBus));
        _firmware = firmware ?? throw new ArgumentNullException(nameof(firmware));
        _acia = acia ?? throw new ArgumentNullException(nameof(acia));
        _aciaBaseAddress = aciaBaseAddress;
        _protectionDongle = protectionDongle ?? throw new ArgumentNullException(nameof(protectionDongle));
    }

    /// <summary>Currently selected 4 KiB SuperPET expansion-RAM bank.</summary>
    public byte SelectedBank => _selectedBank;

    public void Reset()
    {
        Array.Clear(_expansionRam);
        _selectedBank = 0;
        _protectionDongle.Reset();
    }

    public byte Read(ushort address)
    {
        byte value;
        if (address == SuperPetMemoryMap.BankSelectRegister)
            value = _selectedBank;
        else if (InRange(address, SuperPetMemoryMap.ProtectionDongleBaseAddress, SuperPetMemoryMap.ProtectionDongleLength))
            value = _protectionDongle.Read(address);
        else if (InRange(address, _aciaBaseAddress, _acia.Length))
            value = _acia.Read(address);
        else if (InRange(address, SuperPetMemoryMap.ExpansionRamWindow, SuperPetMemoryMap.ExpansionRamWindowLength))
            value = _expansionRam[ExpansionOffset(address)];
        else
            value = TryFindFirmware(address, out var image)
                ? image.Data[address - image.Requirement.Address]
                : _petBus.Read(address);

        Observer?.Invoke(new BusAccess(IsWrite: false, address, value));
        return value;
    }

    public void Write(ushort address, byte value)
    {
        if (address == SuperPetMemoryMap.BankSelectRegister)
        {
            _selectedBank = (byte)(value & (SuperPetMemoryMap.BankCount - 1));
            Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
            return;
        }
        if (InRange(address, SuperPetMemoryMap.ProtectionDongleBaseAddress, SuperPetMemoryMap.ProtectionDongleLength))
        {
            _protectionDongle.Write(address, value);
            Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
            return;
        }
        if (InRange(address, _aciaBaseAddress, _acia.Length))
        {
            _acia.Write(address, value);
            Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
            return;
        }

        if (InRange(address, SuperPetMemoryMap.ExpansionRamWindow, SuperPetMemoryMap.ExpansionRamWindowLength))
        {
            _expansionRam[ExpansionOffset(address)] = value;
            Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
            return;
        }

        if (TryFindFirmware(address, out _))
        {
            Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
            return;
        }

        _petBus.Write(address, value);
        Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
    }

    private bool TryFindFirmware(ushort address, out PetRomImage image)
    {
        foreach (var candidate in _firmware)
        {
            if (address >= candidate.Requirement.Address &&
                address < candidate.Requirement.Address + candidate.Requirement.Length)
            {
                image = candidate;
                return true;
            }
        }

        image = null!;
        return false;
    }

    private static bool InRange(ushort address, ushort baseAddress, uint length) =>
        address >= baseAddress && address < baseAddress + length;

    private int ExpansionOffset(ushort address) =>
        (_selectedBank * SuperPetMemoryMap.ExpansionRamWindowLength) +
        (address - SuperPetMemoryMap.ExpansionRamWindow);
}
