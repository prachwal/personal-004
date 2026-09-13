using PetEmulator.Chips;

namespace PetEmulator.Kaypro;

/// <summary>Raw single-sided Kaypro II disk image compatible with FD1793.</summary>
public sealed class KayproDiskImage : IFD1793DiskImage
{
    public const int Tracks = 40;
    public const int SectorsPerTrack = 10;
    public const int SectorSizeBytes = 512;
    public const int ImageSize = Tracks * SectorsPerTrack * SectorSizeBytes;

    private readonly byte[] _data;

    public KayproDiskImage(byte[] data, bool writeProtected = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != ImageSize)
            throw new ArgumentException($"Kaypro II image must be exactly {ImageSize} bytes.", nameof(data));

        _data = data;
        WriteProtected = writeProtected;
    }

    public bool WriteProtected { get; }
    public int SectorSize => SectorSizeBytes;
    public bool IsDoubleDensity => true;
    public int FirstSectorId => 1;
    public int SectorsOnTrack(int track) => track is >= 0 and < Tracks ? SectorsPerTrack : 0;

    public static KayproDiskImage Load(string path, bool writeProtected = false)
        => new(File.ReadAllBytes(path), writeProtected);

    public byte[] ToArray() => (byte[])_data.Clone();

    public bool TryReadSector(int track, int sector, Span<byte> destination)
    {
        if (!TryGetOffset(track, sector, destination.Length, out var offset))
            return false;

        _data.AsSpan(offset, SectorSizeBytes).CopyTo(destination);
        return true;
    }

    public bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source)
    {
        if (WriteProtected || !TryGetOffset(track, sector, source.Length, out var offset))
            return false;

        source.CopyTo(_data.AsSpan(offset, SectorSizeBytes));
        return true;
    }

    private static bool TryGetOffset(int track, int sector, int length, out int offset)
    {
        offset = 0;
        if (track is < 0 or >= Tracks || sector is < 1 or > SectorsPerTrack || length != SectorSizeBytes)
            return false;

        offset = (track * SectorsPerTrack + sector - 1) * SectorSizeBytes;
        return true;
    }
}
