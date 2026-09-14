using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Pet.Tests.Fonts;

public sealed class PetCharacterRomProviderTests
{
    [Test]
    public void DiscoverFonts_FindsEveryCharacterRomUnderRomsPet()
    {
        var provider = new PetCharacterRomProvider();

        var fonts = provider.DiscoverFonts(FindRomsRoot()).ToArray();

        fonts.Should().NotBeEmpty();
        fonts.Should().OnlyContain(f => f.Name.StartsWith("characters-"));
        fonts.Should().OnlyContain(f => f.Font.GlyphWidth == 8 && f.Font.GlyphHeight == 8);
    }

    [Test]
    public void DiscoverFonts_MissingPetFolder_YieldsNothingInsteadOfThrowing()
    {
        var provider = new PetCharacterRomProvider();
        var empty = Path.Combine(Path.GetTempPath(), $"no-pet-roms-{Guid.NewGuid():N}");

        var fonts = provider.DiscoverFonts(empty).ToArray();

        fonts.Should().BeEmpty();
    }

    private static string FindRomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms");
            if (Directory.Exists(Path.Combine(candidate, "pet")))
                return candidate;
        }
        throw new DirectoryNotFoundException("Could not locate the roms/ directory.");
    }
}
