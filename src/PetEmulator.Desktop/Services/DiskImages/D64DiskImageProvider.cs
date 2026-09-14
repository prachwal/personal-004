using PetEmulator.Pet.CbmDos;

namespace PetEmulator.Desktop.Services.DiskImages;

public sealed class D64DiskImageProvider : IDiskImageProvider
{
    public bool CanOpen(string path) => D64Image.LooksLikeD64(File.ReadAllBytes(path));

    public DiskImageDocument Open(string path)
    {
        var disk = D64Image.Load(path);
        var files = disk.ReadDirectory()
            .Select(entry => new DiskImageFile(
                entry.Filename,
                entry.Type.ToString(),
                entry.SizeInSectors,
                () => disk.ReadFile(entry),
                () => disk.ReadFileSectors(entry)
                    .Select(sector => (sector.Track, sector.Sector)).ToHashSet(),
                $"{entry.StartTrack}/{entry.StartSector}"))
            .ToArray();
        var sectors = disk.ReadSectorMap()
            .Select(sector => new DiskImageSector(sector.Track, sector.Sector, sector.IsAllocated))
            .ToArray();
        var operations = new D64DiskImageOperations(disk);

        return new DiskImageDocument(
            $"Disk: {disk.DiskName.Trim()} / {disk.DiskId.Trim()}",
            $"{Path.GetFileName(path)}  {disk.DiskName.Trim()} / {disk.DiskId.Trim()}  {files.Length} file(s)",
            files,
            sectors,
            operations)
        {
            DirectoryLimit = 144
        };
    }

    private sealed class D64DiskImageOperations(D64Image disk) : IDiskImageOperations
    {
        public bool IsReadOnly => false;
        public byte[] ReadSector(int track, int sector) => disk.ReadSector(track, sector);
        public void WriteSector(int track, int sector, ReadOnlySpan<byte> data) => disk.WriteSector(track, sector, data.ToArray());
        public void CreateFile(string name, ReadOnlySpan<byte> content, string type = "SEQ") =>
            disk.CreateFile(name, content, Enum.TryParse<FileType>(type, true, out var value) ? value : FileType.Seq);
        public void UpdateFile(string name, ReadOnlySpan<byte> content) => disk.UpdateFile(name, content);
        public void DeleteFile(string name) => disk.DeleteFile(name);
        public void RenameFile(string oldName, string newName) => disk.RenameFile(oldName, newName);
        public byte[] SaveToBytes() => disk.SaveToBytes();
        public void SaveTo(string path) => File.WriteAllBytes(path, disk.SaveToBytes());
        public IReadOnlyList<DiskImageFile> ReadFiles() => disk.ReadDirectory()
            .Select(entry => new DiskImageFile(entry.Filename, entry.Type.ToString(), entry.SizeInSectors,
                () => disk.ReadFile(entry),
                () => disk.ReadFileSectors(entry).Select(sector => (sector.Track, sector.Sector)).ToHashSet(),
                $"{entry.StartTrack}/{entry.StartSector}")).ToArray();
        public IReadOnlyList<DiskImageSector> ReadSectors() => disk.ReadSectorMap()
            .Select(sector => new DiskImageSector(sector.Track, sector.Sector, sector.IsAllocated)).ToArray();
    }
}
