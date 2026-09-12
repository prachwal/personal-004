using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet;

/// <summary>Ported from personal-001's PetProfileCatalog. Ids double as ROM subfolder names (<see cref="PetProfile.RomDirectory"/>).</summary>
public static class PetProfileCatalog
{
    public static PetProfile Pet2001_8 { get; } = new("pet-2001-8", "PET 2001-8 / BASIC 1 / 40x25", "BASIC 1", 40, 25, 0x2000, 0x8000, 0x0400, false, "characters-1.901447-08.bin", PetRomManifest.Pet2001_8, PetProfileStatus.Implemented, PetCursorTracking.Basic1Convention, PetKeyboardLayout.Pet2001Graphics) { KeyboardRevision = PetKeyboardRevision.Pet2001Graphics, CassetteConfiguration = PetCassetteConfiguration.InternalAndExternal, ConnectorConfiguration = PetConnectorConfiguration.Pet2001 };

    public static PetProfile Pet2001_32 { get; } = new("pet-2001-32", "PET 2001-32 / BASIC 2 / 40x25", "BASIC 2", 40, 25, 0x8000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics) { KeyboardRevision = PetKeyboardRevision.Pet2001Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Pet2001 };

    public static PetProfile Cbm3008 { get; } = new("cbm-3008", "CBM 3008 / BASIC 2 / 40x25", "BASIC 2", 40, 25, 0x2000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        RomDirectoryOverride = Pet2001_32.RomDirectory, KeyboardRevision = PetKeyboardRevision.Cbm3000Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm3000
    };

    public static PetProfile Cbm3016 { get; } = new("cbm-3016", "CBM 3016 / BASIC 2 / 40x25", "BASIC 2", 40, 25, 0x4000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        RomDirectoryOverride = Pet2001_32.RomDirectory, KeyboardRevision = PetKeyboardRevision.Cbm3000Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm3000
    };

    public static PetProfile Cbm3032 { get; } = new("cbm-3032", "CBM 3032 / BASIC 2 / 40x25", "BASIC 2", 40, 25, 0x8000, 0x8000, 0x0400, false, "characters-2.901447-10.bin", PetRomManifest.Pet2001_32, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Pet2001Graphics)
    {
        RomDirectoryOverride = Pet2001_32.RomDirectory, KeyboardRevision = PetKeyboardRevision.Cbm3000Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm3000
    };

