namespace PetEmulator.Pet.Fonts;

/// <summary>
/// An <see cref="IGlyphFont"/> backed by a flat byte array: <c>GlyphHeight</c> bytes per
/// character code, one byte per row, MSB-first — the layout of a real PET character ROM.
/// Bounds-safe by design so it can sit in a per-frame render loop.
/// </summary>
public sealed class BitmapFont : IGlyphFont
{
    private readonly byte[] _data;

    public int GlyphWidth { get; }
    public int GlyphHeight { get; }

    public BitmapFont(byte[] data, int glyphWidth, int glyphHeight)
    {
        _data = data;
        GlyphWidth = glyphWidth;
        GlyphHeight = glyphHeight;
    }

    public byte GetGlyphRow(byte characterCode, int row)
    {
        if (row < 0 || row >= GlyphHeight) return 0;
        var offset = characterCode * GlyphHeight + row;
        if (offset < 0 || offset >= _data.Length) return 0;
        return _data[offset];
    }
}
