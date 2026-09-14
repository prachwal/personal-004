using Avalonia;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Builds the on-screen <see cref="EmulatorKeyboardLayout"/> for the VIC-20. Visual row
/// grouping is this file's own choice (no photo/technical drawing consulted for exact
/// proportions), but every (row, column) pair is authoritative - copied straight from
/// <see cref="PetEmulator.Vic20.Keyboard.Vic20KeyboardMap"/>'s CellLabels table, itself sourced
/// from the real KERNAL disassembly's "VIC 20 keyboard matrix layout" comment block. The four
/// function keys (F1/F3/F5/F7) sit in their own vertical strip on the right, matching the real
/// VIC-20's distinctive layout (they are not part of the main QWERTY body on real hardware).</summary>
public static class Vic20KeyboardLayoutFactory
{
    private const double KeySize = 40;
    private const double Gap = 5;

    private static string Signal(int row, int column) => $"vic20:{row},{column}";

    public static bool TryParseSignal(string signal, out int row, out int column)
    {
        row = column = 0;
        if (!signal.StartsWith("vic20:", StringComparison.Ordinal))
            return false;
        var parts = signal[6..].Split(',');
        return parts.Length == 2 && int.TryParse(parts[0], out row) && int.TryParse(parts[1], out column);
    }

    public static EmulatorKeyboardLayout Build()
    {
        var keys = new List<EmulatorKeyDefinition>();
        double y = 0;
        double maxX = 0;

        // Row, Column, Label, Width - straight from Vic20KeyboardMap.CellLabels.
        maxX = Math.Max(maxX, AddRow(keys, y, [("←", 1, 0, KeySize), ("1", 0, 0, KeySize), ("2", 7, 0, KeySize),
            ("3", 0, 1, KeySize), ("4", 7, 1, KeySize), ("5", 0, 2, KeySize), ("6", 7, 2, KeySize), ("7", 0, 3, KeySize),
            ("8", 7, 3, KeySize), ("9", 0, 4, KeySize), ("0", 7, 4, KeySize), ("+", 0, 5, KeySize), ("-", 7, 5, KeySize),
            ("£", 0, 6, KeySize), ("HOME", 7, 6, KeySize), ("DEL", 0, 7, KeySize * 1.4)]));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, y, [("CTRL", 2, 0, KeySize * 1.4), ("Q", 6, 0, KeySize), ("W", 1, 1, KeySize),
            ("E", 6, 1, KeySize), ("R", 1, 2, KeySize), ("T", 6, 2, KeySize), ("Y", 1, 3, KeySize), ("U", 6, 3, KeySize),
            ("I", 1, 4, KeySize), ("O", 6, 4, KeySize), ("P", 1, 5, KeySize), ("@", 6, 5, KeySize), ("*", 1, 6, KeySize),
            ("↑", 6, 6, KeySize), ("RETURN", 1, 7, KeySize * 1.6)]));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, y, [("RUN/STOP", 3, 0, KeySize * 1.4), ("A", 2, 1, KeySize), ("S", 5, 1, KeySize),
            ("D", 2, 2, KeySize), ("F", 5, 2, KeySize), ("G", 2, 3, KeySize), ("H", 5, 3, KeySize), ("J", 2, 4, KeySize),
            ("K", 5, 4, KeySize), ("L", 2, 5, KeySize), (":", 5, 5, KeySize), (";", 2, 6, KeySize), ("=", 5, 6, KeySize),
            ("→", 2, 7, KeySize)]));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, y, [("SHIFT", 3, 1, KeySize * 1.6), ("Z", 4, 1, KeySize), ("X", 3, 2, KeySize),
            ("C", 4, 2, KeySize), ("V", 3, 3, KeySize), ("B", 4, 3, KeySize), ("N", 3, 4, KeySize), ("M", 4, 4, KeySize),
            (",", 3, 5, KeySize), (".", 4, 5, KeySize), ("/", 3, 6, KeySize), ("SHIFT", 4, 6, KeySize * 1.6),
            ("↓", 3, 7, KeySize)]));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, y, [("C=", 5, 0, KeySize * 1.4)]));
        keys.Add(new EmulatorKeyDefinition(Signal(4, 0), new Rect(KeySize * 2, y, KeySize * 6, KeySize),
            [KeycapRowBuilder.Legend(KeySize * 2, y, "SPACE", KeySize * 2.5, KeySize / 2 - 6)],
            [Signal(4, 0)], AutomationName: "Space"));
        maxX = Math.Max(maxX, KeySize * 9);
        y += KeySize + Gap;

        // The four function keys stack vertically on the right edge, same as real hardware.
        var fRow = new[] { ("F1", 4, 7), ("F3", 5, 7), ("F5", 6, 7), ("F7", 7, 7) };
        var fx = maxX + Gap * 4;
        var fy = 0.0;
        foreach (var (label, row, column) in fRow)
        {
            keys.Add(new EmulatorKeyDefinition(Signal(row, column), new Rect(fx, fy, KeySize * 1.2, KeySize),
                [KeycapRowBuilder.Legend(fx, fy, label, 4, 12)], [Signal(row, column)], AutomationName: label));
            fy += KeySize + Gap;
        }
        maxX = fx + KeySize * 1.2;

        return new EmulatorKeyboardLayout("vic20", new Size(maxX, y), keys);
    }

    private static double AddRow(List<EmulatorKeyDefinition> keys, double y,
        IReadOnlyList<(string Label, int Row, int Column, double Width)> row) =>
        KeycapRowBuilder.Row(keys, 0, y, KeySize, Gap, row,
            spec => Signal(spec.Row, spec.Column),
            spec => spec.Width,
            (spec, x, keyY) => [KeycapRowBuilder.Legend(x, keyY, spec.Label, 6, spec.Width > KeySize ? spec.Width / 2 - 14 : 12)],
            spec => spec.Label);
}
