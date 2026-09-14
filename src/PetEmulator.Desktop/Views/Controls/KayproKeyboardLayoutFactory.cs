using Avalonia;
using PetEmulator.Kaypro.Keyboard;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Builds the on-screen <see cref="EmulatorKeyboardLayout"/> for the Kaypro II, matching
/// the real keyboard's photo: single number row (each keycap shows its shift symbol stacked above
/// the digit, not a separate symbol row), ESC at the top-left corner, and the arrow-key cluster
/// sitting immediately right of the main block with the numeric pad further right again - not
/// stacked on top of the arrows.
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
/// No Shift key is modeled: the framework's layouts are static once built, so a key's own signal
/// can't change based on another key's toggle state. A stacked pair (e.g. "1"/"!") is genuinely two
/// separate clickable half-height buttons sharing one keycap's footprint, not one physical key -
/// the real keycap is one key with two legends, this is the closest a static layout gets without
/// losing the ability to actually send the shifted byte by clicking it.</summary>
public static class KayproKeyboardLayoutFactory
{
    private const double KeySize = 40;
    private const double Gap = 5;
    private const double StackedShiftHeight = 14;
    private const double StackedMainHeight = KeySize - StackedShiftHeight;

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
        double y = 0;
        double maxX = 0;

        // Row 1: ESC, backtick, the ten stacked digit/symbol pairs, BACKSPACE.
        double x = 0;
        x = AddKey(keys, "row1:ESC", x, y, "ESC", Value("ESC")) + Gap;
        x = AddKey(keys, "row1:`", x, y, "`", Value("`")) + Gap;
        foreach (var (digit, symbol) in DigitShiftPairs)
            x = AddStackedPair(keys, $"row1:{digit}", x, y, symbol, digit) + Gap;
        x = AddWideKey(keys, "row1:BACKSPACE", x, y, "BACKSPACE", Value("BACKSPACE")) + Gap;
        maxX = Math.Max(maxX, x);
        var row1End = x;
        y += KeySize + Gap;

        // Row 2: TAB, QWERTYUIOP, brackets.
        x = 0;
        foreach (var label in new[] { "TAB", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]" })
            x = AddKey(keys, $"row2:{label}", x, y, label, Value(label)) + Gap;
        maxX = Math.Max(maxX, x);
        var row2End = x;
        y += KeySize + Gap;

        // Row 3: CTRL, home row, stacked ;/: and '/", RETURN.
        x = 0;
        x = AddKey(keys, "row3:CTRL-C", x, y, "CTRL", Value("CTRL-C")) + Gap;
        foreach (var label in new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" })
            x = AddKey(keys, $"row3:{label}", x, y, label, Value(label)) + Gap;
        x = AddStackedPair(keys, "row3:;", x, y, ":", ";") + Gap;
        x = AddStackedPair(keys, "row3:'", x, y, "\"", "'") + Gap;
        x = AddWideKey(keys, "row3:RETURN", x, y, "RETURN", Value("RETURN")) + Gap;
        maxX = Math.Max(maxX, x);
        y += KeySize + Gap;

        // Row 4: ZXCVBNM, comma/period/slash (no shift legends modeled for these).
        x = 0;
        foreach (var label in new[] { "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/" })
            x = AddKey(keys, $"row4:{label}", x, y, label, Value(label)) + Gap;
        maxX = Math.Max(maxX, x);
        y += KeySize + Gap;

        // Row 5: SPACE.
        keys.Add(new EmulatorKeyDefinition("row5:SPACE", new Rect(KeySize * 2, y, KeySize * 8, KeySize),
            [KeycapRowBuilder.Legend(KeySize * 2, y, "SPACE", KeySize * 3.5, KeySize / 2 - 6)],
            [Signal(Value("SPACE"))], AutomationName: "Space"));
        maxX = Math.Max(maxX, KeySize * 10 + Gap);
        y += KeySize + Gap;

        // Arrow-key cluster immediately right of the main block (inverted-T: up over left/down/right),
        // spanning rows 1-2's height. The numeric pad sits further right again, not stacked below.
        var arrowX = Math.Max(row1End, row2End) + Gap * 3;
        AddKey(keys, "arrow:up", arrowX + KeySize + Gap, 0, "↑", KayproKeyMap.CursorUp);
        AddKey(keys, "arrow:left", arrowX, KeySize + Gap, "←", KayproKeyMap.CursorLeft);
        AddKey(keys, "arrow:down", arrowX + KeySize + Gap, KeySize + Gap, "↓", KayproKeyMap.CursorDown);
        AddKey(keys, "arrow:right", arrowX + (KeySize + Gap) * 2, KeySize + Gap, "→", KayproKeyMap.CursorRight);
        var arrowBlockEnd = arrowX + (KeySize + Gap) * 3;

        var padX = arrowBlockEnd + Gap * 3;
        var pad = KayproKeyMap.NumericPad;
        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var index = row * 3 + col;
                if (index >= pad.Count) continue;
                AddKey(keys, $"pad:{row},{col}", padX + col * (KeySize + Gap), row * (KeySize + Gap),
                    pad[index].Label, pad[index].Value);
            }
        }
        maxX = Math.Max(maxX, padX + 3 * (KeySize + Gap));

        return new EmulatorKeyboardLayout("kaypro-ii", new Size(maxX, y), keys);
    }

    private static readonly (string Digit, string Symbol)[] DigitShiftPairs =
    [
        ("1", "!"), ("2", "@"), ("3", "#"), ("4", "$"), ("5", "%"),
        ("6", "^"), ("7", "&"), ("8", "*"), ("9", "("), ("0", ")"),
        ("-", "_"), ("=", "+"),
    ];

    private static byte Value(string label) => KayproKeyMap.MainKeys.Single(k => k.Label == label).Value;

    /// <summary>One normal-size key; returns the x just past it.</summary>
    private static double AddKey(List<EmulatorKeyDefinition> keys, string id, double x, double y, string label, byte value)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, KeySize, KeySize),
            [KeycapRowBuilder.Legend(x, y, label, label.Length > 1 ? 4 : 14, 12, size: label.Length > 3 ? 9 : 13)],
            [Signal(value)], AutomationName: label));
        return x + KeySize;
    }

    /// <summary>A 1.6x-wide key (BACKSPACE/RETURN); returns the x just past it.</summary>
    private static double AddWideKey(List<EmulatorKeyDefinition> keys, string id, double x, double y, string label, byte value)
    {
        var width = KeySize * 1.6;
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, width, KeySize),
            [KeycapRowBuilder.Legend(x, y, label, 4, 12, size: 10)], [Signal(value)], AutomationName: label));
        return x + width;
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
