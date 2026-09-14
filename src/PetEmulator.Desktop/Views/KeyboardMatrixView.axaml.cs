using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Desktop.Views.Controls;

namespace PetEmulator.Desktop.Views;

public partial class KeyboardMatrixView : UserControl
{
    private string? _configuredFor;

    public KeyboardMatrixView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ConfigureKeyboard();
            if (DataContext is KeyboardMatrixViewModel viewModel)
                viewModel.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(KeyboardMatrixViewModel.SelectedMachine))
                        ConfigureKeyboard();
                };
        };
    }

    private void OnCellClick(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { DataContext: KeyboardCell cell } && DataContext is KeyboardMatrixViewModel viewModel)
            viewModel.ToggleCell(cell);
    }

    private void ConfigureKeyboard()
    {
        if (DataContext is not KeyboardMatrixViewModel viewModel || viewModel.SelectedMachine == _configuredFor)
            return;
        _configuredFor = viewModel.SelectedMachine;

        if (viewModel.SelectedMachine == "PET")
        {
            MachineKeyboard.Configure(PetGraphicsKeyboardLayoutFactory.Build(), (signal, pressed) =>
            {
                if (PetGraphicsKeyboardLayoutFactory.TryParseSignal(signal, out var row, out var column))
                    viewModel.SetPetMatrixCell(row, column, pressed);
            });
        }
        else
        {
            MachineKeyboard.Configure(Vic20KeyboardLayoutFactory.Build(), (signal, pressed) =>
            {
                if (Vic20KeyboardLayoutFactory.TryParseSignal(signal, out var row, out var column))
                    viewModel.SetVicMatrixCell(row, column, pressed);
            });
        }
    }
}
