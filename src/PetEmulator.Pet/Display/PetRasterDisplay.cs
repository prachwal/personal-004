using PetEmulator.Core;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Pet.Display;

/// <summary>
/// Rasterizes PET screen RAM through a character-ROM font into an ARGB8888 pixel buffer.
/// Pure logic — no rendering/UI dependency; callers own the frame buffer and how it's drawn.
/// </summary>
public sealed class PetRasterDisplay
{
    private const uint Background = 0xFF102810;
    private const uint Foreground = 0xFF8DFF72;

    /// <summary>Rendered frames per cursor blink half-cycle. 30 at a typical ~50-60Hz redraw
    /// cadence lands near real hardware's ~1-2Hz blink; this is deliberately a frame count, not a
    /// wall-clock timer, so blinking stays deterministic regardless of how often the caller
    /// happens to call <see cref="Tick"/>.</summary>
    private const int BlinkPeriodTicks = 30;

    private readonly PetProfile _profile;
    private readonly IMemoryBus _memory;
    private readonly IGlyphFont _font;
    private int _blinkTicks;
    private bool _cursorVisible = true;

    public PetRasterDisplay(PetProfile profile, IMemoryBus memory, IGlyphFont font)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(font);

        var cellCount = profile.Columns * profile.Rows;
        if (cellCount > profile.VideoRamLength)
        {
            throw new ArgumentException(
                $"Profile '{profile.Id}' geometry is inconsistent: {profile.Columns}x{profile.Rows} " +
                $"= {cellCount} cells needs {cellCount} bytes of video RAM, but VideoRamLength is only " +
                $"{profile.VideoRamLength}.", nameof(profile));
        }

        _profile = profile;
        _memory = memory;
        _font = font;
        PixelWidth = profile.Columns * font.GlyphWidth;
        PixelHeight = profile.Rows * font.GlyphHeight;
    }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    /// <summary>Advances the cursor blink phase by one rendered frame. Call once per frame you
    /// actually display (not inside <see cref="Render"/> itself, so re-rendering the same frame
    /// twice - e.g. on a resize - doesn't skew the blink rate).</summary>
    public void Tick()
    {
        if (++_blinkTicks >= BlinkPeriodTicks)
        {
            _blinkTicks = 0;
            _cursorVisible = !_cursorVisible;
        }
    }

    /// <summary>Renders the current screen RAM into <paramref name="frameBuffer"/> (ARGB8888,
    /// 0xAARRGGBB, row-major, length must equal <see cref="PixelWidth"/> * <see cref="PixelHeight"/>).
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

        var cursor = GetCursorPosition();
        for (var row = 0; row < _profile.Rows; row++)
        {
            for (var col = 0; col < _profile.Columns; col++)
            {
                var screenCode = _memory.Read((ushort)(_profile.VideoRamStart + row * _profile.Columns + col));
                // Both PET screen codes and Waterloo's ASCII screen use bit 7 for reverse video.
                // Waterloo marks its menu cursor by writing $A0 (reverse ASCII space), so the bit
                // must be removed before selecting the ASCII character-ROM bank as well.
                var isReverse = (screenCode & 0x80) != 0;
                var invert = isReverse
                    || (_cursorVisible && cursor is { } c && c.Row == row && c.Column == col);
                var characterCode = _profile.ScreenCharacterEncoding == PetScreenCharacterEncoding.Ascii
                    ? 0x100 + (screenCode & 0x7F)
                    : screenCode & 0x7F;
                for (var glyphRow = 0; glyphRow < _font.GlyphHeight; glyphRow++)
                {
                    var bits = _font.GetGlyphRow(characterCode, glyphRow);
                    if (invert)
                        bits = (byte)~bits;
                    var pixelRowOffset = (row * _font.GlyphHeight + glyphRow) * PixelWidth + col * _font.GlyphWidth;
                    for (var bit = 0; bit < _font.GlyphWidth; bit++)
                    {
                        var isSet = (bits & (0x80 >> bit)) != 0;
                        frameBuffer[pixelRowOffset + bit] = isSet ? Foreground : Background;
                    }
                }
            }
        }
    }

    /// <summary>Locates the text cursor via the KERNAL's own zero-page screen-line pointer, at
    /// the addresses <see cref="_profile"/>'s <see cref="PetProfile.CursorTracking"/> gives -
    /// different ROM revisions use different absolute zero-page addresses for the identical
    /// 3-byte shape (see <see cref="PetCursorTracking"/>'s doc comment). Returns null off-screen
    /// (cursor address outside the visible cell range - blanked, or between frames on real
    /// hardware).
    ///
    /// Was a single hardcoded $C4/$C5/$C6 with a CRTC-register branch for BASIC 4 profiles -
    /// wrong on two counts (see docs/pet/cursor-fix.md): CRTC-equipped profiles don't use their
    /// CRTC's hardware cursor register at all (real BASIC 4 KERNALs write it once, to $0000,
    /// during init and never touch it again), and the original PET 2001 (BASIC 1) uses
    /// $E0/$E1/$E2 instead of $C4/$C5/$C6 - a different ROM revision, a different zero-page
    /// layout, confirmed by typing a character and watching which byte increments by exactly 1
    /// each time.</summary>
    private (int Row, int Column)? GetCursorPosition()
    {
        var tracking = _profile.CursorTracking;
        var pointer = (ushort)(_memory.Read(tracking.LineLowAddress) | (_memory.Read(tracking.LineHighAddress) << 8));
        var row = (int)((long)pointer - _profile.VideoRamStart) / _profile.Columns;
        int column = _memory.Read(tracking.ColumnAddress);
        return row >= 0 && row < _profile.Rows && column < _profile.Columns ? (row, column) : null;
    }
}
