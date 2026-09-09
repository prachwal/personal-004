using Avalonia.Controls;

namespace PetEmulator.Desktop;

/// <summary>The PET's composite screen+device-bar+status View, matched by DataTemplate in
/// MainWindow.axaml purely off <see cref="PetMachineViewModel"/>'s concrete type. Re-wires
/// <see cref="Screen"/> to the current <see cref="PetMachineViewModel"/> on every DataContext
/// change (not just once in the constructor) - the shell swaps
/// <see cref="MainWindowViewModel.CurrentMachine"/> to a brand-new instance on every machine/
/// profile switch, and this View instance can be reused by Avalonia's ContentControl across that
/// swap (same DataTemplate match), so imperative event subscription must track the ACTUAL current
/// DataContext, not whatever it was when this View was first constructed.</summary>
public partial class PetMachineView : UserControl
{
    private PetMachineViewModel? _wired;

    public PetMachineView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Rewire();
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
    }

    private void OnFrameReady(object? sender, EventArgs e) => Screen.UpdateFrame(_wired!.FrameBuffer);

    private void OnGeometryChanged(object? sender, EventArgs e) => SetScreenSize();

    private void SetScreenSize()
    {
        var (parWidth, parHeight) = _wired!.PixelAspect;
        Screen.SetSize(_wired.PixelWidth, _wired.PixelHeight, parWidth, parHeight);
    }
}
