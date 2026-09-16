using System.Buffers.Binary;
using System.Text;

namespace PetEmulator.CpcFdc;

public sealed class DskDiskImage
{
    private const int HeaderSize = 0x100;
    private const int TrackHeaderSize = 0x100;
    private readonly List<DskTrack> _tracks = [];

    public int TrackCount { get; private set; }
    public int HeadCount { get; private set; }
    public bool Extended { get; private set; }
    public IReadOnlyList<DskTrack> Tracks => _tracks;

    public static DskDiskImage Load(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize || (!Ascii(data[..8], "MV - CPC") && !Ascii(data[..16], "EXTENDED CPC DSK")))
            throw new InvalidDataException("Not a DSK image.");

        var image = new DskDiskImage
        {
            TrackCount = data[0x30],
            HeadCount = data[0x31],
            Extended = Ascii(data[..16], "EXTENDED CPC DSK")
        };
        if (image.TrackCount == 0 || image.HeadCount == 0) throw new InvalidDataException("DSK has no geometry.");

        int offset = HeaderSize;
        for (int track = 0; track < image.TrackCount; track++)
            for (int head = 0; head < image.HeadCount; head++)
            {
                int length = image.Extended
                    ? data[0x34 + track * image.HeadCount + head] * 0x100
                    : BinaryPrimitives.ReadUInt16LittleEndian(data[0x32..]);
                if (image.Extended && length == 0) continue;
                if (length < TrackHeaderSize || offset + length > data.Length)
                    throw new InvalidDataException("DSK track exceeds image.");
                image._tracks.Add(ParseTrack(data.Slice(offset, length), track, head, image.Extended));
                offset += length;
            }
        return image;
    }

    /// <summary>Serializes the current sector data as a standard or extended CPC DSK image.</summary>
    public byte[] Save()
    {
        var tracks = new byte[TrackCount * HeadCount][];
        for (int cylinder = 0; cylinder < TrackCount; cylinder++)
            for (int head = 0; head < HeadCount; head++)
                tracks[cylinder * HeadCount + head] = BuildTrack(FindTrack((byte)cylinder, (byte)head), cylinder, head);

        int standardTrackLength = tracks.Max(track => track.Length);
        if (!Extended) tracks = tracks.Select(track => PadTrack(track, standardTrackLength)).ToArray();

        int length = HeaderSize + tracks.Where((_, index) => !Extended || FindTrack((byte)(index / HeadCount), (byte)(index % HeadCount)) is not null).Sum(track => track.Length);
        var image = new byte[length];
        Encoding.ASCII.GetBytes(Extended ? "EXTENDED CPC DSK File\r\nDisk-Info\r\n" : "MV - CPCEMU Disk-File\r\nDisk-Info\r\n").CopyTo(image, 0);
        image[0x30] = (byte)TrackCount;
        image[0x31] = (byte)HeadCount;
        if (Extended)
            for (int i = 0; i < tracks.Length; i++)
                if (FindTrack((byte)(i / HeadCount), (byte)(i % HeadCount)) is not null) image[0x34 + i] = (byte)(tracks[i].Length / HeaderSize);

        int offset = HeaderSize;
        for (int i = 0; i < tracks.Length; i++)
        {
            if (Extended && FindTrack((byte)(i / HeadCount), (byte)(i % HeadCount)) is null) continue;
            tracks[i].CopyTo(image, offset);
            offset += tracks[i].Length;
        }
        if (!Extended)
        {
            image[0x32] = (byte)standardTrackLength;
            image[0x33] = (byte)(standardTrackLength >> 8);
        }
        return image;
    }

    public bool TryRead(byte cylinder, byte head, byte sector, byte sizeCode, Span<byte> destination)
    {
        DskSector? found = Find(cylinder, head, sector, sizeCode);
        if (found is null || destination.Length < found.Data.Length) return false;
        found.Data.AsSpan().CopyTo(destination);
        return true;
    }

    public bool TryWrite(byte cylinder, byte head, byte sector, byte sizeCode, ReadOnlySpan<byte> source)
    {
        DskSector? found = Find(cylinder, head, sector, sizeCode);
        if (found is null || source.Length < found.Data.Length) return false;
        source[..found.Data.Length].CopyTo(found.Data);
        return true;
    }

    public DskTrack? FindTrack(byte cylinder, byte head) =>
        _tracks.FirstOrDefault(t => t.Cylinder == cylinder && t.Head == head);

    private DskSector? Find(byte cylinder, byte head, byte sector, byte sizeCode) =>
        FindTrack(cylinder, head)?.Sectors.FirstOrDefault(s => s.Id == sector && s.SizeCode == sizeCode);

    public DskDiskImageSnapshot CaptureState() => new()
    {
        TrackCount = TrackCount, HeadCount = HeadCount, Extended = Extended,
        Tracks = _tracks.Select(track => new DskTrackSnapshot
        {
            Cylinder = track.Cylinder, Head = track.Head,
            Sectors = track.Sectors.Select(sector => new DskSectorSnapshot
            {
                Id = sector.Id, SizeCode = sector.SizeCode, Status1 = sector.Status1, Status2 = sector.Status2, Data = sector.Data.ToArray(),
            }).ToArray(),
        }).ToArray(),
    };

    public static DskDiskImage RestoreState(DskDiskImageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var image = new DskDiskImage { TrackCount = snapshot.TrackCount, HeadCount = snapshot.HeadCount, Extended = snapshot.Extended };
        foreach (DskTrackSnapshot track in snapshot.Tracks)
        {
            var restored = new DskTrack(track.Cylinder, track.Head);
            foreach (DskSectorSnapshot sector in track.Sectors)
                restored.Sectors.Add(new DskSector(sector.Id, sector.SizeCode, sector.Status1, sector.Status2, sector.Data.ToArray()));
            image._tracks.Add(restored);
        }
        return image;
    }

    private static DskTrack ParseTrack(ReadOnlySpan<byte> data, int fallbackCylinder, int fallbackHead, bool extended)
    {
        if (!Ascii(data[..12], "Track-Info")) throw new InvalidDataException("Invalid DSK track header.");
        int cylinder = data[0x10];
        int head = data[0x11];
        int count = data[0x15];
        var track = new DskTrack(cylinder, head);
        int dataOffset = TrackHeaderSize;
        for (int i = 0; i < count; i++)
        {
            int descriptor = 0x18 + i * 8;
            byte sizeCode = data[descriptor + 3];
            int length = extended ? BinaryPrimitives.ReadUInt16LittleEndian(data[(descriptor + 6)..]) : 128 << sizeCode;
            if (length < 0 || dataOffset + length > data.Length) throw new InvalidDataException("Invalid DSK sector length.");
            byte[] sectorData = data.Slice(dataOffset, length).ToArray();
            track.Sectors.Add(new DskSector(data[descriptor + 2], sizeCode, data[descriptor + 4], data[descriptor + 5], sectorData));
            dataOffset += length;
        }
        return track;
    }

    private static bool Ascii(ReadOnlySpan<byte> data, string value) =>
        data.Length >= value.Length && Encoding.ASCII.GetString(data[..value.Length]).Equals(value, StringComparison.Ordinal);

    private static byte[] BuildTrack(DskTrack? track, int cylinder, int head)
    {
        track ??= new DskTrack(cylinder, head);
        int dataLength = track.Sectors.Sum(sector => sector.Data.Length);
        int length = RoundTrackLength(TrackHeaderSize + dataLength);
        var data = new byte[length];
        Encoding.ASCII.GetBytes("Track-Info\r\n").CopyTo(data, 0);
        data[0x10] = (byte)track.Cylinder;
        data[0x11] = (byte)track.Head;
        data[0x15] = (byte)track.Sectors.Count;
        int dataOffset = TrackHeaderSize;
        for (int i = 0; i < track.Sectors.Count; i++)
        {
            DskSector sector = track.Sectors[i];
            int descriptor = 0x18 + i * 8;
            data[descriptor] = (byte)track.Cylinder;
            data[descriptor + 1] = (byte)track.Head;
            data[descriptor + 2] = sector.Id;
            data[descriptor + 3] = sector.SizeCode;
            data[descriptor + 4] = sector.Status1;
            data[descriptor + 5] = sector.Status2;
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(descriptor + 6), (ushort)sector.Data.Length);
            sector.Data.CopyTo(data, dataOffset);
            dataOffset += sector.Data.Length;
        }
        return data;
    }

    private static int RoundTrackLength(int length) => (length + HeaderSize - 1) / HeaderSize * HeaderSize;
    private static byte[] PadTrack(byte[] track, int length) => track.Length == length ? track : [.. track, .. new byte[length - track.Length]];
}
public sealed class DskTrack(int cylinder, int head)
{
    public int Cylinder { get; } = cylinder;
    public int Head { get; } = head;
    public List<DskSector> Sectors { get; } = [];
}

public sealed class DskSector(byte id, byte sizeCode, byte status1, byte status2, byte[] data)
{
    public byte Id { get; } = id;
    public byte SizeCode { get; } = sizeCode;
    public byte Status1 { get; } = status1;
    public byte Status2 { get; } = status2;
    public byte[] Data { get; } = data;
}
