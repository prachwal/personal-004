using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views;

/// <summary>The PET's composite screen+device-bar+status View, matched by DataTemplate in
/// MainWindow.axaml purely off <see cref="PetMachineViewModel"/>'s concrete type. Re-wires
/// <see cref="Screen"/> to the current <see cref="PetMachineViewModel"/> on every DataContext
/// change (not just once in the constructor) - the shell swaps
/// <see cref="MainWindowViewModel.CurrentModule"/> to a brand-new instance on every machine/
/// profile switch, and this View instance can be reused by Avalonia's ContentControl across that
/// swap (same DataTemplate match), so imperative event subscription must track the ACTUAL current
/// DataContext, not whatever it was when this View was first constructed.</summary>
public partial class PetMachineView : UserControl
{
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("View");
    private PetMachineViewModel? _wired;

    public PetMachineView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Rewire();
        // MainWindow only focuses the outer ContentControl hosting this View (see its own
        // comment) - that's OS-level/focus-scope grabbing, not a substitute for the actual leaf
        // control holding keyboard focus. Screen (Focusable="True" in XAML) must be focused
        // directly, both on first load and on every DataContext change (switching machine/profile
        // can reuse this same View instance without a Loaded re-fire).
        Loaded += (_, _) => Screen.Focus();
    }

    private void Rewire()
    {
        if (_wired is { } old)
        {
            old.FrameReady -= OnFrameReady;
            old.GeometryChanged -= OnGeometryChanged;
        }

        _wired = DataContext as PetMachineViewModel;
        if (_wired is null)
            return;

        _wired.FrameReady += OnFrameReady;
        _wired.GeometryChanged += OnGeometryChanged;
        SetScreenSize();
        Screen.Focus();
        var (parWidth, parHeight) = _wired.PixelAspect;
        Log.LogInformation("PetMachineView wired to {Title} ({Width}x{Height} par={ParWidth}:{ParHeight}).",
            _wired.WindowTitle, _wired.PixelWidth, _wired.PixelHeight, parWidth, parHeight);
    }

    private void OnFrameReady(object? sender, EventArgs e) => Screen.UpdateFrame(_wired!.FrameBuffer);

    private void OnGeometryChanged(object? sender, EventArgs e) => SetScreenSize();

    private void SetScreenSize()
    {
        var (parWidth, parHeight) = _wired!.PixelAspect;
        Screen.SetSize(_wired.PixelWidth, _wired.PixelHeight, parWidth, parHeight);
    }
}
