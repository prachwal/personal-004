namespace PetEmulator.Vic20;

/// <summary>
/// Display geometry for a <see cref="Vic20Machine"/> - a separate, explicit configuration
/// contract passed in at construction (not hardcoded in a GUI ViewModel) so a caller can override
/// it (a future PAL variant, a different canvas) without touching the machine's own code. Mirrors
/// PetEmulator.Pet.PetProfile.PixelAspect's role for PET, just as its own small record instead of
/// a field on a bigger profile type (VIC-20 v1 has no profile catalog - see
/// docs/vic20-migration-plan.md's NTSC-only scope cut).
/// </summary>
public sealed record Vic20DisplayConfig(int PixelAspectWidth, int PixelAspectHeight)
{
    /// <summary>NTSC pixel aspect ratio, derived the same way PetProfile.PixelAspect documents
    /// its own values: real hardware paints a physical 4:3 CRT regardless of native pixel count,
    /// so <c>PixelAspect = (4:3 target) / (native PixelWidth:PixelHeight)</c>. With
    /// <see cref="Vic20RasterDisplay"/>'s native 176x184 canvas: (4*184):(3*176) = 736:528,
    /// reduced by their gcd (16) to 46:33 (~1.39:1 - wider than tall, unlike PET's narrower-than-
    /// tall correction, because VIC-20's native aspect (176:184 ~ 0.957:1, near-square) already
    /// undershoots 4:3 more than PET's 40-column 320:200 does).</summary>
    public static Vic20DisplayConfig Ntsc { get; } = new(46, 33);
}
