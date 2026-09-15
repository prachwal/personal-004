using PetEmulator.TestSupport;

namespace PetEmulator.Cpc464.Tests;

/// <summary>CPC464-specific glue for <see cref="ScreenTextOcr"/>: trains a glyph table by typing
/// <c>PRINT "X"</c>/<c>PRINT 0</c> through real firmware on a fresh boot for every character this
/// suite's tests type, and gives those same tests the one verified matrix position for each
/// character (<see cref="KeyFor"/>) - see Cpc464MachineViewModel.Matrix's own doc comment for how
/// each position was confirmed against real rendered/executed output, not a reference table.</summary>
public static class Cpc464ScreenOcr
{
    public const int ScreenWidth = 320, CellWidth = 8, CellHeight = 8;

    private static readonly (int Row, int Col)[] Letters =
    [
        (8,5),(6,6),(7,6),(7,5),(7,2),(6,5),(6,4),(5,4),(4,3),(5,5),(4,5),(4,4),(4,6),(5,6),(4,2),(3,3),
        (8,3),(6,2),(7,4),(6,3),(5,2),(6,7),(7,3),(7,7),(5,3),(8,7),
    ];
    private static readonly (int Row, int Col)[] Digits =
        [(4,0),(8,0),(8,1),(7,1),(7,0),(6,1),(6,0),(5,1),(5,0),(4,1)];

    /// <summary>The one verified matrix position (plus whether Shift is held) for each character
    /// these tests type. Kept in lock-step with Cpc464MachineViewModel.Matrix's own verified subset -
    /// both exist because a desktop key mapping and a test's "type this exact string" helper are
    /// different concerns, not because either one is a fallback for the other.</summary>
    public static (byte Row, byte Col, bool Shift) KeyFor(char ch) => ch switch
    {
        >= 'A' and <= 'Z' => (Row: (byte)Letters[ch - 'A'].Row, Col: (byte)Letters[ch - 'A'].Col, Shift: false),
        >= '0' and <= '9' => (Row: (byte)Digits[ch - '0'].Row, Col: (byte)Digits[ch - '0'].Col, Shift: false),
        ' ' => (5, 7, false),
        '(' => (5, 0, true),
        ')' => (4, 1, true),
        '-' => (3, 1, false),
        '=' => (3, 1, true),
        '+' => (3, 4, true),
        '*' => (3, 5, true),
        ':' => (3, 5, false),
        '"' => (8, 1, true),
        _ => throw new ArgumentOutOfRangeException(nameof(ch), ch, "no verified matrix position for this character"),
    };

    /// <summary>Boots a fresh machine per glyph (never reuses one machine across captures, so an
    /// earlier PRINT's output can't bleed into a later glyph's cell) and reads back what real
    /// firmware actually drew for each character.</summary>
    public static Dictionary<string, char> BuildTable(byte[] rom)
    {
        var table = new Dictionary<string, char>();

        void Capture(char ch, int col, Action<Cpc464Machine> type)
        {
            var machine = new Cpc464Machine(rom);
            machine.Run(2_000_000);
            type(machine);
            Press(machine, 2, 2, false); // Enter
            machine.Run(300_000);
            machine.Bus.GateArray.RenderFrame();
            var glyph = ScreenTextOcr.CaptureGlyph(machine.Bus.GateArray.Pixels, ScreenWidth, CellWidth, CellHeight, row: 10, col);
            table[ScreenTextOcr.GlyphKey(glyph)] = ch;
        }

        // Strings (PRINT "A") don't reserve a sign column - output starts at col 0. Numeric PRINT
        // (PRINT 0) reserves col 0 for sign, so the digit itself lands at col 1 (the same oracle
        // Cpc464BootTests uses for its own digit assertion).
        for (var c = 'A'; c <= 'Z'; c++) { var ch = c; Capture(ch, 0, m => TypeQuoted(m, ch)); }
        for (var c = '0'; c <= '9'; c++)
        {
            var ch = c;
            Capture(ch, 1, m => { foreach (var pc in "PRINT ") TypeBare(m, pc); TypeBare(m, ch); });
        }
        return table;

        void TypeQuoted(Cpc464Machine m, char ch)
        {
            foreach (var c in "PRINT ") TypeBare(m, c);
            TypeBare(m, '"');
            TypeBare(m, ch);
            TypeBare(m, '"');
        }

        void TypeBare(Cpc464Machine m, char ch)
        {
            var (row, col, shift) = KeyFor(ch);
            Press(m, row, col, shift);
        }
    }

    private static void Press(Cpc464Machine machine, byte row, byte col, bool shift)
    {
        const int ShiftColumn = 5;
        if (shift) machine.Bus.Keyboard.SetKey(2, ShiftColumn, true);
        machine.Bus.Keyboard.SetKey(row, col, true);
        machine.Run(25_000);
        machine.Bus.Keyboard.SetKey(row, col, false);
        if (shift) machine.Bus.Keyboard.SetKey(2, ShiftColumn, false);
        // Wider than a plain character needs - completing a keyword can trigger real-time
        // tokenization work in the firmware's line editor that needs more idle time before the next
        // keypress (see Cpc464TapeRealFirmwareRoundTripTests, which reproduced this with "DIM").
        machine.Run(60_000);
    }
}
