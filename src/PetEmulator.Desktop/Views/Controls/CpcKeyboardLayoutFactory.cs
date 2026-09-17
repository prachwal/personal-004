using Avalonia;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Builds the on-screen <see cref="EmulatorKeyboardLayout"/> for the Amstrad CPC464 from
/// measured key geometry (Keyboard/cpc464_key_positions.csv): main block (R1 ESC..DEL, R2
/// TAB..tall RETURN, R3 CAPS..\, R4 SHIFT..SHIFT, R5 CTRL/COPY/SPACE/arrows/wide
/// ENTER), cursor block (↑, ←↓→, CLR), blue numeric pad (7 8 9 - / 4 5 6 , / 1 2 3 + tall ENTER /
/// wide 0 + .) and the front function row (f0-f4).
///
/// Unlike Kaypro (serial bytes), CPC keys are 10x8 matrix cells - each signal is "cpc464:ROW,COL"
/// with an optional ",S" suffix that holds Shift (2,5) while the key is down, exactly like the
/// firmware tests' own PressKey helper. Every cell below comes from one of two sources:
/// - VERIFIED in-repo (Cpc464MachineViewModel.Matrix + Cpc464BootTests, confirmed against real
///   firmware behavior): all letters/digits/space/return/shift/arrows/backspace plus -, ?, comma
///   and dot.
/// - Standard CPC matrix (Retro Isle "Scanning the Keyboard & Joysticks" doc), cross-checked to
///   agree with every verified cell above: ^, CLR, @, [, ], ;, :, CTRL, COPY, \, /, ", *, the
///   cursor duplicates, the small-ENTER cell for the pad ENTER, and the f0-f9/. numpad cells.
/// Two deliberate assumptions, both documented: the pad's -, "," and tall ENTER duplicate their
/// main-block cells (the reference doc's f-list covers only 0-9/.), and f0-f4 get the
/// <see cref="UnknownSignal"/> placeholder - they have no documented matrix cells anywhere, and
/// guessing would type garbage into the machine, so they render as silent no-ops with a tooltip.
/// The R3 measurement overlapping CAPS LOCK/A is treated as a photo artifact and skipped; pad
/// rows align with the main rows (the photo's half-row offset is perspective).</summary>
public static class CpcKeyboardLayoutFactory
{
    public const string UnknownSignal = "cpc464:UNKNOWN";

    private const double KeySize = 36;
    private const double Gap = 4;
    private const double RowPitch = KeySize + Gap;
    private const double SectionGap = 40;

    private static string Signal(byte row, byte col, bool shift = false) =>
        shift ? $"cpc464:{row},{col},S" : $"cpc464:{row},{col}";

    public static bool TryParseSignal(string signal, out byte row, out byte col, out bool shift)
    {
        row = col = 0;
        shift = false;
        if (!signal.StartsWith("cpc464:", StringComparison.Ordinal))
            return false;
        var parts = signal[7..].Split(',');
        if (parts.Length is not (2 or 3))
            return false;
        if (!byte.TryParse(parts[0], out row) || !byte.TryParse(parts[1], out col))
            return false;
        if (row > 9 || col > 7)
            return false;
        if (parts.Length == 3)
        {
            if (parts[2] != "S")
                return false;
            shift = true;
        }

        return true;
    }

