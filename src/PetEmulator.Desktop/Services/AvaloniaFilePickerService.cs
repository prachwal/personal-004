using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace PetEmulator.Desktop.Services;

public sealed class AvaloniaFilePickerService(Window window) : IFilePickerService
{
    public async Task<string?> PickTapeToOpenAsync()
    {
        return await PickOpenFileAsync("Load tape", "Tape images", "*.tap");
    }

    public async Task<string?> PickDiskToOpenAsync()
    {
        return await PickOpenFileAsync("Load disk", "Disk images", "*.d64");
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

    private async Task<string?> PickOpenFileAsync(string title, string filterName, string pattern)
    {
        IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(filterName) { Patterns = [pattern] }]
        });
        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }
}
