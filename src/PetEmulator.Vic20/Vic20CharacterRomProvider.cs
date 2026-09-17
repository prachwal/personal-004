using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Vic20;

/// <summary>Discovers the VIC-20 character ROM (<c>roms/vic20/vic20-chargen.bin</c>) for the
/// Font/Glyph Viewer. Same on-disk layout as a PET character ROM (256 glyphs x 8 rows,
/// MSB-first), so it reuses <see cref="PetCharacterRomLoader"/> rather than duplicating it.</summary>
public sealed class Vic20CharacterRomProvider : ICharacterRomProvider
{
    private readonly ILogger _log;

    public Vic20CharacterRomProvider(ILogger? logger = null) => _log = logger ?? NullLogger.Instance;

    public IEnumerable<FontSource> DiscoverFonts(string romsRoot)
    {
        var path = Path.Combine(romsRoot, "vic20", "vic20-chargen.bin");
        if (File.Exists(path))
        {
            _log.LogInformation("Font discovered: '{Path}'.", path);
            yield return new FontSource("VIC-20 character ROM", new PetCharacterRomLoader().Load(path));
        }
        else
        {
            _log.LogInformation("Font missing: '{Path}'.", path);
        }
    }
}
