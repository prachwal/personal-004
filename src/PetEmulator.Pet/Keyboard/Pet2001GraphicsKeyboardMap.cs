namespace PetEmulator.Pet.Keyboard;

/// <summary>
/// PET 2001 graphics-keyboard matrix map. Letters/digits/Space/Enter/Backspace/Shift transcribed
/// verbatim from personal-001's <c>PetKeyboardMap.PetKeyboardMap.Rules()</c> (ROM-verified real
/// hardware wiring). Punctuation (Quote/Comma/Period/Slash/Semicolon/Colon/Minus and the
/// unshifted graphics-row symbols) is NOT in that source table - personal-001 never mapped it for
/// this keyboard variant (only for CBM 4032/8032). Discovered here via an 80-cell empirical sweep
/// against the same real ROM this repo already imported (press each unclaimed matrix cell alone
/// through a real, booted machine; PET screen codes 0x20-0x3F are byte-identical to ASCII in that
/// range, so the echoed byte reads directly as the character) - see
/// tests/PetEmulator.Pet.Tests/Keyboard/Pet2001GraphicsKeyboardMapPunctuationTests.cs for the
/// sweep result asserted against the live ROM, not just this table's static values.
/// The '+'-family keys (Equal/OemPlus/NumPadAdd) also force-release the two Shift
/// matrix cells (8,0)/(8,5) - see <see cref="PetKeyboardMapPrimitives.PlusForceReleaseShift"/>.
/// </summary>
public sealed class Pet2001GraphicsKeyboardMap : IPetKeyboardMap
{
    public string Id => "pet-2001-graphics";

    private bool _shiftLeftHeld;
    private bool _shiftRightHeld;

    private static readonly Dictionary<string, (int Row, int Column)> Table = new()
    {
        ["KeyA"] = (4, 0), ["KeyB"] = (6, 2), ["KeyC"] = (6, 1), ["KeyD"] = (4, 1), ["KeyE"] = (2, 1), ["KeyF"] = (5, 1),
        ["KeyG"] = (4, 2), ["KeyH"] = (5, 2), ["KeyI"] = (3, 3), ["KeyJ"] = (4, 3), ["KeyK"] = (5, 3), ["KeyL"] = (4, 4),
        ["KeyM"] = (6, 3), ["KeyN"] = (7, 2), ["KeyO"] = (2, 4), ["KeyP"] = (3, 4), ["KeyQ"] = (2, 0), ["KeyR"] = (3, 1),
        ["KeyS"] = (5, 0), ["KeyT"] = (2, 2), ["KeyU"] = (2, 3), ["KeyV"] = (7, 1), ["KeyW"] = (3, 0), ["KeyX"] = (7, 0),
        ["KeyY"] = (3, 2), ["KeyZ"] = (6, 0),
        ["Digit0"] = (8, 6), ["Digit1"] = (6, 6), ["Digit2"] = (7, 6), ["Digit3"] = (6, 7), ["Digit4"] = (4, 6),
        ["Digit5"] = (5, 6), ["Digit6"] = (4, 7), ["Digit7"] = (2, 6), ["Digit8"] = (3, 6), ["Digit9"] = (2, 7),
        ["Space"] = (9, 2), ["Enter"] = (6, 5), ["Backspace"] = (1, 7), ["ShiftLeft"] = (8, 0), ["ShiftRight"] = (8, 5),

        // Empirically confirmed against the real ROM (see class doc comment) - not in
        // personal-001's own table for this keyboard variant.
        ["Quote"] = (1, 0), ["Comma"] = (7, 3), ["Period"] = (9, 6), ["Slash"] = (3, 7),
        ["Semicolon"] = (6, 4), ["Colon"] = (5, 4), ["Minus"] = (8, 7),
    };

    public IReadOnlyList<MatrixAction> Translate(string hostKey, HostKeyEventKind kind)
    {
        if (hostKey == "ShiftLeft") _shiftLeftHeld = kind == HostKeyEventKind.Press;
        if (hostKey == "ShiftRight") _shiftRightHeld = kind == HostKeyEventKind.Press;

        var cursorActions = PetCursorKeyActions.Translate(
            hostKey, kind, 8, 0, _shiftLeftHeld || _shiftRightHeld);
        if (cursorActions is not null)
            return cursorActions;

        if (PetKeyboardMapPrimitives.PlusKeys.Contains(hostKey))
            return PetKeyboardMapPrimitives.ForceReleaseShift(7, 7, kind);
        // Same fix as '+' above: on a modern keyboard '"' needs Shift+' held, but this cell
        // already means '"' unshifted on the real PET - live Shift+Quote was pressing (8,0)/(8,5)
        // alongside it and the real ROM decoded that combo to something else (a graphics glyph,
        // not '"'). Force-release both Shift cells the same way '+' does.
        if (hostKey == "Quote")
            return PetKeyboardMapPrimitives.ForceReleaseShift(1, 0, kind);

        return Table.TryGetValue(hostKey, out var cell)
            ? [new MatrixAction(cell.Row, cell.Column, kind == HostKeyEventKind.Press)]
            : [];
    }

    /// <summary>(Row, Column) -> the character or key name printed there, for display purposes
    /// (e.g. the Keyboard Matrix demo) - derived from this class's own <see cref="Table"/> (single
    /// source of truth, not a separately-maintained copy) plus the '+'-family's forced cell.</summary>
    public static IReadOnlyDictionary<(int Row, int Column), string> CellLabels { get; } = BuildLabels();

    private static IReadOnlyDictionary<(int Row, int Column), string> BuildLabels()
    {
        var labels = new Dictionary<(int, int), string>();
        foreach (var (hostKey, cell) in Table)
            labels[cell] = ToLabel(hostKey);
        labels[(7, 7)] = "+";
        return labels;
    }

    private static string ToLabel(string hostKey)
    {
        if (hostKey.StartsWith("Key", StringComparison.Ordinal))
            return hostKey[3..];
        if (hostKey.StartsWith("Digit", StringComparison.Ordinal))
            return hostKey[5..];

        return hostKey switch
        {
            "Space" => "SPACE",
            "Enter" => "RETURN",
            "Backspace" => "←",
            "ShiftLeft" or "ShiftRight" => "SHIFT",
            "Quote" => "\"",
            "Comma" => ",",
            "Period" => ".",
            "Slash" => "/",
            "Semicolon" => ";",
            "Colon" => ":",
            "Minus" => "-",
            _ => hostKey,
        };
    }
}
