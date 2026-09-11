using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20;

/// <summary>Resource ranges consumed by the existing RAM expansion presets.</summary>
public static class Vic20ExpansionResources
{
    public static IReadOnlyList<Vic20CartridgeResource> For(Vic20ExpansionPreset preset)
    {
        var resources = new List<Vic20CartridgeResource>();
        if (preset is Vic20ExpansionPreset.ThreeK or Vic20ExpansionPreset.All)
            resources.Add(Ram("+3K RAM", Vic20MemoryMap.Block0Start, Vic20MemoryMap.Block0Size));
        if (preset is Vic20ExpansionPreset.EightK or Vic20ExpansionPreset.SixteenK
            or Vic20ExpansionPreset.TwentyFourK or Vic20ExpansionPreset.All)
            resources.Add(Ram("+8K RAM block 1", Vic20MemoryMap.Block1Start, Vic20MemoryMap.Block1Size));
        if (preset is Vic20ExpansionPreset.SixteenK or Vic20ExpansionPreset.TwentyFourK
            or Vic20ExpansionPreset.All)
            resources.Add(Ram("+8K RAM block 2", Vic20MemoryMap.Block2Start, Vic20MemoryMap.Block2Size));
        if (preset is Vic20ExpansionPreset.TwentyFourK or Vic20ExpansionPreset.All)
            resources.Add(Ram("+8K RAM block 3", Vic20MemoryMap.Block3Start, Vic20MemoryMap.Block3Size));
        if (preset is Vic20ExpansionPreset.All)
            resources.Add(Ram("+8K RAM block 5", Vic20MemoryMap.CartridgeStart, Vic20MemoryMap.CartridgeSize));
        return resources;
    }

    private static Vic20CartridgeResource Ram(string name, ushort start, int length) =>
        new(name, start, (ushort)length, Vic20CartridgeResourceKind.Ram,
            Vic20CartridgeResourceAccess.ReadWrite);
}
