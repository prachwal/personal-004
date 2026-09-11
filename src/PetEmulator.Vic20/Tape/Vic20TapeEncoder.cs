using PetEmulator.Pet.Tape;

namespace PetEmulator.Vic20.Tape;

/// <summary>
/// Encodes a header block + payload into a real <c>PetTapeCassetteFormat</c> pulse-cycle stream -
/// the production counterpart of <c>PetEmulator.Pet.Tests.Tape.PetTapeTestEncoder</c> (that one's
/// test-only/internal; this one is used at runtime by <see cref="Vic20Machine"/> to build the
/// tape a real SAVE produces - see that class's doc comment for why a re-encode, not a literal
/// analog capture).
/// </summary>
internal static class Vic20TapeEncoder
{
    /// <summary>Leader tone length (in Short pulses) before each duplicated copy's sync
    /// countdown - matches <c>roms/vic20/test-tapes/hello-vic.tap</c>'s own margins, generous
    /// enough for the real KERNAL's leader-hunt to lock on reliably.</summary>
    private const int LeaderPulses = 4000;

    private const int GapPulses = 400;

    public static List<int> EncodeTape(IReadOnlyList<byte> header, IReadOnlyList<byte> payload)
    {
        var pulses = new List<int>();
        pulses.AddRange(EncodeStream(header, firstCopy: true, LeaderPulses));
        pulses.AddRange(ShortRun(GapPulses));
        pulses.AddRange(EncodeStream(header, firstCopy: false, GapPulses));
        pulses.AddRange(ShortRun(LeaderPulses));
        pulses.AddRange(EncodeStream(payload, firstCopy: true, LeaderPulses));
        pulses.AddRange(ShortRun(GapPulses));
        pulses.AddRange(EncodeStream(payload, firstCopy: false, GapPulses));
        return pulses;
    }

    private static IEnumerable<int> ShortRun(int count) => Enumerable.Repeat(PetTapeCassetteFormat.ShortPulseCycles, count);

    private static List<int> EncodeStream(IReadOnlyList<byte> content, bool firstCopy, int leaderShortPulses)
    {
        var pulses = new List<int>(ShortRun(leaderShortPulses));
        pulses.AddRange(EncodeBytes(PetTapeCassetteFormat.SyncCountdown(firstCopy)));
        pulses.AddRange(EncodeBytes(content));
        byte checksum = 0;
        foreach (var b in content) checksum ^= b;
        pulses.AddRange(EncodeByte(checksum));
        return pulses;
    }

    private static List<int> EncodeBytes(IEnumerable<byte> values)
    {
        var pulses = new List<int>();
        foreach (var v in values) pulses.AddRange(EncodeByte(v));
        return pulses;
    }

    private static List<int> EncodeByte(byte value)
    {
        var pulses = new List<int> { PetTapeCassetteFormat.LongPulseCycles, PetTapeCassetteFormat.MediumPulseCycles };
        var setBits = 0;
        for (var bit = 0; bit < 8; bit++)
        {
            var isSet = (value & (1 << bit)) != 0;
            if (isSet) setBits++;
            pulses.Add(isSet ? PetTapeCassetteFormat.MediumPulseCycles : PetTapeCassetteFormat.ShortPulseCycles);
            pulses.Add(isSet ? PetTapeCassetteFormat.ShortPulseCycles : PetTapeCassetteFormat.MediumPulseCycles);
        }

        var parityBit = setBits % 2 == 0 ? 1 : 0; // 1 XOR every data bit
        pulses.Add(parityBit == 1 ? PetTapeCassetteFormat.MediumPulseCycles : PetTapeCassetteFormat.ShortPulseCycles);
        pulses.Add(parityBit == 1 ? PetTapeCassetteFormat.ShortPulseCycles : PetTapeCassetteFormat.MediumPulseCycles);
        return pulses;
    }
}
