using Avalonia;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Builds the on-screen <see cref="EmulatorKeyboardLayout"/> for the PET 2001 graphics
/// keyboard. Visual row grouping (QWERTY-shaped, standard-ish key sizes) is this file's own
/// choice - no period photo/technical drawing was consulted, so exact key proportions may not
/// match the real chiclet keyboard pixel-for-pixel. The (row, column) pair behind every key IS
/// authoritative, though: copied straight from <see cref="PetEmulator.Pet.Keyboard.Pet2001GraphicsKeyboardMap"/>'s
/// ROM-verified table, not re-derived - pressing a key here fires the exact same matrix cell a
/// real PET 2001 graphics keyboard would.</summary>
public static class PetGraphicsKeyboardLayoutFactory
{
    private const double KeySize = 40;
    private const double Gap = 5;

    /// <summary>The signal ID one on-screen key produces, e.g. "pet:4,0" for the 'A' key
    /// (row 4, column 0) - parse with <see cref="TryParseSignal"/> to drive
    /// <see cref="PetEmulator.Pet.Keyboard.PetKeyboardMatrix"/> directly.</summary>
    private static string Signal(int row, int column) => $"pet:{row},{column}";

    public static bool TryParseSignal(string signal, out int row, out int column)
    {
        row = column = 0;
        if (!signal.StartsWith("pet:", StringComparison.Ordinal))
            return false;
        var parts = signal[4..].Split(',');
        return parts.Length == 2 && int.TryParse(parts[0], out row) && int.TryParse(parts[1], out column);
    }

    public static EmulatorKeyboardLayout Build()
    {
        var keys = new List<EmulatorKeyDefinition>();
        double y = 0;
        double maxX = 0;

        // Row, Column, Label, Width - straight from Pet2001GraphicsKeyboardMap.Table.
        maxX = Math.Max(maxX, AddRow(keys, y, [("\"", 1, 0, KeySize), ("1", 6, 6, KeySize), ("2", 7, 6, KeySize), ("3", 6, 7, KeySize),
            ("4", 4, 6, KeySize), ("5", 5, 6, KeySize), ("6", 4, 7, KeySize), ("7", 2, 6, KeySize), ("8", 3, 6, KeySize),
            ("9", 2, 7, KeySize), ("0", 8, 6, KeySize), ("-", 8, 7, KeySize), ("+", 7, 7, KeySize), ("←", 1, 7, KeySize * 1.5)]));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, y, [("Q", 2, 0, KeySize), ("W", 3, 0, KeySize), ("E", 2, 1, KeySize), ("R", 3, 1, KeySize),
            ("T", 2, 2, KeySize), ("Y", 3, 2, KeySize), ("U", 2, 3, KeySize), ("I", 3, 3, KeySize),
            ("O", 2, 4, KeySize), ("P", 3, 4, KeySize)]));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, y, [("A", 4, 0, KeySize), ("S", 5, 0, KeySize), ("D", 4, 1, KeySize), ("F", 5, 1, KeySize),
            ("G", 4, 2, KeySize), ("H", 5, 2, KeySize), ("J", 4, 3, KeySize), ("K", 5, 3, KeySize),
            ("L", 4, 4, KeySize), (":", 5, 4, KeySize), (";", 6, 4, KeySize), ("RETURN", 6, 5, KeySize * 1.8)]));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, y, [("SHIFT", 8, 0, KeySize * 1.6), ("Z", 6, 0, KeySize), ("X", 7, 0, KeySize), ("C", 6, 1, KeySize),
            ("V", 7, 1, KeySize), ("B", 6, 2, KeySize), ("N", 7, 2, KeySize), ("M", 6, 3, KeySize),
            (",", 7, 3, KeySize), (".", 9, 6, KeySize), ("/", 3, 7, KeySize), ("SHIFT", 8, 5, KeySize * 1.6)]));
        y += KeySize + Gap;

        keys.Add(new EmulatorKeyDefinition("SPACE", new Rect(KeySize * 3, y, KeySize * 6, KeySize),
            [KeycapRowBuilder.Legend(KeySize * 3, y, "SPACE", KeySize * 2.5, KeySize / 2 - 6)],
            [Signal(9, 2)], AutomationName: "Space"));
        maxX = Math.Max(maxX, KeySize * 9);
        y += KeySize + Gap;

        return new EmulatorKeyboardLayout("pet-2001-graphics", new Size(maxX, y), keys);
    }

    private static double AddRow(List<EmulatorKeyDefinition> keys, double y,
        IReadOnlyList<(string Label, int Row, int Column, double Width)> row) =>
        KeycapRowBuilder.Row(keys, 0, y, KeySize, Gap, row,
            spec => Signal(spec.Row, spec.Column),
            spec => spec.Width,
            (spec, x, keyY) => [KeycapRowBuilder.Legend(x, keyY, spec.Label, 6, spec.Width > KeySize ? spec.Width / 2 - 14 : 12)],
            spec => spec.Label);
}
