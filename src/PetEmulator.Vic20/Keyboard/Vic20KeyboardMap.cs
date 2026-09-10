using PetEmulator.Core.Keyboard;

namespace PetEmulator.Vic20.Keyboard;

/// <summary>Translates a host-key-string event (see PetEmulator.Desktop.KeyMapping - Avalonia
/// Key -> "KeyA"/"Digit5"/"Space"/"Enter"/... - already machine-agnostic despite living in the
/// PET-originated Desktop project) into one <see cref="Vic20KeyboardMatrix"/> cell. Mirrors the
/// shape of PetEmulator.Pet.Keyboard.IPetKeyboardMap's Translate for live GUI key handling
/// (distinct from <see cref="Vic20HostKeyMap"/>, which is char-based for scripted typing only -
/// see that class's doc comment for why they aren't merged).
///
/// Every letter/digit/punctuation cell below was already correct (cross-checked against
/// <see cref="Vic20HostKeyMap"/>'s independently, empirically-verified table - identical for
/// every one of the 26 letters, 10 digits, and 7 punctuation keys both tables cover). The five
/// control-key cells (Shift, Ctrl, Escape, Home, Backspace/Delete) were not covered by that
/// empirical sweep and were simply wrong - re-derived here directly from the real KERNAL
/// disassembly's own "VIC 20 keyboard matrix layout" table (vic-20-rom.asm, the row/column
/// comment block under VIA2PA1 $9121), transposed to this class's (row, column) convention
/// (verified by transposing every already-confirmed letter/digit cell first and checking it
/// lands exactly where <see cref="Vic20HostKeyMap"/> says it should, before trusting the
/// transpose for the previously-unverified control cells):
/// LeftShift=(3,1) [was wrongly combined with RightShift at (4,7), which is really F1],
/// RightShift=(4,6), Ctrl=(2,0) [was (7,7), really F7], Escape=(3,0) [RUN/STOP - was (3,7),
/// which is really CRSR-DOWN, a different key entirely], Home=(7,6) [was (0,7), really DEL],
/// Backspace/Delete=(0,7) [was (2,7), really CRSR-RIGHT]. Also adds the real Commodore/C= key
/// (5,0) and the unshifted function keys F1/F3/F5/F7 (their shifted F2/F4/F6/F8 forms need
/// asserting Shift alongside the same cell - this class returns one position per key, no
/// multi-action support yet, so those and the shifted cursor directions stay a documented gap,
/// not a guess).</summary>
public sealed class Vic20KeyboardMap : AtKeyboardMapping
{
    protected override KeyboardMatrixPosition? Map(AtKeyboardKey key) => key switch
    {
        AtKeyboardKey.Enter => new(1, 7),
        AtKeyboardKey.Space => new(4, 0),
        AtKeyboardKey.LeftShift => new(3, 1),
        AtKeyboardKey.RightShift => new(4, 6),
        AtKeyboardKey.LeftCtrl or AtKeyboardKey.RightCtrl => new(2, 0),
        AtKeyboardKey.Escape => new(3, 0), // RUN/STOP
        AtKeyboardKey.Home => new(7, 6),
        AtKeyboardKey.Backspace or AtKeyboardKey.Delete => new(0, 7), // DEL/INST
        AtKeyboardKey.LeftAlt or AtKeyboardKey.RightAlt => new(5, 0), // C=
        AtKeyboardKey.F1 => new(4, 7), AtKeyboardKey.F3 => new(5, 7),
        AtKeyboardKey.F5 => new(6, 7), AtKeyboardKey.F7 => new(7, 7),
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

    /// <summary>(Row, Column) -> the character or key name printed there - the full real 64-cell
    /// matrix, not just what <see cref="AtKeyboardKey"/> happens to have a host key for. Same real
    /// KERNAL disassembly source ("VIC 20 keyboard matrix layout", vic-20-rom.asm under VIA2PA1
    /// $9121) as this class's control-key fixes above, transposed and cross-checked the same
    /// way.</summary>
    public static IReadOnlyDictionary<(int Row, int Column), string> CellLabels { get; } =
        new Dictionary<(int, int), string>
        {
            [(0, 0)] = "1", [(0, 1)] = "3", [(0, 2)] = "5", [(0, 3)] = "7",
            [(0, 4)] = "9", [(0, 5)] = "+", [(0, 6)] = "£", [(0, 7)] = "DEL",

            [(1, 0)] = "←", [(1, 1)] = "W", [(1, 2)] = "R", [(1, 3)] = "Y",
            [(1, 4)] = "I", [(1, 5)] = "P", [(1, 6)] = "*", [(1, 7)] = "RETURN",

            [(2, 0)] = "CTRL", [(2, 1)] = "A", [(2, 2)] = "D", [(2, 3)] = "G",
            [(2, 4)] = "J", [(2, 5)] = "L", [(2, 6)] = ";", [(2, 7)] = "→",

            [(3, 0)] = "RUN/STOP", [(3, 1)] = "SHIFT", [(3, 2)] = "X", [(3, 3)] = "V",
            [(3, 4)] = "N", [(3, 5)] = ",", [(3, 6)] = "/", [(3, 7)] = "↓",

            [(4, 0)] = "SPACE", [(4, 1)] = "Z", [(4, 2)] = "C", [(4, 3)] = "B",
            [(4, 4)] = "M", [(4, 5)] = ".", [(4, 6)] = "SHIFT", [(4, 7)] = "F1",

            [(5, 0)] = "C=", [(5, 1)] = "S", [(5, 2)] = "F", [(5, 3)] = "H",
            [(5, 4)] = "K", [(5, 5)] = ":", [(5, 6)] = "=", [(5, 7)] = "F3",

            [(6, 0)] = "Q", [(6, 1)] = "E", [(6, 2)] = "T", [(6, 3)] = "U",
            [(6, 4)] = "O", [(6, 5)] = "@", [(6, 6)] = "↑", [(6, 7)] = "F5",

            [(7, 0)] = "2", [(7, 1)] = "4", [(7, 2)] = "6", [(7, 3)] = "8",
            [(7, 4)] = "0", [(7, 5)] = "-", [(7, 6)] = "HOME", [(7, 7)] = "F7",
        };
}
