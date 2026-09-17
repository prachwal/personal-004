using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly int _firstSectorId;

    public KayproDiskImage(byte[] data, bool writeProtected = false, int firstSectorId = 1)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != ImageSize)
            throw new ArgumentException($"Kaypro II image must be exactly {ImageSize} bytes.", nameof(data));

        _data = data;
        if (firstSectorId is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(firstSectorId));
        _firstSectorId = firstSectorId;
        WriteProtected = writeProtected;
    }

    public bool WriteProtected { get; }
    public int SectorSize => SectorSizeBytes;
    public bool IsDoubleDensity => true;
    public int FirstSectorId => _firstSectorId;
    public int SectorsOnTrack(int track) => track is >= 0 and < Tracks ? SectorsPerTrack : 0;

    public static KayproDiskImage Load(string path, bool writeProtected = false, ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        log.LogInformation("Loading disk image '{Path}'.", path);
        try
        {
            var image = new KayproDiskImage(File.ReadAllBytes(path), writeProtected);
            log.LogInformation("Disk image loaded '{Path}' ({Size} B).", path, ImageSize);
            return image;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Loading disk image '{Path}' failed.", path);
            throw;
        }
    }

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

    private bool TryGetOffset(int track, int sector, int length, out int offset)
    {
        offset = 0;
        if (track is < 0 or >= Tracks || sector < _firstSectorId || sector >= _firstSectorId + SectorsPerTrack || length != SectorSizeBytes)
            return false;

        offset = (track * SectorsPerTrack + sector - _firstSectorId) * SectorSizeBytes;
        return true;
    }
}
