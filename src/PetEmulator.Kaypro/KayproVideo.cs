namespace PetEmulator.Kaypro;

/// <summary>Kaypro II text video memory: 80 visible columns by 24 rows with a 128-byte row stride.</summary>
public sealed class KayproVideo
{
    public const int Columns = 80;
    public const int Rows = 24;
    public const int LineStride = 128;
    public const int MemorySize = LineStride * Rows;
    public const int CellWidth = 8;
    public const int CellHeight = 10;
    public const int PixelWidth = Columns * CellWidth;
    public const int PixelHeight = Rows * CellHeight;

    private readonly byte[] _memory = new byte[MemorySize];

    public KayproVideoSnapshot CaptureState() => new() { Memory = _memory.ToArray() };

    public void RestoreState(KayproVideoSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Memory.Length != _memory.Length)
            throw new ArgumentException("Invalid Kaypro video memory size.", nameof(state));
        state.Memory.CopyTo(_memory, 0);
    }

    public byte Read(ushort offset) => offset < MemorySize ? _memory[offset] : (byte)0xFF;

    public void Write(ushort offset, byte value)
    {
        if (offset < MemorySize)
            _memory[offset] = value;
    }

    public void Reset() => Array.Fill(_memory, (byte)0x20);

    public string GetText()
    {
        var text = new char[Columns * Rows];
        for (var row = 0; row < Rows; row++)
        for (var column = 0; column < Columns; column++)
        {
            var value = _memory[row * LineStride + column];
            text[row * Columns + column] = value is >= 0x20 and <= 0x7E ? (char)value : ' ';
        }

        return new string(text);
    }

    /// <summary>Renders the visible 80×24 text window using the Kaypro CRT geometry.</summary>
    public void Render(Span<uint> framebuffer, KayproFont font, uint foreground = 0xFF33FF66, uint background = 0xFF000000)
    {
        if (framebuffer.Length < PixelWidth * PixelHeight)
            throw new ArgumentException("The framebuffer is smaller than the Kaypro raster.", nameof(framebuffer));

        framebuffer[..(PixelWidth * PixelHeight)].Fill(background);
        for (var row = 0; row < Rows; row++)
        for (var column = 0; column < Columns; column++)
        {
            var code = _memory[row * LineStride + column];
            var x = column * CellWidth;
            var y = row * CellHeight;
            for (var glyphRow = 0; glyphRow < KayproFont.GlyphHeight; glyphRow++)
            {
                var bits = font.GetRow(code, glyphRow);
                var destination = (y + glyphRow) * PixelWidth + x;
                for (var bit = 0; bit < CellWidth; bit++)
                    if ((bits & (0x80 >> bit)) != 0)
                        framebuffer[destination + bit] = foreground;
            }
        }
    }
}
