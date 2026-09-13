namespace PetEmulator.Chips;

public static class Z80SioSdlcCodec
{
    public const byte Flag = 0x7E;

    public static IReadOnlyList<bool> EncodeFrame(IReadOnlyList<byte> payload)
    {
        var body = payload.Concat(new[]
        {
            (byte)(Z80SioCrc.ComputeSdlc(payload) & 0xFF),
            (byte)(Z80SioCrc.ComputeSdlc(payload) >> 8),
        }).ToArray();
        var bits = EncodeByte(Flag).ToList();
        var ones = 0;
        foreach (var value in body)
        {
            foreach (var bit in EncodeByte(value))
            {
                bits.Add(bit);
                ones = bit ? ones + 1 : 0;
                if (ones == 5)
                {
                    bits.Add(false);
                    ones = 0;
                }
            }
        }
        bits.AddRange(EncodeByte(Flag));
        return bits;
    }

    public static IReadOnlyList<bool> EncodeAbort() => Enumerable.Repeat(true, 7).ToArray();

    public static Z80SioSdlcDecodeResult DecodeFrame(IReadOnlyList<bool> bits)
    {
        var flag = EncodeByte(Flag);
        var start = Find(bits, flag, 0);
        if (start < 0) return new(false, [], false, false, false);
        var end = Find(bits, flag, start + flag.Length);
        var abort = HasAbort(bits, start + flag.Length, end < 0 ? bits.Count : end);
        if (abort) return new(false, [], false, true, false);
        if (end < 0) return new(false, [], false, false, false);

        var bodyBits = bits.Skip(start + flag.Length).Take(end - start - flag.Length).ToList();
        var unstuffed = RemoveStuffing(bodyBits);
        if (unstuffed.Count < 16 || unstuffed.Count % 8 != 0)
            return new(false, [], false, false, true);
        var bytes = ToBytes(unstuffed).ToArray();
        var payload = bytes[..^2];
        var received = (ushort)(bytes[^2] | (bytes[^1] << 8));
        var expected = Z80SioCrc.ComputeSdlc(payload);
        return new(received == expected, payload, received == expected, false, true);
    }

    private static bool[] EncodeByte(byte value) => Enumerable.Range(0, 8).Select(bit => (value & (1 << bit)) != 0).ToArray();
    private static int Find(IReadOnlyList<bool> bits, IReadOnlyList<bool> pattern, int start)
    {
        for (var index = start; index <= bits.Count - pattern.Count; index++)
            if (pattern.Select((bit, offset) => bits[index + offset] == bit).All(x => x)) return index;
        return -1;
    }

    private static bool HasAbort(IReadOnlyList<bool> bits, int start, int end)
    {
        var ones = 0;
        for (var i = start; i < end; i++)
        {
            ones = bits[i] ? ones + 1 : 0;
            if (ones >= 7) return true;
        }
        return false;
    }

    private static List<bool> RemoveStuffing(IReadOnlyList<bool> bits)
    {
        var result = new List<bool>();
        var ones = 0;
        for (var i = 0; i < bits.Count; i++)
        {
            var bit = bits[i];
            result.Add(bit);
            ones = bit ? ones + 1 : 0;
            if (ones == 5 && i + 1 < bits.Count && !bits[i + 1])
            {
                i++;
                ones = 0;
            }
        }
        return result;
    }

    private static IEnumerable<byte> ToBytes(IReadOnlyList<bool> bits)
    {
        for (var offset = 0; offset < bits.Count; offset += 8)
        {
            byte value = 0;
            for (var bit = 0; bit < 8; bit++)
                if (bits[offset + bit]) value |= (byte)(1 << bit);
            yield return value;
        }
    }
}
