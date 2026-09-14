using System.Text;

namespace PetEmulator.Pet.CbmDos;

/// <summary>
/// Pure D64 image parsing/writing: BAM, directory chain, sector allocation. No CPU/PIA/bus coupling.
/// </summary>
public sealed class D64Image
{
    private static readonly int[] SectorsPerTrack =
    [
        21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21,
        19, 19, 19, 19, 19, 19, 19,
        18, 18, 18, 18, 18, 18,
        17, 17, 17, 17, 17
    ];

    private static int TotalSectors => 683;

    private readonly byte[] _data;

    public string DiskName { get; }
    public string DiskId { get; }
    public string DosType { get; }

    private D64Image(byte[] data, string diskName, string diskId, string dosType)
    {
        _data = data;
        DiskName = diskName;
        DiskId = diskId;
        DosType = dosType;
    }

    public static D64Image Load(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        return Parse(data);
    }

    public static D64Image Load(byte[] data) => Parse(data);

    /// <summary>Content-based sniff for format detection: D64 has no magic byte, so the only
    /// signal available is file length matching one of the standard 1541 image sizes (with or
    /// without the trailing per-track error-info bytes some tools append).</summary>
    public static bool LooksLikeD64(byte[] data) => data.Length is 174848 or 175531 or 196608 or 197376;

    private static D64Image Parse(byte[] data)
    {
        int bamOff = TrackSectorToOffset(18, 0);

        string diskName = ReadPetAscii(data, bamOff + 144, 16);
        string diskId = ReadPetAscii(data, bamOff + 162, 2);
        string dosType = ReadPetAscii(data, bamOff + 165, 2);

        return new D64Image(data, diskName, diskId, dosType);
    }

    public List<DirEntry> ReadDirectory()
    {
        var result = new List<DirEntry>();
        int dirTrack = _data[TrackSectorToOffset(18, 0)];
        int dirSector = _data[TrackSectorToOffset(18, 0) + 1];

        int track = dirTrack;
        int sector = dirSector;
        var visited = new HashSet<(int, int)>();

        while (track > 0 && visited.Add((track, sector)))
        {
            int off = TrackSectorToOffset(track, sector);
            int nextTrack = _data[off];
            int nextSector = _data[off + 1];

            for (int i = 0; i < 8; i++)
            {
                int entry = off + i * 32;
                byte ft = _data[entry + 2];
                if (ft == 0)
                    continue;

                byte typeVal = (byte)(ft & 0x07);
                if (typeVal == 0)
                    continue;

                var type = (FileType)typeVal;
                bool closed = (ft & 0x80) != 0;
                bool locked = (ft & 0x40) != 0;
                byte startTrack = _data[entry + 3];
                byte startSector = _data[entry + 4];

                var filenameBytes = new byte[16];
                Array.Copy(_data, entry + 5, filenameBytes, 0, 16);

                int size = _data[entry + 30] | (_data[entry + 31] << 8);

                result.Add(new DirEntry(type, closed, locked,
                    startTrack, startSector, filenameBytes, size));
            }

            track = nextTrack;
            sector = nextSector;
        }

        return result;
    }

    /// <summary>Returns the BAM allocation state for every sector on a standard 35-track D64.</summary>
    public IReadOnlyList<D64SectorInfo> ReadSectorMap()
    {
        var result = new List<D64SectorInfo>(TotalSectors);
        var bam = TrackSectorToOffset(18, 0);

        for (var track = 1; track <= SectorsPerTrack.Length; track++)
        {
            var bamEntry = bam + 4 + (track - 1) * 4;
            var bitmap = _data[bamEntry + 1] |
                         (_data[bamEntry + 2] << 8) |
                         (_data[bamEntry + 3] << 16);
            for (var sector = 0; sector < SectorsPerTrack[track - 1]; sector++)
                result.Add(new D64SectorInfo(track, sector, (bitmap & (1 << sector)) == 0));
        }

        return result;
    }