    public static EmulatorKeyboardLayout Build()
    {
        var keys = new List<EmulatorKeyDefinition>();
        const double r1Y = 0;
        const double r2Y = RowPitch;
        const double r3Y = 2 * RowPitch;
        const double r4Y = 3 * RowPitch;
        const double r5Y = 4 * RowPitch;
        double maxX = 0;

        // Main R1: ESC 1..0 - ^ CLR DEL(wide 60).
        double x = 0;
        x = AddKey(keys, "r1:ESC", x, r1Y, "ESC", 8, 2) + Gap;
        foreach (var (label, row, col) in new[] { ("1", 8, 0), ("2", 8, 1), ("3", 7, 1), ("4", 7, 0), ("5", 6, 1), ("6", 6, 0), ("7", 5, 1), ("8", 5, 0), ("9", 4, 1), ("0", 4, 0) })
            x = AddKey(keys, $"r1:{label}", x, r1Y, label, (byte)row, (byte)col) + Gap;
        x = AddKey(keys, "r1:-", x, r1Y, "-", 3, 1) + Gap;
        x = AddKey(keys, "r1:^", x, r1Y, "^", 3, 0) + Gap;
        x = AddKey(keys, "r1:CLR", x, r1Y, "CLR", 2, 0) + Gap;
        x = AddWideKey(keys, "r1:DEL", x, r1Y, "DEL", 9, 7, widthPx: 60) + Gap;
        maxX = Math.Max(maxX, x);

        // Main R2: TAB(wide 58) Q..P @ [ ] blank DEL.
        x = 0;
        x = AddWideKey(keys, "r2:TAB", x, r2Y, "TAB", 8, 4, widthPx: 58) + Gap;
        foreach (var (label, row, col) in new[] { ("Q", 8, 3), ("W", 7, 3), ("E", 7, 2), ("R", 6, 2), ("T", 6, 3), ("Y", 5, 3), ("U", 5, 2), ("I", 4, 3), ("O", 4, 2), ("P", 3, 3) })
            x = AddKey(keys, $"r2:{label}", x, r2Y, label, (byte)row, (byte)col) + Gap;
        x = AddKey(keys, "r2:@", x, r2Y, "@", 3, 2) + Gap;
        x = AddKey(keys, "r2:[", x, r2Y, "[", 2, 1) + Gap;
        x = AddKey(keys, "r2:]", x, r2Y, "]", 2, 3) + Gap;
        x = AddBlankKey(keys, "r2:BLANK", x, r2Y) + Gap;
        keys.Add(new EmulatorKeyDefinition("r2:RETURN",
            new Rect(x, r2Y, 60, 2 * KeySize + Gap),
            [KeycapRowBuilder.Legend(x, r2Y, "RETURN", 4, KeySize - 4, size: 10)],
            [Signal(2, 2)], AutomationName: "Return"));
        x += 60 + Gap;
        maxX = Math.Max(maxX, x);

        // Main R3: CAPS(wide 57) A..L (G with BELL hint) ; : * \.
        x = 0;
        x = AddWideKey(keys, "r3:CAPS", x, r3Y, "CAPS", 8, 6, widthPx: 57, fontSize: 9) + Gap;
        foreach (var (label, row, col) in new[] { ("A", 8, 5), ("S", 7, 4), ("D", 7, 5), ("F", 6, 5), ("H", 5, 4), ("J", 5, 5), ("K", 4, 5), ("L", 4, 4) })
            x = AddKey(keys, $"r3:{label}", x, r3Y, label, (byte)row, (byte)col) + Gap;
        x = AddBellKey(keys, x, r3Y) + Gap;
        x = AddKey(keys, "r3:;", x, r3Y, ";", 3, 4) + Gap;
        x = AddKey(keys, "r3::", x, r3Y, ":", 3, 5) + Gap;
        x = AddShiftedKey(keys, "r3:*", x, r3Y, "*", 3, 5) + Gap;
        x = AddKey(keys, "r3:\\", x, r3Y, "\\", 2, 6) + Gap;
        maxX = Math.Max(maxX, x);

        // Main R4: SHIFT(71) Z..M " . / ↑ SHIFT(65).
        x = 0;
        x = AddWideKey(keys, "r4:SHIFT-L", x, r4Y, "SHIFT", 2, 5, widthPx: 71) + Gap;
        foreach (var (label, row, col) in new[] { ("Z", 8, 7), ("X", 7, 7), ("C", 7, 6), ("V", 6, 7), ("B", 6, 6), ("N", 5, 6), ("M", 4, 6) })
            x = AddKey(keys, $"r4:{label}", x, r4Y, label, (byte)row, (byte)col) + Gap;
        x = AddShiftedKey(keys, "r4:\"", x, r4Y, "\"", 8, 1) + Gap;
        x = AddKey(keys, "r4:.", x, r4Y, ".", 4, 7) + Gap;
        x = AddKey(keys, "r4:/", x, r4Y, "/", 3, 6) + Gap;
        x = AddKey(keys, "r4:UP", x, r4Y, "↑", 0, 0) + Gap;
        x = AddWideKey(keys, "r4:SHIFT-R", x, r4Y, "SHIFT", 2, 5, widthPx: 65) + Gap;
        maxX = Math.Max(maxX, x);

        // Main R5: CTRL COPY SPACE(wide 248) ← ↓ → ENTER(wide 100).
        x = 0;
        x = AddWideKey(keys, "r5:CTRL", x, r5Y, "CTRL", 2, 7, widthPx: 60) + Gap;
        x = AddWideKey(keys, "r5:COPY", x, r5Y, "COPY", 1, 1, widthPx: 60) + Gap;
        keys.Add(new EmulatorKeyDefinition("r5:SPACE", new Rect(x, r5Y, 248, KeySize),
            [KeycapRowBuilder.Legend(x, r5Y, "SPACE", 110, KeySize / 2 - 6)],
            [Signal(5, 7)], AutomationName: "Space"));
        x += 248 + Gap;
        x = AddKey(keys, "r5:LEFT", x, r5Y, "←", 1, 0) + Gap;
        x = AddKey(keys, "r5:DOWN", x, r5Y, "↓", 0, 2) + Gap;
        x = AddKey(keys, "r5:RIGHT", x, r5Y, "→", 0, 1) + Gap;
        x = AddWideKey(keys, "r5:ENTER", x, r5Y, "ENTER", 2, 2, widthPx: 100) + Gap;
        maxX = Math.Max(maxX, x);
        var bottomY = r5Y + RowPitch;

        // Cursor block right of main: ↑, ←↓→, CLR below.
        var cursorX = maxX + SectionGap;
        AddKey(keys, "cursor:UP", cursorX + RowPitch, r1Y, "↑", 0, 0);
        AddKey(keys, "cursor:LEFT", cursorX, r2Y, "←", 1, 0);
        AddKey(keys, "cursor:DOWN", cursorX + RowPitch, r2Y, "↓", 0, 2);
        AddKey(keys, "cursor:RIGHT", cursorX + 2 * RowPitch, r2Y, "→", 0, 1);
        AddKey(keys, "cursor:CLR", cursorX + RowPitch, r3Y, "CLR", 2, 0);
        maxX = cursorX + 3 * RowPitch;

        // Numeric pad right of cursor: 7 8 9 - / 4 5 6 , / 1 2 3 + tall ENTER /
        // wide 0 + . - pad rows align with R1-R4 (the photo's half-row offset is perspective).
        // Pad -, "," and ENTER duplicate their main-block cells (the reference matrix lists
        // only f0-f9/. for the pad); pad digits use their own f-cells.
        var padX = maxX + SectionGap;
        foreach (var (label, row, col, pr, pc) in new[]
                 {
                     ("7", 1, 2, 0, 0), ("8", 1, 3, 0, 1), ("9", 0, 3, 0, 2), ("-", 3, 1, 0, 3),
                     ("4", 2, 4, 1, 0), ("5", 1, 4, 1, 1), ("6", 0, 4, 1, 2), (",", 3, 7, 1, 3),
                     ("1", 1, 5, 2, 0), ("2", 1, 6, 2, 1), ("3", 0, 5, 2, 2),
                 })
        {
            AddKey(keys, $"pad:{label}", padX + pc * RowPitch, r1Y + pr * RowPitch,
                label, (byte)row, (byte)col, accent: "accent-blue");
        }

        keys.Add(new EmulatorKeyDefinition("pad:ENTER",
            new Rect(padX + 3 * RowPitch, r3Y, 46, 2 * KeySize + Gap),
            [KeycapRowBuilder.Legend(padX + 3 * RowPitch, r3Y, "ENTER", 4, KeySize - 4, size: 10)],
            [Signal(0, 6)], AutomationName: "Enter", Accent: "accent-blue"));
        keys.Add(new EmulatorKeyDefinition("pad:0",
            new Rect(padX, r4Y, 2 * KeySize + Gap, KeySize),
            [KeycapRowBuilder.Legend(padX, r4Y, "0", KeySize - 5, 12)],
            [Signal(1, 7)], AutomationName: "0", Accent: "accent-blue"));
        keys.Add(new EmulatorKeyDefinition("pad:.",
            new Rect(padX + 2 * RowPitch, r4Y, KeySize, KeySize),
            [KeycapRowBuilder.Legend(padX + 2 * RowPitch, r4Y, ".", KeySize / 2 - 2, 12)],
            [Signal(0, 7)], AutomationName: ".", Accent: "accent-blue"));
        maxX = padX + 3 * RowPitch + 46;

        // Front function row f0-f4: no documented matrix cells anywhere (not in the ViewModel
        // table, not in the reference matrix, not in the firmware tests) - silent placeholders
        // with tooltips instead of guesses that would type garbage.
        var fX = maxX + SectionGap;
        for (var f = 0; f < 5; f++)
        {
            keys.Add(new EmulatorKeyDefinition($"f:{f}",
                new Rect(fX + f * RowPitch, r3Y, KeySize, KeySize),
                [KeycapRowBuilder.Legend(fX + f * RowPitch, r3Y, $"f{f}", 12, 12, color: "#808080", size: 10)],
                [UnknownSignal], AutomationName: $"f{f}",
                ToolTip: $"f{f} - no documented matrix cell, sends nothing"));
        }

        maxX = fX + 5 * RowPitch;
        return new EmulatorKeyboardLayout("cpc464", new Size(maxX, bottomY), keys);
    }

