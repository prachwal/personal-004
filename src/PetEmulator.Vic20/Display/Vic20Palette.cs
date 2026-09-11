namespace PetEmulator.Vic20.Display;

/// <summary>The VIC-I's 16 fixed colors. Ported from cpu-vibe-001's Vic20Palette (real hardware
/// values, not implementation-specific) - RGB tuples here instead of a separate ToRgb step, since
/// this repo's raster display writes straight to an ARGB8888 uint[] (see
/// <see cref="Vic20RasterDisplay"/>).</summary>
public static class Vic20Palette
{
    private static readonly (byte R, byte G, byte B)[] Rgb =
    [
        (0, 0, 0), (255, 255, 255), (136, 0, 0), (170, 255, 238),
        (204, 68, 204), (0, 204, 85), (0, 0, 170), (238, 238, 119),
        (221, 136, 85), (170, 102, 68), (238, 119, 119), (170, 255, 255),
        (187, 136, 255), (170, 255, 170), (119, 119, 255), (255, 255, 170),
    ];

    /// <summary>0xAARRGGBB, alpha opaque - matches PetRasterDisplay's frame-buffer format.</summary>
    public static uint ToArgb(byte colorIndex)
    {
        var (r, g, b) = Rgb[colorIndex & 0x0F];
        return 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
    }
}
