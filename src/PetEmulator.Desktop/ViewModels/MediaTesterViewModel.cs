using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.Services.DiskImages;
using PetEmulator.Pet.CbmDos;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class MediaTesterViewModel : ObservableObject, IShellModule
{
    private readonly IFilePickerService _filePicker;
    private readonly IDiskImageProviderRegistry _imageProviders;
    private DiskImageDocument? _image;


    [ObservableProperty]
    private string _statusText = "Open a disk image";

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

    [ObservableProperty]
    private string _fileName = "NEWFILE";

    [ObservableProperty]
    private string _newFileName = "";

    [ObservableProperty]
    private string _fileType = "SEQ";

    [ObservableProperty]
    private byte[] _contentBytes = [];

    [ObservableProperty]
    private string _contentDump = "";

    [ObservableProperty]
    private string _validationError = "";

    [ObservableProperty]
    private string _operationStatus = "";

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private string _sortColumn = "Name";

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private bool _isEditPanelVisible;

    public string DiskTitle => _image?.Title ?? "Disk: —";
    public bool HasImage => _image is not null;
    public bool IsReadOnly => _image?.Operations.IsReadOnly ?? true;
    public bool CanModify => HasImage && !IsReadOnly && string.IsNullOrEmpty(ValidationError);
    public string ContentSummary => $"{ContentBytes.Length} bytes";
    public int TotalSectors => SectorMap.Count;
    public int FreeSectors => SectorMap.Count(sector => !sector.IsAllocated);
    public int UsedSectors => TotalSectors - FreeSectors;
    public int FileCount => Directory.Count;
    public int? DirectoryLimit => _image?.DirectoryLimit;
    public string DiskSummary => HasImage
        ? $"{FreeSectors}/{TotalSectors} blocks free • {FileCount} files • directory {FileCount}/{DirectoryLimit?.ToString() ?? "—"}"
        : "No disk image loaded";

    public MediaTesterViewModel(
        IFilePickerService filePicker,
        IDiskImageProviderRegistry? imageProviders = null)
    {
        _filePicker = filePicker;
        _imageProviders = imageProviders ?? new DiskImageProviderRegistry();
    }

    public string WindowTitle => "Media Tester";
    public ObservableCollection<MediaDirectoryEntry> Directory { get; } = [];
    public ObservableCollection<MediaDirectoryEntry> FilteredDirectory { get; } = [];
    public ObservableCollection<MediaSectorViewModel> SectorMap { get; } = [];

    [RelayCommand]
    private async Task OpenDisk()
    {
        var path = await _filePicker.PickDiskToOpenAsync();
        if (path is null)
            return;
        try
        {
            LoadDisk(path);
        }
        catch (Exception exception)
        {
            ClearImage();
            StatusText = $"Unable to open {Path.GetFileName(path)}";
            OperationStatus = exception.Message;
        }
    }

    [RelayCommand]
    private void NewFile()
    {
        SelectedEntry = null;
        FileName = "NEWFILE";
        NewFileName = "";
        FileType = "SEQ";
        SetContentBytes([]);
        IsEditPanelVisible = true;
    }

    [RelayCommand]
    private void CloseEditor() => IsEditPanelVisible = false;

    [RelayCommand]
    private void SortBy(string? column)
    {
        if (string.IsNullOrWhiteSpace(column))
            return;
        if (SortColumn.Equals(column, StringComparison.OrdinalIgnoreCase))
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        RefreshFilteredDirectory();
    }

    [RelayCommand]
    private async Task ImportFile()
    {
        var path = await _filePicker.PickHostFileToOpenAsync();
        if (path is not null)
            ImportFile(path);
    }

    public void ImportFile(string path)
    {
        try
        {
            EnsureWritable();
            FileName = Path.GetFileName(path).ToUpperInvariant();
            NewFileName = "";
            SetContentBytes(File.ReadAllBytes(path));
            IsEditPanelVisible = true;
            OperationStatus = $"Loaded {Path.GetFileName(path)} for import.";
        }
        catch (Exception exception)
        {
            OperationStatus = exception.Message;
        }
    }

    [RelayCommand]
    private async Task ExportFile()
    {
        if (SelectedEntry is null)
        {
            OperationStatus = "Select a file to export.";
            return;
        }
        var path = await _filePicker.PickHostFileToSaveAsync(SelectedEntry.Name);
        if (path is null)
            return;
        try
        {
            File.WriteAllBytes(path, SelectedEntry.ReadContent());
            OperationStatus = $"Exported {Path.GetFileName(path)}";
        }
        catch (Exception exception)
        {
            OperationStatus = exception.Message;
        }
    }

    public void LoadDisk(string path)
    {
        _image = null;
        Directory.Clear();
        SectorMap.Clear();
        _image = _imageProviders.Open(path);
        RefreshImageView();
        StatusText = _image.Status;
        OnPropertyChanged(nameof(DiskTitle));
        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(IsReadOnly));
        RaiseCanModifyChanged();
        OnPropertyChanged(nameof(DiskSummary));
        OnPropertyChanged(nameof(TotalSectors));
        OnPropertyChanged(nameof(FreeSectors));
        OnPropertyChanged(nameof(UsedSectors));
        OnPropertyChanged(nameof(FileCount));
        OnPropertyChanged(nameof(DirectoryLimit));
        RefreshFilteredDirectory();
        SelectedEntry = Directory.FirstOrDefault(entry => entry.SizeInSectors > 0) ?? Directory.FirstOrDefault();
        if (SelectedEntry is not null)
            PreviewFile(SelectedEntry);
    }

    private void ClearImage()
    {
        _image = null;
        Directory.Clear();
        FilteredDirectory.Clear();
        SectorMap.Clear();
        SelectedEntry = null;
        OnPropertyChanged(nameof(DiskTitle));
        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(IsReadOnly));
        RaiseCanModifyChanged();
        OnPropertyChanged(nameof(DiskSummary));
        OnPropertyChanged(nameof(TotalSectors));
        OnPropertyChanged(nameof(FreeSectors));
        OnPropertyChanged(nameof(UsedSectors));
        OnPropertyChanged(nameof(FileCount));
        OnPropertyChanged(nameof(DirectoryLimit));
    }

    public void PreviewFile(MediaDirectoryEntry entry)
    {
        if (_image is null)
            return;

        var bytes = entry.ReadContent();
        PreviewTitle = entry.Name;
        var isProgram = entry.Type == global::PetEmulator.Pet.CbmDos.FileType.Prg.ToString() && bytes.Length >= 2;
        PreviewStartAddress = isProgram ? (ushort)(bytes[0] | bytes[1] << 8) : (ushort)0;
        PreviewBytes = isProgram ? bytes[2..] : bytes;
        StartAddress = isProgram
            ? $"Start address: ${PreviewStartAddress:X4}"
            : entry.StartLocation is not null
                ? $"Start sector: {entry.StartLocation}"
                : $"{entry.Type} file: {entry.SizeInSectors} sectors";

        var selected = entry.ReadSectors();
        foreach (var sector in SectorMap)
            sector.IsSelected = selected.Contains((sector.Track, sector.Sector));

    }

    /// <summary>Shows the raw bytes of one physical sector (clicked on the sector map) in the same
    /// hex/ASCII preview used for file content - lets you inspect a sector directly, regardless of
    /// which file (if any) owns it.</summary>
    public void PreviewSector(MediaSectorViewModel sector)
    {
        if (_image is null)
            return;
        byte[] bytes;
        try
        {
            bytes = _image.Operations.ReadSector(sector.Track, sector.Sector);
        }
        catch (Exception exception)
        {
            OperationStatus = exception.Message;
            return;
        }
        PreviewTitle = $"Sector {sector.Track}/{sector.Sector}";
        PreviewStartAddress = 0;
        PreviewBytes = bytes;
        StartAddress = $"Track {sector.Track}, sector {sector.Sector} ({(sector.IsAllocated ? "allocated" : "free")})";
    }

    partial void OnSelectedEntryChanged(MediaDirectoryEntry? value)
    {
        if (value is not null)
        {
            FileName = value.Name;
            NewFileName = value.Name;
            SetContentBytes(value.ReadContent());
            IsEditPanelVisible = true;
            PreviewFile(value);
        }
        else
        {
            PreviewTitle = "No file selected";
            PreviewBytes = Array.Empty<byte>();
            SetContentBytes([]);
            StartAddress = "Start address: —";
            foreach (var sector in SectorMap)
                sector.IsSelected = false;
            IsEditPanelVisible = false;
        }
    }

    partial void OnFilterTextChanged(string value) => RefreshFilteredDirectory();

    partial void OnContentDumpChanged(string value)
    {
        if (TryParseHexDump(value, out var bytes, out var error))
        {
            ContentBytes = bytes;
            ValidationError = "";
        }
        else
            ValidationError = error;

        OnPropertyChanged(nameof(ContentSummary));
        OnPropertyChanged(nameof(CanModify));
    }

    partial void OnValidationErrorChanged(string value) => RaiseCanModifyChanged();

    private void RaiseCanModifyChanged()
    {
        OnPropertyChanged(nameof(CanModify));
        CreateFileCommand.NotifyCanExecuteChanged();
        UpdateFileCommand.NotifyCanExecuteChanged();
        DeleteFileCommand.NotifyCanExecuteChanged();
        RenameFileCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private void CreateFile()
    {
        ExecuteMutation(() =>
        {
            EnsureWritable();
            EnsureValidContent();
            _image!.Operations.CreateFile(FileName, ContentBytes, FileType);
        }, $"Created {FileName}");
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private void UpdateFile()
    {
        ExecuteMutation(() =>
        {
            EnsureWritable();
            EnsureValidContent();
            _image!.Operations.UpdateFile(FileName, ContentBytes);
        }, $"Updated {FileName}");
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private void DeleteFile()
    {
        ExecuteMutation(() =>
        {
            EnsureWritable();
            _image!.Operations.DeleteFile(FileName);
        }, $"Deleted {FileName}");
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private void RenameFile()
    {
        ExecuteMutation(() =>
        {
            EnsureWritable();
            _image!.Operations.RenameFile(FileName, NewFileName);
        }, $"Renamed {FileName} to {NewFileName}");
    }

    [RelayCommand]
    private async Task SaveDisk()
    {
        if (_image is null)
        {
            OperationStatus = "No disk image is open.";
            return;
        }

        var path = await _filePicker.PickDiskToSaveAsync();
        if (path is null)
            return;
        try
        {
            _image.Operations.SaveTo(path);
            OperationStatus = $"Saved {Path.GetFileName(path)}";
        }
        catch (Exception exception)
        {
            OperationStatus = exception.Message;
        }
    }

    private void ExecuteMutation(Action mutation, string success)
    {
        try
        {
            mutation();
            var selectedName = FileName;
            RefreshImageView();
            SelectedEntry = Directory.FirstOrDefault(entry => entry.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
            OperationStatus = success;
        }
        catch (Exception exception)
        {
            OperationStatus = exception.Message;
        }
    }

    private void RefreshImageView()
    {
        if (_image is null)
            return;
        Directory.Clear();
        SectorMap.Clear();
        foreach (var file in _image.Operations.ReadFiles())
            Directory.Add(new MediaDirectoryEntry(file.Name, file.Type, file.SizeInSectors,
                file.ReadContent, file.ReadSectors, file.StartLocation));
        foreach (var sector in _image.Operations.ReadSectors())
            SectorMap.Add(new MediaSectorViewModel(sector.Track, sector.Sector, sector.IsAllocated));
        OnPropertyChanged(nameof(DiskSummary));
        OnPropertyChanged(nameof(TotalSectors));
        OnPropertyChanged(nameof(FreeSectors));
        OnPropertyChanged(nameof(UsedSectors));
        OnPropertyChanged(nameof(FileCount));
        RefreshFilteredDirectory();
    }

    private void RefreshFilteredDirectory()
    {
        var query = FilterText.Trim();
        IEnumerable<MediaDirectoryEntry> entries = Directory.Where(entry =>
            query.Length == 0 || entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        entries = SortColumn.ToUpperInvariant() switch
        {
            "TYPE" => SortAscending ? entries.OrderBy(entry => entry.Type).ThenBy(entry => entry.Name)
                : entries.OrderByDescending(entry => entry.Type).ThenBy(entry => entry.Name),
            "SIZE" => SortAscending ? entries.OrderBy(entry => entry.SizeInSectors).ThenBy(entry => entry.Name)
                : entries.OrderByDescending(entry => entry.SizeInSectors).ThenBy(entry => entry.Name),
            _ => SortAscending ? entries.OrderBy(entry => entry.Name)
                : entries.OrderByDescending(entry => entry.Name)
        };
        FilteredDirectory.Clear();
        foreach (var entry in entries)
            FilteredDirectory.Add(entry);
    }

    public static string FormatHexDump(IReadOnlyList<byte> bytes)
    {
        var lines = new List<string>((bytes.Count + 15) / 16);
        for (var offset = 0; offset < bytes.Count; offset += 16)
        {
            var count = Math.Min(16, bytes.Count - offset);
            var values = bytes.Skip(offset).Take(count).ToArray();
            var hex = string.Join(' ', values.Select(value => value.ToString("X2"))).PadRight(47);
            var ascii = new string(values.Select(ToAscii).ToArray()).PadRight(16);
            lines.Add($"{offset:X4}  {hex}  |{ascii}|");
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static bool TryParseHexDump(string value, out byte[] bytes, out string error)
    {
        var parsed = new List<byte>();
        var lineNumber = 0;
        foreach (var rawLine in value.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (line.Length < 4 || !int.TryParse(line[..4], System.Globalization.NumberStyles.HexNumber, null, out _))
            {
                bytes = [];
                error = $"Line {lineNumber}: invalid offset; expected four hex digits.";
                return false;
            }

            var content = line[4..];
            var separator = content.IndexOf('|');
            if (separator >= 0)
                content = content[..separator];
            var tokens = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Length != 2 || !byte.TryParse(token, System.Globalization.NumberStyles.HexNumber, null, out var parsedByte))
                {
                    bytes = [];
                    error = $"Line {lineNumber}: invalid byte '{token}'.";
                    return false;
                }
                parsed.Add(parsedByte);
            }
            if (tokens.Length > 16)
            {
                bytes = [];
                error = $"Line {lineNumber}: maximum is 16 bytes.";
                return false;
            }
        }
        bytes = parsed.ToArray();
        error = "";
        return true;
    }

    private static char ToAscii(byte value) => value is >= 0x20 and <= 0x7E ? (char)value : '.';

    private void SetContentBytes(IReadOnlyList<byte> bytes)
    {
        ContentBytes = bytes.ToArray();
        ContentDump = FormatHexDump(ContentBytes);
        ValidationError = "";
        OnPropertyChanged(nameof(ContentSummary));
    }

    private void EnsureValidContent()
    {
        if (!string.IsNullOrEmpty(ValidationError))
            throw new InvalidOperationException(ValidationError);
    }

    private void EnsureWritable()
    {
        if (_image is null)
            throw new InvalidOperationException("No disk image is open.");
        if (_image.Operations.IsReadOnly)
            throw new InvalidOperationException("The disk image is read-only.");
    }

    public void Dispose() { }

}

public sealed record MediaDirectoryEntry(
    string Name,
    string Type,
    int SizeInSectors,
    Func<byte[]> ReadContent,
    Func<IReadOnlySet<(int Track, int Sector)>> ReadSectors,
    string? StartLocation)
{
    public string TypeGlyph => Type.ToUpperInvariant() switch
    {
        "PRG" => "▶",
        "SEQ" => "▤",
        "USR" => "◆",
        "REL" => "▦",
        "CP/M" => "▥",
        _ => "•"
    };
}

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
