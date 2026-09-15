using NUnit.Framework;

namespace PetEmulator.Cpc464.Tests;

/// <summary>End-to-end real-firmware boot: run the actual Amstrad ROM to the BASIC "Ready" prompt,
/// type a program through the emulated keyboard matrix exactly as the firmware's own PPI keyboard
/// scan sees it, and confirm the rendered output pixels are the expected glyph - not a shortcut
/// that pokes memory or calls BASIC internals directly.</summary>
public sealed class Cpc464BootTests
{
    private const int CellWidth = 8, CellHeight = 8, ScreenWidth = 320;

    [Test]
    [Category("BinaryBoot")]
    public void RealFirmwareBootsAndExecutesPrintOnePlusOne()
    {
        var rom = File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), "roms", "cpc464", "cpc464.rom"));

        // Ground truth for what the ROM's own font draws for '2' - captured independently in the
        // same run rather than hard-coded, so the assertion can't rot if the character ROM ever
        // changes. "PRINT 2" echoes '2' on the input line at column 6 (after "PRINT ").
        var reference = new Cpc464Machine(rom);
        Boot(reference);
        Type(reference, "PRINT 2");
        var referenceTwo = Glyph(reference, row: 9, col: 6);

        var machine = new Cpc464Machine(rom);
        Boot(machine);
        // The real Amstrad "+" is Shift + matrix (3,4) - the bare key at (3,4) is a different,
        // unrelated glyph; found by an exhaustive matrix sweep against a "PRINT 1<key>1" oracle
        // after the plain (unshifted) key silently produced the wrong result.
        Type(machine, "PRINT 1", withShift: false);
        TypeKey(machine, 3, 4, withShift: true);
        Type(machine, "1", withShift: false);
        PressKey(machine, 2, 2); // Enter
        machine.Run(300_000);

        // RowText is a coarse blank/non-blank silhouette, not decoded text: "PRINT" (5 marks),
        // the space, then "1+1" (3 marks) for the 9 characters just typed.
        var inputLine = RowText(machine, 9);
        Assert.That(inputLine, Does.StartWith("##### ###"), $"echoed input line silhouette was '{inputLine}'");

        var readyAgain = RowText(machine, 11);
        Assert.That(readyAgain, Is.EqualTo(RowText(machine, 8)),
            $"expected 'Ready' to reappear after execution (no syntax error); row11 was '{readyAgain}'");

        var result = Glyph(machine, row: 10, col: 1); // column 0 is BASIC's sign column
        Assert.That(result, Is.EqualTo(referenceTwo), "PRINT 1+1 did not render the digit '2'");
    }

    private static void Boot(Cpc464Machine machine) => machine.Run(2_000_000);

    private static void Type(Cpc464Machine machine, string text, bool withShift = false)
    {
        foreach (var ch in text)
        {
            var (row, col) = KeyFor(ch);
            PressKey(machine, row, col, withShift);
        }
    }

    private static void TypeKey(Cpc464Machine machine, byte row, byte col, bool withShift) =>
        PressKey(machine, row, col, withShift);

    /// <summary>Holds a key long enough for the firmware's PPI keyboard scan (driven by the Gate
    /// Array's ~300 Hz interrupt) to see it on at least one pass, then releases it with enough
    /// idle time for the scan to see the release before the next key goes down - both margins
    /// found empirically (a few keyboard-scan interrupts' worth of instructions).</summary>
    private static void PressKey(Cpc464Machine machine, byte row, byte col, bool withShift = false)
    {
        const int Shift = 5;
        if (withShift) machine.Bus.Keyboard.SetKey(2, Shift, true);
        machine.Bus.Keyboard.SetKey(row, col, true);
        machine.Run(25_000);
        machine.Bus.Keyboard.SetKey(row, col, false);
        if (withShift) machine.Bus.Keyboard.SetKey(2, Shift, false);
        machine.Run(15_000);
    }

    /// <summary>The 10x8 physical matrix positions this test needs, verified by matching
    /// echoed/rendered glyphs against the real firmware (see the class doc comment).</summary>
    private static (byte Row, byte Col) KeyFor(char ch) => ch switch
    {
        'P' => (3, 3),
        'R' => (6, 2),
        'I' => (4, 3),
        'N' => (5, 6),
        'T' => (6, 3),
        ' ' => (5, 7),
        '1' => (8, 0),
        '2' => (8, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(ch), ch, "no matrix position mapped for this character"),
    };

    private static byte[] Glyph(Cpc464Machine machine, int row, int col)
    {
        machine.Bus.GateArray.RenderFrame();
        var pixels = machine.Bus.GateArray.Pixels;
        var background = MostCommonInk(pixels, row, col);
        var glyph = new byte[CellWidth * CellHeight];
        for (var dy = 0; dy < CellHeight; dy++)
        for (var dx = 0; dx < CellWidth; dx++)
            glyph[dy * CellWidth + dx] = (byte)(pixels[PixelIndex(row, col, dx, dy)] == background ? 0 : 1);
        return glyph;
    }

    /// <summary>One text row as a coarse string: '#' where a cell has any ink different from its
    /// own majority (background) colour, ' ' otherwise - enough to compare whole lines (echoed
    /// input, the "Ready" prompt) without needing an exact per-glyph oracle for every character.</summary>
    private static string RowText(Cpc464Machine machine, int row)
    {
        machine.Bus.GateArray.RenderFrame();
        var pixels = machine.Bus.GateArray.Pixels;
        var sb = new System.Text.StringBuilder();
        for (var col = 0; col < ScreenWidth / CellWidth; col++)
        {
            var background = MostCommonInk(pixels, row, col);
            var foreground = false;
            for (var dy = 0; dy < CellHeight && !foreground; dy++)
            for (var dx = 0; dx < CellWidth; dx++)
                if (pixels[PixelIndex(row, col, dx, dy)] != background) { foreground = true; break; }
            sb.Append(foreground ? '#' : ' ');
        }
        return sb.ToString().TrimEnd();
    }

    private static byte MostCommonInk(byte[] pixels, int row, int col)
    {
        var counts = new Dictionary<byte, int>();
        for (var dy = 0; dy < CellHeight; dy++)
        for (var dx = 0; dx < CellWidth; dx++)
        {
            var value = pixels[PixelIndex(row, col, dx, dy)];
            counts[value] = counts.GetValueOrDefault(value) + 1;
        }
        return counts.OrderByDescending(kv => kv.Value).First().Key;
    }

    private static int PixelIndex(int row, int col, int dx, int dy) =>
        (row * CellHeight + dy) * ScreenWidth + col * CellWidth + dx;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
