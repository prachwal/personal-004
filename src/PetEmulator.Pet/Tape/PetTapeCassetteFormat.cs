namespace PetEmulator.Pet.Tape;

/// <summary>
/// The Commodore Datasette cassette encoding: every cassette-based Commodore machine (PET,
/// VIC-20, C64, C16/Plus4) shares the same physical cassette port and the same KERNAL-level
/// pulse encoding, just with a machine-specific CPU clock. Data is a stream of square-wave
/// pulses classified into three widths; pairs of pulses encode one data bit, and each byte is
/// framed by a start marker and an odd-parity bit.
/// </summary>
public static class PetTapeCassetteFormat
{
    public const int ShortPulseCycles = 352;
    public const int MediumPulseCycles = 512;
    public const int LongPulseCycles = 672;

    private const int ShortMediumThreshold = (ShortPulseCycles + MediumPulseCycles) / 2;
    private const int MediumLongThreshold = (MediumPulseCycles + LongPulseCycles) / 2;

    /// <summary>The synchronization byte countdown that precedes a data stream's content, per
    /// Simon's Guide and c64-wiki: $09..$01 for the second copy of a duplicated stream, with the
    /// top bit additionally set ($89..$81) for the first copy.</summary>
    public static IReadOnlyList<byte> SyncCountdown(bool firstCopy) =>
        firstCopy ? [0x89, 0x88, 0x87, 0x86, 0x85, 0x84, 0x83, 0x82, 0x81]
                  : [0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01];

    public static PetTapePulseKind Classify(int cycles) =>
        cycles < ShortMediumThreshold ? PetTapePulseKind.Short
        : cycles < MediumLongThreshold ? PetTapePulseKind.Medium
        : PetTapePulseKind.Long;
}

public enum PetTapePulseKind { Short, Medium, Long }
