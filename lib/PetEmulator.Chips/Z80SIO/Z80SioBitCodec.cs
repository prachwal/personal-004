namespace PetEmulator.Chips;

public static class Z80SioBitCodec
{
    public static IReadOnlyList<bool> EncodeAsync(byte value, Z80SioAsyncFormat format)
    {
        Validate(format);
        var bits = new List<bool>(16) { false };
        for (var bit = 0; bit < format.DataBits; bit++)
            bits.Add((value & (1 << bit)) != 0);

        if (format.ParityEnabled)
        {
            var ones = CountOnes(value, format.DataBits);
            bits.Add(format.EvenParity ? (ones % 2 != 0) : (ones % 2 == 0));
        }

        var stopCount = format.StopBits >= 1.5 ? 2 : 1;
        for (var bit = 0; bit < stopCount; bit++)
            bits.Add(true);
        return bits;
    }

    public static Z80SioAsyncDecodeResult DecodeAsync(
        IReadOnlyList<bool> bits,
        Z80SioAsyncFormat format)
    {
        Validate(format);
        var required = 1 + format.DataBits + (format.ParityEnabled ? 1 : 0) + (format.StopBits >= 1.5 ? 2 : 1);
        if (bits.Count < required)
            return new(false, 0, false, false, 0);

        var framingError = bits[0];
        byte value = 0;
        for (var bit = 0; bit < format.DataBits; bit++)
            if (bits[1 + bit]) value |= (byte)(1 << bit);

        var parityError = false;
        var index = 1 + format.DataBits;
        if (format.ParityEnabled)
        {
            var expected = CountOnes(value, format.DataBits) % 2 != 0;
            if (!format.EvenParity) expected = !expected;
            parityError = bits[index++] != expected;
        }

        for (; index < required; index++)
            framingError |= !bits[index];
        return new(!framingError && !parityError, value, parityError, framingError, required);
    }

    private static int CountOnes(byte value, int dataBits)
    {
        var count = 0;
        for (var bit = 0; bit < dataBits; bit++)
            if ((value & (1 << bit)) != 0) count++;
        return count;
    }

    private static void Validate(Z80SioAsyncFormat format)
    {
        if (format.DataBits is < 5 or > 8 || format.StopBits is not (1 or 1.5 or 2))
            throw new ArgumentOutOfRangeException(nameof(format));
    }
}
