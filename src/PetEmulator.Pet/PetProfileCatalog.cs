using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet;

/// <summary>Ported from personal-001's PetProfileCatalog. Ids double as ROM subfolder names (<see cref="PetProfile.RomDirectory"/>).</summary>
public static class PetProfileCatalog
{
    public static PetProfile Pet2001_8 { get; } = new("pet-2001-8", "PET 2001-8 / BASIC 1 / 40x25", "BASIC 1", 40, 25, 0x2000, 0x8000, 0x0400, false, "characters-1.901447-08.bin", PetRomManifest.Pet2001_8, PetProfileStatus.Implemented, PetCursorTracking.Basic1Convention, PetKeyboardLayout.Pet2001Graphics);

    public static PetProfile Pet2001_32 { get; } = new("pet-2001-32", "PET 2001-32 / BASIC 2 / 40x25", "BASIC 2", 40, 25, 0x8000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics);

    public static PetProfile Cbm3008 { get; } = new("cbm-3008", "CBM 3008 / BASIC 2 / 40x25", "BASIC 2", 40, 25, 0x2000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        RomDirectoryOverride = Pet2001_32.RomDirectory
    };

    public static PetProfile Cbm3016 { get; } = new("cbm-3016", "CBM 3016 / BASIC 2 / 40x25", "BASIC 2", 40, 25, 0x4000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        RomDirectoryOverride = Pet2001_32.RomDirectory
    };

    public static PetProfile Cbm3032 { get; } = new("cbm-3032", "CBM 3032 / BASIC 2 / 40x25", "BASIC 2", 40, 25, 0x8000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        RomDirectoryOverride = Pet2001_32.RomDirectory
    };

    public static PetProfile Cbm4032 { get; } = new("cbm-4032", "CBM 4032 / BASIC 4 / 40x25", "BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032);

    public static PetProfile Cbm8032 { get; } = new("cbm-8032", "CBM 8032 / BASIC 4 / 80x25", "BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Cbm8032, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032);

    public static IReadOnlyList<PetProfile> All { get; } = [Pet2001_8, Pet2001_32, Cbm3008, Cbm3016, Cbm3032, Cbm4032, Cbm8032];

    /// <summary>Looks up a profile by <see cref="PetProfile.Id"/> (e.g. "pet-2001-32") - the
    /// string form a script/CLI command line points at, as opposed to <see cref="All"/>'s typed
    /// enumeration.</summary>
    public static PetProfile Find(string id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException(
            $"Unknown PET profile '{id}'. Available: {string.Join(", ", All.Select(p => p.Id))}.");
}
