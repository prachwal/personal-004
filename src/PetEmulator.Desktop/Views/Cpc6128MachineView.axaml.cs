using Avalonia.Controls;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views;

public partial class Cpc6128MachineView : UserControl
{
    private Cpc6128MachineViewModel? _wired;
    public Cpc6128MachineView() { InitializeComponent(); DataContextChanged += (_, _) => Rewire(); }
    private void Rewire()
    {
        if (_wired is not null) _wired.FrameReady -= OnFrameReady;
        _wired = DataContext as Cpc6128MachineViewModel;
        if (_wired is null) return;
        _wired.FrameReady += OnFrameReady;
        Screen.SetSize(_wired.PixelWidth, _wired.PixelHeight, 1, 1);
        Screen.UpdateFrame(_wired.FrameBuffer);
        Screen.Focus();
    }
    private void OnFrameReady(object? sender, EventArgs e) => Screen.UpdateFrame(_wired!.FrameBuffer);
}
