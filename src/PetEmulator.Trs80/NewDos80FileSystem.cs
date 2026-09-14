namespace PetEmulator.Trs80;

/// <summary>Read-only NEWDOS/80 (TRS-DOS family) directory/file reader for a <see cref="Jv1DiskImage"/>.
///
/// Supports the standard single-density layout verified against a real NEWDOS/80 v2.0 system disk:
/// 5 sectors/granule (SPG), 2 granules/lump (GPL), 10 sectors/track (SPT) - i.e. 1 lump == 1 track,
/// with the directory located empirically (by DOS-identifier byte) rather than assumed at a fixed
/// track. Non-standard GPL/SPG configurations (double density, hard-drive PDRIVE variants) are out
/// of scope - they need a real sample to verify against, same as this one was.
///
/// Format reference: "TRS-80 Hacker's Handbook for NEWDOS/80", Chapter 5 "Various"
/// (Directory Entries / Granule Allocation Table / Hash Index Table, pages 65-68).</summary>
public sealed class NewDos80FileSystem
{
    private const int SectorsPerTrack = Jv1DiskImage.SectorsPerTrack;
    private const int SectorSize = Jv1DiskImage.SectorSizeBytes;
    private const int SectorsPerGranule = 5;
    private const int GranulesPerLump = 2;
    private const int EntrySize = 32;
    private const int EntriesPerSector = SectorSize / EntrySize;
    private const int DosIdentifierOffset = 0xCB;
    private const byte NewDos80Identifier = 0x82;

    private readonly Jv1DiskImage _disk;

    public int DirectoryTrack { get; }
    public string DiskName { get; }

    public NewDos80FileSystem(Jv1DiskImage disk)
    {
        _disk = disk ?? throw new ArgumentNullException(nameof(disk));
        DirectoryTrack = FindDirectoryTrack(disk);
        DiskName = ReadAscii(_disk.ReadSector(DirectoryTrack, 0), 0xD0, 8);
    }

    private static int FindDirectoryTrack(Jv1DiskImage disk)
    {
        for (var track = 0; track < disk.Tracks; track++)
            if (disk.ReadSector(track, 0)[DosIdentifierOffset] == NewDos80Identifier)
                return track;
        throw new InvalidDataException(
            "Not a NEWDOS/80 disk: GAT signature (byte 0xCB = 0x82) not found on any track.");
    }

    public IReadOnlyList<NewDos80DirEntry> ReadDirectory()
    {
        var result = new List<NewDos80DirEntry>();
        foreach (var sector in EnumerateFdeSectors())
            for (var slot = 0; slot < EntriesPerSector; slot++)
                if (TryDecode(sector.AsSpan(slot * EntrySize, EntrySize), out var decoded))
                    result.Add(decoded);
        return result;
    }

    public byte[] ReadFile(NewDos80DirEntry entry)
    {
        using var stream = new MemoryStream();
        var remaining = entry.SizeInSectors;
        foreach (var (track, sector) in WalkExtents(entry))
        {
            if (remaining <= 0)
                break;
            var data = _disk.ReadSector(track, sector);
            var count = remaining == 1 ? (entry.EofOffset == 0 ? SectorSize : entry.EofOffset) : SectorSize;
            stream.Write(data, 0, count);
            remaining--;
        }
        return stream.ToArray();
    }

    public IReadOnlySet<(int Track, int Sector)> ReadFileSectors(NewDos80DirEntry entry)
    {
        var result = new HashSet<(int, int)>();
        var remaining = entry.SizeInSectors;
        foreach (var (track, sector) in WalkExtents(entry))
        {
            if (remaining <= 0)
                break;
            result.Add((track, sector));
            remaining--;
        }
        return result;
    }

