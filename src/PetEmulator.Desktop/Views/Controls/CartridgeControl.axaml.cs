using Avalonia.Controls;
using Avalonia.Input;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Desktop.Views;

namespace PetEmulator.Desktop.Views.Controls;

public partial class CartridgeControl : UserControl
{
    public CartridgeControl() => InitializeComponent();

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button || DataContext is not Vic20MachineViewModel machine)
            return;

        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var dialog = new CartridgeMountWindow
        {
            DataContext = new CartridgeMountViewModel(machine, new AvaloniaFilePickerService(owner)),
        };
        await dialog.ShowDialog(owner);
    }
}
