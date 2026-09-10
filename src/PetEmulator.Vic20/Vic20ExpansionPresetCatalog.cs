namespace PetEmulator.Vic20;

public static class Vic20ExpansionPresetCatalog
{
    public static IReadOnlyList<(Vic20ExpansionPreset Preset, string Label)> All { get; } =
    [
        (Vic20ExpansionPreset.Unexpanded, "VIC-20 (unexpanded)"),
        (Vic20ExpansionPreset.ThreeK, "VIC-20 +3K"),
        (Vic20ExpansionPreset.EightK, "VIC-20 +8K"),
        (Vic20ExpansionPreset.SixteenK, "VIC-20 +16K"),
        (Vic20ExpansionPreset.TwentyFourK, "VIC-20 +24K"),
        (Vic20ExpansionPreset.All, "VIC-20 +All (35K)"),
    ];
}
