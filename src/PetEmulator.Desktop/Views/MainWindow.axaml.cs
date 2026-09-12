using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PetEmulator.Desktop.Input;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(new AvaloniaFilePickerService(this));
        DataContext = _viewModel;

        _viewModel.CloseRequested += (_, _) => Close();

        // Receive machine cursor keys before Menu handles them. Do not forward keys whose
        // original source is a MenuItem: those belong to the application menu itself.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);

        // Some window managers (WSLg included) don't hand a new top-level window OS keyboard
        // focus on their own - focusing MachineHost (an Avalonia-internal focus scope) is
        // meaningless until the WINDOW itself actually has it, so grab both, and re-grab on every
        // activation since a WM can silently drop it again (alt-tab away and back, etc). Screen
        // geometry/frame wiring now lives in PetMachineView/Vic20MachineView (see those classes'
        // Rewire()) - swapped per machine, not owned here.
        Loaded += (_, _) => { Focus(); MachineHost.Focus(); };
        Activated += (_, _) => { Focus(); MachineHost.Focus(); };
        Closed += (_, _) => _viewModel.Dispose();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source is not MenuItem)
            _viewModel.HandleKey(e.Key, HostKeyEventKind.Press);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Source is not MenuItem)
            _viewModel.HandleKey(e.Key, HostKeyEventKind.Release);
    }

}
