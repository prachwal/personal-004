using Avalonia;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Ported from personal-002. Shared row-layout helper for on-screen keyboards: one big
/// main glyph plus small colored corner legends, laid out row by row left to right, each key's
/// origin advancing by its own width plus a fixed gap - factors that walk out so a machine's
/// layout file only supplies its own per-key text/color/size table.</summary>
public static class KeycapRowBuilder
{
    /// <summary>
    /// Adds one row of keys starting at (x, y), advancing x by each key's width + gap.
    /// <paramref name="legends"/> receives the key's own origin so callers can place corner
    /// legends via <see cref="Legend"/> without re-deriving it. Returns the x past the last key,
    /// so the next row (or a following wide key) can continue from it.
    /// </summary>
    public static double Row<T>(List<EmulatorKeyDefinition> keys, double x, double y, double height, double gap,
        IReadOnlyList<T> row, Func<T, string> signal, Func<T, double> width,
        Func<T, double, double, IReadOnlyList<EmulatorKeyLegend>> legends, Func<T, string>? automationName = null)
    {
        foreach (T spec in row)
        {
            double w = width(spec);
            string id = signal(spec);
            keys.Add(new EmulatorKeyDefinition(id, new Rect(x, y, w, height), legends(spec, x, y),
                [id], AutomationName: automationName?.Invoke(spec) ?? id));
            x += w + gap;
        }
        return x;
    }

    /// <summary>A legend at a fixed offset from the key's own origin, so callers hand in offsets, not absolute coordinates.</summary>
    public static EmulatorKeyLegend Legend(double keyX, double keyY, string text, double dx, double dy, string? color = null, double size = 13) =>
        new(text, new Point(keyX + dx, keyY + dy), color, size);
}
