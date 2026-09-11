namespace PetEmulator.Desktop.ViewModels;

/// <summary>A ready-to-run VIC-20 test program and its optional non-conflicting RAM expansion.</summary>
public sealed record Vic20ProgramProfile(
    string Id,
    string Name,
    string CartridgeFileName,
    string? RamImageFileName,
    string? RamRange)
{
    public bool IsEmpty => string.IsNullOrEmpty(Id);

    public static Vic20ProgramProfile None { get; } =
        new(string.Empty, "-- BRAK --", string.Empty, string.Empty, string.Empty);
}

public static class Vic20ProgramProfileCatalog
{
    public static IReadOnlyList<Vic20ProgramProfile> All { get; } =
    [
        Vic20ProgramProfile.None,
        new("alphoids", "Alphoids", "Alphoids-autostart.crt", null, null),
        new("alien-blitz-ntsc", "Alien Blitz NTSC", "Alien-Blitz-NTSC-autostart.crt", "vic20-ram-24k.bin", "$2000-$7FFF"),
        new("alien-blitz-pal", "Alien Blitz PAL", "Alien-Blitz-PAL-autostart.crt", "vic20-ram-24k.bin", "$2000-$7FFF"),
        new("sound-test", "VIC-20 Sound Test", "vic20-sound-test-autostart.crt", null, null),
    ];
}
