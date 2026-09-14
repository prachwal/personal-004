using Avalonia.Controls;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Desktop.Views.Controls;

namespace PetEmulator.Desktop.Views;

public partial class KayproMachineView : UserControl
{
    private KayproMachineViewModel? _wired;

    public KayproMachineView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Rewire();
        Loaded += (_, _) => Screen.Focus();
    }

    private void Rewire()
    {
        if (_wired is { } old)
        {
            old.FrameReady -= OnFrameReady;
            old.GeometryChanged -= OnGeometryChanged;
        }

        _wired = DataContext as KayproMachineViewModel;
        if (_wired is null)
            return;

        _wired.FrameReady += OnFrameReady;
        _wired.GeometryChanged += OnGeometryChanged;
        SetScreenSize();
        Screen.Focus();

        var machine = _wired;
        OnScreenKeyboard.Configure(KayproKeyboardLayoutFactory.Build(), (signal, pressed) =>
        {
            if (pressed && KayproKeyboardLayoutFactory.TryParseSignal(signal, out var value))
                machine.SendKeyboardByte(value);
        });
    }

    private void OnFrameReady(object? sender, EventArgs e) => Screen.UpdateFrame(_wired!.FrameBuffer);
    private void OnGeometryChanged(object? sender, EventArgs e) => SetScreenSize();

    private void SetScreenSize()
    {
        var (width, height) = _wired!.PixelAspect;
        Screen.SetSize(_wired.PixelWidth, _wired.PixelHeight, width, height);
    }
}
