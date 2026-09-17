using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.Pet.Fonts;

/// <summary>Discovers every PET character-ROM dump under <c>roms/pet/</c> (any
/// "characters-*.bin" file, searched recursively) - drop a new dump in and it appears in the
/// Font/Glyph Viewer without a code change.</summary>
public sealed class PetCharacterRomProvider : ICharacterRomProvider
{
    private readonly ILogger _log;

    public PetCharacterRomProvider(ILogger? logger = null) => _log = logger ?? NullLogger.Instance;

    public IEnumerable<FontSource> DiscoverFonts(string romsRoot)
    {
        var petRoot = Path.Combine(romsRoot, "pet");
        if (!Directory.Exists(petRoot))
        {
            _log.LogInformation("Font discovery: '{Directory}' does not exist; no PET fonts.", petRoot);
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(petRoot, "characters-*.bin", SearchOption.AllDirectories))
        {
            _log.LogInformation("Font discovered: '{Path}'.", path);
            yield return new FontSource(Path.GetFileName(path), new PetCharacterRomLoader().Load(path));
        }
    }
}
