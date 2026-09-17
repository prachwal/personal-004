using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Kaypro;

/// <summary>Adapts <see cref="KayproFont"/> (raw ROM index, not the screen-code-masked
/// <see cref="KayproFont.GetRow"/> used for CRT rendering) to <see cref="IGlyphFont"/> so it can
/// be browsed like any other character ROM.</summary>
internal sealed class KayproGlyphFont(KayproFont font) : IGlyphFont
{
    public int GlyphWidth => KayproFont.GlyphWidth;
    public int GlyphHeight => KayproFont.GlyphHeight;
    public byte GetGlyphRow(int characterCode, int row) => font.GetRawRow(characterCode, row);
}

/// <summary>Discovers the Kaypro II character ROM (<c>roms/kaypro/kaypro-81-146.bin</c>) for the
/// Font/Glyph Viewer.</summary>
public sealed class KayproCharacterRomProvider : ICharacterRomProvider
{
    private readonly ILogger _log;

    public KayproCharacterRomProvider(ILogger? logger = null) => _log = logger ?? NullLogger.Instance;

    public IEnumerable<FontSource> DiscoverFonts(string romsRoot)
    {
        var path = Path.Combine(romsRoot, "kaypro", "kaypro-81-146.bin");
        if (File.Exists(path))
        {
            _log.LogInformation("Font discovered: '{Path}'.", path);
            yield return new FontSource("Kaypro II character ROM", new KayproGlyphFont(KayproFont.Load(path)));
        }
        else
        {
            _log.LogInformation("Font missing: '{Path}'.", path);
        }
    }
}
