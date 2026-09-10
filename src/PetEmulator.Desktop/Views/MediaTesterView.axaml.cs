using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views;

public partial class MediaTesterView : UserControl
{
    public MediaTesterView() => InitializeComponent();

    private void OnEntrySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: MediaDirectoryEntry entry } && DataContext is MediaTesterViewModel tester)
            tester.PreviewFile(entry);
    }
}
