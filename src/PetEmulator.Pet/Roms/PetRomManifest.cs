namespace PetEmulator.Pet.Roms;

public sealed record PetRomRequirement(string Path, uint Address, uint Length);

public static class PetRomManifest
{
    public static IReadOnlyList<PetRomRequirement> Pet2001_8 { get; } =
    [
        new("rom-1-c000.901439-01.bin", 0xC000, 0x0800),
        new("rom-1-c800.901439-05.bin", 0xC800, 0x0800),
        new("rom-1-d000.901439-02.bin", 0xD000, 0x0800),
        new("rom-1-d800.901439-06.bin", 0xD800, 0x0800),
        new("rom-1-e000.901439-03.bin", 0xE000, 0x0800),
        new("rom-1-f000.901439-04.bin", 0xF000, 0x0800),
        new("rom-1-f800.901439-07.bin", 0xF800, 0x0800)
    ];

    public static IReadOnlyList<PetRomRequirement> Pet2001_32 { get; } =
    [
        new("basic-2.901465-01-02.bin", 0xC000, 0x2000),
        new("edit-2-n.901447-24.bin", 0xE000, 0x0800),
        new("kernal-2.901465-03.bin", 0xF000, 0x1000)
    ];

    public static IReadOnlyList<PetRomRequirement> Cbm4032 { get; } =
    [
        new("basic-4-b000.901465-23.bin", 0xB000, 0x1000),
        new("basic-4-c000.901465-20.bin", 0xC000, 0x1000),
        new("basic-4-d000.901465-21.bin", 0xD000, 0x1000),
        new("edit-4-40-n-60Hz.901499-01.bin", 0xE000, 0x0800),
        new("kernal-4.901465-22.bin", 0xF000, 0x1000)
    ];

    public static IReadOnlyList<PetRomRequirement> Cbm8032 { get; } =
    [
        new("basic-4.901465-23-20-21.bin", 0xB000, 0x3000),
        new("edit-4-80-b-60Hz.901474-03.bin", 0xE000, 0x0800),
        new("kernal-4.901465-22.bin", 0xF000, 0x1000)
    ];
}
