using Avalonia.Media;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Bindable state for the dedicated primary disk-drive indicator.</summary>
public interface IDiskDriveViewModel
{
    bool DiskLoaded { get; }

    bool DiskBusy { get; }

    IBrush DiskIconBrush { get; }
}
