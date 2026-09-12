namespace PetEmulator.Pet.Fonts;

/// <summary>A loaded, queryable bitmap font: fixed-size glyphs, one row of pixel bits at a time.</summary>
public interface IGlyphFont
{
    /// <summary>Pixels per glyph row (e.g. 8).</summary>
    int GlyphWidth { get; }

    /// <summary>Rows per glyph (e.g. 8).</summary>
    int GlyphHeight { get; }

    /// <summary>
    /// One row of the glyph for <paramref name="characterCode"/> (a character-ROM index; values
    /// above 255 address the second 2 KB bank of a 4 KB PET character ROM), MSB = leftmost pixel.
    /// Must never throw — out-of-range
    /// <paramref name="characterCode"/> or <paramref name="row"/> return a blank (0) row.
    /// </summary>
    byte GetGlyphRow(int characterCode, int row);
}