    /// <summary>Returns the sector chain used by a directory entry, in disk order.</summary>
    public IReadOnlyList<D64SectorAddress> ReadFileSectors(DirEntry entry)
    {
        var result = new List<D64SectorAddress>();
        var track = entry.StartTrack;
        var sector = entry.StartSector;
        var visited = new HashSet<(int Track, int Sector)>();

        while (track > 0 && visited.Add((track, sector)))
        {
            result.Add(new D64SectorAddress(track, sector));
            var offset = TrackSectorToOffset(track, sector);
            track = _data[offset];
            sector = _data[offset + 1];
        }

        return result;
    }

    public byte[] ReadFile(DirEntry entry)
    {
        byte[] result;

        int track = entry.StartTrack;
        int sector = entry.StartSector;

        using (var ms = new MemoryStream())
        {
            while (track > 0)
            {
                int off = TrackSectorToOffset(track, sector);
                byte nextTrack = _data[off];
                byte nextSector = _data[off + 1];

                ms.Write(_data, off + 2, 254);

                if (nextTrack == 0)
                {
                    int validBytes = nextSector;
                    if (validBytes == 0)
                        validBytes = 256;

                    ms.SetLength(ms.Length - 254 + validBytes);
                    result = ms.ToArray();
                    return result;
                }

                track = nextTrack;
                sector = nextSector;
            }

            result = ms.ToArray();
        }

        return result;
    }

    public static int TrackSectorToOffset(int track, int sector)
    {
        int offset = 0;
        for (int t = 1; t < track; t++)
            offset += SectorsPerTrack[t - 1] * 256;
        return offset + sector * 256;
    }

    private static string ReadPetAscii(byte[] data, int offset, int length)
    {
        int end = offset + length;
        while (end > offset && (data[end - 1] == 0xA0 || data[end - 1] == 0x00 || data[end - 1] == 0x20))
            end--;

        return Encoding.ASCII.GetString(data, offset, end - offset);
    }

    public static byte[] CreateEmpty()
    {
        return new byte[TotalSectors * 256];
    }

    /// <summary>A genuinely mountable/writable blank disk - unlike <see cref="CreateEmpty"/> (a
    /// bare zero-filled buffer of the right size, with an all-zero BAM), this actually formats
    /// one: directory chain pointing at track 18/sector 1, every track's free-sector count and
    /// bitmap set (all sectors free except track 18's own sectors 0 - the BAM itself - and 1 -
    /// the first, initially-empty directory sector), and the disk name/ID/DOS-type fields
    /// <see cref="Parse"/> reads back. Without this, <see cref="TryAllocateSector"/> can never
    /// succeed (every track's free count reads 0) and <see cref="CbmDosEngine"/>'s SAVE silently
    /// writes nothing while still reporting success - a real bug this type's own first "blank
    /// disk" helper reproduced, found while adding a "New Disk" feature (see
    /// docs/pet/disk-testing-strategy.md).</summary>
    public static byte[] CreateFormatted(string diskName, string diskId, string dosType = "2A")
    {
        var data = new byte[TotalSectors * 256];
        int bamOff = TrackSectorToOffset(18, 0);

        data[bamOff] = 18; // first directory track
        data[bamOff + 1] = 1; // first directory sector
        data[bamOff + 2] = 0x41; // DOS version byte ('A') - real 1541 convention

        for (var t = 1; t <= 35; t++)
        {
            int entry = bamOff + 4 + (t - 1) * 4;
            int sectors = SectorsPerTrack[t - 1];
            int bitmap = (1 << sectors) - 1; // every bit set = every sector free
            int reserved = 0;
            if (t == 18)
            {
                bitmap &= ~0x03; // sectors 0 (BAM) and 1 (first dir sector) are already used
                reserved = 2;
            }

            data[entry] = (byte)(sectors - reserved);
            data[entry + 1] = (byte)(bitmap & 0xFF);
            data[entry + 2] = (byte)((bitmap >> 8) & 0xFF);
            data[entry + 3] = (byte)((bitmap >> 16) & 0xFF);
        }

        WritePetAscii(data, bamOff + 144, diskName, 16);
        data[bamOff + 160] = 0xA0;
        data[bamOff + 161] = 0xA0;
        WritePetAscii(data, bamOff + 162, diskId, 2);
        data[bamOff + 164] = 0xA0;
        WritePetAscii(data, bamOff + 165, dosType, 2);
        data[bamOff + 167] = 0xA0;
        data[bamOff + 168] = 0xA0;

        // The first directory sector: end of chain (next track 0), all 8 entry slots empty -
        // ReadDirectory/AddDirectoryEntry both treat a zero file-type byte as "unused slot".
        int dirOff = TrackSectorToOffset(18, 1);
        data[dirOff] = 0;
        data[dirOff + 1] = 0xFF;

        return data;
    }

