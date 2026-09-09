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

        Loaded += (_, _) => Screen.Focus();
        Activated += (_, _) => Screen.Focus();
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
