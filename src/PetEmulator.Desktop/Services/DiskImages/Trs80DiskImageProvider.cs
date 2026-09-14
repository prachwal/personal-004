using PetEmulator.Trs80;

namespace PetEmulator.Desktop.Services.DiskImages;

/// <summary>Reads TRS-80 disk images. Content-based, not extension-based: DMK has a real header
/// shape to sniff (<see cref="DmkDiskImage.LooksLikeDmk"/>); JV1 has no magic at all (it's a bare
/// sector dump), so its only signal is a size that's a multiple of one single-density track and
/// an actual NEWDOS/80 GAT signature inside - CanOpen fully attempts the parse rather than
/// guessing from a size check alone. Two container formats (DMK, JV1) x two filesystems
/// (LS-DOS 6, NEWDOS/80) are supported in the combination each was verified against: DMK+LS-DOS 6
/// and JV1+NEWDOS/80. A JV1 disk holding an LS-DOS 6 filesystem (or vice versa) is not attempted -
/// add that combination once a real sample exists to verify against.
/// Read-only: NEWDOS/80 and LS-DOS 6 write support (CRUD, allocation) is not implemented.</summary>
public sealed class Trs80DiskImageProvider : IDiskImageProvider
{
    public bool CanOpen(string path)
    {
        try
        {
            Open(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public DiskImageDocument Open(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return TryOpenDmk(bytes, path) ?? TryOpenJv1(bytes, path)
            ?? throw new NotSupportedException($"'{Path.GetFileName(path)}' is not a recognized TRS-80 disk image (DMK or JV1).");
    }

    private static DiskImageDocument? TryOpenDmk(byte[] bytes, string path)
    {
        if (!DmkDiskImage.LooksLikeDmk(bytes, out _))
            return null;
        var disk = DmkDiskImage.Load(bytes);
        var fileSystem = new LsDos6FileSystem(disk);
        var operations = new LsDos6Operations(disk, fileSystem, bytes);
        var files = operations.ReadFiles();
        var sectors = operations.ReadSectors();
        return new DiskImageDocument(
            $"Disk: {fileSystem.DiskName} (LS-DOS 6)",
            $"{Path.GetFileName(path)}  {fileSystem.DiskName}  LS-DOS 6  {files.Count} file(s)",
            files,
            sectors,
            operations);
    }

    private static DiskImageDocument? TryOpenJv1(byte[] bytes, string path)
    {
        Jv1DiskImage disk;
        try
        {
            disk = Jv1DiskImage.Load(bytes);
        }
        catch (InvalidDataException)
        {
            return null;
        }

        NewDos80FileSystem fileSystem;
        try
        {
            fileSystem = new NewDos80FileSystem(disk);
        }
        catch (InvalidDataException)
        {
            return null; // Right container shape, but no NEWDOS/80 GAT signature inside.
        }

        var operations = new NewDos80Operations(disk, fileSystem, bytes);
        var files = operations.ReadFiles();
        var sectors = operations.ReadSectors();
        return new DiskImageDocument(
            $"Disk: {fileSystem.DiskName} (NEWDOS/80)",
            $"{Path.GetFileName(path)}  {fileSystem.DiskName}  NEWDOS/80  {files.Count} file(s)",
            files,
            sectors,
            operations);
    }

    private const string ReadOnlyMessage = "TRS-80 disks are read-only in this build.";

    private sealed class NewDos80Operations(Jv1DiskImage disk, NewDos80FileSystem fileSystem, byte[] rawBytes) : IDiskImageOperations
    {
        public bool IsReadOnly => true;
        public byte[] ReadSector(int track, int sector) => disk.ReadSector(track, sector);
        public void WriteSector(int track, int sector, ReadOnlySpan<byte> data) => throw new NotSupportedException(ReadOnlyMessage);
        public void CreateFile(string name, ReadOnlySpan<byte> content, string type = "SEQ") => throw new NotSupportedException(ReadOnlyMessage);
        public void UpdateFile(string name, ReadOnlySpan<byte> content) => throw new NotSupportedException(ReadOnlyMessage);
        public void DeleteFile(string name) => throw new NotSupportedException(ReadOnlyMessage);
        public void RenameFile(string oldName, string newName) => throw new NotSupportedException(ReadOnlyMessage);
        public byte[] SaveToBytes() => (byte[])rawBytes.Clone();
        public void SaveTo(string path) => File.WriteAllBytes(path, rawBytes);

        public IReadOnlyList<DiskImageFile> ReadFiles() => fileSystem.ReadDirectory()
            .Select(entry => new DiskImageFile(
                entry.FileName,
                entry.Extension.Length == 0 ? "NEWDOS" : entry.Extension,
                entry.SizeInSectors,
                () => fileSystem.ReadFile(entry),
                () => fileSystem.ReadFileSectors(entry),
                $"{entry.Extents[0].Lump}/{entry.Extents[0].Granule}"))
            .ToArray();

        public IReadOnlyList<DiskImageSector> ReadSectors() => fileSystem.ReadSectorMap()
            .Select(sector => new DiskImageSector(sector.Track, sector.Sector, sector.IsAllocated))
            .ToArray();
    }

    private sealed class LsDos6Operations(DmkDiskImage disk, LsDos6FileSystem fileSystem, byte[] rawBytes) : IDiskImageOperations
    {
        public bool IsReadOnly => true;
        public byte[] ReadSector(int track, int sector) => disk.ReadSector(track, sector);
        public void WriteSector(int track, int sector, ReadOnlySpan<byte> data) => throw new NotSupportedException(ReadOnlyMessage);
        public void CreateFile(string name, ReadOnlySpan<byte> content, string type = "SEQ") => throw new NotSupportedException(ReadOnlyMessage);
        public void UpdateFile(string name, ReadOnlySpan<byte> content) => throw new NotSupportedException(ReadOnlyMessage);
        public void DeleteFile(string name) => throw new NotSupportedException(ReadOnlyMessage);
        public void RenameFile(string oldName, string newName) => throw new NotSupportedException(ReadOnlyMessage);
        public byte[] SaveToBytes() => (byte[])rawBytes.Clone();
        public void SaveTo(string path) => File.WriteAllBytes(path, rawBytes);

        public IReadOnlyList<DiskImageFile> ReadFiles() => fileSystem.ReadDirectory()
            .Select(entry => new DiskImageFile(
                entry.FileName,
                entry.Extension.Length == 0 ? "LS-DOS" : entry.Extension,
                entry.SizeInSectors,
                () => fileSystem.ReadFile(entry),
                () => fileSystem.ReadFileSectors(entry),
                $"{entry.Extents[0].Cylinder}/{entry.Extents[0].Granule}"))
            .ToArray();

        public IReadOnlyList<DiskImageSector> ReadSectors() => fileSystem.ReadSectorMap()
            .Select(sector => new DiskImageSector(sector.Track, sector.Sector, sector.IsAllocated))
            .ToArray();
    }
}
