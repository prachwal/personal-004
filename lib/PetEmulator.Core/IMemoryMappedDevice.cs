namespace PetEmulator.Core;

/// <summary>A clocked device that also occupies an address range on a machine bus.</summary>
public interface IMemoryMappedDevice : IDevice, IMemoryBus
{
}
