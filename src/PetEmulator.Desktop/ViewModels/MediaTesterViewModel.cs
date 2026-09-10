using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Desktop.Services;
using PetEmulator.Pet.CbmDos;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class MediaTesterViewModel : ObservableObject, IShellModule
{
    private readonly IFilePickerService _filePicker;
    private D64Image? _disk;

    [ObservableProperty]
    private string _statusText = "Open a D64 disk image";

    public MediaTesterViewModel(IFilePickerService filePicker) => _filePicker = filePicker;

    public string WindowTitle => "Media Tester";
    public ObservableCollection<MediaDirectoryEntry> Directory { get; } = [];

    [RelayCommand]
    private async Task OpenDisk()
    {
        var path = await _filePicker.PickDiskToOpenAsync();
        if (path is not null)
            LoadDisk(path);
    }

    public void LoadDisk(string path)
    {
        _disk = D64Image.Load(path);
        Directory.Clear();
        foreach (var entry in _disk.ReadDirectory())
            Directory.Add(new MediaDirectoryEntry(entry.Filename, entry.Type.ToString(), entry.SizeInSectors));
        StatusText = $"{Path.GetFileName(path)}  {_disk.DiskName.Trim()} / {_disk.DiskId.Trim()}  " +
            $"{Directory.Count} file(s)";
    }

    public void Dispose() { }
}

public sealed record MediaDirectoryEntry(string Name, string Type, int SizeInSectors);