    public static PetProfile Cbm4032 { get; } = new("cbm-4032", "CBM 4032 / BASIC 4 / 40x25", "BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032) { KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm4000 };

    public static PetProfile Cbm4008Crtc40N60 { get; } = new("cbm-4008-crtc-40n60", "CBM 4008 / CRTC 40n60 / BASIC 4 / 40x25", "BASIC 4", 40, 25, 0x2000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        RomDirectoryOverride = Cbm4032.RomDirectory, KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm4000
    };

    public static PetProfile Cbm4016Crtc40N60 { get; } = new("cbm-4016-crtc-40n60", "CBM 4016 / CRTC 40n60 / BASIC 4 / 40x25", "BASIC 4", 40, 25, 0x4000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        RomDirectoryOverride = Cbm4032.RomDirectory, KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm4000
    };

    public static PetProfile Cbm4032Crtc40N50 { get; } = new("cbm-4032-crtc-40n50", "CBM 4032 / CRTC 40n50 / BASIC 4 / 40x25", "BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032_40N50, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        RomDirectoryOverride = Cbm4032.RomDirectory, KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm4000
    };

    public static PetProfile Cbm4032Crtc40B50 { get; } = new("cbm-4032-crtc-40b50", "CBM 4032 / CRTC 40b50 / BASIC 4 / 40x25", "BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032_40B50, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        RomDirectoryOverride = Cbm4032.RomDirectory, KeyboardRevision = PetKeyboardRevision.Cbm4000Graphics, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm4000
    };

    public static PetProfile Cbm4032Crtc40B60 { get; } = new("cbm-4032-crtc-40b60", "CBM 4032 / CRTC 40b60 / BASIC 4 / 40x25", "BASIC 4", 40, 25, 0x8000, 0x8000, 0x0400, true, "characters-2.901447-10.bin", PetRomManifest.Cbm4032_40B60, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        RomDirectoryOverride = Cbm4032.RomDirectory
    };

    public static PetProfile Cbm8032 { get; } = new("cbm-8032", "CBM 8032 / BASIC 4 / 80x25", "BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Cbm8032, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032) { KeyboardRevision = PetKeyboardRevision.Cbm8000Business, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm8000 };

    public static PetProfile Cbm8032Crtc80B50 { get; } = new("cbm-8032-crtc-80b50", "CBM 8032 / CRTC 80b50 / BASIC 4 / 80x25", "BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Cbm8032_80B50, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        RomDirectoryOverride = Cbm8032.RomDirectory, KeyboardRevision = PetKeyboardRevision.Cbm8000Business, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm8000
    };

    public static PetProfile Cbm8016Converted80N50 { get; } = new("cbm-8016-converted-80n50", "CBM 8016 / converted 80n50 editor / BASIC 4 / 80x25", "BASIC 4", 80, 25, 0x4000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Cbm8016Converted80N50, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        RomDirectoryOverride = Cbm8032.RomDirectory, KeyboardRevision = PetKeyboardRevision.Unverified, CassetteConfiguration = PetCassetteConfiguration.Unverified, ConnectorConfiguration = PetConnectorConfiguration.Unverified
    };

    public static PetProfile Converted80NUnknown { get; } = new("pet-converted-80n-unknown", "PET / converted unknown 80n editor / BASIC 4 / 80x25", "BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters-2.901447-10.bin", PetRomManifest.Converted80NUnknown, PetProfileStatus.Implemented, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm4032)
    {
        RomDirectoryOverride = Cbm8032.RomDirectory, KeyboardRevision = PetKeyboardRevision.Unverified, CassetteConfiguration = PetCassetteConfiguration.Unverified, ConnectorConfiguration = PetConnectorConfiguration.Unverified
    };

    public static PetProfile Cbm8096French { get; } = new("cbm-8096-french", "CBM 8096 French / BASIC 4 / 80x25 / placeholder", "BASIC 4", 80, 25, 0x10000, 0x8000, 0x0800, true, "characters-french.bin", PetRomManifest.Cbm8096French, PetProfileStatus.Placeholder, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        RomDirectoryOverride = Cbm8032.RomDirectory, MemoryExpansion = new PetMemoryExpansion(0xFFF0, 0x8000), KeyboardRevision = PetKeyboardRevision.Cbm8000Business, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm8000
    };

    public static PetProfile Cbm8296 { get; } = new("cbm-8296", "CBM 8296 / BASIC 4 / 80x25 / placeholder", "BASIC 4", 80, 25, 0x10000, 0x8000, 0x0800, true, "characters-324242-01.bin", PetRomManifest.Cbm8296, PetProfileStatus.Placeholder, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        RomDirectoryOverride = Cbm8032.RomDirectory, MemoryExpansion = new PetMemoryExpansion(0xFFF0, 0x10000), KeyboardRevision = PetKeyboardRevision.Cbm8000Business, CassetteConfiguration = PetCassetteConfiguration.ExternalOnly, ConnectorConfiguration = PetConnectorConfiguration.Cbm8000
    };

    public static PetProfile SuperPet6502 { get; } = new("superpet-6502", "SuperPET / 6502 mode / 80x25 / placeholder", "BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters.901640-01.bin", PetRomManifest.Cbm8032, PetProfileStatus.Placeholder, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        RomDirectoryOverride = Cbm8032.RomDirectory,
        ExpansionRomManifest = PetRomManifest.SuperPetWaterloo50Hz,
        AciaBaseAddress = SuperPetMemoryMap.AciaBaseAddress,
        InitialProcessor = SuperPetProcessor.Mos6502
    };

    public static PetProfile SuperPet6809 { get; } = new("superpet", "SuperPET / Waterloo 6809 / 80x25 / placeholder", "BASIC 4", 80, 25, 0x8000, 0x8000, 0x0800, true, "characters.901640-01.bin", PetRomManifest.Cbm8032, PetProfileStatus.Placeholder, PetCursorTracking.Basic2Convention, PetKeyboardLayout.Cbm8032)
    {
        RomDirectoryOverride = Cbm8032.RomDirectory,
        ExpansionRomManifest = PetRomManifest.SuperPetWaterloo50Hz,
        AciaBaseAddress = SuperPetMemoryMap.AciaBaseAddress,
        InitialProcessor = SuperPetProcessor.Motorola6809
    };

    /// <summary>Legacy alias for the default CLI profile, which represents 6809 mode.</summary>
    public static PetProfile SuperPet => SuperPet6809;

    public static IReadOnlyList<PetProfile> Planned { get; } = [Cbm8096French, Cbm8296, SuperPet6502, SuperPet6809];

    public static IReadOnlyList<PetProfile> All { get; } = [Pet2001_8, Pet2001_32, Cbm3008, Cbm3016, Cbm3032, Cbm4008Crtc40N60, Cbm4016Crtc40N60, Cbm4032, Cbm4032Crtc40N50, Cbm4032Crtc40B50, Cbm4032Crtc40B60, Cbm8032, Cbm8032Crtc80B50, Cbm8016Converted80N50, Converted80NUnknown];

    /// <summary>Profiles that passed the ROM and memory-bus checks and may be selected by a host.</summary>
    public static IReadOnlyList<PetProfile> Available { get; } = [.. All, SuperPet6502, SuperPet6809];

    /// <summary>Looks up a profile by <see cref="PetProfile.Id"/> (e.g. "pet-2001-32") - the
    /// string form a script/CLI command line points at, as opposed to <see cref="All"/>'s typed
    /// enumeration.</summary>
    public static PetProfile Find(string id) =>
        Available.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException(
            $"Unknown PET profile '{id}'. Available: {string.Join(", ", Available.Select(p => p.Id))}.");
}
