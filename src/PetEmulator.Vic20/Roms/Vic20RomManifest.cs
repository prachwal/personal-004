namespace PetEmulator.Vic20.Roms;

public sealed record Vic20RomRequirement(string Path, uint Address, uint Length);

/// <summary>Real VIC-20 ROM images - see roms/vic20/README.md for provenance.</summary>
public static class Vic20RomManifest
{
    public static IReadOnlyList<Vic20RomRequirement> Ntsc { get; } =
    [
        new("vic20-chargen.bin", 0x8000, 0x1000),
        new("basic.bin", 0xC000, 0x2000),
        new("kernal.bin", 0xE000, 0x2000),
    ];
}
