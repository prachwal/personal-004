using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Bindable state for the dedicated primary disk-drive indicator
/// (<c>DiskDriveControl</c>): activity LED, eject button and name tooltip.</summary>
public interface IDiskDriveViewModel
{
    void LoadDisk(string path);

    bool DiskLoaded { get; }

    bool DiskBusy { get; }

    IBrush DiskIconBrush { get; }

    /// <summary>Mounted image file name for the widget tooltip - null when no disk is in.</summary>
    string? DiskName { get; }

    /// <summary>Ejects the current disk (drive goes empty, icon goes gray).</summary>
    IRelayCommand EjectDiskCommand { get; }
}
