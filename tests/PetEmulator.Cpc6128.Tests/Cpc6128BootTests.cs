using NUnit.Framework;

namespace PetEmulator.Cpc6128.Tests;

/// <summary>End-to-end real-firmware boot for the CPC6128. The test types through the emulated
/// keyboard matrix and validates rendered output, so it does not rely on BASIC memory shortcuts.
/// </summary>
public sealed class Cpc6128BootTests
{
    private const int CellWidth = 8, CellHeight = 8, ScreenWidth = 320;

    [Test]
    [Category("BinaryBoot")]
    public void RealFirmwareBootsAndExecutesPrintOnePlusOne()
    {
        var rom = File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), "roms", "cpc6128", "cpc6128.rom"));

        var reference = new Cpc6128Machine(rom);
        Boot(reference);
        Type(reference, "PRINT 2");
        PressKey(reference, 2, 2);
        reference.Run(300_000);
        var referenceTwo = Glyph(reference, row: 10, col: 1);

        var machine = new Cpc6128Machine(rom);
        Boot(machine);
        Type(machine, "PRINT 1");
        PressKey(machine, 3, 4, withShift: true);
        Type(machine, "1");
        PressKey(machine, 2, 2);
        machine.Run(300_000);

        var inputLine = RowText(machine, 9);
        Assert.That(inputLine, Does.StartWith("##### ###"),
            $"echoed input line silhouette was '{inputLine}'");

        var readyAgain = RowText(machine, 11);
        Assert.That(readyAgain, Is.EqualTo(RowText(machine, 8)),
            $"expected 'Ready' to reappear after execution; row11 was '{readyAgain}'");

        Assert.That(Glyph(machine, row: 10, col: 1), Is.EqualTo(referenceTwo),
            "PRINT 1+1 did not render the digit '2'");
    }

    private static void Boot(Cpc6128Machine machine) => machine.Run(2_000_000);

    private static void Type(Cpc6128Machine machine, string text)
    {
        foreach (var ch in text)
        {
            var (row, col, shift) = KeyFor(ch);
            PressKey(machine, row, col, shift);
        }
    }

    private static void PressKey(Cpc6128Machine machine, byte row, byte col, bool withShift = false)
    {
        const int Shift = 5;
        if (withShift) machine.Keyboard.SetKey(2, Shift, true);
        machine.Keyboard.SetKey(row, col, true);
        machine.Run(25_000);
        machine.Keyboard.SetKey(row, col, false);
        if (withShift) machine.Keyboard.SetKey(2, Shift, false);
        machine.Run(15_000);
    }

    private static (byte Row, byte Col, bool Shift) KeyFor(char ch) => ch switch
    {
        'P' => (3, 3, false),
        'R' => (6, 2, false),
        'I' => (4, 3, false),
        'N' => (5, 6, false),
        'T' => (6, 3, false),
        ' ' => (5, 7, false),
        '1' => (8, 0, false),
        '2' => (8, 1, false),
        _ => throw new ArgumentOutOfRangeException(nameof(ch), ch, "no matrix position mapped for this character"),
    };

    private static byte[] Glyph(Cpc6128Machine machine, int row, int col)
    {
        machine.GateArray.RenderFrame();
        var pixels = machine.GateArray.Pixels;
        var background = MostCommonInk(pixels, row, col);
        var glyph = new byte[CellWidth * CellHeight];
        for (var dy = 0; dy < CellHeight; dy++)
        for (var dx = 0; dx < CellWidth; dx++)
            glyph[dy * CellWidth + dx] = (byte)(pixels[PixelIndex(row, col, dx, dy)] == background ? 0 : 1);
        return glyph;
    }

    private static string RowText(Cpc6128Machine machine, int row)
    {
        machine.GateArray.RenderFrame();
        var pixels = machine.GateArray.Pixels;
        var text = new System.Text.StringBuilder();
        for (var col = 0; col < ScreenWidth / CellWidth; col++)
        {
            var background = MostCommonInk(pixels, row, col);
            var foreground = false;
            for (var dy = 0; dy < CellHeight && !foreground; dy++)
            for (var dx = 0; dx < CellWidth; dx++)
                if (pixels[PixelIndex(row, col, dx, dy)] != background) { foreground = true; break; }
            text.Append(foreground ? '#' : ' ');
        }
        return text.ToString().TrimEnd();
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
        return counts.OrderByDescending(pair => pair.Value).First().Key;
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
