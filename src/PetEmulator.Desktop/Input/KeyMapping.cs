using Avalonia.Input;
using PetEmulator.Core.Keyboard;

namespace PetEmulator.Desktop.Input;

/// <summary>
/// Translates an Avalonia <see cref="Key"/> into the host-key vocabulary
/// <c>IPetKeyboardMap</c> implementations expect (<c>"KeyA"</c>, <c>"Digit0"</c>, ...).
/// Written as an explicit switch, not derived from the enum name, per the design brief.
/// </summary>
internal static class KeyMapping
{
    public static string? ToHostKey(AtKeyboardKey key)
    {
        if (key is >= AtKeyboardKey.A and <= AtKeyboardKey.Z)
            return "Key" + key;
        if (key is >= AtKeyboardKey.D1 and <= AtKeyboardKey.D9)
            return "Digit" + (key - AtKeyboardKey.D1 + 1);

        return key switch
        {
            AtKeyboardKey.D0 => "Digit0",
            AtKeyboardKey.Space => "Space",
            AtKeyboardKey.Enter => "Enter",
            AtKeyboardKey.Backspace => "Backspace",
            AtKeyboardKey.LeftShift => "ShiftLeft",
            AtKeyboardKey.RightShift => "ShiftRight",
            AtKeyboardKey.OemPlus => "OemPlus",
            AtKeyboardKey.NumPadAdd => "NumPadAdd",
            AtKeyboardKey.OemMinus => "Minus",
            AtKeyboardKey.OemQuotes => "Quote",
            AtKeyboardKey.OemComma => "Comma",
            AtKeyboardKey.OemPeriod => "Period",
            AtKeyboardKey.OemQuestion => "Slash",
            AtKeyboardKey.OemSemicolon => "Semicolon",
            _ => null
        };
    }

    public static AtKeyboardKey? ToAtKeyboardKey(Key key) => key switch
    {
        Key.Escape => AtKeyboardKey.Escape,
        >= Key.F1 and <= Key.F12 => AtKeyboardKey.F1 + (key - Key.F1),
        >= Key.A and <= Key.Z => AtKeyboardKey.A + (key - Key.A),
        >= Key.D0 and <= Key.D9 => AtKeyboardKey.D1 + ((key - Key.D0 + 9) % 10),
        Key.Space => AtKeyboardKey.Space,
        Key.Enter or Key.Return => AtKeyboardKey.Enter,
        Key.Back => AtKeyboardKey.Backspace,
        Key.LeftShift => AtKeyboardKey.LeftShift,
        Key.RightShift => AtKeyboardKey.RightShift,
        Key.LeftCtrl => AtKeyboardKey.LeftCtrl,
        Key.RightCtrl => AtKeyboardKey.RightCtrl,
        Key.LeftAlt => AtKeyboardKey.LeftAlt,
        Key.RightAlt => AtKeyboardKey.RightAlt,
        Key.OemPlus => AtKeyboardKey.OemPlus,
        Key.Add => AtKeyboardKey.NumPadAdd,
        Key.OemMinus => AtKeyboardKey.OemMinus,
        Key.OemQuotes => AtKeyboardKey.OemQuotes,
        Key.OemComma => AtKeyboardKey.OemComma,
        Key.OemPeriod => AtKeyboardKey.OemPeriod,
        Key.OemQuestion => AtKeyboardKey.OemQuestion,
        Key.OemSemicolon => AtKeyboardKey.OemSemicolon,
        Key.Left => AtKeyboardKey.Left,
        Key.Right => AtKeyboardKey.Right,
        Key.Up => AtKeyboardKey.Up,
        Key.Down => AtKeyboardKey.Down,
        Key.Insert => AtKeyboardKey.Insert,
        Key.Delete => AtKeyboardKey.Delete,
        Key.Home => AtKeyboardKey.Home,
        Key.End => AtKeyboardKey.End,
        Key.PageUp => AtKeyboardKey.PageUp,
        Key.PageDown => AtKeyboardKey.PageDown,
        _ => null
    };

    public static string? ToHostKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => "Key" + key,
        >= Key.D0 and <= Key.D9 => "Digit" + (key - Key.D0),
        Key.Space => "Space",
        // Avalonia's Key enum names the physical Enter/Return key "Return" - Key.Enter exists
        // separately (numpad enter on some layouts) and never fires for the main key. Confirmed
        // empirically: a real KeyDown for the main Enter key reports e.Key == Key.Return, not
        // Key.Enter - mapping only Key.Enter silently ate every Enter press.
        Key.Enter or Key.Return => "Enter",
        Key.Back => "Backspace",
        Key.LeftShift => "ShiftLeft",
        Key.RightShift => "ShiftRight",
        Key.OemPlus => "OemPlus",
        Key.Add => "NumPadAdd",
        Key.OemMinus => "Minus",
        Key.OemQuotes => "Quote",
        Key.OemComma => "Comma",
        Key.OemPeriod => "Period",
        Key.OemQuestion => "Slash",
        Key.OemSemicolon => "Semicolon",
        _ => null
    };
}
