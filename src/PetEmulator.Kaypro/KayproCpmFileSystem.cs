namespace PetEmulator.Kaypro;

/// <summary>Read-only CP/M 2.2 directory and allocation view for Kaypro II images.</summary>
public sealed class KayproCpmFileSystem
{
    // KayproDiskImage exposes zero-based physical tracks. Track 0 is the
    // system track; CP/M directory starts on track 1.
    private const int ReservedTracks = 1;
    private const int DirectorySectors = 4;
    private const int DirectoryEntrySize = 32;
    private const int RecordsPerSector = KayproDiskImage.SectorSizeBytes / DirectoryEntrySize;
    private const int RecordSize = 128;
    private const int BlockSize = 1024;

    private readonly KayproDiskImage _disk;

    public KayproCpmFileSystem(KayproDiskImage disk)
    {
        _disk = disk ?? throw new ArgumentNullException(nameof(disk));
        if (disk.FirstSectorId != 0)
            throw new ArgumentException("CP/M Kaypro images must use zero-based sector IDs.", nameof(disk));
    }

    public IReadOnlyList<KayproCpmFileEntry> ReadDirectory()
    {
        var sizes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in ReadRawEntries())
        {
            if (entry[0] == 0xE5)
                continue;
            var name = FormatName(entry);
            sizes[name] = sizes.GetValueOrDefault(name) + entry[15] * RecordSize;
        }

