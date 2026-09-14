namespace PetEmulator.Trs80;

/// <summary>Reads TRS-80 DMK disk images (David Keil's format): a 16-byte header followed by,
/// for each track/side, a 128-byte IDAM pointer table and the raw MFM/FM track data those
/// pointers address. Read-only; does not validate CRCs (fine for reading well-formed,
/// non-copy-protected disks - which is what a file browser needs).
///
/// Format reference: "DMK-Format-Details.txt" (openMSX docs, mirroring David Keil's original
/// TRS-80 emulator documentation). Verified against a real double-density Model 4 disk image
/// (single-sided in practice: side 1 present but unformatted on every track).</summary>
public sealed class DmkDiskImage
{
    private const int HeaderSize = 16;
    private const int TrackHeaderSize = 128;
    private const int MaxIdamEntries = TrackHeaderSize / 2;
    private const int IdOffsetMask = 0x3FFF;
    private const byte IdAddressMark = 0xFE;
    private const int MaxGapScan = 64;

    private readonly byte[] _data;
    private readonly int _trackLength;

    public int Tracks { get; }
    public int Sides { get; }

    /// <summary>Sectors per side, per track, inferred from track 0 side 0's IDAM count.
    /// Assumes uniform formatting across the disk (true for any normally-formatted disk).</summary>
    public int SectorsPerSide { get; }

    private DmkDiskImage(byte[] data, int tracks, int sides, int trackLength, int sectorsPerSide)
    {
        _data = data;
        Tracks = tracks;
        Sides = sides;
        _trackLength = trackLength;
        SectorsPerSide = sectorsPerSide;
    }

    public static DmkDiskImage Load(string path) => Load(File.ReadAllBytes(path));

    public static DmkDiskImage Load(byte[] data)
    {
        if (!LooksLikeDmk(data, out var reason))
            throw new InvalidDataException($"Not a DMK image: {reason}");

        var tracks = data[1];
        var trackLength = data[2] | (data[3] << 8);
        var singleSided = (data[4] & 0x10) != 0;
        var sides = singleSided ? 1 : 2;
        var image = new DmkDiskImage(data, tracks, sides, trackLength, 0);
        var sectorsPerSide = image.CountIdamEntries(0, 0);
        return new DmkDiskImage(data, tracks, sides, trackLength, sectorsPerSide);
    }

    /// <summary>Cheap structural sniff for content-based format detection: header shape plus a
    /// file-length upper bound (real files may be shorter than the nominal tracks*sides*trackLength
    /// when trailing tracks were never formatted - DMK never reclaims allocated space but also
    /// never pads unformatted trailing tracks, so under-length is normal; over-length is not).</summary>
    public static bool LooksLikeDmk(byte[] data, out string reason)
    {
        if (data.Length < HeaderSize + TrackHeaderSize)
        {
            reason = "file too short for a DMK header + one track header";
            return false;
        }
        if (data[0] != 0x00 && data[0] != 0xFF)
        {
            reason = $"byte 0 (write-protect flag) is 0x{data[0]:X2}, expected 0x00 or 0xFF";
            return false;
        }
        var tracks = data[1];
        if (tracks is 0 or > 96)
        {
            reason = $"byte 1 (track count) is {tracks}, outside the plausible 1-96 range";
            return false;
        }
        var trackLength = data[2] | (data[3] << 8);
        if (trackLength < TrackHeaderSize + 512 || trackLength > 0x2940)
        {
            reason = $"track length {trackLength} outside the plausible DMK range";
            return false;
        }
        var singleSided = (data[4] & 0x10) != 0;
        var sides = singleSided ? 1 : 2;
        var maxSize = HeaderSize + (long)tracks * sides * trackLength;
        if (data.Length > maxSize)
        {
            reason = $"file length {data.Length} exceeds the header's declared capacity ({maxSize})";
            return false;
        }
        reason = "";
        return true;
    }

    public byte[] ReadSector(int track, int side, int sector)
    {
        if (!TryReadSector(track, side, sector, out var result))
            throw new ArgumentOutOfRangeException(nameof(sector), $"DMK sector {track}/{side}/{sector} not found.");
        return result;
    }

    /// <summary>Logical single-sided-style addressing: sector 0..SectorsPerSide-1 come from side 0,
    /// SectorsPerSide..2*SectorsPerSide-1 continue on side 1 - matching how TRSDOS-family
    /// filesystems address double-sided cylinders as one wide track.</summary>
    public byte[] ReadSector(int track, int logicalSector)
    {
        var side = SectorsPerSide == 0 ? 0 : logicalSector / SectorsPerSide;
        var sector = SectorsPerSide == 0 ? logicalSector : logicalSector % SectorsPerSide;
        return ReadSector(track, side, sector);
    }

    private bool TryReadSector(int track, int side, int sector, out byte[] result)
    {
        result = [];
        if (!TryTrackOffset(track, side, out var trackOffset))
            return false;

        for (var i = 0; i < MaxIdamEntries; i++)
        {
            var pointer = _data[trackOffset + i * 2] | (_data[trackOffset + i * 2 + 1] << 8);
            if (pointer == 0)
                break;

            var idamOffset = trackOffset + (pointer & IdOffsetMask);
            if (idamOffset + 7 > _data.Length || _data[idamOffset] != IdAddressMark)
                continue;
            if (_data[idamOffset + 3] != sector)
                continue;

            var lengthCode = _data[idamOffset + 4];
            var sectorLength = 128 << lengthCode;
            var scanStart = idamOffset + 7;
            for (var offset = scanStart; offset < Math.Min(scanStart + MaxGapScan, _data.Length); offset++)
            {
                if (_data[offset] != 0xFB && _data[offset] != 0xF8)
                    continue;
                var dataStart = offset + 1;
                if (dataStart + sectorLength > _data.Length)
                    break;
                result = _data[dataStart..(dataStart + sectorLength)];
                return true;
            }
        }
        return false;
    }

    private int CountIdamEntries(int track, int side)
    {
        if (!TryTrackOffset(track, side, out var trackOffset))
            return 0;
        var count = 0;
        for (var i = 0; i < MaxIdamEntries; i++)
        {
            var pointer = _data[trackOffset + i * 2] | (_data[trackOffset + i * 2 + 1] << 8);
            if (pointer == 0)
                break;
            count++;
        }
        return count;
    }

    private bool TryTrackOffset(int track, int side, out int offset)
    {
        offset = 0;
        if (track < 0 || track >= Tracks || side < 0 || side >= Sides)
            return false;
        var candidate = HeaderSize + (track * Sides + side) * _trackLength;
        if (candidate + _trackLength > _data.Length)
            return false; // Trailing track/side never formatted - DMK simply omits it from the file.
        offset = candidate;
        return true;
    }
}
