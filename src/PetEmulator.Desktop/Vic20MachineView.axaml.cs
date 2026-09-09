using Avalonia.Controls;

namespace PetEmulator.Desktop;

/// <summary>The VIC-20's composite screen+device-bar+status View - mirrors
/// <see cref="PetMachineView"/>'s re-wiring shape exactly (see that class's doc comment for why
/// it's not just constructor-time subscription).</summary>
public partial class Vic20MachineView : UserControl
{
    private Vic20MachineViewModel? _wired;

    public Vic20MachineView()
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

        _wired = DataContext as Vic20MachineViewModel;
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