    /// <summary>Per-sector allocation state, derived from the GAT bitmap (bit N = granule N of the
    /// track/lump; under the standard layout GPL=2, so bits 0 and 1 cover sectors 0-4 and 5-9).</summary>
    public IReadOnlyList<(int Track, int Sector, bool IsAllocated)> ReadSectorMap()
    {
        var gat = _disk.ReadSector(DirectoryTrack, 0);
        var result = new List<(int, int, bool)>(_disk.Tracks * SectorsPerTrack);
        for (var lump = 0; lump < _disk.Tracks; lump++)
        {
            var bits = gat[lump];
            for (var granule = 0; granule < GranulesPerLump; granule++)
            {
                var allocated = (bits & (1 << granule)) != 0;
                var baseSector = granule * SectorsPerGranule;
                for (var offset = 0; offset < SectorsPerGranule; offset++)
                    result.Add((lump, baseSector + offset, allocated));
            }
        }
        return result;
    }

    /// <summary>Walks a file's extents as a globally-contiguous granule stream (lump*GPL + firstGranule
    /// gives the starting granule index; the extent then runs for grans*SPG sectors, wrapping across
    /// track boundaries as needed) - verified against real extent/size pairs on the reference disk.</summary>
    private static IEnumerable<(int Track, int Sector)> WalkExtents(NewDos80DirEntry entry)
    {
        foreach (var (lump, granuleByte) in entry.Extents)
        {
            if (lump == 0xFF)
                continue;
            var firstGranule = (granuleByte >> 5) & 0x7;
            var granuleCount = (granuleByte & 0x1F) + 1;
            var startLinear = lump * SectorsPerTrack + firstGranule * SectorsPerGranule;
            var sectorCount = granuleCount * SectorsPerGranule;
            for (var i = 0; i < sectorCount; i++)
            {
                var linear = startLinear + i;
                yield return (linear / SectorsPerTrack, linear % SectorsPerTrack);
            }
        }
    }

    private IEnumerable<byte[]> EnumerateFdeSectors()
    {
        var hit = _disk.ReadSector(DirectoryTrack, 1);
        var fdeSectorCount = SectorsPerTrack + hit[0x1F] - 2;
        var track = DirectoryTrack;
        var sector = 2;
        for (var i = 0; i < fdeSectorCount; i++)
        {
            yield return _disk.ReadSector(track, sector);
            sector++;
            if (sector >= SectorsPerTrack)
            {
                sector = 0;
                track++;
            }
        }
    }

    private static bool TryDecode(ReadOnlySpan<byte> entry, out NewDos80DirEntry decoded)
    {
        decoded = null!;
        var flags = entry[0];
        var active = (flags & 0x10) != 0;
        var isExtended = (flags & 0x90) == 0x90; // FXDE recognition code: bits 7 & 4 both 1.
        if (!active || isExtended)
            return false;

        var name = ReadAscii(entry, 5, 8);
        var extension = ReadAscii(entry, 13, 3);
        var eofOffset = entry[3];
        var sectorCount = entry[20] | (entry[21] << 8);
        var extents = new (byte Lump, byte Granule)[]
        {
            (entry[22], entry[23]), (entry[24], entry[25]), (entry[26], entry[27]), (entry[28], entry[29])
        };
        decoded = new NewDos80DirEntry(name, extension, sectorCount, eofOffset, extents);
        return true;
    }

    private static string ReadAscii(ReadOnlySpan<byte> data, int offset, int length)
    {
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            var value = data[offset + i];
            chars[i] = value is >= 0x20 and <= 0x7E ? (char)value : ' ';
        }
        return new string(chars).TrimEnd();
    }
}

public sealed record NewDos80DirEntry(
    string Name,
    string Extension,
    int SizeInSectors,
    byte EofOffset,
    (byte Lump, byte Granule)[] Extents)
{
    public string FileName => Extension.Length == 0 ? Name : $"{Name}.{Extension}";
    public int SizeInBytes => SizeInSectors == 0 ? 0 : (SizeInSectors - 1) * 256 + (EofOffset == 0 ? 256 : EofOffset);
}
