using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet;

public enum PetProfileStatus
{
    Implemented,
    Placeholder
}

/// <summary>ROM-verified keyboard wiring/scan table used by a PET profile.</summary>
public enum PetKeyboardLayout
{
    Pet2001Graphics,
    Cbm4032,
    Cbm8032
}

/// <summary>Video hardware used by a PET profile's mainboard revision.</summary>
public enum PetVideoHardware
{
    Discrete,
    Crtc
}

public enum PetKeyboardRevision
{
    Pet2001Graphics,
    Cbm3000Graphics,
    Cbm4000Graphics,
    Cbm8000Business,
    Unverified
}

public enum PetCassetteConfiguration
{
    InternalAndExternal,
    ExternalOnly,
    Unverified
}

public enum PetConnectorConfiguration
{
    Pet2001,
    Cbm3000,
    Cbm4000,
    Cbm8000,
    Unverified
}

/// <summary>Control-register and RAM capacity of the 8096/8296 expansion board.</summary>
public sealed record PetMemoryExpansion(ushort ControlRegisterAddress, uint ExpansionRamSize)
{
    public const byte Enabled = 0x80;
    public const byte IoPeekThrough = 0x40;
    public const byte ScreenPeekThrough = 0x20;
    public const byte UpperWriteProtect = 0x02;
    public const byte LowerWriteProtect = 0x01;

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
    PetProfileStatus Status,
    PetCursorTracking CursorTracking,
    PetKeyboardLayout KeyboardLayout)
{
    public bool IsImplemented => Status == PetProfileStatus.Implemented;

    /// <summary>Explicit video-hardware classification derived from the profile's I/O model.</summary>
    public PetVideoHardware VideoHardware => RequiresCrtc ? PetVideoHardware.Crtc : PetVideoHardware.Discrete;

    public string Geometry => $"{Columns}x{Rows}";

    /// <summary>Optional shared ROM-set folder. This is used when several hardware models use
    /// the same verified BASIC/editor/KERNAL images but differ in RAM capacity.</summary>
    public string? RomDirectoryOverride { get; init; }

    public PetKeyboardRevision KeyboardRevision { get; init; } = PetKeyboardRevision.Unverified;

    public PetCassetteConfiguration CassetteConfiguration { get; init; } = PetCassetteConfiguration.Unverified;

    public PetConnectorConfiguration ConnectorConfiguration { get; init; } = PetConnectorConfiguration.Unverified;

    public PetMemoryExpansion? MemoryExpansion { get; init; }

    /// <summary>Subfolder name under <c>roms/pet/</c> holding this profile's ROM set.</summary>
    public string RomDirectory => RomDirectoryOverride ?? Id;

    /// <summary>Optional firmware for an expansion processor or banked board. It is declared for
    /// inventory and validation now; the current PET bus does not map it until that hardware is
    /// implemented.</summary>
    public IReadOnlyList<PetRomRequirement>? ExpansionRomManifest { get; init; }

    /// <summary>Optional ACIA base address for an expansion profile, such as SuperPET.</summary>
    public ushort? AciaBaseAddress { get; init; }

    /// <summary>
    /// Physical width:height ratio of one on-screen pixel on real PET/CBM hardware - pixels are
    /// not square. 40-column models are 5:6 (slightly taller than wide); 80-column models double
    /// the horizontal resolution into the same physical screen width, so their pixel is half as
    /// wide again: 5:12. Both correction factors resolve the same ~4:3 physical CRT (320x5 /
    /// 200x6 = 640x5 / 200x12 = 4:3) - real period monitors, not square pixels, are what made
    /// both column counts look right on the same tube.
    /// </summary>
    public (int Width, int Height) PixelAspect => Columns == 40 ? (5, 6) : (5, 12);
}
