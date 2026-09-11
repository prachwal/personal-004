using Avalonia.Controls;
using PetEmulator.Desktop.ViewModels.ChipTests;

namespace PetEmulator.Desktop.Views.Controls;

public partial class ChipVisualControl : UserControl
{
    private IChipVisualViewModel? _wired;

    public ChipVisualControl()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Rewire();
    }

    private void Rewire()
    {
        if (_wired is not null)
            _wired.FrameReady -= OnFrameReady;

        _wired = DataContext as IChipVisualViewModel;
        if (_wired is null)
            return;

        _wired.FrameReady += OnFrameReady;
        var (parWidth, parHeight) = _wired.PixelAspect;
        Screen.SetSize(_wired.PixelWidth, _wired.PixelHeight, parWidth, parHeight);
        Screen.UpdateFrame(_wired.FrameBuffer);
    }

    private void OnFrameReady(object? sender, EventArgs e) => Screen.UpdateFrame(_wired!.FrameBuffer);
}
