using Avalonia;
using PetEmulator.Kaypro.Keyboard;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Builds the on-screen <see cref="EmulatorKeyboardLayout"/> for the Kaypro II, matching
/// the real keyboard's photo: single number row (each keycap shows its shift symbol stacked above
/// the digit), ESC top-left, four arrows in a row between )0 and -=, stacked =+ and ~/`, wide
/// BACK SPACE closing row 1; TAB/QWERTY row with brackets and DEL; CTRL/CAPS LOCK home row with
/// big RETURN and backslash; wide SHIFT keys flanking ZXCVBNM with stacked ,&lt; .&gt; /? pairs
/// and LINE FEED closing row 4; wide SPACE; and a 4-column blue numeric pad (7 8 9 - / 4 5 6 , /
/// 1 2 3 + tall ENTER / wide 0 + .) right of the main block.
///
/// Unlike PET/VIC-20, this isn't a (row, column) matrix - see <see cref="KayproKeyMap"/>'s doc
/// comment for why the real hardware has none to be faithful to. Each key's SignalId directly
/// encodes the byte it sends ("kaypro:XX" hex), so <see cref="EmulatorKeyboardView"/>'s callback
/// can hand it straight to <c>KayproMachine.FeedKeyboardByte</c>. Several distinct physical keys
/// legitimately send the SAME byte (e.g. the numeric pad's "7" and the main keyboard's "7") - the
/// shared-signal ref-counting <see cref="EmulatorKeyboardState"/> already supports for dual Shift
/// keys handles that the same way; each such key still needs its own unique <c>Id</c>, which is
/// NOT the same string as its signal here (see <see cref="Id"/>).
///
/// SHIFT (left/right, shared "kaypro:SHIFT" signal) and CAPS LOCK ("kaypro:CAPS") are real
/// momentary keys, not static legends: <c>KayproMachineViewModel.SendKeyboardSignal</c> tracks
/// Shift-held state and the Caps latch, uppercasing a-z letters while either is active
/// (digits/symbols pass through - click their stacked shift halves instead, exactly like the
/// stacked-pair design below). A stacked pair (e.g. "1"/"!") is genuinely two separate clickable
/// half-height buttons sharing one keycap's footprint, not one physical key - the real keycap is
/// one key with two legends, this is the closest a static layout gets without losing the ability
/// to actually send the shifted byte by clicking it.</summary>
public static class KayproKeyboardLayoutFactory
{
    public const string ShiftSignal = "kaypro:SHIFT";
    public const string CapsSignal = "kaypro:CAPS";

    /// <summary>Signal for keycaps with no known code (the row-2 key with an unreadable legend) -
    /// the view-model callback silently ignores it, so the key is a visual placeholder that sends
    /// nothing rather than a guess that sends the wrong byte.</summary>
    public const string UnknownSignal = "kaypro:UNKNOWN";
    private const double KeySize = 40;
    private const double Gap = 5;
    private const double StackedShiftHeight = 14;
    private const double StackedMainHeight = KeySize - StackedShiftHeight;
    private const double RowPitch = KeySize + Gap;
    private const double PadGapX = 40;

    /// <summary>Measured key widths, photo pixels (≈ our 40px unit - standard keys measured
    /// 40-41): TAB 58, CAPS 57, SHIFT 71/65, LINE FEED 55, RETURN 74, BACKSPACE 45, SPACE 439,
    /// numpad 0 87, numpad ENTER 46 wide. CTRL/ESC/DEL/\ measured 40-44, kept at standard 40.</summary>
    private const double TabWidth = 58;
    private const double CapsWidth = 57;
    private const double ShiftLeftWidth = 71;
    private const double ShiftRightWidth = 65;
    private const double LineFeedWidth = 55;
    private const double ReturnWidth = 74;
    private const double BackspaceWidth = 45;
    private const double EnterPadWidth = 46;
    private const double ZeroPadWidth = 87;

    private static string Signal(byte value) => $"kaypro:{value:X2}";

    public static bool TryParseSignal(string signal, out byte value)
    {
        value = 0;
        return signal.StartsWith("kaypro:", StringComparison.Ordinal) &&
               byte.TryParse(signal[7..], System.Globalization.NumberStyles.HexNumber, null, out value);
    }

