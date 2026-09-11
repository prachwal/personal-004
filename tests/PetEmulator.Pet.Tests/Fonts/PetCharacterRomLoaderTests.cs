using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Fonts;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.Fonts;

public class PetCharacterRomLoaderTests
{
    private static string Pet2001Rom8 => Path.Combine(
        RomLocator.Directory("pet-2001-8", "characters-1.901447-08.bin"), "characters-1.901447-08.bin");

    private static string Pet2001Rom32 => Path.Combine(
        RomLocator.Directory("pet-2001-32", "characters-2.901447-10.bin"), "characters-2.901447-10.bin");

    [TestCase(nameof(Pet2001Rom8))]
    [TestCase(nameof(Pet2001Rom32))]
    public void Load_RealRom_HasSaneGlyphDimensions(string which)
    {
        var path = which == nameof(Pet2001Rom8) ? Pet2001Rom8 : Pet2001Rom32;
        var font = new PetCharacterRomLoader().Load(path);

        font.GlyphWidth.Should().Be(8);
        font.GlyphHeight.Should().Be(8);
    }

    [TestCase(nameof(Pet2001Rom8))]
    [TestCase(nameof(Pet2001Rom32))]
    public void GetGlyphRow_FullByteRange_NeverThrows(string which)
    {
        var path = which == nameof(Pet2001Rom8) ? Pet2001Rom8 : Pet2001Rom32;
        var font = new PetCharacterRomLoader().Load(path);

        for (var code = 0; code <= 255; code++)
        {
            for (var row = -1; row <= 8; row++)
            {
                var act = () => font.GetGlyphRow((byte)code, row);
                act.Should().NotThrow();
            }
        }

        // explicit edge values called out by spec
        font.GetGlyphRow(0, 0);
        font.GetGlyphRow(255, 7);
    }

    [TestCase(nameof(Pet2001Rom8))]
    [TestCase(nameof(Pet2001Rom32))]
    public void GetGlyphRow_LetterA_IsNotBlank(string which)
    {
        // PET screen code 0x01 is 'A' (screen codes, not ASCII: 0x00='@', 0x01='A' ... 0x1A='Z').
        var path = which == nameof(Pet2001Rom8) ? Pet2001Rom8 : Pet2001Rom32;
        var font = new PetCharacterRomLoader().Load(path);

        byte all = 0;
        for (var row = 0; row < font.GlyphHeight; row++)
            all |= font.GetGlyphRow(0x01, row);

        all.Should().NotBe(0, "screen code 0x01 ('A') should render a real glyph, not blank garbage");
    }

    [TestCase(nameof(Pet2001Rom8))]
    [TestCase(nameof(Pet2001Rom32))]
    public void GetGlyphRow_Space_IsBlank(string which)
    {
        var path = which == nameof(Pet2001Rom8) ? Pet2001Rom8 : Pet2001Rom32;
        var font = new PetCharacterRomLoader().Load(path);

        byte all = 0;
        for (var row = 0; row < font.GlyphHeight; row++)
            all |= font.GetGlyphRow(0x20, row);

        all.Should().Be(0, "screen code 0x20 (space) should be blank");
    }

    [Test]
    public void Load_WrongSizedFile_ThrowsInvalidData()
    {
        var badFile = Path.Combine(Path.GetTempPath(), $"bad-charrom-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(badFile, new byte[] { 1, 2, 3 }); // not a multiple of 8
        try
        {
            var act = () => new PetCharacterRomLoader().Load(badFile);
            act.Should().Throw<InvalidDataException>();
        }
        finally
        {
            File.Delete(badFile);
        }
    }

    [Test]
    public void Load_MissingFile_Throws()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.bin");
        var act = () => new PetCharacterRomLoader().Load(missing);
        act.Should().Throw<FileNotFoundException>();
    }
}
