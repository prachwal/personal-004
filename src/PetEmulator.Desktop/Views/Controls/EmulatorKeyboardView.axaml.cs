using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Ported from personal-002 (Terminal.Avalonia.Controls). Renders an
/// <see cref="EmulatorKeyboardLayout"/> as a canvas of <see cref="EmulatorKeyButton"/>s scaled to
/// fit via <see cref="Viewbox"/>, and drives an <see cref="EmulatorKeyboardState"/> from their
/// press/release events.</summary>
public sealed partial class EmulatorKeyboardView : UserControl
{
    private EmulatorKeyboardState? _state;
    private readonly List<EmulatorKeyButton> _buttons = [];

    public EmulatorKeyboardView()
    {
        InitializeComponent();
        DetachedFromVisualTree += (_, _) => ReleaseAll();
    }

    public IReadOnlySet<string> PressedKeyIds => _state?.PressedKeyIds ?? new HashSet<string>();

    public void Configure(EmulatorKeyboardLayout layout, Action<string, bool> setKeyState)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(setKeyState);
        ReleaseAll();
        KeyboardCanvas.Children.Clear();
        _buttons.Clear();
        KeyboardCanvas.Width = layout.DesignSize.Width;
        KeyboardCanvas.Height = layout.DesignSize.Height;
        _state = new EmulatorKeyboardState(layout, setKeyState);
        foreach (EmulatorKeyDefinition key in layout.Keys) AddKey(key);
    }

    public void ReleaseAll()
    {
        _state?.ReleaseAll();
        foreach (EmulatorKeyButton button in _buttons) button.Release();
    }

    private void AddKey(EmulatorKeyDefinition key)
    {
        var button = new EmulatorKeyButton { Width = key.Bounds.Width, Height = key.Bounds.Height, Content = KeyContent(key) };
        if (key.Accent is not null) button.Classes.Add(key.Accent);
        AutomationProperties.SetName(button, key.AutomationName ?? key.Id);
        ToolTip.SetTip(button, key.ToolTip ?? string.Join(", ", key.HostAliases ?? []));
        Canvas.SetLeft(button, key.Bounds.X);
        Canvas.SetTop(button, key.Bounds.Y);
        button.Pressed += (_, _) => _state?.Press(key.Id);
        button.Released += (_, _) => _state?.ReleaseMomentary(key.Id);
        KeyboardCanvas.Children.Add(button);
        _buttons.Add(button);
    }

    private static Control KeyContent(EmulatorKeyDefinition key)
    {
        var canvas = new Canvas();
        foreach (EmulatorKeyLegend legend in key.Legends)
        {
            var text = new TextBlock { Text = legend.Text, FontSize = legend.FontSize };
            if (legend.Color is not null) text.Foreground = SolidColorBrush.Parse(legend.Color);
            Canvas.SetLeft(text, legend.Anchor.X - key.Bounds.X);
            Canvas.SetTop(text, legend.Anchor.Y - key.Bounds.Y);
            canvas.Children.Add(text);
        }
        return canvas;
    }
}
