using PetEmulator.Kaypro;

namespace PetEmulator.Desktop.Services.DiskImages;

public sealed class KayproDiskImageProvider : IDiskImageProvider
{
    /// <summary>Content-based sniff: TeleDisk images start with the "TD"/"td" magic; a raw Kaypro
    /// II image has no magic byte, so an exact match of the fixed image size is the only signal
    /// available (the same limitation JV1 has - see Trs80DiskImageProvider).</summary>
    public bool CanOpen(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return IsTd0(bytes) || bytes.Length == KayproDiskImage.ImageSize;
    }

    private static bool IsTd0(byte[] bytes) =>
        bytes.Length >= 2 && (bytes[0] == 'T' || bytes[0] == 't') && (bytes[1] == 'D' || bytes[1] == 'd');

    public DiskImageDocument Open(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var image = IsTd0(bytes)
            ? LoadTd0(path)
            : new KayproDiskImage(bytes, firstSectorId: 0);
        var cpm = new KayproCpmFileSystem(image);
        var files = cpm.ReadDirectory()
            .Select(entry => new DiskImageFile(
                entry.Name,
                "CP/M",
                entry.SizeInSectors,
                () => cpm.ReadFile(entry.Name),
                () => cpm.ReadFileSectors(entry.Name)))
            .ToArray();
        var sectors = cpm.ReadSectorMap()
            .Select(sector => new DiskImageSector(sector.Track, sector.Sector, sector.IsAllocated))
            .ToArray();
        var operations = new KayproDiskImageOperations(image, cpm);

        return new DiskImageDocument(
            "Disk: Kaypro II CP/M 2.2",
            $"{Path.GetFileName(path)}  Kaypro II CP/M 2.2  {files.Length} file(s)",
            files,
            sectors,
            operations)
        {
            DirectoryLimit = 64
        };
    }

    private static KayproDiskImage LoadTd0(string path)
    {
        using var stream = File.OpenRead(path);
        return KayproTd0Reader.Read(stream);
    }

    private sealed class KayproDiskImageOperations(KayproDiskImage disk, KayproCpmFileSystem cpm)
        : IDiskImageOperations
    {
        public bool IsReadOnly => disk.WriteProtected;
        public byte[] ReadSector(int track, int sector) => cpm.ReadSector(track, sector);
        public void WriteSector(int track, int sector, ReadOnlySpan<byte> data) => cpm.WriteSector(track, sector, data);
        public void CreateFile(string name, ReadOnlySpan<byte> content, string type = "SEQ") => cpm.CreateFile(name, content);
        public void UpdateFile(string name, ReadOnlySpan<byte> content) => cpm.UpdateFile(name, content);
        public void DeleteFile(string name) => cpm.DeleteFile(name);
        public void RenameFile(string oldName, string newName) => cpm.RenameFile(oldName, newName);
        public byte[] SaveToBytes() => disk.ToArray();
        public void SaveTo(string path) => File.WriteAllBytes(path, disk.ToArray());
        public IReadOnlyList<DiskImageFile> ReadFiles() => cpm.ReadDirectory()
            .Select(entry => new DiskImageFile(entry.Name, "CP/M", entry.SizeInSectors,
                () => cpm.ReadFile(entry.Name), () => cpm.ReadFileSectors(entry.Name))).ToArray();
        public IReadOnlyList<DiskImageSector> ReadSectors() => cpm.ReadSectorMap()
            .Select(sector => new DiskImageSector(sector.Track, sector.Sector, sector.IsAllocated)).ToArray();
    }
}
