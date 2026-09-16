using PetEmulator.CpcFdc;
using NUnit.Framework;

namespace PetEmulator.Cpc6128.Tests;

public sealed class Cpc6128Cpm22BootTests
{
    [Test]
    [Category("BinaryBoot")]
    public void RealCpm22DiskBootsToSystemPrompt()
    {
        var machine = new Cpc6128Machine(File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), "roms", "cpc6128", "cpc6128.rom")));
        machine.LoadDisk(0, DskDiskImage.Load(File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), "roms", "cpc6128", "6128SP_4.DSK"))));
        machine.Run(10_000_000);

        machine.GateArray.RenderFrame();
        Assert.That(machine.Fdc.ReadMainStatus() & 0x80, Is.EqualTo(0x80),
            "the CP/M disk should be ready after firmware boot");
        Assert.That(RowText(machine, 6), Does.Contain("##### ###"),
            "real firmware did not render the CP/M A> prompt");

        Assert.That(machine.CycleCount, Is.GreaterThan(0));
    }

    private static string RowText(Cpc6128Machine machine, int row)
    {
        var pixels = machine.GateArray.Pixels;
        var text = new System.Text.StringBuilder();
        for (var col = 0; col < 40; col++)
        {
            var background = MostCommonInk(pixels, row, col);
            var foreground = false;
            for (var dy = 0; dy < 8 && !foreground; dy++)
            for (var dx = 0; dx < 8; dx++)
                if (pixels[(row * 8 + dy) * 320 + col * 8 + dx] != background) { foreground = true; break; }
            text.Append(foreground ? '#' : ' ');
        }
        return text.ToString().TrimEnd();
    }

    private static byte MostCommonInk(byte[] pixels, int row, int col)
    {
        var counts = new Dictionary<byte, int>();
        for (var dy = 0; dy < 8; dy++)
        for (var dx = 0; dx < 8; dx++)
        {
            var value = pixels[(row * 8 + dy) * 320 + col * 8 + dx];
            counts[value] = counts.GetValueOrDefault(value) + 1;
        }
        return counts.OrderByDescending(pair => pair.Value).First().Key;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
