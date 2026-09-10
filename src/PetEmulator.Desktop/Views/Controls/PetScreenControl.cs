using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>
/// Displays a PET frame buffer with genuine two-pass scaling: nearest-neighbor up to an integer
/// prescale factor (crisp pixels), then a high-quality blit of that prescaled bitmap into the
/// final aspect-preserving, letterboxed destination rect (smooths only the small residual scale).
/// </summary>
public sealed class PetScreenControl : Control
{
    private WriteableBitmap? _source;
    private int _nativeWidth;
    private int _nativeHeight;
    private int _parWidth = 1;
    private int _parHeight = 1;
    private byte[] _byteBuffer = [];

    /// <summary>(Re)allocates the backing bitmap for a new native resolution. Call once per
    /// profile/font change. <paramref name="pixelAspectWidth"/>/<paramref name="pixelAspectHeight"/>
    /// is the physical width:height ratio of ONE source pixel on real hardware (not 1:1 - PET
    /// pixels aren't square, see <see cref="PetEmulator.Pet.PetProfile.PixelAspect"/>); default
    /// 1:1 renders square pixels for callers that don't care.</summary>
    public void SetSize(int pixelWidth, int pixelHeight, int pixelAspectWidth = 1, int pixelAspectHeight = 1)
    {
        _parWidth = pixelAspectWidth;
        _parHeight = pixelAspectHeight;

        if (pixelWidth == _nativeWidth && pixelHeight == _nativeHeight)
            return;

        _nativeWidth = pixelWidth;
        _nativeHeight = pixelHeight;
        _source = new WriteableBitmap(
            new PixelSize(pixelWidth, pixelHeight),
            new Vector(96, 96),
            PixelFormats.Bgra8888,
            AlphaFormat.Opaque);
    }

    /// <summary>Uploads one ARGB8888 (0xAARRGGBB) frame and repaints. Little-endian memory layout
    /// of that format is byte-identical to Bgra8888, so this is a straight copy.</summary>
    public void UpdateFrame(uint[] frameBuffer)
    {
        if (_source is null)
            return;

        var byteLength = frameBuffer.Length * 4;
        if (_byteBuffer.Length != byteLength)
            _byteBuffer = new byte[byteLength];
        Buffer.BlockCopy(frameBuffer, 0, _byteBuffer, 0, byteLength);

        using var fb = _source.Lock();
        Marshal.Copy(_byteBuffer, 0, fb.Address, byteLength);

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds;
        if (_source is null || _nativeWidth <= 0 || _nativeHeight <= 0 || bounds.Width < 1 || bounds.Height < 1)
            return;

        // Real PET pixels aren't square (see PetProfile.PixelAspect) - a source pixel occupies
        // _parWidth:_parHeight physical units, not 1:1. "Logical" size below is the frame's true
        // physical proportions; fitting THAT into the window (not the raw pixel count) is what
        // makes 40- and 80-column screens both land on the same ~4:3 tube instead of the 80-column
        // one coming out visibly squashed.
        var logicalWidth = _nativeWidth * _parWidth;
        var logicalHeight = _nativeHeight * _parHeight;
        var scale = Math.Min(bounds.Width / logicalWidth, bounds.Height / logicalHeight);
        if (scale <= 0)
            return;

        // Pass 1: nearest-neighbor prescale each axis to the largest integer zoom that fits,
        // independently per axis since the physical scale per source pixel differs in X and Y -
        // keeps pixel edges crisp instead of letting the final blit interpolate them.
        var prescaleX = Math.Max(1, (int)Math.Floor(scale * _parWidth));
        var prescaleY = Math.Max(1, (int)Math.Floor(scale * _parHeight));
        var prescaledSize = new PixelSize(_nativeWidth * prescaleX, _nativeHeight * prescaleY);
        using var prescaled = new RenderTargetBitmap(prescaledSize);
        using (var prescaleContext = prescaled.CreateDrawingContext())
        {
            using (prescaleContext.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            {
                prescaleContext.DrawImage(
                    _source,
                    new Rect(0, 0, _nativeWidth, _nativeHeight),
                    new Rect(0, 0, prescaledSize.Width, prescaledSize.Height));
            }
        }

        // Pass 2: high-quality blit of the prescaled bitmap into the final, aspect-correct,
        // letterboxed destination rect - smooths only the small residual (non-integer, and the
        // per-axis PAR) scale factor left after pass 1's integer prescale.
        var destWidth = logicalWidth * scale;
        var destHeight = logicalHeight * scale;
        var destRect = new Rect((bounds.Width - destWidth) / 2, (bounds.Height - destHeight) / 2, destWidth, destHeight);

        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.HighQuality }))
        {
            context.DrawImage(prescaled, new Rect(0, 0, prescaledSize.Width, prescaledSize.Height), destRect);
        }
    }
}
