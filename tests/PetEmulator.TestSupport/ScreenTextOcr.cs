namespace PetEmulator.TestSupport;

/// <summary>Reads a text-mode screen back as decoded characters by matching each character cell's
/// pixel silhouette against caller-trained reference glyphs, instead of eyeballing raw pixel dumps or
/// falling back to a coarse blank/non-blank silhouette (which can't tell "2" from "Ready" apart). Any
/// machine whose framebuffer is a flat row-major array of per-pixel colour indices with a fixed
/// character-cell size can use this: train the table once per test run by rendering known characters
/// through that machine's own real firmware (e.g. typing <c>PRINT "X"</c> on a fresh boot for a BASIC
/// machine) and reuse it for every screen assertion in that machine's suite - see
/// Cpc464ScreenOcr.BuildTable for a worked example. The background/foreground split is per-cell
/// majority-colour (whichever colour covers more than half the cell is "background"), which is robust
/// to different foreground/background colour pairs across machines and video modes.</summary>
public static class ScreenTextOcr
{
    /// <summary>Extracts one character cell as a background/foreground bitmask (0 = background,
    /// 1 = foreground), the shape <see cref="GlyphKey"/> and <see cref="DecodeRow"/> expect.</summary>
    public static byte[] CaptureGlyph(byte[] pixels, int screenWidth, int cellWidth, int cellHeight, int row, int col)
    {
        var background = MostCommonValue(pixels, screenWidth, cellWidth, cellHeight, row, col);
        var glyph = new byte[cellWidth * cellHeight];
        for (var dy = 0; dy < cellHeight; dy++)
        for (var dx = 0; dx < cellWidth; dx++)
            glyph[dy * cellWidth + dx] = (byte)(pixels[PixelIndex(screenWidth, cellWidth, cellHeight, row, col, dx, dy)] == background ? 0 : 1);
        return glyph;
    }

    /// <summary>A glyph's bitmask as a dictionary key - stable and human-inspectable (each pixel
    /// becomes a '0' or '1' character) rather than a hash, so a mismatch is diffable in a debugger.</summary>
    public static string GlyphKey(byte[] glyph) => string.Concat(glyph.Select(b => (char)('0' + b)));

    /// <summary>Decodes one text row into a string, looking each cell's glyph up in <paramref name="table"/>
    /// (from <see cref="CaptureGlyph"/>/<see cref="GlyphKey"/>). A blank cell always decodes to a space
    /// without consulting the table (every machine's blank cell looks the same: all-background). A
    /// non-blank cell absent from the table decodes to <paramref name="unknown"/> - expected for glyphs
    /// the caller never trained (e.g. tokenized keywords rendered differently from typed letters), and a
    /// deliberate signal rather than a silent wrong guess.</summary>
    public static string DecodeRow(byte[] pixels, int screenWidth, int columns, int cellWidth, int cellHeight,
        int row, IReadOnlyDictionary<string, char> table, char unknown = '?')
    {
        var sb = new System.Text.StringBuilder(columns);
        for (var col = 0; col < columns; col++)
        {
            var glyph = CaptureGlyph(pixels, screenWidth, cellWidth, cellHeight, row, col);
            sb.Append(glyph.All(v => v == 0) ? ' ' : table.TryGetValue(GlyphKey(glyph), out var ch) ? ch : unknown);
        }
        return sb.ToString().TrimEnd();
    }

    private static byte MostCommonValue(byte[] pixels, int screenWidth, int cellWidth, int cellHeight, int row, int col)
    {
        var counts = new Dictionary<byte, int>();
        for (var dy = 0; dy < cellHeight; dy++)
        for (var dx = 0; dx < cellWidth; dx++)
        {
            var value = pixels[PixelIndex(screenWidth, cellWidth, cellHeight, row, col, dx, dy)];
            counts[value] = counts.GetValueOrDefault(value) + 1;
        }
        return counts.OrderByDescending(kv => kv.Value).First().Key;
    }

    private static int PixelIndex(int screenWidth, int cellWidth, int cellHeight, int row, int col, int dx, int dy) =>
        (row * cellHeight + dy) * screenWidth + col * cellWidth + dx;
}
