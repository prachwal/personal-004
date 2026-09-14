using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Ported from personal-002. A keycap that stays pressed while ANY pointer holds it down
/// (tracks pointer IDs, not just the last one - real multi-touch chording) or while Space/Enter is
/// held after receiving keyboard focus, and releases cleanly on capture loss (e.g. dragging off the
/// window) instead of getting stuck down.</summary>
public sealed class EmulatorKeyButton : Button
{
    public static readonly StyledProperty<ICommand?> PressCommandProperty =
        AvaloniaProperty.Register<EmulatorKeyButton, ICommand?>(nameof(PressCommand));
    public static readonly StyledProperty<ICommand?> ReleaseCommandProperty =
        AvaloniaProperty.Register<EmulatorKeyButton, ICommand?>(nameof(ReleaseCommand));

    private readonly HashSet<int> _pointers = [];
    private bool _keyboardPressed;
    private bool _pressed;

    public ICommand? PressCommand { get => GetValue(PressCommandProperty); set => SetValue(PressCommandProperty, value); }
    public ICommand? ReleaseCommand { get => GetValue(ReleaseCommandProperty); set => SetValue(ReleaseCommandProperty, value); }
    public event EventHandler? Pressed;
    public event EventHandler? Released;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pointers.Add(e.Pointer.Id);
        Press();
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _pointers.Remove(e.Pointer.Id);
        ReleaseIfIdle();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pointers.Remove(e.Pointer.Id);
        ReleaseIfIdle();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Space or Key.Enter)
        {
            _keyboardPressed = true;
            Press();
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key is Key.Space or Key.Enter)
        {
            _keyboardPressed = false;
            ReleaseIfIdle();
            e.Handled = true;
        }
    }

    public void Press()
    {
        if (_pressed) return;
        _pressed = true;
        Pressed?.Invoke(this, EventArgs.Empty);
        if (PressCommand?.CanExecute(CommandParameter) == true) PressCommand.Execute(CommandParameter);
    }

    public void Release()
    {
        _pointers.Clear();
        _keyboardPressed = false;
        if (!_pressed) return;
        _pressed = false;
        Released?.Invoke(this, EventArgs.Empty);
        if (ReleaseCommand?.CanExecute(CommandParameter) == true) ReleaseCommand.Execute(CommandParameter);
    }

    private void ReleaseIfIdle()
    {
        if (_pointers.Count == 0 && !_keyboardPressed) Release();
    }
}
