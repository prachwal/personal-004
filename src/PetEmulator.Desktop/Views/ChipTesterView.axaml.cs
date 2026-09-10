using Avalonia.Controls;
using Avalonia.Interactivity;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views;

public partial class ChipTesterView : UserControl
{
    public ChipTesterView() => InitializeComponent();

    private void OnPinToggled(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch { DataContext: PinRow pin, IsChecked: var level } &&
            DataContext is ChipTesterViewModel tester && pin.IsWritable)
            tester.SelectedScenario?.SetPin(pin.Name, level == true);
    }
}
