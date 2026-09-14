namespace PetEmulator.Pet.Keyboard;

/// <summary>
/// CBM 8032: 80-column BASIC 4 machine with the "B/business" keyboard ("edit-4-80-b" ROM) - a
/// genuinely different physical keyboard from the CBM 4032's. Table transcribed verbatim from
/// personal-001's <c>Cbm8032KeyboardMap.Rules()</c> (ROM-verified via a full 80-cell matrix
/// sweep).
///
/// Its '+' is the opposite and more stateful quirk from the PET 2001/CBM 4032 '+': it needs
/// the PET's own Shift row ASSERTED at (6,6) (its unshifted meaning at (2,6) is ';'), not
/// cleared. Releasing '+' must NOT blindly clear (6,6) if the real ShiftRight key is still
/// physically held, or it would cut off that still-held Shift - so this map tracks
/// <see cref="_shiftRightHeld"/> and only clears (6,6) on '+' release when ShiftRight isn't
/// down. This is why instances of this map must not be shared/static (see
/// <see cref="IPetKeyboardMap"/>).
/// </summary>
public sealed class Cbm8032KeyboardMap : IPetKeyboardMap
{
    public string Id => "pet-cbm-8032";

    private bool _shiftRightHeld;
    private bool _shiftLeftHeld;

    private static readonly Dictionary<string, (int Row, int Column)> Table = new()
    {
        ["KeyA"] = (3, 0), ["KeyB"] = (6, 2), ["KeyC"] = (6, 1), ["KeyD"] = (3, 1), ["KeyE"] = (5, 1), ["KeyF"] = (2, 2),
        ["KeyG"] = (3, 2), ["KeyH"] = (2, 3), ["KeyI"] = (4, 5), ["KeyJ"] = (3, 3), ["KeyK"] = (2, 5),
        // (4,0) - its position in every other CBM 8032 variant - is actually an 8-column tab jump
        // on this board; confirmed by the same full-matrix sweep as Enter/Backspace/Space (see
        // docs/pet/superpet-6809-boot-hang.md). Real 'l' is (3,5).
        ["KeyL"] = (3, 5),
        ["KeyM"] = (8, 3), ["KeyN"] = (7, 2), ["KeyO"] = (5, 5), ["KeyP"] = (4, 6), ["KeyQ"] = (5, 0), ["KeyR"] = (4, 2),
        ["KeyS"] = (2, 1), ["KeyT"] = (5, 2), ["KeyU"] = (5, 3), ["KeyV"] = (7, 1), ["KeyW"] = (4, 1), ["KeyX"] = (8, 1),
        ["KeyY"] = (4, 3), ["KeyZ"] = (7, 0),
        ["Digit0"] = (7, 4), ["Digit1"] = (1, 0), ["Digit2"] = (0, 0), ["Digit3"] = (6, 7), ["Digit4"] = (1, 1),
        ["Digit5"] = (0, 1), ["Digit6"] = (9, 2), ["Digit7"] = (1, 2), ["Digit8"] = (0, 2), ["Digit9"] = (1, 7),
        // Space/Enter/Backspace corrected by a live full-80-cell sweep against a real, booting
        // SuperPET 6809 (see docs/pet/superpet-6809-boot-hang.md) - the previous (4,7)/(3,6)/(6,5)
        // positions inherited from personal-001 (a plain CBM 8032, not SuperPET) were wrong on
        // this hardware: (4,7) is an unwired/dead cell (matrix press has no effect at all), (3,6)
        // is actually '@' (not yet given its own host-key entry), and (6,5) is one of eleven cells
        // that all funnel into the same disk-load fallback the Waterloo ROM uses for a scan result
        // it doesn't otherwise recognize - i.e. also not a real key, not Backspace.
        //
        // Enter needed a second pass: a bare keypress with nothing typed first can't distinguish a
        // real "submit the line" from "cursor to home", since an empty line submitted just
        // redraws the same menu (looking identical to a home/reset). Confirmed by typing a letter
        // first, then pressing the candidate cell: (5,4) turned out to be cursor-down (moves to
        // the next row at the SAME column - the column a plain newline would reset to 0 stayed put
        // at whatever the typed text had left it at), while (3,4) is the real Enter - typing "S"
        // then pressing it replaces the whole menu with the "setup" module's own screen
        // (BAUD/PARITY/STOPBITS), proving it actually submitted "S" as a command.
        // Space (0,5) and Backspace (7,6) were confirmed by the first (typing-free) sweep: Space
        // advances the cursor one column with no visible glyph, Backspace moves it back one column
        // - neither is ambiguous the way Enter was, so didn't need the second pass.
        ["Space"] = (0, 5),
        ["Enter"] = (3, 4), ["Backspace"] = (7, 6),
        ["Minus"] = (0, 3),
        // ShiftLeft is a plain matrix key; ShiftRight needs its own stateful handling below
        // because it shares its cell with the '+' key's forced-Shift trick.
        ["ShiftLeft"] = (6, 0)
    };

    public IReadOnlyList<MatrixAction> Translate(string hostKey, HostKeyEventKind kind)
    {
        if (hostKey == "ShiftRight")
        {
            _shiftRightHeld = kind == HostKeyEventKind.Press;
            return [new MatrixAction(6, 6, _shiftRightHeld)];
        }

        if (hostKey == "ShiftLeft")
            _shiftLeftHeld = kind == HostKeyEventKind.Press;

        var cursorActions = PetCursorKeyActions.Translate(
            hostKey, kind, 6, 0, _shiftLeftHeld);
        if (cursorActions is not null)
            return cursorActions;

        if (PetKeyboardMapPrimitives.PlusKeys.Contains(hostKey))
        {
            if (kind == HostKeyEventKind.Press)
                return [new MatrixAction(2, 6, true), new MatrixAction(6, 6, true)];

            List<MatrixAction> actions = [new MatrixAction(2, 6, false)];
            if (!_shiftRightHeld)
                actions.Add(new MatrixAction(6, 6, false));
            return actions;
        }

        return Table.TryGetValue(hostKey, out var cell)
            ? [new MatrixAction(cell.Row, cell.Column, kind == HostKeyEventKind.Press)]
            : [];
    }
}
