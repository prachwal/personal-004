using PetEmulator.Core;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Trs80.Display;

public sealed class Trs80RasterDisplay
{
    public const int Columns = 64, Rows = 16, PixelWidth = 384, PixelHeight = 192;
    private readonly IMemoryBus _memory;
    private readonly IGlyphFont _font;
    private int _scanlineTStates;
    public Trs80RasterDisplay(IMemoryBus memory, IGlyphFont font) { _memory = memory; _font = font; }
    public int Scanline { get; private set; }
    public event EventHandler? FrameChanged;
    public void Tick(int tStates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tStates);
        _scanlineTStates += tStates;
        while (_scanlineTStates >= 113)
        {
            _scanlineTStates -= 113;
            if (++Scanline < 262) continue;
            Scanline = 0;
            FrameChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public void Render(uint[] frameBuffer)
    {
        if (frameBuffer.Length != PixelWidth * PixelHeight) throw new ArgumentException("Invalid TRS-80 frame size.", nameof(frameBuffer));
        Array.Fill(frameBuffer, 0xFF000000u);
        for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Columns; col++)
            {
                var code = _memory.Read((ushort)(Trs80MemoryMap.VideoRamStart + row * Columns + col));
                if (code is >= 0x80 and <= 0xBF) RenderSemigraphic(frameBuffer, col, row, code);
                else RenderCharacter(frameBuffer, col, row, code);
            }
    }
    private void RenderCharacter(uint[] buffer, int col, int row, byte code)
    {
        for (var y = 0; y < 12; y++)
        {
            // Pass the full code (not code & 0x7F) - Trs80CharacterFont.GetGlyphRow itself
            // switches ROM bank on bit 7 (characterCode >= 128) before masking to the in-bank
            // offset, so pre-masking here made that second bank unreachable for codes 0xC0-0xFF.
            var bits = _font.GetGlyphRow(code, y);
            for (var x = 0; x < 6; x++) if ((bits & (0x80 >> x)) != 0) SetPixel(buffer, col * 6 + x, row * 12 + y);
        }
    }
    private static void RenderSemigraphic(uint[] buffer, int col, int row, byte code)
    {
        var blocks = code & 0x3F;
        for (var block = 0; block < 6; block++) if ((blocks & (1 << block)) != 0)
            for (var y = block / 2 * 4; y < block / 2 * 4 + 4; y++)
                for (var x = block % 2 * 3; x < block % 2 * 3 + 3; x++) SetPixel(buffer, col * 6 + x, row * 12 + y);
    }
    private static void SetPixel(uint[] buffer, int x, int y) => buffer[y * PixelWidth + x] = 0xFFFFFFFFu;
}
