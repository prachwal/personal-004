namespace PetEmulator.Pet;

/// <summary>
/// Where a PET/CBM KERNAL tracks the text cursor in zero page: the current screen-line pointer
/// (low byte, high byte) plus a column byte - the same 3-byte shape across BASIC 1/2/4, but at
/// DIFFERENT absolute zero-page addresses per ROM revision. A real, empirically-confirmed
/// hardware fact, not derivable from anything else <see cref="PetProfile"/> already carries
/// (unlike e.g. <see cref="PetProfile.PixelAspect"/>, computed from Columns) - hence its own
/// per-profile field.
///
/// Confirmed by typing one character repeatedly and watching which zero-page byte increments by
/// exactly 1 each time (the column), and which pair combines with
/// <see cref="PetProfile.VideoRamStart"/> to land in a genuinely valid, live-tracked screen
/// offset (the line pointer) - see docs/pet/cursor-fix.md for the full investigation, including
/// the CRTC-register red herring this replaced (real BASIC 4 KERNALs never use their CRTC's
/// hardware cursor register for this - see that doc).
/// </summary>
public sealed record PetCursorTracking(ushort LineLowAddress, ushort LineHighAddress, ushort ColumnAddress)
{
    /// <summary>BASIC 2/4's convention ($C4/$C5/$C6) - shared by every implemented profile except
    /// the original PET 2001 (BASIC 1).</summary>
    public static PetCursorTracking Basic2Convention { get; } = new(0x00C4, 0x00C5, 0x00C6);

    /// <summary>BASIC 1's convention (PET 2001-8 only) - same shape, shifted zero-page addresses.</summary>
    public static PetCursorTracking Basic1Convention { get; } = new(0x00E0, 0x00E1, 0x00E2);
}
