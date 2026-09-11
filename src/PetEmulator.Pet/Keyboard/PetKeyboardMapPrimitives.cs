namespace PetEmulator.Pet.Keyboard;

/// <summary>Shared data/helpers for the PET keyboard map variants.</summary>
internal static class PetKeyboardMapPrimitives
{
    /// <summary>Host aliases for the '+' key. On the PET 2001 graphics keyboard and CBM 4032,
    /// the matrix cell they share decodes to '+' only when the PET's own Shift row is clear; a
    /// host Shift held to type '+' (Shift+Equal on a modern keyboard) would otherwise also drive
    /// that Shift row and echo a graphics character instead.</summary>
    internal static readonly string[] PlusKeys = ["Equal", "OemPlus", "NumPadAdd"];

    /// <summary>General fix for any key whose PET matrix cell already fully encodes the printed
    /// character without needing the PET's own Shift row (the '+' key above; also Quote - PET's
    /// graphics/4032 keyboards give '"' its own dedicated cell, but a modern host keyboard needs
    /// Shift+' to type it, so a live key event drives the host's physical Shift matrix cell too).
    /// Presses or releases the target cell to match the event, and force-releases both Shift
    /// cells either way (same effects list on both Press and Release - only the target cell's own
    /// state follows the event kind), so an incidentally-held host Shift can't corrupt what the
    /// real ROM reads back.</summary>
    internal static IReadOnlyList<MatrixAction> ForceReleaseShift(int row, int column, HostKeyEventKind kind) =>
    [
        new MatrixAction(row, column, kind == HostKeyEventKind.Press),
        new MatrixAction(8, 0, false),
        new MatrixAction(8, 5, false)
    ];
}
