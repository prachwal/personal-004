using PetEmulator.Core;

namespace PetEmulator.Pet.Devices;

/// <summary>Live <see cref="IDeviceStatus"/> view of one mounted IEEE-488 disk drive (see
/// <see cref="PetMachine.MountDisk"/>) - one instance per mounted drive, so a GUI can show
/// "Drive 8: game.d64", "Drive 9: data.d64" side by side once a profile has more than one
/// attached.</summary>
public sealed class PetIeeeDriveStatus(int deviceNumber, string diskName) : IDeviceStatus
{
    public string Id => $"ieee488:{deviceNumber}";

    public string Icon => "💾";

    public string DisplayName => $"Drive {deviceNumber}";

    public string StatusText => diskName;
}
