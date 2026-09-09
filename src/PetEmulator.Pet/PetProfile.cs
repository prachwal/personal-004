using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet;

public enum PetProfileStatus
{
    Implemented,
    Placeholder
}

/// <summary>
/// A hardware configuration of the Commodore PET/CBM family: BASIC version, screen geometry,
/// RAM/video RAM layout, and the exact ROM set it needs.
/// </summary>
/// <remarks>
/// Ported from personal-001's PetProfile. <see cref="RomDirectory"/> is new here: this repo groups
/// each profile's ROMs (main ROM set + character ROM) into its own subfolder under
/// <c>roms/pet/</c> instead of a single flat directory keyed by filename convention, so a profile
/// carries the folder name that holds exactly the ROMs <see cref="RomManifest"/> and
/// <see cref="CharacterRomPath"/> point at.
/// </remarks>
public sealed record PetProfile(
    string Id,
    string Name,
    string BasicVersion,
    int Columns,
    int Rows,
    uint RamSize,
    uint VideoRamStart,
    uint VideoRamLength,
    bool RequiresCrtc,
    string CharacterRomPath,
    IReadOnlyList<PetRomRequirement> RomManifest,
    PetProfileStatus Status)
{
    public bool IsImplemented => Status == PetProfileStatus.Implemented;

    public string Geometry => $"{Columns}x{Rows}";

    /// <summary>Subfolder name under <c>roms/pet/</c> holding this profile's ROM set.</summary>
    public string RomDirectory => Id;
}
