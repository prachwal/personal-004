using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Tests.Tape;

/// <summary>Encodes pulse sequences mirroring PetTapePulseDecoder/PetTapePulseTrack's expected
/// format, for round-trip testing (no real .tap fixture exists in this repo).</summary>
internal static class PetTapeTestEncoder
{
    public static List<int> EncodeByte(byte value)
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

    public static List<int> EncodeBytes(IEnumerable<byte> values)
    {
        var pulses = new List<int>();
        foreach (var value in values) pulses.AddRange(EncodeByte(value));
        return pulses;
    }

    /// <summary>Encodes a full duplicated-copy data stream: an optional Short-pulse leader, the
    /// 9-byte sync countdown for the given copy, the content bytes, and their XOR checksum.</summary>
    public static List<int> EncodeStream(IReadOnlyList<byte> content, bool firstCopy, int leaderShortPulses = 20)
    {
        var pulses = new List<int>(Enumerable.Repeat(PetTapeCassetteFormat.ShortPulseCycles, leaderShortPulses));
        pulses.AddRange(EncodeBytes(PetTapeCassetteFormat.SyncCountdown(firstCopy)));
        pulses.AddRange(EncodeBytes(content));
        byte checksum = 0;
        foreach (var b in content) checksum ^= b;
        pulses.AddRange(EncodeByte(checksum));
        return pulses;
    }
}
