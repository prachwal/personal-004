using NUnit.Framework;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproCharacterRomProviderTests
{
    [Test]
    public void DiscoverFonts_FindsTheKayproCharacterRom()
    {
        var provider = new KayproCharacterRomProvider();

        var fonts = provider.DiscoverFonts(FindRomsRoot()).ToArray();

        Assert.That(fonts, Has.Length.EqualTo(1));
        Assert.That(fonts[0].Name, Is.EqualTo("Kaypro II character ROM"));
        Assert.That(fonts[0].Font.GlyphWidth, Is.EqualTo(8));
        Assert.That(fonts[0].Font.GlyphHeight, Is.EqualTo(8));
    }

    [Test]
    public void DiscoveredFont_LetterGlyphIsNotBlank()
    {
        var provider = new KayproCharacterRomProvider();
        var font = provider.DiscoverFonts(FindRomsRoot()).Single().Font;

        // Screen code 0x80 (first visible glyph slot per KayproFont.GetRow's masking) should
        // render something, not a blank ROM gap - a loose sanity check that the raw-index adapter
        // reads the same ROM bytes GetRow does, just without the screen-code mask/invert.
        byte all = 0;
        for (var row = 0; row < font.GlyphHeight; row++)
            all |= font.GetGlyphRow(0x80, row);

        Assert.That(all, Is.Not.Zero);
    }

    [Test]
    public void DiscoverFonts_MissingRom_YieldsNothingInsteadOfThrowing()
    {
        var provider = new KayproCharacterRomProvider();
        var empty = Path.Combine(Path.GetTempPath(), $"no-kaypro-roms-{Guid.NewGuid():N}");

        var fonts = provider.DiscoverFonts(empty).ToArray();

        Assert.That(fonts, Is.Empty);
    }

    private static string FindRomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms");
            if (File.Exists(Path.Combine(candidate, "kaypro", "kaypro-81-146.bin")))
                return candidate;
        }
        throw new DirectoryNotFoundException("Could not locate roms/kaypro/kaypro-81-146.bin.");
    }
}
