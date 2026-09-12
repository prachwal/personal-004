using Avalonia.Controls;

namespace PetEmulator.Desktop.Views;

public partial class CartridgeMountWindow : Window
{
    public CartridgeMountWindow() => InitializeComponent();

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
