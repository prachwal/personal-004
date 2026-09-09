using Avalonia.Controls;
using Avalonia.Input;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.GeometryChanged += (_, _) => SetScreenSize();
        _viewModel.FrameReady += (_, _) => Screen.UpdateFrame(_viewModel.FrameBuffer);
        _viewModel.CloseRequested += (_, _) => Close();

        SetScreenSize();

        // Some window managers (WSLg included) don't hand a new top-level window OS keyboard
        // focus on their own - focusing the Screen control (an Avalonia-internal focus scope) is
        // meaningless until the WINDOW itself actually has it, so grab both, and re-grab on every
        // activation since a WM can silently drop it again (alt-tab away and back, etc).
        Loaded += (_, _) => { Focus(); Screen.Focus(); };
        Activated += (_, _) => { Focus(); Screen.Focus(); };
        Closed += (_, _) => _viewModel.Dispose();
    }

    private void SetScreenSize()
    {
        var (parWidth, parHeight) = _viewModel.PixelAspect;
        Screen.SetSize(_viewModel.PixelWidth, _viewModel.PixelHeight, parWidth, parHeight);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e) => _viewModel.HandleKey(e.Key, HostKeyEventKind.Press);

    private void OnKeyUp(object? sender, KeyEventArgs e) => _viewModel.HandleKey(e.Key, HostKeyEventKind.Release);
}
