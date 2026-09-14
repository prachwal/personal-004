using Avalonia;
using PetEmulator.Kaypro.Keyboard;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Builds the on-screen <see cref="EmulatorKeyboardLayout"/> for the Kaypro II.
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
/// can't change based on another key's toggle state. Shifted symbols with no separate legend of
/// their own (!@#$%^&amp;*()_+") are exposed as their own standalone buttons instead of sharing a
/// slot with their unshifted digit - a deliberate simplification, not an oversight.</summary>
public static class KayproKeyboardLayoutFactory
{
    private const double KeySize = 40;
    private const double Gap = 5;

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

        maxX = Math.Max(maxX, AddRow(keys, "row1", y, [
            "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=",
        ], wideLast: "BACKSPACE"));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, "row2", y, [
            "TAB", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]",
        ]));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, "row3", y, [
            "CTRL-C", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'",
        ], wideLast: "RETURN"));
        y += KeySize + Gap;

        maxX = Math.Max(maxX, AddRow(keys, "row4", y, [
            "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/",
        ]));
        y += KeySize + Gap;

        // Shifted symbols with no unshifted key of their own - see class doc comment.
        maxX = Math.Max(maxX, AddRow(keys, "row5", y, [
            "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "+", "\"", ":",
        ]));
        y += KeySize + Gap;

        keys.Add(new EmulatorKeyDefinition("row6:SPACE", new Rect(KeySize * 2, y, KeySize * 8, KeySize),
            [KeycapRowBuilder.Legend(KeySize * 2, y, "SPACE", KeySize * 3.5, KeySize / 2 - 6)],
            [Signal(KayproKeyMap.MainKeys.Single(k => k.Label == "SPACE").Value)], AutomationName: "Space"));
        AddKey(keys, "row6:ESC", KeySize * 10.5, y, "ESC",
            KayproKeyMap.MainKeys.Single(k => k.Label == "ESC").Value);
        maxX = Math.Max(maxX, KeySize * 11.5 + Gap);
        y += KeySize + Gap;

        // Arrow keys, inverted-T layout, to the right of the main body.
        var arrowX = maxX + Gap * 4;
        AddKey(keys, "arrow:up", arrowX + KeySize + Gap, 0, "↑", KayproKeyMap.CursorUp);
        AddKey(keys, "arrow:left", arrowX, KeySize + Gap, "←", KayproKeyMap.CursorLeft);
        AddKey(keys, "arrow:down", arrowX + KeySize + Gap, KeySize + Gap, "↓", KayproKeyMap.CursorDown);
        AddKey(keys, "arrow:right", arrowX + (KeySize + Gap) * 2, KeySize + Gap, "→", KayproKeyMap.CursorRight);
        var arrowBlockWidth = (KeySize + Gap) * 3;

        // Numeric pad, calculator-style (3x3 digits + 0 + operators), below the arrow keys.
        var padX = arrowX;
        var padY = (KeySize + Gap) * 2 + Gap * 2;
        var pad = KayproKeyMap.NumericPad;
        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var index = row * 3 + col;
                if (index >= pad.Count) continue;
                AddKey(keys, $"pad:{row},{col}", padX + col * (KeySize + Gap), padY + row * (KeySize + Gap),
                    pad[index].Label, pad[index].Value);
            }
        }

        maxX = Math.Max(maxX, arrowX + Math.Max(arrowBlockWidth, KeySize * 3) + KeySize);
        y = Math.Max(y, padY + 4 * (KeySize + Gap));

        return new EmulatorKeyboardLayout("kaypro-ii", new Size(maxX, y), keys);
    }

    private static double AddRow(List<EmulatorKeyDefinition> keys, string rowId, double y,
        IReadOnlyList<string> labels, string? wideLast = null)
    {
        double x = 0;
        foreach (var label in labels)
        {
            var value = KayproKeyMap.MainKeys.Single(k => k.Label == label).Value;
            AddKey(keys, $"{rowId}:{label}", x, y, label, value);
            x += KeySize + Gap;
        }
        if (wideLast is not null)
        {
            var value = KayproKeyMap.MainKeys.Single(k => k.Label == wideLast).Value;
            var width = KeySize * 1.6;
            keys.Add(new EmulatorKeyDefinition($"{rowId}:{wideLast}", new Rect(x, y, width, KeySize),
                [KeycapRowBuilder.Legend(x, y, wideLast, 4, 12, size: 10)], [Signal(value)], AutomationName: wideLast));
            x += width + Gap;
        }
        return x;
    }

    private static void AddKey(List<EmulatorKeyDefinition> keys, string id, double x, double y, string label, byte value)
    {
        keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, KeySize, KeySize),
            [KeycapRowBuilder.Legend(x, y, label, label.Length > 1 ? 4 : 14, 12, size: label.Length > 3 ? 9 : 13)],
            [Signal(value)], AutomationName: label));
    }
}
