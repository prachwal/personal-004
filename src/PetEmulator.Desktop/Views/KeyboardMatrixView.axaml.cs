using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views;

public partial class KeyboardMatrixView : UserControl
{
    public KeyboardMatrixView() => InitializeComponent();

    private void OnCellClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { DataContext: KeyboardCell cell } && DataContext is KeyboardMatrixViewModel viewModel)
            viewModel.ToggleCell(cell);
    }
}
