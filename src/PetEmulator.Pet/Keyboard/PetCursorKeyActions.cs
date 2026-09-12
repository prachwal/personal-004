namespace PetEmulator.Pet.Keyboard;

/// <summary>Expands host cursor keys to the two physical PET cursor keys.</summary>
internal static class PetCursorKeyActions
{
    private const int HorizontalRow = 0;
    private const int HorizontalColumn = 7;
    private const int VerticalRow = 1;
    private const int VerticalColumn = 6;

    internal static IReadOnlyList<MatrixAction>? Translate(
        string hostKey,
        HostKeyEventKind kind,
        int shiftRow,
        int shiftColumn,
        bool shiftHeld)
    {
        var (row, column, needsShift) = hostKey switch
        {
            "Left" => (HorizontalRow, HorizontalColumn, false),
            "Right" => (HorizontalRow, HorizontalColumn, true),
            "Down" => (VerticalRow, VerticalColumn, false),
            "Up" => (VerticalRow, VerticalColumn, true),
            _ => (-1, -1, false)
        };

        if (row < 0)
            return null;

        var pressed = kind == HostKeyEventKind.Press;
        if (!needsShift)
            return [new MatrixAction(row, column, pressed)];

        var actions = new List<MatrixAction>(2);
        if (pressed)
        {
            if (!shiftHeld)
                actions.Add(new MatrixAction(shiftRow, shiftColumn, true));
            actions.Add(new MatrixAction(row, column, true));
        }
        else
        {
            actions.Add(new MatrixAction(row, column, false));
            if (!shiftHeld)
                actions.Add(new MatrixAction(shiftRow, shiftColumn, false));
        }

        return actions;
    }
}
