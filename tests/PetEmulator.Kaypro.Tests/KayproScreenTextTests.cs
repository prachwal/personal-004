using NUnit.Framework;

namespace PetEmulator.Kaypro.Tests;

/// <summary>Kaypro already has the ideal "read the screen as text" tool - <see cref="KayproVideo.GetText"/>
/// reads real character codes directly out of video RAM (see its own doc comment: 80x24, ASCII, no
/// remapping needed), unlike PET/VIC-20 (Commodore screen codes, see PetEmulator.TestSupport.CbmScreenCode)
/// or CPC464 (no character buffer at all - needs pixel-glyph OCR, see ScreenTextOcr). This test is
/// the missing proof that GetText() actually round-trips real typed keyboard input, the same
/// guarantee this suite's sibling machines now each have one way or another.</summary>
public sealed class KayproScreenTextTests
{
    [Test]
    [Category("BinaryBoot")]
    public void RealBoot_TypedCommandEchoesThroughGetText()
    {
        var root = FindRepositoryRoot();
        var machine = new KayproMachine();
        machine.LoadMonitorRom(File.ReadAllBytes(Path.Combine(root, "roms", "kaypro", "kaypro-81-149c.bin")));
        machine.InsertDisk(0, new KayproDiskImage(
            File.ReadAllBytes(Path.Combine(root, "roms", "kaypro", "cpm22-rom149.dsk")),
            firstSectorId: 0));
        machine.Reset();

        for (var batch = 0; batch < 40 && !machine.Video.GetText().Contains("A>"); batch++)
            machine.Run(100_000);
        Assert.That(machine.Video.GetText(), Does.Contain("A>"), "CP/M should boot to the A> prompt");

        foreach (var b in "DIR"u8)
        {
            machine.FeedKeyboardByte(b);
            machine.Run(50_000);
        }

        Assert.That(machine.Video.GetText(), Does.Contain("A>DIR"),
            "typing D-I-R through the real PIO keyboard byte queue should echo on screen exactly as typed");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