    private static void WritePetAscii(byte[] data, int offset, string value, int length)
    {
        for (var i = 0; i < length; i++)
            data[offset + i] = i < value.Length ? (byte)value[i] : (byte)0xA0;
    }

    public void WriteSector(int track, int sector, byte[] data)
    {
        if (data.Length != 256)
            throw new ArgumentException("Sector data must be 256 bytes", nameof(data));
        int off = TrackSectorToOffset(track, sector);
        Array.Copy(data, 0, _data, off, 256);
    }

    public byte[] ReadSector(int track, int sector)
    {
        var data = new byte[256];
        Array.Copy(_data, TrackSectorToOffset(track, sector), data, 0, data.Length);
        return data;
    }

    public void CreateFile(string filename, ReadOnlySpan<byte> content, FileType type = FileType.Seq)
    {
        ValidateFilename(filename);
        if (ReadDirectory().Any(entry => entry.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase)))
            throw new IOException($"File already exists: {filename}");

        var sectors = AllocateFileSectors(content.Length);
        WriteFileSectors(sectors, content);
        var entry = new DirEntry(type, true, false,
            (byte)(sectors.Count == 0 ? 0 : sectors[0].Track),
            (byte)(sectors.Count == 0 ? 0 : sectors[0].Sector),
            EncodeFilename(filename), (sectors.Count));
        if (!TryAddDirectoryEntry(entry))
        {
            foreach (var sector in sectors) FreeSector(sector.Track, sector.Sector);
            throw new IOException("Directory is full.");
        }
    }

    public void UpdateFile(string filename, ReadOnlySpan<byte> content)
    {
        var entry = FindDirectoryEntry(filename, out _);
        DeleteFile(filename);
        try
        {
            CreateFile(filename, content, entry.Type);
        }
        catch
        {
            throw new IOException($"Unable to update file: {filename}");
        }
    }

    public void DeleteFile(string filename)
    {
        var entry = FindDirectoryEntry(filename, out var offset);
        FreeFileSectors(entry);
        _data[offset + 2] = 0;
    }

    public void RenameFile(string oldName, string newName)
    {
        ValidateFilename(newName);
        if (ReadDirectory().Any(entry => entry.Filename.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            throw new IOException($"File already exists: {newName}");
        _ = FindDirectoryEntry(oldName, out var offset);
        EncodeFilename(newName).CopyTo(_data, offset + 5);
    }

    private List<(int Track, int Sector)> AllocateFileSectors(int length)
    {
        var sectors = new List<(int Track, int Sector)>((length + 253) / 254);
        while (length > 0)
        {
            if (!TryAllocateSector(out var track, out var sector))
            {
                foreach (var allocated in sectors) FreeSector(allocated.Track, allocated.Sector);
                throw new IOException("Disk is full.");
            }
            sectors.Add((track, sector));
            length -= 254;
        }
        return sectors;
    }

    private void WriteFileSectors(IReadOnlyList<(int Track, int Sector)> sectors, ReadOnlySpan<byte> content)
    {
        for (var index = 0; index < sectors.Count; index++)
        {
            var data = new byte[256];
            var next = index + 1 < sectors.Count ? sectors[index + 1] : (0, Math.Min(254, content.Length - index * 254));
            data[0] = (byte)next.Item1;
            data[1] = (byte)next.Item2;
            var count = Math.Min(254, content.Length - index * 254);
            content.Slice(index * 254, count).CopyTo(data.AsSpan(2));
            WriteSector(sectors[index].Track, sectors[index].Sector, data);
        }
    }

    private void FreeFileSectors(DirEntry entry)
    {
        var track = entry.StartTrack;
        var sector = entry.StartSector;
        var visited = new HashSet<(int, int)>();
        while (track > 0 && visited.Add((track, sector)))
        {
            var offset = TrackSectorToOffset(track, sector);
            var nextTrack = _data[offset];
            var nextSector = _data[offset + 1];
            FreeSector(track, sector);
            track = nextTrack;
            sector = nextSector;
        }
    }

    private void FreeSector(int track, int sector)
    {
        var bamEntry = TrackSectorToOffset(18, 0) + 4 + (track - 1) * 4;
        var bitmap = _data[bamEntry + 1] | (_data[bamEntry + 2] << 8) | (_data[bamEntry + 3] << 16);
        if ((bitmap & (1 << sector)) != 0)
            return;
        bitmap |= 1 << sector;
        _data[bamEntry + 1] = (byte)bitmap;
        _data[bamEntry + 2] = (byte)(bitmap >> 8);
        _data[bamEntry + 3] = (byte)(bitmap >> 16);
        _data[bamEntry]++;
    }

    private bool TryAddDirectoryEntry(DirEntry entry)
    {
        var dirTrack = _data[TrackSectorToOffset(18, 0)];
        var dirSector = _data[TrackSectorToOffset(18, 0) + 1];
        var track = dirTrack;
        var sector = dirSector;
        var visited = new HashSet<(int, int)>();
        while (track > 0 && visited.Add((track, sector)))
        {
            var offset = TrackSectorToOffset(track, sector);
            for (var index = 0; index < 8; index++)
            {
                var entryOffset = offset + index * 32;
                if (_data[entryOffset + 2] == 0)
                {
                    FillDirEntry(entryOffset, entry);
                    return true;
                }
            }
            track = _data[offset];
            sector = _data[offset + 1];
        }
        return false;
    }

    private DirEntry FindDirectoryEntry(string filename, out int offset)
    {
        ValidateFilename(filename);
        var target = filename.Trim();
        var dirTrack = _data[TrackSectorToOffset(18, 0)];
        var dirSector = _data[TrackSectorToOffset(18, 0) + 1];
        var visited = new HashSet<(int, int)>();
        while (dirTrack > 0 && visited.Add((dirTrack, dirSector)))
        {
            var sectorOffset = TrackSectorToOffset(dirTrack, dirSector);
            for (var index = 0; index < 8; index++)
            {
                var entryOffset = sectorOffset + index * 32;
                var type = (FileType)(_data[entryOffset + 2] & 0x07);
                if (type != 0 && ReadFilename(entryOffset).Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    offset = entryOffset;
                    return new DirEntry(type, (_data[entryOffset + 2] & 0x80) != 0,
                        (_data[entryOffset + 2] & 0x40) != 0, _data[entryOffset + 3], _data[entryOffset + 4],
                        _data.AsSpan(entryOffset + 5, 16).ToArray(), _data[entryOffset + 30] | (_data[entryOffset + 31] << 8));
                }
            }
            dirTrack = _data[sectorOffset];
            dirSector = _data[sectorOffset + 1];
        }
        throw new FileNotFoundException($"File not found: {filename}");
    }

    private string ReadFilename(int offset) => Encoding.Latin1.GetString(_data, offset + 5, 16)
        .TrimEnd('\xA0', '\0', ' ');

    private static byte[] EncodeFilename(string filename)
    {
        var result = Enumerable.Repeat((byte)0xA0, 16).ToArray();
        Encoding.ASCII.GetBytes(filename.Trim()).AsSpan(0, Math.Min(16, filename.Trim().Length)).CopyTo(result);
        return result;
    }

    private static void ValidateFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename) || filename.Trim().Length > 16)
            throw new ArgumentException("CBM filename must contain 1 to 16 characters.", nameof(filename));
    }

    public bool TryAllocateSector(out int track, out int sector)
    {
        for (int t = 1; t <= 35; t++)
        {
            int bamEntry = TrackSectorToOffset(18, 0) + 4 + (t - 1) * 4;
            int freeCount = _data[bamEntry];
            if (freeCount <= 0)
                continue;

            int ns = SectorsPerTrack[t - 1];
            int b1 = _data[bamEntry + 1] & 0xFF;
            int b2 = _data[bamEntry + 2] & 0xFF;
            int b3 = _data[bamEntry + 3] & 0xFF;
            int bitmap = b1 | (b2 << 8) | (b3 << 16);

            for (int s = 0; s < ns; s++)
            {
                if ((bitmap & (1 << s)) != 0)
                {
                    bitmap &= ~(1 << s);
                    _data[bamEntry + 1] = (byte)(bitmap & 0xFF);
                    _data[bamEntry + 2] = (byte)((bitmap >> 8) & 0xFF);
                    _data[bamEntry + 3] = (byte)((bitmap >> 16) & 0xFF);
                    _data[bamEntry] = (byte)(freeCount - 1);
                    track = t;
                    sector = s;
                    return true;
                }
            }
        }
        track = 0;
        sector = 0;
        return false;
    }

    public void AddDirectoryEntry(DirEntry entry)
    {
        int dirTrack = _data[TrackSectorToOffset(18, 0)];
        int dirSector = _data[TrackSectorToOffset(18, 0) + 1];

        int track = dirTrack;
        int sector = dirSector;
        var visited = new HashSet<(int, int)>();

        while (track > 0 && visited.Add((track, sector)))
        {
            int off = TrackSectorToOffset(track, sector);
            int nextTrack = _data[off];
            int nextSector = _data[off + 1];

            for (int i = 0; i < 8; i++)
            {
                int entryOff = off + i * 32;
                if (_data[entryOff + 2] == 0)
                {
                    FillDirEntry(entryOff, entry);
                    return;
                }
            }

            track = nextTrack;
            sector = nextSector;
        }

        int newOff = TrackSectorToOffset(dirTrack, sector);
        if (!TryAllocateSector(out int newTrack, out int newSector))
            return;

        _data[newOff] = (byte)newTrack;
        _data[newOff + 1] = (byte)newSector;
        int newSecOff = TrackSectorToOffset(newTrack, newSector);
        _data[newSecOff] = 0;
        _data[newSecOff + 1] = 0;
        FillDirEntry(newSecOff + 2, entry);
    }

    private void FillDirEntry(int off, DirEntry entry)
    {
        _data[off + 2] = (byte)((byte)entry.Type | 0x80);
        _data[off + 3] = entry.StartTrack;
        _data[off + 4] = entry.StartSector;
        for (int i = 0; i < 16; i++)
            _data[off + 5 + i] = i < entry.FilenameBytes.Length ? entry.FilenameBytes[i] : (byte)0xA0;
        _data[off + 30] = (byte)(entry.SizeInSectors & 0xFF);
        _data[off + 31] = (byte)((entry.SizeInSectors >> 8) & 0xFF);
    }

    public byte[] SaveToBytes() => (byte[])_data.Clone();
}