    public static EmulatorKeyboardLayout Build()
    {
        var keys = new List<EmulatorKeyDefinition>();
        const double arrowsY = 0;
        const double r1Y = RowPitch;
        const double r2Y = 2 * RowPitch;
        const double r3Y = 3 * RowPitch;
        const double r4Y = 4 * RowPitch;
        const double r5Y = 5 * RowPitch;
        double maxX = 0;

        // Row 1: ESC, ten stacked digit/symbol pairs, stacked -=, =+ and ~/`, BACK SPACE.
        double x = 0;
        x = AddKey(keys, "row1:ESC", x, r1Y, "ESC", Value("ESC")) + Gap;
        foreach (var (digit, symbol) in DigitShiftPairs)
            x = AddStackedPair(keys, $"row1:{digit}", x, r1Y, symbol, digit) + Gap;
        x = AddStackedPair(keys, "row1:-", x, r1Y, "_", "-") + Gap;
        x = AddStackedPair(keys, "row1:=", x, r1Y, "+", "=") + Gap;
        x = AddStackedPair(keys, "row1:`", x, r1Y, "~", "`") + Gap;
        x = AddWideKey(keys, "row1:BACKSPACE", x, r1Y, "BACK SPACE", Value("BACKSPACE"), widthPx: BackspaceWidth, fontSize: 7) + Gap;
        maxX = Math.Max(maxX, x);
        var row1End = x - Gap;

        // Cursor row above R1's right end (measured x-centers sit over the -= through BACKSPACE
        // columns, one pitch up - not inline in R1 as an earlier photo reading had it).
        var arrowX = row1End - (4 * KeySize + 3 * Gap);
        foreach (var (label, value) in new[] { ("↑", KayproKeyMap.CursorUp), ("↓", KayproKeyMap.CursorDown), ("←", KayproKeyMap.CursorLeft), ("→", KayproKeyMap.CursorRight) })
            arrowX = AddKey(keys, $"arrow:{label}", arrowX, arrowsY, label, value) + Gap;

        // Row 2: TAB, QWERTYUIOP, brackets, blank key with an unreadable legend, DEL.
        x = 0;
        x = AddWideKey(keys, "row2:TAB", x, r2Y, "TAB", Value("TAB"), widthPx: TabWidth) + Gap;
        foreach (var label in new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]" })
            x = AddKey(keys, $"row2:{label}", x, r2Y, label, Value(label)) + Gap;
        x = AddBlankKey(keys, "row2:BLANK", x, r2Y) + Gap;
        x = AddKey(keys, "row2:DEL", x, r2Y, "DEL", Value("DEL")) + Gap;
        maxX = Math.Max(maxX, x);

        // Row 3: CTRL, CAPS LOCK, home row (G carries its BELL hint as a second legend), big
        // RETURN, backslash.
        x = 0;
        x = AddKey(keys, "row3:CTRL-C", x, r3Y, "CTRL", Value("CTRL-C")) + Gap;
        x = AddWideKeyRaw(keys, "row3:CAPS", x, r3Y, "CAPS LOCK", CapsSignal, widthPx: CapsWidth, fontSize: 9) + Gap;
        foreach (var label in new[] { "A", "S", "D", "F", "H", "J", "K", "L" })
            x = AddKey(keys, $"row3:{label}", x, r3Y, label, Value(label)) + Gap;
        x = AddBellKey(keys, x, r3Y) + Gap;
        x = AddStackedPair(keys, "row3:;", x, r3Y, ":", ";") + Gap;
        x = AddStackedPair(keys, "row3:'", x, r3Y, "\"", "'") + Gap;
        x = AddWideKey(keys, "row3:RETURN", x, r3Y, "RETURN", Value("RETURN"), widthPx: ReturnWidth) + Gap;
        x = AddKey(keys, "row3:\\", x, r3Y, "\\", Value("\\")) + Gap;
        maxX = Math.Max(maxX, x);

        // Row 4: wide SHIFT, ZXCVBNM, stacked ,< .> /?, wide SHIFT, LINE FEED.
        x = 0;
        x = AddShiftKey(keys, "row4:SHIFT-LEFT", x, r4Y, widthPx: ShiftLeftWidth) + Gap;
        foreach (var label in new[] { "Z", "X", "C", "V", "B", "N", "M" })
            x = AddKey(keys, $"row4:{label}", x, r4Y, label, Value(label)) + Gap;
        x = AddStackedPair(keys, "row4:,", x, r4Y, "<", ",") + Gap;
        x = AddStackedPair(keys, "row4:.", x, r4Y, ">", ".") + Gap;
        x = AddStackedPair(keys, "row4:/", x, r4Y, "?", "/") + Gap;
        x = AddShiftKey(keys, "row4:SHIFT-RIGHT", x, r4Y, widthPx: ShiftRightWidth) + Gap;
        x = AddWideKey(keys, "row4:LINEFEED", x, r4Y, "LINE FEED", Value("LINE FEED"), widthPx: LineFeedWidth, fontSize: 9) + Gap;
        maxX = Math.Max(maxX, x);

        // Row 5: SPACE (measured 439 wide, starting ~1.5 keys in).
        keys.Add(new EmulatorKeyDefinition("row5:SPACE", new Rect(65, r5Y, 439, KeySize),
            [KeycapRowBuilder.Legend(65, r5Y, "SPACE", 200, KeySize / 2 - 6)],
            [Signal(Value("SPACE"))], AutomationName: "Space"));
        var bottomY = r5Y + RowPitch;

        // Numeric pad right of the main block: 7 8 9 - / 4 5 6 , / 1 2 3 + tall ENTER /
        // wide 0 + . - pad rows align with R1-R4 (the photo's half-row offset is perspective).
        var padX = maxX + PadGapX;
        var pad = KayproKeyMap.NumericPad;
        for (var index = 0; index < 11; index++)
        {
            var row = index / 4;
            var col = index % 4;
            AddKey(keys, $"pad:{row},{col}", padX + col * RowPitch, r1Y + row * RowPitch,
                pad[index].Label, pad[index].Value, accent: "accent-blue");
        }

        var enterValue = pad[11].Value;
        keys.Add(new EmulatorKeyDefinition("pad:ENTER",
            new Rect(padX + 3 * RowPitch, r2Y, EnterPadWidth, 2 * KeySize + Gap),
            [KeycapRowBuilder.Legend(padX + 3 * RowPitch, r2Y, "ENTER", 4, KeySize - 4, size: 10)],
            [Signal(enterValue)], AutomationName: "Enter", Accent: "accent-blue"));
        keys.Add(new EmulatorKeyDefinition("pad:0",
            new Rect(padX, r4Y, ZeroPadWidth, KeySize),
            [KeycapRowBuilder.Legend(padX, r4Y, "0", 38, 12)],
            [Signal(pad[12].Value)], AutomationName: "0", Accent: "accent-blue"));
        keys.Add(new EmulatorKeyDefinition("pad:.",
            new Rect(padX + 2 * RowPitch, r4Y, KeySize, KeySize),
            [KeycapRowBuilder.Legend(padX + 2 * RowPitch, r4Y, ".", KeySize / 2 - 2, 12)],
            [Signal(pad[13].Value)], AutomationName: ".", Accent: "accent-blue"));
        maxX = Math.Max(maxX, padX + 3 * RowPitch + EnterPadWidth);

        return new EmulatorKeyboardLayout("kaypro-ii", new Size(maxX, bottomY), keys);
    }

