using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20CharacterRomProviderTests
{
    [Test]
    public void DiscoverFonts_FindsTheVic20Chargen()
    {
        var provider = new Vic20CharacterRomProvider();

        var fonts = provider.DiscoverFonts(FindRomsRoot()).ToArray();

        fonts.Should().ContainSingle();
        fonts[0].Name.Should().Be("VIC-20 character ROM");
        fonts[0].Font.GlyphWidth.Should().Be(8);
        fonts[0].Font.GlyphHeight.Should().Be(8);
    }

    [Test]
    public void DiscoverFonts_MissingChargen_YieldsNothingInsteadOfThrowing()
    {
        var provider = new Vic20CharacterRomProvider();
        var empty = Path.Combine(Path.GetTempPath(), $"no-vic20-roms-{Guid.NewGuid():N}");

        var fonts = provider.DiscoverFonts(empty).ToArray();

        fonts.Should().BeEmpty();
    }

    private static string FindRomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms");
            if (File.Exists(Path.Combine(candidate, "vic20", "vic20-chargen.bin")))
                return candidate;
        }
        throw new DirectoryNotFoundException("Could not locate roms/vic20/vic20-chargen.bin.");
    }
}
