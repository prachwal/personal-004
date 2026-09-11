namespace PetEmulator.Desktop.ViewModels;

/// <summary>One cartridge image used by a ready-to-run VIC-20 configuration.</summary>
public sealed record Vic20ProgramCartridge(string FileName, string? PluginId = null);

/// <summary>A ready-to-run VIC-20 configuration expressed only as mounted cartridges.</summary>
public sealed record Vic20ProgramProfile(
    string Id,
    string Name,
    IReadOnlyList<Vic20ProgramCartridge> Cartridges)
{
    public bool IsEmpty => string.IsNullOrEmpty(Id);

    public static Vic20ProgramProfile None { get; } =
        new(string.Empty, "-- BRAK --", []);
}

public static class Vic20ProgramProfileCatalog
{
    public static IReadOnlyList<Vic20ProgramProfile> All { get; } =
    [
        Vic20ProgramProfile.None,
        new("alphoids", "Alphoids", [new Vic20ProgramCartridge("Alphoids-autostart.crt")]),
        new("alien-blitz-ntsc", "Alien Blitz NTSC", [
            new("vic20-ram-24k.bin", "vic20-ram-24k"),
            new("Alien-Blitz-NTSC-autostart.crt")]),
        new("alien-blitz-pal", "Alien Blitz PAL", [
            new("vic20-ram-24k.bin", "vic20-ram-24k"),
            new("Alien-Blitz-PAL-autostart.crt")]),
        new("sound-test", "VIC-20 Sound Test", [new Vic20ProgramCartridge("vic20-sound-test-autostart.crt")]),
        new("ram-3k", "VIC-20 +3K RAM", [new("vic20-ram-3k.bin", "vic20-ram-3k")]),
        new("ram-8k", "VIC-20 +8K RAM", [new("vic20-ram-8k.bin", "vic20-ram-8k")]),
        new("ram-16k", "VIC-20 +16K RAM", [new("vic20-ram-16k.bin", "vic20-ram-16k")]),
        new("ram-24k", "VIC-20 +24K RAM", [new("vic20-ram-24k.bin", "vic20-ram-24k")]),
        new("ram-35k", "VIC-20 +35K RAM", [new("vic20-ram-35k.bin", "vic20-ram-35k")]),
        new("rtc", "MC146818 RTC", [new("vic20-mc146818-rtc.bin", "vic20-mc146818-rtc")]),
    ];
}
