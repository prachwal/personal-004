namespace PetEmulator.Kaypro;

internal sealed class KayproTd0LzhufDecoder
{
    private const int N = 4096;
    private const int F = 60;
    private const int Threshold = 2;
    private const int NChar = 314;
    private const int T = 627;
    private const int R = 626;
    private const int MaxFreq = 0x8000;

    private static readonly byte[] DCode = BuildDCode();
    private static readonly byte[] DLength = BuildDLength();

    private readonly Stream source;
    private readonly byte[] text = new byte[N];
    private readonly ushort[] frequency = new ushort[T + 1];
    private readonly int[] parent = new int[T + NChar];
    private readonly int[] children = new int[T];
    private ushort bitBuffer;
    private int bitCount;
    private int writeIndex = N - F;
    private int copyPosition;
    private int copyRemaining;

    public KayproTd0LzhufDecoder(Stream source)
    {
        this.source = source;
        Array.Fill(text, (byte)' ', 0, N - F);
        StartHuffman();
    }

    public int ReadByte()
    {
        if (copyRemaining != 0)
        {
            var value = text[(copyPosition++) & (N - 1)];
            copyRemaining--;
            Store(value);
            return value;
        }

        var symbol = DecodeChar();
        if (symbol < 256)
        {
            Store((byte)symbol);
            return symbol;
        }

        copyPosition = (writeIndex - DecodePosition() - 1) & (N - 1);
        copyRemaining = symbol - 255 + Threshold;
        return ReadByte();
    }

    private void Store(byte value)
    {
        text[writeIndex] = value;
        writeIndex = (writeIndex + 1) & (N - 1);
    }

    private int GetBit()
    {
        EnsureBits(1);
        var bit = (bitBuffer & 0x8000) != 0 ? 1 : 0;
        bitBuffer <<= 1;
        bitCount--;
        return bit;
    }

    private int GetByte()
    {
        EnsureBits(8);
        var value = bitBuffer >> 8;
        bitBuffer <<= 8;
        bitCount -= 8;
        return value;
    }

    private void EnsureBits(int count)
    {
        while (bitCount < count)
        {
            var value = source.ReadByte();
            if (value < 0) throw new EndOfStreamException("Unexpected end of advanced TD0 stream.");
            bitBuffer |= (ushort)(value << (8 - bitCount));
            bitCount += 8;
        }
    }

    private void StartHuffman()
    {
        for (var i = 0; i < NChar; i++)
        {
            frequency[i] = 1;
            children[i] = i + T;
            parent[i + T] = i;
        }

        var node = 0;
        for (var next = NChar; next <= R; next++, node += 2)
        {
            frequency[next] = (ushort)(frequency[node] + frequency[node + 1]);
            children[next] = node;
            parent[node] = parent[node + 1] = next;
        }

        frequency[T] = ushort.MaxValue;
        parent[R] = 0;
    }

    private int DecodeChar()
    {
        var node = children[R];
        while (node < T) node = children[node + GetBit()];
        var symbol = node - T;
        Update(symbol);
        return symbol;
    }

    private int DecodePosition()
    {
        var value = GetByte();
        var code = DCode[value] << 6;
        var length = DLength[value] - 2;
        while (length-- != 0) value = (value << 1) + GetBit();
        return code | (value & 0x3f);
    }

    private void Update(int symbol)
    {
        if (frequency[R] == MaxFreq) Reconstruct();
        var node = parent[symbol + T];
        do
        {
            var value = ++frequency[node];
            var swap = node + 1;
            if (value > frequency[swap])
            {
                while (value > frequency[++swap]) { }
                swap--;
                frequency[node] = frequency[swap];
                frequency[swap] = value;

                var child = children[node];
                parent[child] = swap;
                if (child < T) parent[child + 1] = swap;

                child = children[swap];
                children[swap] = children[node];
                parent[child] = node;
                if (child < T) parent[child + 1] = node;
                children[node] = child;
                node = swap;
            }
        } while ((node = parent[node]) != 0);
    }

    private void Reconstruct()
    {
        var leaf = 0;
        for (var node = 0; node < T; node++)
        {
            if (children[node] >= T)
            {
                frequency[leaf] = (ushort)((frequency[node] + 1) / 2);
                children[leaf++] = children[node];
            }
        }

        var current = 0;
        for (var next = NChar; next < T; current += 2, next++)
        {
            var value = frequency[current] + frequency[current + 1];
            var insert = next - 1;
            while (insert >= 0 && value < frequency[insert]) insert--;
            insert++;
            Array.Copy(frequency, insert, frequency, insert + 1, (next - insert) * 1);
            frequency[insert] = (ushort)value;
            Array.Copy(children, insert, children, insert + 1, next - insert);
            children[insert] = current;
        }

        for (var node = 0; node < T; node++)
        {
            var child = children[node];
            parent[child] = node;
            if (child < T) parent[child + 1] = node;
        }
    }

    private static byte[] BuildDCode()
    {
        var result = new byte[256];
        var ranges = new[] { 0, 32, 48, 64, 80, 88, 96, 104, 112, 120, 128, 136, 144 };
        for (var code = 0; code < ranges.Length - 1; code++) Array.Fill(result, (byte)code, ranges[code], ranges[code + 1] - ranges[code]);
        for (var index = 144; index < 192; index += 4) Array.Fill(result, (byte)(12 + (index - 144) / 4), index, 4);
        for (var index = 192; index < 240; index += 2) Array.Fill(result, (byte)(24 + (index - 192) / 2), index, 2);
        for (var index = 240; index < 256; index++) result[index] = (byte)(48 + index - 240);
        return result;
    }

    private static byte[] BuildDLength()
    {
        var result = new byte[256];
        Array.Fill(result, (byte)3, 0, 32);
        Array.Fill(result, (byte)4, 32, 48);
        Array.Fill(result, (byte)5, 80, 64);
        Array.Fill(result, (byte)6, 144, 48);
        Array.Fill(result, (byte)7, 192, 48);
        Array.Fill(result, (byte)8, 240, 16);
        return result;
    }
}
