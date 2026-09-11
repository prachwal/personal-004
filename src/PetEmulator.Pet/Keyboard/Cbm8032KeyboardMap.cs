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

    private static readonly Dictionary<string, (int Row, int Column)> Table = new()
    {
        ["KeyA"] = (3, 0), ["KeyB"] = (6, 2), ["KeyC"] = (6, 1), ["KeyD"] = (3, 1), ["KeyE"] = (5, 1), ["KeyF"] = (2, 2),
        ["KeyG"] = (3, 2), ["KeyH"] = (2, 3), ["KeyI"] = (4, 5), ["KeyJ"] = (3, 3), ["KeyK"] = (2, 5), ["KeyL"] = (4, 0),
        ["KeyM"] = (8, 3), ["KeyN"] = (7, 2), ["KeyO"] = (5, 5), ["KeyP"] = (4, 6), ["KeyQ"] = (5, 0), ["KeyR"] = (4, 2),
        ["KeyS"] = (2, 1), ["KeyT"] = (5, 2), ["KeyU"] = (5, 3), ["KeyV"] = (7, 1), ["KeyW"] = (4, 1), ["KeyX"] = (8, 1),
        ["KeyY"] = (4, 3), ["KeyZ"] = (7, 0),
        ["Digit0"] = (7, 4), ["Digit1"] = (1, 0), ["Digit2"] = (0, 0), ["Digit3"] = (6, 7), ["Digit4"] = (1, 1),
        ["Digit5"] = (0, 1), ["Digit6"] = (9, 2), ["Digit7"] = (1, 2), ["Digit8"] = (0, 2), ["Digit9"] = (1, 7),
        ["Space"] = (4, 7),
        // Enter and Backspace echo no printable code so a text sweep can't confirm them
        // directly; confirmed behaviorally in the source (does the line execute / does a typed
        // letter revert). Notably (6,5) is Backspace here, not Enter, unlike the other variants.
        ["Enter"] = (3, 6), ["Backspace"] = (6, 5),
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
