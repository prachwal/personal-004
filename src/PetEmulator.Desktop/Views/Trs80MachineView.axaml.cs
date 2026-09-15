using Avalonia.Controls;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views;

public partial class Trs80MachineView : UserControl
{
    private Trs80MachineViewModel? _wired;
    public Trs80MachineView() { InitializeComponent(); DataContextChanged += (_, _) => Rewire(); Loaded += (_, _) => Screen.Focus(); }
    private void Rewire() { if (_wired is not null) _wired.FrameReady -= OnFrameReady; _wired = DataContext as Trs80MachineViewModel; if (_wired is null) return; _wired.FrameReady += OnFrameReady; Screen.SetSize(_wired.PixelWidth, _wired.PixelHeight, _wired.PixelAspect.Width, _wired.PixelAspect.Height); Screen.UpdateFrame(_wired.FrameBuffer); Screen.Focus(); }
    private void OnFrameReady(object? sender, EventArgs e) => Screen.UpdateFrame(_wired!.FrameBuffer);
}
