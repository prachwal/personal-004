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

    // PET KERNAL zero-page cursor tracking: current screen line pointer lo/hi, then cursor
    // column. Real hardware addresses, not this repo's invention - and used for EVERY profile,
    // CRTC-equipped or not (see GetCursorPosition's doc comment for why a CRTC-branch used to
    // exist here and why it was wrong).
    private const ushort CursorLineLowAddress = 0x00C4;
    private const ushort CursorLineHighAddress = 0x00C5;
    private const ushort CursorColumnAddress = 0x00C6;

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
                var invert = _cursorVisible && cursor is { } c && c.Row == row && c.Column == col;
                for (var glyphRow = 0; glyphRow < _font.GlyphHeight; glyphRow++)
                {
                    var bits = _font.GetGlyphRow(screenCode, glyphRow);
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

    /// <summary>Locates the text cursor via the KERNAL's own zero-page screen-line pointer
    /// (same $C4/$C5/$C6 convention for every profile - CRTC-equipped or not). Returns null
    /// off-screen (cursor address outside the visible cell range - blanked, or between frames on
    /// real hardware).
    ///
    /// Was branched on <see cref="_crtc"/> - CRTC-equipped profiles (BASIC 4, CBM 4032/8032) used
    /// the CRTC's own hardware cursor register (R14/R15) instead. Wrong: traced with
    /// <see cref="PetMachine.BusObserver"/> against a real boot - the real BASIC 4 KERNAL writes
    /// R14/R15 exactly once during CRTC init (to $0000, immediately invalid - offset
    /// $0000-DisplayStartAddress is always negative) and never touches them again for the rest of
    /// a session; $C4/$C5/$C6 hold a genuinely valid, live-tracked position throughout (confirmed
    /// against both cbm-4032 and cbm-8032). This KERNAL simply doesn't drive the CRTC's hardware
    /// cursor feature - real PET/CBM firmware blinks the cursor entirely in software (screen-RAM
    /// character inversion, same as every other profile), regardless of whether a CRTC is
    /// present.</summary>
    private (int Row, int Column)? GetCursorPosition()
    {
        var pointer = (ushort)(_memory.Read(CursorLineLowAddress) | (_memory.Read(CursorLineHighAddress) << 8));
        var row = (int)((long)pointer - _profile.VideoRamStart) / _profile.Columns;
        int column = _memory.Read(CursorColumnAddress);
        return row >= 0 && row < _profile.Rows && column < _profile.Columns ? (row, column) : null;
    }
}
