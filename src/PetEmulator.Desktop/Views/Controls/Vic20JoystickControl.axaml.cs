using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Vic20;

namespace PetEmulator.Desktop.Views.Controls;

public partial class Vic20JoystickControl : UserControl
{
    public Vic20JoystickControl()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble);
        AddHandler(PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Bubble);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        if (e.Source is not Button button || !TryGetInput(button, out var input))
            return;

        if (DataContext is Vic20MachineViewModel machine)
        {
            e.Pointer.Capture(button);
            machine.SetJoystickInput(input, true);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Captured is not Button button || !TryGetInput(button, out var input))
            return;

        if (DataContext is Vic20MachineViewModel machine)
            machine.SetJoystickInput(input, false);

        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        var button = e.Source as Button ?? sender as Button;
        if (button is null || !TryGetInput(button, out var input))
            return;

        if (DataContext is Vic20MachineViewModel machine)
            machine.SetJoystickInput(input, false);
    }

    private static bool TryGetInput(Button button, out Vic20JoystickInput input) =>
        Enum.TryParse(button.Tag?.ToString(), ignoreCase: true, out input);
}
