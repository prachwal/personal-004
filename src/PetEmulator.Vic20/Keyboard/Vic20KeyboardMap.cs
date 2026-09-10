using PetEmulator.Core.Keyboard;

namespace PetEmulator.Vic20.Keyboard;

/// <summary>Translates a host-key-string event (see PetEmulator.Desktop.KeyMapping - Avalonia
/// Key -> "KeyA"/"Digit5"/"Space"/"Enter"/... - already machine-agnostic despite living in the
/// PET-originated Desktop project) into one <see cref="Vic20KeyboardMatrix"/> cell. Mirrors the
/// shape of PetEmulator.Pet.Keyboard.IPetKeyboardMap's Translate for live GUI key handling
/// (distinct from <see cref="Vic20HostKeyMap"/>, which is char-based for scripted typing only -
/// see that class's doc comment for why they aren't merged).</summary>
public sealed class Vic20KeyboardMap : AtKeyboardMapping
{
    protected override KeyboardMatrixPosition? Map(AtKeyboardKey key) => key switch
    {
        AtKeyboardKey.Enter => new(1, 7),
        AtKeyboardKey.Space => new(4, 0),
        AtKeyboardKey.LeftShift or AtKeyboardKey.RightShift => new(4, 7),
        AtKeyboardKey.LeftCtrl or AtKeyboardKey.RightCtrl => new(7, 7),
        AtKeyboardKey.Escape => new(3, 7),
        AtKeyboardKey.Home => new(0, 7),
        AtKeyboardKey.Backspace or AtKeyboardKey.Delete => new(2, 7),
        AtKeyboardKey.A => new(2, 1), AtKeyboardKey.B => new(4, 3),
        AtKeyboardKey.C => new(4, 2), AtKeyboardKey.D => new(2, 2),
        AtKeyboardKey.E => new(6, 1), AtKeyboardKey.F => new(5, 2),
        AtKeyboardKey.G => new(2, 3), AtKeyboardKey.H => new(5, 3),
        AtKeyboardKey.I => new(1, 4), AtKeyboardKey.J => new(2, 4),
        AtKeyboardKey.K => new(5, 4), AtKeyboardKey.L => new(2, 5),
        AtKeyboardKey.M => new(4, 4), AtKeyboardKey.N => new(3, 4),
        AtKeyboardKey.O => new(6, 4), AtKeyboardKey.P => new(1, 5),
        AtKeyboardKey.Q => new(6, 0), AtKeyboardKey.R => new(1, 2),
        AtKeyboardKey.S => new(5, 1), AtKeyboardKey.T => new(6, 2),
        AtKeyboardKey.U => new(6, 3), AtKeyboardKey.V => new(3, 3),
        AtKeyboardKey.W => new(1, 1), AtKeyboardKey.X => new(3, 2),
        AtKeyboardKey.Y => new(1, 3), AtKeyboardKey.Z => new(4, 1),
        AtKeyboardKey.D1 => new(0, 0), AtKeyboardKey.D2 => new(7, 0),
        AtKeyboardKey.D3 => new(0, 1), AtKeyboardKey.D4 => new(7, 1),
        AtKeyboardKey.D5 => new(0, 2), AtKeyboardKey.D6 => new(7, 2),
        AtKeyboardKey.D7 => new(0, 3), AtKeyboardKey.D8 => new(7, 3),
        AtKeyboardKey.D9 => new(0, 4), AtKeyboardKey.D0 => new(7, 4),
        AtKeyboardKey.OemMinus => new(7, 5), AtKeyboardKey.OemPlus => new(0, 5),
        AtKeyboardKey.OemSemicolon => new(2, 6), AtKeyboardKey.OemQuotes => new(5, 5),
        AtKeyboardKey.OemComma => new(3, 5), AtKeyboardKey.OemPeriod => new(4, 5),
        AtKeyboardKey.OemQuestion => new(3, 6),
        _ => null,
    };
}
