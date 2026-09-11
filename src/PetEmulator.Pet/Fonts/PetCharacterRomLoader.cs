namespace PetEmulator.Pet.Fonts;

/// <summary>
/// Loads a real PET character-ROM dump (2048 bytes = 256 chars × 8 rows, one byte per row,
/// MSB-first) into a <see cref="BitmapFont"/>.
/// </summary>
public sealed class PetCharacterRomLoader : IFontLoader
{
    private const int GlyphHeight = 8;

    public IGlyphFont Load(string binFilePath)
    {
        var data = File.ReadAllBytes(binFilePath);
        if (data.Length == 0 || data.Length % GlyphHeight != 0)
        {
            throw new InvalidDataException(
                $"'{binFilePath}' is not a valid PET character ROM: expected a non-empty size that's " +
                $"a multiple of {GlyphHeight} bytes (one row per byte, {GlyphHeight} rows per glyph), got {data.Length}.");
        }

        return new BitmapFont(data, glyphWidth: 8, glyphHeight: GlyphHeight);
    }
}
