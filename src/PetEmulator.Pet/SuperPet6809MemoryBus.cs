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
    private readonly IMemoryBus _petBus;
    private readonly IReadOnlyList<PetRomImage> _firmware;
    private readonly MOS6551 _acia;
    private readonly ushort _aciaBaseAddress;

    public SuperPet6809MemoryBus(IMemoryBus petBus, IReadOnlyList<PetRomImage> firmware, MOS6551 acia, ushort aciaBaseAddress)
    {
        _petBus = petBus ?? throw new ArgumentNullException(nameof(petBus));
        _firmware = firmware ?? throw new ArgumentNullException(nameof(firmware));
        _acia = acia ?? throw new ArgumentNullException(nameof(acia));
        _aciaBaseAddress = aciaBaseAddress;
    }

    public byte Read(ushort address)
    {
        if (InRange(address, _aciaBaseAddress, _acia.Length))
            return _acia.Read(address);
        return TryFindFirmware(address, out var image)
            ? image.Data[address - image.Requirement.Address]
            : _petBus.Read(address);
    }

    public void Write(ushort address, byte value)
    {
        if (InRange(address, _aciaBaseAddress, _acia.Length))
        {
            _acia.Write(address, value);
            return;
        }

        if (TryFindFirmware(address, out _))
            return;

        _petBus.Write(address, value);
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
}
