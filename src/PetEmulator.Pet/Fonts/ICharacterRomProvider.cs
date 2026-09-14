namespace PetEmulator.Pet.Fonts;

/// <summary>One named, loaded character-ROM font a machine module makes available to the
/// Font/Glyph Viewer.</summary>
public sealed record FontSource(string Name, IGlyphFont Font);

/// <summary>Discovers the character ROM(s) one machine module ships, given the <c>roms/</c> root.
/// Each machine project implements this once; the Font/Glyph Viewer aggregates every registered
/// provider instead of hardcoding a per-machine file scan - adding a font for a new machine never
/// touches PetEmulator.Desktop.</summary>
public interface ICharacterRomProvider
{
    IEnumerable<FontSource> DiscoverFonts(string romsRoot);
}
