namespace PetEmulator.Desktop.Services;

public interface IFilePickerService
{
    Task<string?> PickTapeToOpenAsync();

    Task<string?> PickDiskToOpenAsync();

    Task<string?> PickDiskToSaveAsync();

    Task<string?> PickCartridgeToOpenAsync();

    Task<string?> PickCartridgePluginToOpenAsync();
}
