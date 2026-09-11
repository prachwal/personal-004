using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20;

/// <summary>One selectable VIC-20 hardware configuration and its concrete RAM cartridge image.</summary>
public sealed record Vic20ExpansionProfile(
    Vic20ExpansionPreset Preset,
    string Label,
    IReadOnlyList<Vic20CartridgeResource> Resources);

public static class Vic20ExpansionPresetCatalog
{
    public static IReadOnlyList<Vic20ExpansionProfile> All { get; } =
    [
        new(Vic20ExpansionPreset.Unexpanded, "VIC-20 (unexpanded)", []),
        new(Vic20ExpansionPreset.ThreeK, "VIC-20 +3K", Vic20ExpansionResources.For(Vic20ExpansionPreset.ThreeK)),
        new(Vic20ExpansionPreset.EightK, "VIC-20 +8K", Vic20ExpansionResources.For(Vic20ExpansionPreset.EightK)),
        new(Vic20ExpansionPreset.SixteenK, "VIC-20 +16K", Vic20ExpansionResources.For(Vic20ExpansionPreset.SixteenK)),
        new(Vic20ExpansionPreset.TwentyFourK, "VIC-20 +24K", Vic20ExpansionResources.For(Vic20ExpansionPreset.TwentyFourK)),
        new(Vic20ExpansionPreset.All, "VIC-20 +All (35K)", Vic20ExpansionResources.For(Vic20ExpansionPreset.All)),
    ];

    public static Vic20ExpansionProfile Get(Vic20ExpansionPreset preset) =>
        All.First(profile => profile.Preset == preset);
}
