namespace PetEmulator.Pet.Fonts;

/// <summary>Loads an <see cref="IGlyphFont"/> from a raw character-ROM dump on disk.</summary>
public interface IFontLoader
{
    /// <summary>
    /// Reads <paramref name="binFilePath"/> and returns a queryable font.
    /// Throws <see cref="InvalidDataException"/> if the file's size doesn't match a character ROM.
    /// </summary>
    IGlyphFont Load(string binFilePath);
}
