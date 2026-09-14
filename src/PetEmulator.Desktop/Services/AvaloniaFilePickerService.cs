using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace PetEmulator.Desktop.Services;

public sealed class AvaloniaFilePickerService(Window window) : IFilePickerService
{
    public async Task<string?> PickTapeToOpenAsync()
    {
        return await PickOpenFileAsync("Load tape", "Tape images", ["*.tap"]);
    }

    public async Task<string?> PickDiskToOpenAsync()
    {
        return await PickOpenFileAsync("Load disk", "Disk images", ["*.d64", "*.dsk", "*.td0", "*.jv1", "*.dmk"]);
    }

    public async Task<string?> PickDiskToSaveAsync()
    {
        IStorageFile? file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "New disk",
            SuggestedFileName = "New Disk.d64",
            FileTypeChoices = [new FilePickerFileType("Disk images") { Patterns = ["*.d64"] }]
        });
        return file?.Path.LocalPath;
    }

    public async Task<string?> PickHostFileToOpenAsync() =>
        await PickOpenFileAsync("Import file", "All files", ["*.*"]);

    public async Task<string?> PickHostFileToSaveAsync(string suggestedFileName)
    {
        IStorageFile? file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export file",
            SuggestedFileName = suggestedFileName
        });
        return file?.Path.LocalPath;
    }

    public async Task<string?> PickCartridgeToOpenAsync() =>
        await PickOpenFileAsync("Load VIC-20 cartridge", "VIC-20 cartridge images", ["*.bin", "*.crt", "*.prg"]);

    public async Task<string?> PickCartridgePluginToOpenAsync() =>
        await PickOpenFileAsync("Load VIC-20 cartridge plugin", "Cartridge plugins", ["*.dll"]);

    private async Task<string?> PickOpenFileAsync(string title, string filterName, IReadOnlyList<string> patterns)
    {
        IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(filterName) { Patterns = patterns }]
        });
        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }
}
