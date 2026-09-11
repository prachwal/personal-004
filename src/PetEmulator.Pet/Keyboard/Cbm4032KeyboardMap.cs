namespace PetEmulator.Pet.Keyboard;

/// <summary>
/// CBM 4032: 40-column BASIC 4 machine with the "N/normal" keyboard ("edit-4-40-n" ROM).
/// Table computed (not hand-transcribed) from personal-001's
/// <c>Cbm4032KeyboardMap.Lookup</c> byte array and <c>Basic4KeyCodes()</c> table via the
/// source's own formula (<c>index = Array.IndexOf(Lookup, code); row = 9 - index/8;
/// column = 7 - index%8;</c>), then baked in as a literal table - see the scratch script used
/// to derive it, and <c>Cbm4032KeyboardMapTests</c> for the sanity checks against that
/// computation. Note: ShiftLeft and ShiftRight share host code 0x00 in <c>Basic4KeyCodes()</c>,
/// so <c>Array.IndexOf</c> resolves both to the same first match - both genuinely land on
/// (8,5) per the source's own algorithm, not a transcription error.
/// Same '+'-family (Equal/OemPlus/NumPadAdd) force-release-Shift behavior as the PET 2001
/// graphics keyboard applies here too (shared helper).
/// </summary>
public sealed class Cbm4032KeyboardMap : IPetKeyboardMap
{
    public string Id => "pet-cbm-4032";

    private static readonly Dictionary<string, (int Row, int Column)> Table = new()
    {
        ["KeyA"] = (4, 0), ["KeyB"] = (6, 2), ["KeyC"] = (6, 1), ["KeyD"] = (4, 1), ["KeyE"] = (2, 1), ["KeyF"] = (5, 1),
        ["KeyG"] = (4, 2), ["KeyH"] = (5, 2), ["KeyI"] = (3, 3), ["KeyJ"] = (4, 3), ["KeyK"] = (5, 3), ["KeyL"] = (4, 4),
        ["KeyM"] = (6, 3), ["KeyN"] = (7, 2), ["KeyO"] = (2, 4), ["KeyP"] = (3, 4), ["KeyQ"] = (2, 0), ["KeyR"] = (3, 1),
        ["KeyS"] = (5, 0), ["KeyT"] = (2, 2), ["KeyU"] = (2, 3), ["KeyV"] = (7, 1), ["KeyW"] = (3, 0), ["KeyX"] = (7, 0),
        ["KeyY"] = (3, 2), ["KeyZ"] = (6, 0),
        ["Digit0"] = (8, 6), ["Digit1"] = (6, 6), ["Digit2"] = (7, 6), ["Digit3"] = (6, 7), ["Digit4"] = (4, 6),
        ["Digit5"] = (5, 6), ["Digit6"] = (4, 7), ["Digit7"] = (2, 6), ["Digit8"] = (3, 6), ["Digit9"] = (2, 7),
        ["Space"] = (9, 2), ["Enter"] = (6, 5), ["Backspace"] = (1, 7),
        ["ShiftLeft"] = (8, 5), ["ShiftRight"] = (8, 5),
        ["Minus"] = (8, 7), ["Quote"] = (1, 0), ["Comma"] = (7, 3)
    };

    public IReadOnlyList<MatrixAction> Translate(string hostKey, HostKeyEventKind kind)
    {
        if (PetKeyboardMapPrimitives.PlusKeys.Contains(hostKey))
            return PetKeyboardMapPrimitives.ForceReleaseShift(7, 7, kind);
        // Same fix as '+', and the same live-keyboard bug as Pet2001GraphicsKeyboardMap: '"'
        // is its own unshifted cell here too, but a host keyboard needs Shift+' to type it.
        if (hostKey == "Quote")
            return PetKeyboardMapPrimitives.ForceReleaseShift(1, 0, kind);

        return Table.TryGetValue(hostKey, out var cell)
            ? [new MatrixAction(cell.Row, cell.Column, kind == HostKeyEventKind.Press)]
            : [];
    }
}
