namespace PetEmulator.Kaypro;

/// <summary>Reads the sector-bearing portion of a TeleDisk image.</summary>
public static class KayproTd0Reader
{
    private const int FileHeaderSize = 12;
    private const int SectorHeaderSize = 6;

    public static KayproDiskImage Read(Stream source, bool writeProtected = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        var header = ReadExactly(new StreamReader(source), FileHeaderSize);
        if (header[0] != 'T' && header[0] != 't' || header[1] != 'D' && header[1] != 'd')
            throw new InvalidDataException("Not a TeleDisk image.");

        IByteReader reader = header[0] == 't' ? new LzhufReader(new KayproTd0LzhufDecoder(source)) : new StreamReader(source);

        if ((header[7] & 0x80) != 0)
        {
            var comment = ReadExactly(reader, 10);
            var length = comment[2] | (comment[3] << 8);
            _ = ReadExactly(reader, length);
        }

        var image = new byte[KayproDiskImage.ImageSize];
        var seen = new bool[KayproDiskImage.Tracks * KayproDiskImage.SectorsPerTrack];

        while (true)
        {
            var track = ReadExactly(reader, 4);
            if (track[0] == 0xFF) break;
            if (track[0] != KayproDiskImage.SectorsPerTrack || track[1] >= KayproDiskImage.Tracks || (track[2] & 0x7F) != 0)
                throw new InvalidDataException($"TD0 track geometry is not Kaypro II: sectors={track[0]}, track={track[1]}, head={track[2]}.");

            for (var index = 0; index < track[0]; index++)
            {
                var sector = ReadSector(reader, KayproDiskImage.SectorSizeBytes);
                if (sector.Track != track[1] || (sector.Head & 0x7F) != (track[2] & 0x7F) ||
                    sector.Id >= KayproDiskImage.SectorsPerTrack ||
                    sector.Data.Length != KayproDiskImage.SectorSizeBytes)
                    throw new InvalidDataException("TD0 sector geometry is not Kaypro II.");

                // KPII-149 uses zero-based TD0 IDs; KayproDiskImage exposes the
                // controller-facing one-based IDs used by FD1793.
                var slot = sector.Track * KayproDiskImage.SectorsPerTrack + sector.Id;
                if (seen[slot]) throw new InvalidDataException($"Duplicate TD0 sector {sector.Track}/{sector.Id}.");
                seen[slot] = true;
                sector.Data.CopyTo(image, slot * KayproDiskImage.SectorSizeBytes);
            }
        }

        if (seen.Any(found => !found)) throw new InvalidDataException("TD0 image has missing Kaypro sectors.");
        return new KayproDiskImage(image, writeProtected, firstSectorId: 0);
    }

    private static TeleDiskSector ReadSector(IByteReader source, int expectedSize)
    {
        var header = ReadExactly(source, SectorHeaderSize);
        var size = 128 << header[3];
        if (size != expectedSize) throw new InvalidDataException("Unsupported TD0 sector size.");

        var control = header[4] == 0x10 ? Array.Empty<byte>() : ReadExactly(source, 3);
        var encoding = header[4] == 0x10 ? (byte)0 : control[2];
        if (header[4] == 0x10) return new TeleDiskSector(header[0], header[1], header[2], new byte[size]);

        var data = encoding switch
        {
            0 => ReadExactly(source, size),
            1 => Repeat(source, size),
            2 => ReadFragments(source, size),
            _ => throw new InvalidDataException("Unsupported TD0 sector encoding.")
        };
        return new TeleDiskSector(header[0], header[1], header[2], data);
    }

    private static byte[] Repeat(IByteReader source, int size)
    {
        var count = ReadUInt16(source);
        var pattern = ReadExactly(source, 2);
        if (count * 2 != size) throw new InvalidDataException("Invalid TD0 repeated-sector length.");
        var data = new byte[size];
        for (var index = 0; index < size; index++) data[index] = pattern[index & 1];
        return data;
    }

    private static byte[] ReadFragments(IByteReader source, int size)
    {
        var data = new byte[size];
        var offset = 0;
        while (offset < size)
        {
            var control = ReadExactly(source, 2);
            var flag = control[0];
            var count = control[1];
            if (flag == 0)
            {
                if (count == 0 || offset + count > size) throw new InvalidDataException("Invalid TD0 literal fragment.");
                ReadExactly(source, count).CopyTo(data, offset);
                offset += count;
            }
            else
            {
                var patternSize = 1 << flag;
                if (flag > 4 || count == 0 || offset + patternSize * count > size)
                    throw new InvalidDataException("Invalid TD0 pattern fragment.");
                var pattern = ReadExactly(source, patternSize);
                for (var repeat = 0; repeat < count; repeat++)
                {
                    pattern.CopyTo(data, offset);
                    offset += patternSize;
                }
            }
        }
        return data;
    }

    private static ushort ReadUInt16(IByteReader source)
    {
        var bytes = ReadExactly(source, 2);
        return (ushort)(bytes[0] | (bytes[1] << 8));
    }

    private static byte[] ReadExactly(IByteReader source, int length)
    {
        var data = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var value = source.ReadByte();
            if (value < 0) throw new EndOfStreamException("Unexpected end of TD0 image.");
            data[offset++] = (byte)value;
        }
        return data;
    }

    private sealed record TeleDiskSector(byte Track, byte Head, byte Id, byte[] Data);

    private interface IByteReader { int ReadByte(); }
    private sealed class StreamReader(Stream source) : IByteReader { public int ReadByte() => source.ReadByte(); }
    private sealed class LzhufReader(KayproTd0LzhufDecoder decoder) : IByteReader { public int ReadByte() => decoder.ReadByte(); }
}
