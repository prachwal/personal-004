namespace PetEmulator.Pet.Tape;

/// <summary>
/// Decodes a stream of cassette pulse widths (in PET CPU cycles) into the bytes they encode,
/// per the Commodore Datasette bit/byte framing (see <see cref="PetTapeCassetteFormat"/>). Each
/// byte is: a (Long, Medium) start marker, 8 data bit-pairs (LSB first; (Short, Medium) = 0,
/// (Medium, Short) = 1), then one odd-parity bit-pair (parity = 1 XOR every data bit, so the
/// total set bits including parity is always odd).
/// </summary>
public static class PetTapePulseDecoder
{
    /// <summary>Decodes every byte in <paramref name="pulseCycles"/> (which must contain nothing
    /// but back-to-back encoded bytes - no leader, sync, or gap pulses).</summary>
    public static IReadOnlyList<byte> DecodeBytes(IReadOnlyList<int> pulseCycles)
    {
        ArgumentNullException.ThrowIfNull(pulseCycles);
        var kinds = pulseCycles.Select(PetTapeCassetteFormat.Classify).ToArray();
        var bytes = new List<byte>();
        var index = 0;
        while (index < kinds.Length)
            bytes.Add(DecodeByte(kinds, ref index));
        return bytes;
    }

    /// <summary>Decodes one byte starting at <paramref name="index"/>, advancing it past the 20
    /// pulses consumed (start marker + 8 data bits + parity). Used by <see cref="DecodeBytes"/>
    /// and by <see cref="PetTapePulseTrack"/> to read sync/content/checksum bytes in sequence
    /// from the same underlying pulse-kind array.</summary>
    internal static byte DecodeByte(IReadOnlyList<PetTapePulseKind> kinds, ref int index)
    {
        if (index + 1 >= kinds.Count || kinds[index] != PetTapePulseKind.Long || kinds[index + 1] != PetTapePulseKind.Medium)
            throw new FormatException($"Expected a byte start marker (Long, Medium) at pulse {index}, found ({kinds[index]}, {(index + 1 < kinds.Count ? kinds[index + 1] : (PetTapePulseKind?)null)}).");
        index += 2;

        byte value = 0;
        for (var bit = 0; bit < 8; bit++)
        {
            if (index + 1 >= kinds.Count)
                throw new FormatException($"Truncated data bit {bit} at pulse {index}.");
            value |= (byte)(DecodeBit(kinds[index], kinds[index + 1], bit) << bit);
            index += 2;
        }

        if (index + 1 >= kinds.Count)
            throw new FormatException($"Truncated parity bit at pulse {index}.");
        var parityBit = DecodeBit(kinds[index], kinds[index + 1], 8);
        index += 2;

        var setBits = System.Numerics.BitOperations.PopCount(value);
        var expectedParity = setBits % 2 == 0 ? 1 : 0; // odd parity: total set bits (incl. parity) must be odd
        if (parityBit != expectedParity)
            throw new FormatException($"Parity mismatch decoding byte (${value:X2}): expected parity bit {expectedParity}, got {parityBit}.");

        return value;
    }

    private static int DecodeBit(PetTapePulseKind first, PetTapePulseKind second, int bitIndex) => (first, second) switch
    {
        (PetTapePulseKind.Short, PetTapePulseKind.Medium) => 0,
        (PetTapePulseKind.Medium, PetTapePulseKind.Short) => 1,
        _ => throw new FormatException($"Invalid bit-pair ({first}, {second}) for bit {bitIndex}.")
    };
}
