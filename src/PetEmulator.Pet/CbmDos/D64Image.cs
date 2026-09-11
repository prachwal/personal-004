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
    /// docs/pet-disk-testing-strategy.md).</summary>
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
