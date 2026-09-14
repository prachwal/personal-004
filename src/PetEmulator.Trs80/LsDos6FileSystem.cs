namespace PetEmulator.Trs80;

/// <summary>Read-only LS-DOS 6 / TRSDOS 6 (Model 4) directory/file reader for a <see cref="DmkDiskImage"/>.
///
/// Field layout verified against the official "Programmer's Guide to TRSDOS Version 6" (MISOSYS,
/// chapters 5.2-5.4) and cross-checked against a real LS-DOS 6.3 disk image: the directory cylinder
/// pointer (track 0/sector 0, byte 2) led straight to a GAT whose DOS-version byte (0xCB) read 0x63
/// and whose pack name/date fields decoded as plausible ASCII.
///
/// The FPDE layout (name/extension/EOF/LRL/extent-table byte offsets) is byte-for-byte identical to
/// NEWDOS/80's (see <see cref="NewDos80FileSystem"/>) - both descend from the same TRSDOS lineage -
/// except: the FXDE flag is bit 7 of byte 0 alone (not bit 7 AND bit 4 together), the "sector count"
/// field is named ERN (Ending Record Number) with the same size formula, and the DOS-identifier byte
/// is a version number (0x6X for LS-DOS/LDOS 6.x) rather than a fixed constant.</summary>
public sealed class LsDos6FileSystem
{
    private const int SectorSize = 256;
    private const int EntrySize = 32;
    private const int EntriesPerSector = SectorSize / EntrySize;
    private const int SectorsPerGranule = 6; // 5.25" double-density: 18 sectors/track, 6/granule, 3 granules/track.
    private const int DosVersionOffset = 0xCB;

    private readonly DmkDiskImage _disk;

    public int DirectoryTrack { get; }
    public int SectorsPerTrack { get; }
    public string DiskName { get; }

    public LsDos6FileSystem(DmkDiskImage disk)
    {
        _disk = disk ?? throw new ArgumentNullException(nameof(disk));
        SectorsPerTrack = disk.SectorsPerSide;
        DirectoryTrack = FindDirectoryTrack(disk);
        DiskName = ReadAscii(_disk.ReadSector(DirectoryTrack, 0), 0xD0, 8);
    }

    private static int FindDirectoryTrack(DmkDiskImage disk)
    {
        // Track 0/sector 0, byte 2 points straight at the directory cylinder (Programmer's Guide
        // §5.1). Verify with the DOS-version byte before trusting it - a corrupt or non-LS-DOS
        // disk could have a stray value there.
        var boot = disk.ReadSector(0, 0);
        var candidate = boot[2];
        if (candidate < disk.Tracks && (disk.ReadSector(candidate, 0)[DosVersionOffset] & 0xF0) == 0x60)
            return candidate;

        for (var track = 0; track < disk.Tracks; track++)
            if ((disk.ReadSector(track, 0)[DosVersionOffset] & 0xF0) == 0x60)
                return track;

        throw new InvalidDataException(
            "Not an LS-DOS/LDOS 6.x disk: no GAT with DOS-version byte 0x60-0x6F found.");
    }

    public IReadOnlyList<LsDos6DirEntry> ReadDirectory()
    {
        var result = new List<LsDos6DirEntry>();
        foreach (var sector in EnumerateFdeSectors())
            for (var slot = 0; slot < EntriesPerSector; slot++)
                if (TryDecode(sector.AsSpan(slot * EntrySize, EntrySize), out var decoded))
                    result.Add(decoded);
        return result;
    }

    public byte[] ReadFile(LsDos6DirEntry entry)
    {
        using var stream = new MemoryStream();
        var remaining = entry.SizeInSectors;
        foreach (var (track, sector) in WalkExtents(entry, SectorsPerTrack))
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

    public IReadOnlySet<(int Track, int Sector)> ReadFileSectors(LsDos6DirEntry entry)
    {
        var result = new HashSet<(int, int)>();
        var remaining = entry.SizeInSectors;
        foreach (var (track, sector) in WalkExtents(entry, SectorsPerTrack))
        {
            if (remaining <= 0)
                break;
            result.Add((track, sector));
            remaining--;
        }
        return result;
    }

    /// <summary>Per-sector allocation state from the GAT bitmap (bit N = granule N of the cylinder;
    /// a set bit means allocated/unavailable, per Programmer's Guide §5.2.1).</summary>
    public IReadOnlyList<(int Track, int Sector, bool IsAllocated)> ReadSectorMap()
    {
        var gat = _disk.ReadSector(DirectoryTrack, 0);
        var granulesPerTrack = SectorsPerTrack / SectorsPerGranule;
        var result = new List<(int, int, bool)>(_disk.Tracks * SectorsPerTrack);
        for (var track = 0; track < _disk.Tracks; track++)
        {
            var bits = gat[track];
            for (var granule = 0; granule < granulesPerTrack; granule++)
            {
                var allocated = (bits & (1 << granule)) != 0;
                var baseSector = granule * SectorsPerGranule;
                for (var offset = 0; offset < SectorsPerGranule; offset++)
                    result.Add((track, baseSector + offset, allocated));
            }
        }
        return result;
    }

    private static IEnumerable<(int Track, int Sector)> WalkExtents(LsDos6DirEntry entry, int sectorsPerTrack)
    {
        foreach (var (cylinder, granuleByte) in entry.Extents)
        {
            if (cylinder == 0xFF && granuleByte == 0xFF)
                continue;
            var firstGranule = (granuleByte >> 5) & 0x7;
            var granuleCount = (granuleByte & 0x1F) + 1;
            var startLinear = cylinder * sectorsPerTrack + firstGranule * SectorsPerGranule;
            var sectorCount = granuleCount * SectorsPerGranule;
            for (var i = 0; i < sectorCount; i++)
            {
                var linear = startLinear + i;
                yield return (linear / sectorsPerTrack, linear % sectorsPerTrack);
            }
        }
    }

    private IEnumerable<byte[]> EnumerateFdeSectors()
    {
        // HIT columns = SectorsPerTrack - 2 (GAT + HIT), per Programmer's Guide §5.3.
        var fdeSectorCount = SectorsPerTrack - 2;
        for (var sector = 2; sector < 2 + fdeSectorCount; sector++)
            yield return _disk.ReadSector(DirectoryTrack, sector);
    }

    private static bool TryDecode(ReadOnlySpan<byte> entry, out LsDos6DirEntry decoded)
    {
        decoded = null!;
        var attributes = entry[0];
        var active = (attributes & 0x10) != 0;
        var isExtended = (attributes & 0x80) != 0; // Bit 7 alone marks an FXDE (Programmer's Guide §5.4.1).
        if (!active || isExtended)
            return false;

        var name = ReadAscii(entry, 5, 8);
        var extension = ReadAscii(entry, 13, 3);
        var eofOffset = entry[3];
        var endingRecordNumber = entry[20] | (entry[21] << 8);
        var extents = new (byte Cylinder, byte Granule)[]
        {
            (entry[22], entry[23]), (entry[24], entry[25]), (entry[26], entry[27]), (entry[28], entry[29])
        };
        decoded = new LsDos6DirEntry(name, extension, endingRecordNumber, eofOffset, extents);
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

public sealed record LsDos6DirEntry(
    string Name,
    string Extension,
    int SizeInSectors,
    byte EofOffset,
    (byte Cylinder, byte Granule)[] Extents)
{
    public string FileName => Extension.Length == 0 ? Name : $"{Name}.{Extension}";
    public int SizeInBytes => SizeInSectors == 0 ? 0 : (SizeInSectors - 1) * 256 + (EofOffset == 0 ? 256 : EofOffset);
}