        return sizes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new KayproCpmFileEntry(pair.Key, pair.Value,
                (pair.Value + 511) / 512))
            .ToArray();
    }

    public byte[] ReadFile(string name)
    {
        var target = name.Trim().ToUpperInvariant();
        var extents = ReadRawEntries()
            .Where(entry => entry[0] != 0xE5 && FormatName(entry) == target)
            .OrderBy(entry => entry[12] | (entry[14] << 8))
            .ToArray();
        if (extents.Length == 0)
            throw new FileNotFoundException($"CP/M file not found: {name}");

        using var output = new MemoryStream();
        foreach (var entry in extents)
        {
            var records = entry[15];
            for (var pointer = 0; pointer < 16 && records > 0; pointer++)
            {
                var block = entry[16 + pointer];
                if (block == 0)
                    break;
                var blockBytes = ReadBlock(block);
                var bytes = Math.Min(BlockSize, records * RecordSize);
                output.Write(blockBytes, 0, bytes);
                records -= (byte)((bytes + RecordSize - 1) / RecordSize);
            }
        }

        return output.ToArray();
    }

    public byte[] ReadSector(int track, int sector)
    {
        var data = new byte[KayproDiskImage.SectorSizeBytes];
        if (!_disk.TryReadSector(track, sector, data))
            throw new ArgumentOutOfRangeException($"Invalid Kaypro sector {track}/{sector}.");
        return data;
    }

    public void WriteSector(int track, int sector, ReadOnlySpan<byte> data)
    {
        if (!_disk.TryWriteSector(track, sector, data))
            throw new IOException($"Unable to write Kaypro sector {track}/{sector}.");
    }

    public void CreateFile(string name, ReadOnlySpan<byte> content)
    {
        ValidateName(name);
        if (ReadDirectory().Any(entry => entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new IOException($"CP/M file already exists: {name}");
        if (content.Length % RecordSize != 0)
            throw new ArgumentException("CP/M content must be a multiple of 128 bytes.", nameof(content));

        var recordCount = content.Length / RecordSize;
        var blockCount = (content.Length + BlockSize - 1) / BlockSize;
        var blocks = FindFreeBlocks(blockCount);
        for (var index = 0; index < blocks.Count; index++)
            WriteBlock(blocks[index], content.Slice(index * BlockSize, Math.Min(BlockSize, content.Length - index * BlockSize)));

        var extentCount = Math.Max(1, (recordCount + 127) / 128);
        var slots = FindFreeDirectorySlots(extentCount);
        var encodedName = EncodeName(name);
        for (var extent = 0; extent < extentCount; extent++)
        {
            var records = Math.Min(128, Math.Max(0, recordCount - extent * 128));
            var entry = new byte[DirectoryEntrySize];
            entry[0] = 0;
            encodedName.CopyTo(entry, 1);
            entry[12] = (byte)(extent & 0x1F);
            entry[14] = (byte)(extent >> 5);
            entry[15] = (byte)(records == 0 ? 0 : records == 128 ? 128 : records);
            for (var pointer = 0; pointer < 16; pointer++)
            {
                var blockIndex = extent * 16 + pointer;
                if (blockIndex >= blocks.Count || pointer * 8 >= records)
                    break;
                entry[16 + pointer] = (byte)blocks[blockIndex];
            }
            WriteDirectoryEntry(slots[extent], entry);
        }
    }

    public void UpdateFile(string name, ReadOnlySpan<byte> content)
    {
        DeleteFile(name);
        try
        {
            CreateFile(name, content);
        }
        catch
        {
            throw new IOException($"Unable to update CP/M file: {name}");
        }
    }

    public void DeleteFile(string name)
    {
        ValidateName(name);
        var locations = FindFileLocations(name);
        if (locations.Count == 0)
            throw new FileNotFoundException($"CP/M file not found: {name}");
        foreach (var location in locations)
        {
            var sector = ReadSector(location.Track, location.Sector);
            sector[location.Offset] = 0xE5;
            WriteSector(location.Track, location.Sector, sector);
        }
    }

    public void RenameFile(string oldName, string newName)
    {
        ValidateName(newName);
        if (ReadDirectory().Any(entry => entry.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            throw new IOException($"CP/M file already exists: {newName}");
        var locations = FindFileLocations(oldName);
        if (locations.Count == 0)
            throw new FileNotFoundException($"CP/M file not found: {oldName}");
        var encoded = EncodeName(newName);
        foreach (var location in locations)
        {
            var sector = ReadSector(location.Track, location.Sector);
            encoded.CopyTo(sector, location.Offset + 1);
            WriteSector(location.Track, location.Sector, sector);
        }
    }

    public IReadOnlySet<(int Track, int Sector)> ReadFileSectors(string name)
    {
        var target = name.Trim().ToUpperInvariant();
        var sectors = new HashSet<(int Track, int Sector)>();
        foreach (var entry in ReadRawEntries().Where(entry => entry[0] != 0xE5 && FormatName(entry) == target))
        {
            var records = entry[15];
            for (var pointer = 0; pointer < 16 && records > 0; pointer++)
            {
                var block = entry[16 + pointer];
                if (block == 0)
                    break;
                AddBlockSectors(sectors, block);
                records -= (byte)Math.Min(8, (int)records);
            }
        }
        return sectors;
    }

    public IReadOnlyList<(int Track, int Sector, bool IsAllocated)> ReadSectorMap()
    {
        var allocated = new HashSet<(int Track, int Sector)>();
        for (var track = 0; track < ReservedTracks; track++)
            for (var sector = 0; sector < KayproDiskImage.SectorsPerTrack; sector++)
                allocated.Add((track, sector));
        for (var sector = 0; sector < DirectorySectors; sector++)
            allocated.Add((ReservedTracks, sector));

        foreach (var entry in ReadRawEntries().Where(entry => entry[0] != 0xE5))
            for (var pointer = 0; pointer < 16 && entry[16 + pointer] != 0; pointer++)
                AddBlockSectors(allocated, entry[16 + pointer]);

        return Enumerable.Range(0, KayproDiskImage.Tracks)
            .SelectMany(track => Enumerable.Range(0, KayproDiskImage.SectorsPerTrack)
                .Select(sector => (track, sector, allocated.Contains((track, sector)))))
            .ToArray();
    }

    private IEnumerable<byte[]> ReadRawEntries()
    {
        for (var sector = 0; sector < DirectorySectors; sector++)
        {
            var bytes = new byte[KayproDiskImage.SectorSizeBytes];
            if (!_disk.TryReadSector(ReservedTracks, sector, bytes))
                yield break;
            for (var offset = 0; offset < bytes.Length; offset += DirectoryEntrySize)
                yield return bytes.AsSpan(offset, DirectoryEntrySize).ToArray();
        }
    }

    private byte[] ReadBlock(int block)
    {
        var bytes = new byte[BlockSize];
        var firstSector = block * 2;
        for (var sector = 0; sector < 2; sector++)
        {
            var absolute = firstSector + sector;
            var track = ReservedTracks + absolute / KayproDiskImage.SectorsPerTrack;
            var sectorOnTrack = absolute % KayproDiskImage.SectorsPerTrack;
            if (!_disk.TryReadSector(track, sectorOnTrack, bytes.AsSpan(sector * 512, 512)))
                throw new InvalidDataException($"CP/M block {block} points outside the Kaypro image.");
        }
        return bytes;
    }

    private void WriteBlock(int block, ReadOnlySpan<byte> content)
    {
        var data = new byte[BlockSize];
        content.CopyTo(data);
        var firstSector = block * 2;
        for (var sector = 0; sector < 2; sector++)
        {
            var absolute = firstSector + sector;
            var track = ReservedTracks + absolute / KayproDiskImage.SectorsPerTrack;
            var sectorOnTrack = absolute % KayproDiskImage.SectorsPerTrack;
            WriteSector(track, sectorOnTrack, data.AsSpan(sector * 512, 512));
        }
    }

    private List<int> FindFreeBlocks(int count)
    {
        var used = ReadRawEntries().Where(entry => entry[0] != 0xE5)
            .SelectMany(entry => Enumerable.Range(0, 16).Select(index => (int)entry[16 + index]))
            .Where(block => block != 0).ToHashSet();
        var free = Enumerable.Range(2, KayproDiskImage.ImageSize / BlockSize - 2)
            .Where(block => !used.Contains(block)).Take(count).ToList();
        if (free.Count != count)
            throw new IOException("CP/M disk is full.");
        return free;
    }

    private List<(int Track, int Sector, int Offset)> FindFreeDirectorySlots(int count)
    {
        var result = new List<(int, int, int)>();
        for (var sector = 0; sector < DirectorySectors && result.Count < count; sector++)
        {
            var data = ReadSector(ReservedTracks, sector);
            for (var offset = 0; offset < data.Length && result.Count < count; offset += DirectoryEntrySize)
                if (data[offset] == 0xE5)
                    result.Add((ReservedTracks, sector, offset));
        }
        if (result.Count != count)
            throw new IOException("CP/M directory is full.");
        return result;
    }

    private List<(int Track, int Sector, int Offset)> FindFileLocations(string name)
    {
        var target = name.Trim().ToUpperInvariant();
        var result = new List<(int, int, int)>();
        for (var sector = 0; sector < DirectorySectors; sector++)
        {
            var data = ReadSector(ReservedTracks, sector);
            for (var offset = 0; offset < data.Length; offset += DirectoryEntrySize)
            {
                var entry = data.AsSpan(offset, DirectoryEntrySize).ToArray();
                if (entry[0] != 0xE5 && FormatName(entry) == target)
                    result.Add((ReservedTracks, sector, offset));
            }
        }
        return result;
    }

    private void WriteDirectoryEntry((int Track, int Sector, int Offset) location, byte[] entry)
    {
        var sector = ReadSector(location.Track, location.Sector);
        entry.CopyTo(sector, location.Offset);
        WriteSector(location.Track, location.Sector, sector);
    }

    private static byte[] EncodeName(string name)
    {
        var parts = name.Trim().ToUpperInvariant().Split('.', 2);
        if (parts[0].Length is < 1 or > 8 || parts.Length == 2 && parts[1].Length > 3)
            throw new ArgumentException("CP/M names must use 1-8 character names and a 0-3 character extension.", nameof(name));
        var result = Enumerable.Repeat((byte)' ', 11).ToArray();
        System.Text.Encoding.ASCII.GetBytes(parts[0]).CopyTo(result, 0);
        if (parts.Length == 2)
            System.Text.Encoding.ASCII.GetBytes(parts[1]).CopyTo(result, 8);
        return result;
    }

    private static void ValidateName(string name) => _ = EncodeName(name);

    private void AddBlockSectors(HashSet<(int Track, int Sector)> sectors, int block)
    {
        var firstSector = block * 2;
        for (var offset = 0; offset < 2; offset++)
        {
            var absolute = firstSector + offset;
            sectors.Add((ReservedTracks + absolute / KayproDiskImage.SectorsPerTrack,
                absolute % KayproDiskImage.SectorsPerTrack));
        }
    }

    private static string FormatName(byte[] entry)
    {
        var name = new string(entry.Skip(1).Take(8).Select(value => (char)(value & 0x7F)).ToArray()).Trim();
        var extension = new string(entry.Skip(9).Take(3).Select(value => (char)(value & 0x7F)).ToArray()).Trim();
        return extension.Length == 0 ? name : $"{name}.{extension}";
    }

}

public sealed record KayproCpmFileEntry(string Name, int SizeBytes, int SizeInSectors);
