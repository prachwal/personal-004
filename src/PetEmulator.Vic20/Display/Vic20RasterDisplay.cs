using PetEmulator.Core;
using PetEmulator.Chips;

namespace PetEmulator.Vic20.Display;

/// <summary>
/// Rasterizes VIC-20 screen RAM through the char ROM/color RAM into an ARGB8888 pixel buffer.
/// Pure logic - no rendering/UI dependency, mirrors PetEmulator.Pet.Display.PetRasterDisplay's
/// shape. Algorithm ported from cpu-vibe-001's Vic20Video.RenderChar (multicolor 2-bit-pair
/// decode included), rewritten to read everything through one <see cref="IMemoryBus"/> (this
/// repo's Vic20MemoryBus already decodes char ROM/color RAM/screen RAM uniformly - no separate
/// per-region read delegate needed, unlike the reference implementation's three).
/// </summary>
public sealed class Vic20RasterDisplay
{
    // Fixed canvas, not the actively-configured Columns*8 x Rows*8/16 - real VIC-20 resolution is
    // chip-register-driven and unknown until the KERNAL configures it during boot (unlike PET's
    // profile-fixed geometry), so PixelWidth/PixelHeight must be constant from construction for a
    // GUI to size its screen control once. 176x184 is the reference implementation's real
    // measured NTSC-visible-area maximum; unconfigured/off-screen cells simply render as border.
    public const int MaxWidth = 176;
    public const int MaxHeight = 184;
    private const int CharWidth = 8;

    private readonly IMemoryBus _memory;
    private readonly MOS6560 _vic;

    public Vic20RasterDisplay(IMemoryBus memory, MOS6560 vic)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        _vic = vic ?? throw new ArgumentNullException(nameof(vic));
    }

    public int PixelWidth => MaxWidth;

    public int PixelHeight => MaxHeight;

    /// <summary>No cursor blink modeled (VIC-20 KERNAL draws its own cursor via screen-RAM
    /// reverse-video toggling, unlike PET's separate hardware/KERNAL cursor tracking
    /// PetRasterDisplay reads directly) - present only so a caller can Tick() both display types
    /// identically.</summary>
    public void Tick() { }

    /// <summary>Renders the current screen RAM into <paramref name="frameBuffer"/> (ARGB8888,
    /// row-major, length must equal <see cref="PixelWidth"/> * <see cref="PixelHeight"/>).
    /// Caller-allocated so it can be reused every frame without a new allocation.</summary>
    public void Render(uint[] frameBuffer)
    {
        ArgumentNullException.ThrowIfNull(frameBuffer);
        var expectedLength = PixelWidth * PixelHeight;
        if (frameBuffer.Length != expectedLength)
        {
            throw new ArgumentException(
                $"frameBuffer.Length must equal PixelWidth * PixelHeight ({expectedLength}), got {frameBuffer.Length}.",
                nameof(frameBuffer));
        }

        var borderColor = Vic20Palette.ToArgb(_vic.BorderColor);
        Array.Fill(frameBuffer, borderColor);

        var cols = _vic.Columns;
        var rows = _vic.Rows;
        if (cols == 0 || rows == 0)
            return; // KERNAL hasn't configured the VIC yet - leave the border-filled frame as-is

        var screenAddr = _vic.ScreenAddr;
        var charAddr = _vic.CharAddr;
        var colorAddr = Vic20MemoryMap.ColorRamStart + _vic.ColorMatrixOffset;
        var screenColor = _vic.ScreenColor;
        var auxColor = _vic.AuxColor;
        var reverse = _vic.ReverseMode;
        var charHeight = _vic.DoubleHeightChars ? 16 : 8;

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var screenCode = _memory.Read((ushort)(screenAddr + row * cols + col));
                var charReverse = (screenCode & 0x80) != 0;
                var charIndex = screenCode & 0x7F;
                var colorIndex = (byte)(_memory.Read((ushort)(colorAddr + col + row * cols)) & 0x0F);

                var ink = colorIndex;
                var paper = screenColor;
                if (reverse || charReverse)
                    (ink, paper) = (paper, ink);
                if (ink == 8)
                    ink = auxColor;

                RenderChar(frameBuffer, col * CharWidth, row * charHeight, charIndex, charAddr, charHeight,
                    ink, paper, colorIndex, screenColor, auxColor);
            }
        }
    }

    private void RenderChar(
        uint[] frameBuffer, int px, int py, int charIndex, int charAddr, int charHeight,
        byte ink, byte paper, byte colorIndex, byte screenColor, byte auxColor)
    {
        var multicolor = (colorIndex & 0x08) != 0;
        var inkArgb = Vic20Palette.ToArgb(ink);
        var paperArgb = Vic20Palette.ToArgb(paper);
        var screenArgb = Vic20Palette.ToArgb(screenColor);
        var auxArgb = Vic20Palette.ToArgb(auxColor);

        for (var y = 0; y < charHeight; y++)
        {
            var glyphRow = _memory.Read((ushort)(charAddr + charIndex * 8 + y % 8));
            for (var x = 0; x < CharWidth; x++)
            {
                var pixelX = px + x;
                var pixelY = py + y;
                if ((uint)pixelX >= MaxWidth || (uint)pixelY >= MaxHeight)
                    continue;

                uint color;
                if (multicolor)
                {
                    // Each byte packs four 2-bit pixel pairs MSB-first: bits 7:6, 5:4, 3:2, 1:0 -
                    // shift must drop by 2 per pair (6,6,4,4,2,2,0,0 for x=0..7). The reference
                    // implementation this was ported from used `6 - (x/2)` (drops by 1: 6,6,5,5,
                    // 4,4,3,3) - a bug, not intentionally different hardware behavior.
                    var pair = (glyphRow >> (6 - x / 2 * 2)) & 0x03;
                    color = pair switch
                    {
                        0 => screenArgb,
                        1 => auxArgb,
                        2 => paperArgb,
                        _ => inkArgb,
                    };
                }
                else
                {
                    var isSet = (glyphRow & (0x80 >> x)) != 0;
                    color = isSet ? inkArgb : paperArgb;
                }

                frameBuffer[pixelY * MaxWidth + pixelX] = color;
            }
        }
    }
}
