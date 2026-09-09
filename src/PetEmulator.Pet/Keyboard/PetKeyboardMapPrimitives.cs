namespace PetEmulator.Pet.Keyboard;

/// <summary>Shared data/helpers for the PET keyboard map variants.</summary>
internal static class PetKeyboardMapPrimitives
{
    /// <summary>Host aliases for the '+' key. On the PET 2001 graphics keyboard and CBM 4032,
    /// the matrix cell they share decodes to '+' only when the PET's own Shift row is clear; a
    /// host Shift held to type '+' (Shift+Equal on a modern keyboard) would otherwise also drive
    /// that Shift row and echo a graphics character instead.</summary>
    internal static readonly string[] PlusKeys = ["Equal", "OemPlus", "NumPadAdd"];

    /// <summary>PET 2001 graphics keyboard / CBM 4032 '+' behavior: press or release the target
    /// cell to match the event, and force-release both Shift cells either way (same effects list
    /// on both Press and Release - only the target cell's own state follows the event kind).</summary>
    internal static IReadOnlyList<MatrixAction> PlusForceReleaseShift(int row, int column, HostKeyEventKind kind) =>
    [
        new MatrixAction(row, column, kind == HostKeyEventKind.Press),
        new MatrixAction(8, 0, false),
        new MatrixAction(8, 5, false)
    ];
}
