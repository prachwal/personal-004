using Avalonia.Media;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Bindable state for the dedicated primary disk-drive indicator.</summary>
public interface IDiskDriveViewModel
{
    void LoadDisk(string path);

    bool DiskLoaded { get; }

    bool DiskBusy { get; }

    IBrush DiskIconBrush { get; }
}
