using Avalonia.Input;

namespace PetEmulator.Desktop;

/// <summary>
/// Translates an Avalonia <see cref="Key"/> into the host-key vocabulary
/// <c>IPetKeyboardMap</c> implementations expect (<c>"KeyA"</c>, <c>"Digit0"</c>, ...).
/// Written as an explicit switch, not derived from the enum name, per the design brief.
/// </summary>
internal static class KeyMapping
{
    public static string? ToHostKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => "Key" + key,
        >= Key.D0 and <= Key.D9 => "Digit" + (key - Key.D0),
        Key.Space => "Space",
        Key.Enter => "Enter",
        Key.Back => "Backspace",
        Key.LeftShift => "ShiftLeft",
        Key.RightShift => "ShiftRight",
        Key.OemPlus => "OemPlus",
        Key.Add => "NumPadAdd",
        Key.OemMinus => "Minus",
        Key.OemQuotes => "Quote",
        Key.OemComma => "Comma",
        _ => null
    };
}
