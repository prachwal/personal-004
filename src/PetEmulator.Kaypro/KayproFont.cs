namespace PetEmulator.Kaypro;

/// <summary>
/// Kaypro II 81-146A character ROM mapping.
///
/// The ROM is 256 glyphs × 8 rows.  The visible glyphs live in the upper
/// half, bit 7 is not part of the screen-code selection, and a zero bit turns
/// the CRT beam on; those three hardware details are intentionally kept here
/// instead of leaking into the Avalonia renderer.
/// </summary>
public sealed class KayproFont
{
    public const int GlyphWidth = 8;
    public const int GlyphHeight = 8;
    public const int GlyphCount = 256;
    public const int RomSize = GlyphCount * GlyphHeight;

    private readonly byte[] _rom;

    private KayproFont(byte[] rom) => _rom = rom;

    public static KayproFont Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var rom = File.ReadAllBytes(path);
        if (rom.Length != RomSize)
            throw new InvalidDataException($"Kaypro character ROM must be {RomSize} bytes.");
        return new KayproFont(rom);
    }

    /// <summary>Returns an inverted, MSB-first raster row for a VRAM screen code.</summary>
    public byte GetRow(byte screenCode, int row)
    {
        if ((uint)row >= GlyphHeight)
            throw new ArgumentOutOfRangeException(nameof(row));

        var glyph = 0x80 | (screenCode & 0x7F);
        return (byte)~_rom[glyph * GlyphHeight + row];
    }
}
