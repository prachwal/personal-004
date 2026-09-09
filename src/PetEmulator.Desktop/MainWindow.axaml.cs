using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    // File pickers need a TopLevel (this window), which is why this glue lives here rather than
    // on the ViewModel - MainWindowViewModel.LoadTape/LoadDisk do the actual work once a path is
    // picked. Best-effort: a bad file just logs instead of crashing the render loop, since there's
    // no toast/dialog mechanism in this app yet to surface it in the UI itself.
    private async void OnLoadTapeClick(object? sender, RoutedEventArgs e) =>
        await LoadFileAsync("Load tape", "Tape images", ["*.tap"], _viewModel.LoadTape);

    private async void OnLoadDiskClick(object? sender, RoutedEventArgs e) =>
        await LoadFileAsync("Load disk", "Disk images", ["*.d64"], _viewModel.LoadDisk);

    private async Task LoadFileAsync(string title, string filterName, string[] patterns, Action<string> load)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(filterName) { Patterns = patterns }]
        });
        if (files.Count == 0)
            return;

        try
        {
            load(files[0].Path.LocalPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{title} failed: {ex.Message}");
        }
    }
}
