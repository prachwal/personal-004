using Avalonia;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Ported from personal-002's <c>Terminal.Avalonia.Controls</c> (same framework, proven
/// across CPC464/6128, KIM-1, TRS-80, ZX80/81, ZX Spectrum) - declarative on-screen keyboard shape,
/// independent of which machine it's wired to.</summary>
public sealed record EmulatorKeyboardLayout
{
    public EmulatorKeyboardLayout(string id, Size designSize, IReadOnlyList<EmulatorKeyDefinition> keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(keys);
        if (designSize.Width <= 0 || designSize.Height <= 0) throw new ArgumentOutOfRangeException(nameof(designSize));
        if (keys.Select(key => key.Id).Distinct(StringComparer.Ordinal).Count() != keys.Count) throw new ArgumentException("Key IDs must be unique.", nameof(keys));

        foreach (EmulatorKeyDefinition key in keys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key.Id);
            if (key.Bounds.Width <= 0 || key.Bounds.Height <= 0 ||
                key.Bounds.X < 0 || key.Bounds.Y < 0 || key.Bounds.Right > designSize.Width || key.Bounds.Bottom > designSize.Height)
                throw new ArgumentOutOfRangeException(nameof(keys), $"Key '{key.Id}' is outside the layout.");
            if (key.SignalIds is null || key.SignalIds.Count == 0 || key.SignalIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException($"Key '{key.Id}' has no signals.", nameof(keys));
        }

        Id = id;
        DesignSize = designSize;
        Keys = keys;
    }

    public string Id { get; }
    public Size DesignSize { get; }
    public IReadOnlyList<EmulatorKeyDefinition> Keys { get; }
}

/// <param name="Accent">Optional CSS-style class (e.g. "accent-red") selecting a non-default keycap
/// color from <see cref="EmulatorKeyboardView"/>'s built-in accent styles - null keeps the default
/// dark keycap.</param>
public sealed record EmulatorKeyDefinition(string Id, Rect Bounds, IReadOnlyList<EmulatorKeyLegend> Legends,
    IReadOnlyList<string> SignalIds, EmulatorKeyBehavior Behavior = EmulatorKeyBehavior.Momentary,
    string? AutomationName = null, string? ToolTip = null, IReadOnlyList<string>? HostAliases = null,
    string? Accent = null);

/// <param name="Color">Hex brush (e.g. "#FF0000") or null to inherit the key's default foreground.</param>
/// <param name="FontSize">Defaults to 13 (the size every existing on-screen keyboard was designed around).</param>
public sealed record EmulatorKeyLegend(string Text, Point Anchor, string? Color = null, double FontSize = 13);
public enum EmulatorKeyBehavior { Momentary, Toggle }