    private static readonly (string Digit, string Symbol)[] DigitShiftPairs =
    [
        ("1", "!"), ("2", "@"), ("3", "#"), ("4", "$"), ("5", "%"),
        ("6", "^"), ("7", "&"), ("8", "*"), ("9", "("), ("0", ")"),
    ];

    private static byte Value(string label) => KayproKeyMap.MainKeys.Single(k => k.Label == label).Value;

    /// <summary>One normal-size key; returns the x just past it.</summary>
    private static double AddKey(List<EmulatorKeyDefinition> keys, string id, double x, double y,
        string label, byte value, string? accent = null)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, KeySize, KeySize),
            [KeycapRowBuilder.Legend(x, y, label, label.Length > 1 ? 4 : 14, 12, size: label.Length > 3 ? 9 : 13)],
            [Signal(value)], AutomationName: label, Accent: accent));
        return x + KeySize;
    }

    /// <summary>A wide key with measured pixel width; returns the x just past it.</summary>
    private static double AddWideKey(List<EmulatorKeyDefinition> keys, string id, double x, double y,
        string label, byte value, double widthPx, double fontSize = 10) =>
        AddWideKeyRaw(keys, id, x, y, label, Signal(value), widthPx, fontSize);

    /// <summary>Wide key with a raw (non-byte) signal - CAPS LOCK's latch signal.</summary>
    private static double AddWideKeyRaw(List<EmulatorKeyDefinition> keys, string id, double x, double y,
        string label, string signal, double widthPx, double fontSize = 10)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, widthPx, KeySize),
            [KeycapRowBuilder.Legend(x, y, label, 4, 12, size: fontSize)], [signal], AutomationName: label));
        return x + widthPx;
    }

    /// <summary>A real SHIFT key: raw <see cref="ShiftSignal"/> (not a byte), tracked as a held
    /// modifier by the view-model callback - see the class doc comment.</summary>
    private static double AddShiftKey(List<EmulatorKeyDefinition> keys, string id, double x, double y,
        double widthPx)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, widthPx, KeySize),
            [KeycapRowBuilder.Legend(x, y, "SHIFT", 4, 12, size: 10)], [ShiftSignal], AutomationName: "Shift"));
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
            [Signal(Value("G"))], AutomationName: "G"));
        return x + KeySize;
    }

    /// <summary>Two half-height keys stacked in one keycap's footprint - the shift symbol on top
    /// (small), the base character below (normal size) - see class doc comment for why this is two
    /// buttons, not one. Returns the x just past the pair.</summary>
    private static double AddStackedPair(List<EmulatorKeyDefinition> keys, string idPrefix, double x, double y,
        string shiftLabel, string mainLabel)
    {
        var shiftValue = Value(shiftLabel);
        var mainValue = Value(mainLabel);
        keys.Add(new EmulatorKeyDefinition($"{idPrefix}:shift", new Rect(x, y, KeySize, StackedShiftHeight),
            [KeycapRowBuilder.Legend(x, y, shiftLabel, KeySize / 2 - 4, 1, size: 10)],
            [Signal(shiftValue)], AutomationName: shiftLabel));
        keys.Add(new EmulatorKeyDefinition($"{idPrefix}:main", new Rect(x, y + StackedShiftHeight, KeySize, StackedMainHeight),
            [KeycapRowBuilder.Legend(x, y + StackedShiftHeight, mainLabel, KeySize / 2 - 5, StackedMainHeight / 2 - 8, size: 13)],
            [Signal(mainValue)], AutomationName: mainLabel));
        return x + KeySize;
    }
}
