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
        // NOT `key is >= AtKeyboardKey.A and <= AtKeyboardKey.Z` - the same class of bug as
        // ToAtKeyboardKey's arithmetic below: AtKeyboardKey's A-Z aren't ordinally contiguous
        // (declared in QWERTY-row order, interspersed with punctuation/Enter/Shift), so that
        // range only happens to contain A/D/F/G/H/J/K/L/S/Z - the other 16 letters
        // (B/C/E/I/M/N/O/P/Q/R/T/U/V/W/X/Y) fell through to the switch below, matched nothing,
        // and returned null (a dead key on real PET/VIC-20 keyboards), while the range also
        // wrongly swallowed Enter/LeftShift/OemQuotes/OemSemicolon/OemBackslashIso (their
        // ordinals also happen to fall inside [A,Z]), returning "KeyEnter" etc. instead of ever
        // reaching their real cases below. Verified against the exact enum ordinals before
        // writing this fix (10 letters "worked" by ordinal coincidence, not by correctness).
        if (key is AtKeyboardKey.A or AtKeyboardKey.B or AtKeyboardKey.C or AtKeyboardKey.D
            or AtKeyboardKey.E or AtKeyboardKey.F or AtKeyboardKey.G or AtKeyboardKey.H
            or AtKeyboardKey.I or AtKeyboardKey.J or AtKeyboardKey.K or AtKeyboardKey.L
            or AtKeyboardKey.M or AtKeyboardKey.N or AtKeyboardKey.O or AtKeyboardKey.P
            or AtKeyboardKey.Q or AtKeyboardKey.R or AtKeyboardKey.S or AtKeyboardKey.T
            or AtKeyboardKey.U or AtKeyboardKey.V or AtKeyboardKey.W or AtKeyboardKey.X
            or AtKeyboardKey.Y or AtKeyboardKey.Z)
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
        // NOT `AtKeyboardKey.A + (key - Key.A)` - real bug, found live ("V types ',', Q types
        // 'C', E types 'G', ..."): that arithmetic assumes AtKeyboardKey's A-Z are alphabetically
        // contiguous. They aren't - AtKeyboardKey.cs declares them in QWERTY-row order (Q W E R T
        // Y U I O P / A S D F G H J K L / Z X C V B N M, interspersed with punctuation keys), so
        // an alphabetical offset from Avalonia's Key.A lands on a same-row-position-but-wrong
        // letter (e.g. V is the 22nd letter alphabetically (offset 21); AtKeyboardKey.A's ordinal
        // + 21 lands 21 members further into that QWERTY-ordered block, which is OemComma, not
        // V). Verified against the exact enum ordinals before writing this fix, not guessed.
        // Explicit per-letter mapping (matching identifiers, immune to either enum's declaration
        // order) instead - also matches this class's own doc comment ("explicit switch, not
        // derived from the enum name").
        Key.A => AtKeyboardKey.A, Key.B => AtKeyboardKey.B, Key.C => AtKeyboardKey.C,
        Key.D => AtKeyboardKey.D, Key.E => AtKeyboardKey.E, Key.F => AtKeyboardKey.F,
        Key.G => AtKeyboardKey.G, Key.H => AtKeyboardKey.H, Key.I => AtKeyboardKey.I,
        Key.J => AtKeyboardKey.J, Key.K => AtKeyboardKey.K, Key.L => AtKeyboardKey.L,
        Key.M => AtKeyboardKey.M, Key.N => AtKeyboardKey.N, Key.O => AtKeyboardKey.O,
        Key.P => AtKeyboardKey.P, Key.Q => AtKeyboardKey.Q, Key.R => AtKeyboardKey.R,
        Key.S => AtKeyboardKey.S, Key.T => AtKeyboardKey.T, Key.U => AtKeyboardKey.U,
        Key.V => AtKeyboardKey.V, Key.W => AtKeyboardKey.W, Key.X => AtKeyboardKey.X,
        Key.Y => AtKeyboardKey.Y, Key.Z => AtKeyboardKey.Z,
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
