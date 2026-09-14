namespace PetEmulator.Kaypro.Keyboard;

/// <summary>Byte table for the Kaypro II's 76-key detachable keyboard.
///
/// Unlike PET/VIC-20, there is no keyboard matrix exposed to the emulated system to be faithful
/// to: the real Kaypro II keyboard is a self-contained serial device with its own internal
/// encoder, connected by a 4-wire cable (+5V, ground, TX, RX) that sends "standard 7-bit ASCII
/// characters" - confirmed verbatim in the KAYPRO II Dealer Reference Manual (Non-Linear Systems,
/// 1982), section 1-5 "KEYBOARD" and Table 1-5 "KEYBOARD CABLE PINOUTS". The CPU-visible interface
/// (see <see cref="KayproSioWiring.EnqueueKeyboardByte"/>) is a plain received byte, nothing more -
/// so the "matrix faithful to the original" for Kaypro is this byte table, not a (row, column)
/// grid.
///
/// Confidence per group (all cross-checked against the KAYPRO II User's Guide, 1982):
/// - Letters/digits/Space/Return/Backspace/Tab/Esc: standard 7-bit ASCII - directly stated
///   ("standard 7-bit ASCII characters") and confirmed against the User's Guide's own "Keyboard
///   ASCII Codes" table (page 47-48).
/// - The four arrow keys: the User's Guide states they move the cursor "UP/DOWN/LEFT/RIGHT" (only
///   in certain programs, not CP/M's own line editor) and are, like the numeric pad, "user
///   programmable through the CONFIG program" - no single fixed code is baked into hardware ROM.
///   The values here (0x0B/0x0A/0x08/0x0C) are the ADM-3A cursor-movement convention the Kaypro
///   video section imitates for OUTPUT (confirmed: Backspace=non-destructive left, Line Feed=down,
///   Vertical Tab=up, Form Feed=non-destructive right) - real ADM-3A-compatible terminals of this
///   era conventionally wire the matching cursor keys to send the SAME codes their own display
///   logic interprets as cursor movement. This is the FACTORY DEFAULT this class models; a real
///   keyboard reconfigured via CONFIG could send anything.
/// - The 14-key numeric pad: NOT given a distinct default byte table in either manual (both simply
///   say it's "user programmable"). Modeled here as sending the same bytes as the equivalent
///   top-row digit/operator keys - a reasonable, commonly-cited default for a "calculator-style"
///   auxiliary pad, but NOT verified against a factory ROM dump. Treat this group as the one
///   genuinely uncertain part of this table.</summary>
public static class KayproKeyMap
{
    public const byte WarmBoot = 0x03; // CTRL-C
    public const byte CursorUp = 0x0B; // VT - ADM-3A convention, see class doc comment.
    public const byte CursorDown = 0x0A; // LF
    public const byte CursorLeft = 0x08; // BS
    public const byte CursorRight = 0x0C; // FF

    /// <summary>Every key this table knows about, in a stable authoring order (used to build the
    /// on-screen keyboard) - label, byte, and whether it repeats when held (every real key does
    /// except CTRL/ESC/RETURN per the User's Guide, but this emulator's on-screen keyboard only
    /// ever sends one byte per click regardless).</summary>
    public static IReadOnlyList<(string Label, byte Value)> MainKeys { get; } = BuildMainKeys();

    public static IReadOnlyList<(string Label, byte Value)> NumericPad { get; } = BuildNumericPad();

    public static IReadOnlyList<(string Label, byte Value)> ArrowKeys { get; } =
    [
        ("↑", CursorUp), ("↓", CursorDown), ("←", CursorLeft), ("→", CursorRight),
    ];

    private static IReadOnlyList<(string, byte)> BuildMainKeys()
    {
        var keys = new List<(string, byte)>();
        for (var c = 'A'; c <= 'Z'; c++)
            keys.Add((c.ToString(), (byte)char.ToLowerInvariant(c)));
        for (var d = '0'; d <= '9'; d++)
            keys.Add((d.ToString(), (byte)d));

        keys.AddRange(
        [
            ("SPACE", 0x20), ("RETURN", 0x0D), ("BACKSPACE", 0x08), ("TAB", 0x09), ("ESC", 0x1B),
            ("-", (byte)'-'), ("=", (byte)'='), (",", (byte)','), (".", (byte)'.'), ("/", (byte)'/'),
            (";", (byte)';'), ("'", (byte)'\''), ("[", (byte)'['), ("]", (byte)']'), ("`", (byte)'`'),
            ("!", (byte)'!'), ("@", (byte)'@'), ("#", (byte)'#'), ("$", (byte)'$'), ("%", (byte)'%'),
            ("^", (byte)'^'), ("&", (byte)'&'), ("*", (byte)'*'), ("(", (byte)'('), (")", (byte)')'),
            ("_", (byte)'_'), ("+", (byte)'+'), (":", (byte)':'), ("\"", (byte)'"'),
            ("CTRL-C", WarmBoot),
        ]);
        return keys;
    }

    private static IReadOnlyList<(string, byte)> BuildNumericPad()
    {
        // See class doc comment: not a verified factory default, modeled as duplicating the
        // equivalent digit/operator keys.
        var keys = new List<(string, byte)>();
        for (var d = '7'; d <= '9'; d++) keys.Add((d.ToString(), (byte)d));
        for (var d = '4'; d <= '6'; d++) keys.Add((d.ToString(), (byte)d));
        for (var d = '1'; d <= '3'; d++) keys.Add((d.ToString(), (byte)d));
        keys.Add(("0", (byte)'0'));
        keys.Add((".", (byte)'.'));
        keys.Add(("+", (byte)'+'));
        keys.Add(("-", (byte)'-'));
        keys.Add(("*", (byte)'*'));
        keys.Add(("/", (byte)'/'));
        return keys;
    }
}