    private static double AddKey(List<EmulatorKeyDefinition> keys, string id, double x, double y,
        string label, byte row, byte col, double widthPx = KeySize, double fontSize = 13, string? accent = null)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, widthPx, KeySize),
            [KeycapRowBuilder.Legend(x, y, label, label.Length > 1 ? 4 : (widthPx - 10) / 2, 12, size: label.Length > 3 ? 9 : fontSize)],
            [Signal(row, col)], AutomationName: label, Accent: accent));
        return x + widthPx;
    }

    private static double AddWideKey(List<EmulatorKeyDefinition> keys, string id, double x, double y,
        string label, byte row, byte col, double widthPx, double fontSize = 10)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, widthPx, KeySize),
            [KeycapRowBuilder.Legend(x, y, label, 4, 12, size: fontSize)], [Signal(row, col)], AutomationName: label));
        return x + widthPx;
    }

    /// <summary>The row-2 key with an unreadable legend (see the geometry CSV): a blank keycap
    /// with the <see cref="UnknownSignal"/> placeholder - clicking it is a safe no-op.</summary>
    private static double AddBlankKey(List<EmulatorKeyDefinition> keys, string id, double x, double y)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, KeySize, KeySize), [],
            [UnknownSignal], AutomationName: "Unknown key", ToolTip: "Legend unreadable on the reference photo - sends nothing"));
        return x + KeySize;
    }

    /// <summary>G key carrying its BELL hint as a small second legend, exactly like the photo.</summary>
    private static double AddBellKey(List<EmulatorKeyDefinition> keys, double x, double y)
    {
        keys.Add(new EmulatorKeyDefinition("row3:G", new Rect(x, y, KeySize, KeySize),
            [KeycapRowBuilder.Legend(x, y, "BELL", 10, 1, size: 8),
             KeycapRowBuilder.Legend(x, y, "G", 14, 12)],
            [Signal(6, 4)], AutomationName: "G"));
        return x + KeySize;
    }

    /// <summary>A keycap that only produces its character with Shift held (", *, ?-family):
    /// the ",S" signal holds matrix Shift (2,5) for the press, exactly like the firmware tests'
    /// own PressKey helper.</summary>
    private static double AddShiftedKey(List<EmulatorKeyDefinition> keys, string id, double x, double y,
        string label, byte row, byte col)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, KeySize, KeySize),
            [KeycapRowBuilder.Legend(x, y, label, KeySize / 2 - 5, 12)],
            [Signal(row, col, shift: true)], AutomationName: label));
        return x + KeySize;
    }
}
