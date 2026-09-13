namespace PetEmulator.Chips;

public enum Z80PioRevision
{
    Z8420,
    Z84C20,
}

public enum Z80PioTechnology
{
    Nmos,
    Cmos,
}

public sealed record Z80PioProfile(
    Z80PioRevision Revision,
    Z80PioTechnology Technology,
    double MaximumClockMHz,
    bool SupportsStaticClock,
    byte InvalidReadValue,
    byte UnsupportedRegisterReadValue)
{
    public static Z80PioProfile For(Z80PioRevision revision) => revision switch
    {
        Z80PioRevision.Z8420 => new(revision, Z80PioTechnology.Nmos, 6.17, false, 0xFF, 0x00),
        Z80PioRevision.Z84C20 => new(revision, Z80PioTechnology.Cmos, 8.00, true, 0xFF, 0x00),
        _ => throw new ArgumentOutOfRangeException(nameof(revision)),
    };
}

public enum Z80PioMode : byte
{
    Output = 0,
    Input = 1,
    Bidirectional = 2,
    BitControl = 3,
}

public enum Z80PioControlPhase : byte
{
    Ready,
    Mode3IoSelect,
    InterruptMask,
}
