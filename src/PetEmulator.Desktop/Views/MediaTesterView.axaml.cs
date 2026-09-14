using Avalonia.Controls;
using Avalonia.Input;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views;

public partial class MediaTesterView : UserControl
{
    public MediaTesterView() => InitializeComponent();

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var file = e.DataTransfer.TryGetFiles()?.FirstOrDefault();
        if (file is not null && DataContext is MediaTesterViewModel viewModel)
            viewModel.ImportFile(file.Path.LocalPath);
    }

    private void OnDirectoryPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed ||
            DataContext is not MediaTesterViewModel viewModel ||
            sender is not Control { DataContext: MediaDirectoryEntry entry })
            return;

        viewModel.SelectedEntry = entry;
    }

    private void OnSectorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MediaTesterViewModel viewModel &&
            sender is Control { DataContext: MediaSectorViewModel sector })
            viewModel.PreviewSector(sector);
    }
}
