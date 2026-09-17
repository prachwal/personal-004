using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Shared lighter-than-black surround for <c>EmulatorScreenControl</c>: a light frame makes
/// the rendered image bounds visible against the dark window. Replaces the same hardcoded Border
/// repeated in every machine view. Brushes resolve from <c>App.axaml</c> on attach (with
/// hardcoded fallbacks matching it, for previewer/test hosts without application resources).</summary>
public sealed class MachineScreenFrame : Border
{
    public MachineScreenFrame()
    {
        BorderThickness = new Thickness(1);
        Padding = new Thickness(8);
        Background = new SolidColorBrush(Color.Parse("#FF2E2E2E"));
        BorderBrush = new SolidColorBrush(Color.Parse("#FF6A6A6A"));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current?.FindResource("ScreenFrameBackgroundBrush") is IBrush background)
            Background = background;
        if (Application.Current?.FindResource("ScreenFrameBorderBrush") is IBrush border)
            BorderBrush = border;
    }
}
