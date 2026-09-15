namespace PetEmulator.Trs80;

public static class Trs80CassetteEncoding
{
    public const byte Sync = 0xA5;
    public const double ClockMHz = 1.774;
    public const double PostSyncExtraDelayMicros = 1034;
    public static readonly (double DurationMicros, bool High)[] ZeroBit = [(128, true), (1871, false)];
    public static readonly (double DurationMicros, bool High)[] OneBit = [(128, true), (128, false), (876, true), (988, false)];
    public static int ToTStates(double microseconds, double clockMHz = ClockMHz) => (int)Math.Round(microseconds * clockMHz);
}
