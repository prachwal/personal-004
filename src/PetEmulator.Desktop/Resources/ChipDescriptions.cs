using System.Resources;

namespace PetEmulator.Desktop.Resources;

/// <summary>Plain-text chip descriptions for Chip Tester's "Opis" tab, kept out of code as an
/// embedded .resx resource rather than hardcoded strings - swappable/translatable without
/// touching any view model. No Visual Studio single-file-generator dependency (that codegen step
/// doesn't run under a plain `dotnet build`/CLI workflow) - a small hand-written
/// <see cref="ResourceManager"/> wrapper instead, keyed by chip name.</summary>
public static class ChipDescriptions
{
    private static readonly ResourceManager Manager =
        new("PetEmulator.Desktop.Resources.ChipDescriptions", typeof(ChipDescriptions).Assembly);

    /// <summary>Looks up a chip's description by resource key (e.g. "Mos6522"). Falls back to the
    /// key itself if the resource is missing, rather than throwing - a missing description
    /// shouldn't break the Chip Tester UI.</summary>
    public static string Get(string key) => Manager.GetString(key) ?? key;
}
