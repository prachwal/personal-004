namespace PetEmulator.Pet.Keyboard;

/// <summary>
/// PET 2001 graphics-keyboard matrix map. Row/column table transcribed verbatim from
/// personal-001's <c>PetKeyboardMap.PetKeyboardMap.Rules()</c> (ROM-verified real hardware
/// wiring). The '+'-family keys (Equal/OemPlus/NumPadAdd) also force-release the two Shift
/// matrix cells (8,0)/(8,5) - see <see cref="PetKeyboardMapPrimitives.PlusForceReleaseShift"/>.
/// </summary>
public sealed class Pet2001GraphicsKeyboardMap : IPetKeyboardMap
{
    public string Id => "pet-2001-graphics";

    private static readonly Dictionary<string, (int Row, int Column)> Table = new()
    {
        ["KeyA"] = (4, 0), ["KeyB"] = (6, 2), ["KeyC"] = (6, 1), ["KeyD"] = (4, 1), ["KeyE"] = (2, 1), ["KeyF"] = (5, 1),
        ["KeyG"] = (4, 2), ["KeyH"] = (5, 2), ["KeyI"] = (3, 3), ["KeyJ"] = (4, 3), ["KeyK"] = (5, 3), ["KeyL"] = (4, 4),
        ["KeyM"] = (6, 3), ["KeyN"] = (7, 2), ["KeyO"] = (2, 4), ["KeyP"] = (3, 4), ["KeyQ"] = (2, 0), ["KeyR"] = (3, 1),
        ["KeyS"] = (5, 0), ["KeyT"] = (2, 2), ["KeyU"] = (2, 3), ["KeyV"] = (7, 1), ["KeyW"] = (3, 0), ["KeyX"] = (7, 0),
        ["KeyY"] = (3, 2), ["KeyZ"] = (6, 0),
        ["Digit0"] = (8, 6), ["Digit1"] = (6, 6), ["Digit2"] = (7, 6), ["Digit3"] = (6, 7), ["Digit4"] = (4, 6),
        ["Digit5"] = (5, 6), ["Digit6"] = (4, 7), ["Digit7"] = (2, 6), ["Digit8"] = (3, 6), ["Digit9"] = (2, 7),
        ["Space"] = (9, 2), ["Enter"] = (6, 5), ["Backspace"] = (1, 7), ["ShiftLeft"] = (8, 0), ["ShiftRight"] = (8, 5)
    };

    public IReadOnlyList<MatrixAction> Translate(string hostKey, HostKeyEventKind kind)
    {
        if (PetKeyboardMapPrimitives.PlusKeys.Contains(hostKey))
            return PetKeyboardMapPrimitives.PlusForceReleaseShift(7, 7, kind);

        return Table.TryGetValue(hostKey, out var cell)
            ? [new MatrixAction(cell.Row, cell.Column, kind == HostKeyEventKind.Press)]
            : [];
    }
}
