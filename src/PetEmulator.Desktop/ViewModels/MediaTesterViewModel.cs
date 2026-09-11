using System.Collections.ObjectModel;
using Avalonia.Media;
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

    [ObservableProperty]
    private IReadOnlyList<byte> _previewBytes = Array.Empty<byte>();

    [ObservableProperty]
    private ushort _previewStartAddress;

    [ObservableProperty]
    private MediaDirectoryEntry? _selectedEntry;

    [ObservableProperty]
    private string _previewTitle = "No file selected";

    [ObservableProperty]
    private string _startAddress = "Start address: —";

    public string DiskTitle => _disk is null
        ? "Disk: —"
        : $"Disk: {_disk.DiskName.Trim()} / {_disk.DiskId.Trim()}";

    public MediaTesterViewModel(IFilePickerService filePicker) => _filePicker = filePicker;

    public string WindowTitle => "Media Tester";
    public ObservableCollection<MediaDirectoryEntry> Directory { get; } = [];
    public ObservableCollection<MediaSectorViewModel> SectorMap { get; } = [];

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
            Directory.Add(new MediaDirectoryEntry(entry.Filename, entry.Type.ToString(), entry.SizeInSectors, entry));
        SectorMap.Clear();
        foreach (var sector in _disk.ReadSectorMap())
            SectorMap.Add(new MediaSectorViewModel(sector.Track, sector.Sector, sector.IsAllocated));
        StatusText = $"{Path.GetFileName(path)}  {_disk.DiskName.Trim()} / {_disk.DiskId.Trim()}  " +
            $"{Directory.Count} file(s)";
        OnPropertyChanged(nameof(DiskTitle));
        SelectedEntry = Directory.FirstOrDefault();
    }

    public void PreviewFile(MediaDirectoryEntry entry)
    {
        if (_disk is null)
            return;

        var bytes = _disk.ReadFile(entry.Entry);
        PreviewTitle = entry.Name;
        var isProgram = entry.Type == FileType.Prg.ToString() && bytes.Length >= 2;
        PreviewStartAddress = isProgram ? (ushort)(bytes[0] | bytes[1] << 8) : (ushort)0;
        PreviewBytes = isProgram ? bytes[2..] : bytes;
        StartAddress = isProgram
            ? $"Start address: ${PreviewStartAddress:X4}"
            : $"Start sector: {entry.Entry.StartTrack}/{entry.Entry.StartSector}";

        var selected = _disk.ReadFileSectors(entry.Entry).ToHashSet();
        foreach (var sector in SectorMap)
            sector.IsSelected = selected.Contains(new D64SectorAddress(sector.Track, sector.Sector));

    }

    partial void OnSelectedEntryChanged(MediaDirectoryEntry? value)
    {
        if (value is not null)
            PreviewFile(value);
    }

    public void Dispose() { }
}

public sealed record MediaDirectoryEntry(string Name, string Type, int SizeInSectors, DirEntry Entry);

public sealed partial class MediaSectorViewModel : ObservableObject
{
    public MediaSectorViewModel(int track, int sector, bool isAllocated)
    {
        Track = track;
        Sector = sector;
        IsAllocated = isAllocated;
    }

    public int Track { get; }
    public int Sector { get; }
    public bool IsAllocated { get; }
    public string ToolTip => $"Track {Track}, sector {Sector}";
    public IBrush Brush => IsSelected
        ? Brushes.DodgerBlue
        : IsAllocated ? Brushes.OrangeRed : Brushes.DimGray;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush))]
    private bool _isSelected;
}
