namespace PetEmulator.Pet.Fonts;

/// <summary>Discovers every PET character-ROM dump under <c>roms/pet/</c> (any
/// "characters-*.bin" file, searched recursively) - drop a new dump in and it appears in the
/// Font/Glyph Viewer without a code change.</summary>
public sealed class PetCharacterRomProvider : ICharacterRomProvider
{
    public IEnumerable<FontSource> DiscoverFonts(string romsRoot)
    {
        var petRoot = Path.Combine(romsRoot, "pet");
        if (!Directory.Exists(petRoot))
            yield break;

        foreach (var path in Directory.EnumerateFiles(petRoot, "characters-*.bin", SearchOption.AllDirectories))
            yield return new FontSource(Path.GetFileName(path), new PetCharacterRomLoader().Load(path));
    }
}
