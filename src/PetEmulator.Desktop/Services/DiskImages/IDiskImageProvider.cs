namespace PetEmulator.Desktop.Services.DiskImages;

public interface IDiskImageProvider
{
    bool CanOpen(string path);

    DiskImageDocument Open(string path);
}

public interface IDiskImageProviderRegistry
{
    DiskImageDocument Open(string path);
}

public sealed record DiskImageDocument(
    string Title,
    string Status,
    IReadOnlyList<DiskImageFile> Files,
    IReadOnlyList<DiskImageSector> Sectors,
    IDiskImageOperations Operations)
{
    public int? DirectoryLimit { get; init; }
}

public interface IDiskImageOperations
{
    bool IsReadOnly { get; }

    byte[] ReadSector(int track, int sector);

    void WriteSector(int track, int sector, ReadOnlySpan<byte> data);

    void CreateFile(string name, ReadOnlySpan<byte> content, string type = "SEQ");

    void UpdateFile(string name, ReadOnlySpan<byte> content);

    void DeleteFile(string name);

    void RenameFile(string oldName, string newName);

    byte[] SaveToBytes();

    void SaveTo(string path);

    IReadOnlyList<DiskImageFile> ReadFiles();

    IReadOnlyList<DiskImageSector> ReadSectors();
}

public sealed record DiskImageFile(
    string Name,
    string Type,
    int SizeInSectors,
    Func<byte[]> ReadContent,
    Func<IReadOnlySet<(int Track, int Sector)>> ReadSectors,
    string? StartLocation = null);

public sealed record DiskImageSector(int Track, int Sector, bool IsAllocated);
