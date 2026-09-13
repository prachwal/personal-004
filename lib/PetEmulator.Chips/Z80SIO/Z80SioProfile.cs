namespace PetEmulator.Chips;

public enum Z80SioRevision
{
    Sio0,
    Sio1,
    Sio2,
    Z8440,
}

public sealed record Z80SioProfile(
    Z80SioRevision Revision,
    bool SupportsRr3,
    bool SupportsDaisyChain,
    byte InvalidReadValue,
    byte UnsupportedRegisterReadValue)
{
    public static Z80SioProfile For(Z80SioRevision revision) => revision switch
    {
        Z80SioRevision.Sio0 => new(revision, false, false, 0xFF, 0x00),
        Z80SioRevision.Sio1 => new(revision, false, true, 0xFF, 0x00),
        Z80SioRevision.Sio2 => new(revision, true, true, 0xFF, 0x00),
        Z80SioRevision.Z8440 => new(revision, true, true, 0xFF, 0x00),
        _ => throw new ArgumentOutOfRangeException(nameof(revision)),
    };
}
