using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.Trs80;

/// <summary>Raw JV1 TRS-80 disk image: no container header, a linear track-major dump of
/// single-density 256-byte sectors (10 sectors/track). Track count is inferred from file size.</summary>
public sealed class Jv1DiskImage
{
    public const int SectorSizeBytes = 256;
    public const int SectorsPerTrack = 10;

    private readonly byte[] _data;

    public int Tracks { get; }

    private Jv1DiskImage(byte[] data, int tracks)
    {
        _data = data;
        Tracks = tracks;
    }

    public static Jv1DiskImage Load(string path, ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        log.LogInformation("Loading JV1 '{Path}'.", path);
        try
        {
            var image = Load(File.ReadAllBytes(path));
            log.LogInformation("JV1 loaded '{Path}' ({Tracks} tracks).", path, image.Tracks);
            return image;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Loading JV1 '{Path}' failed.", path);
            throw;
        }
    }

    public static Jv1DiskImage Load(byte[] data)
    {
        const int trackSize = SectorsPerTrack * SectorSizeBytes;
        if (data.Length == 0 || data.Length % trackSize != 0)
            throw new InvalidDataException(
                $"JV1 image size ({data.Length} bytes) is not a multiple of {trackSize} " +
                $"(a {SectorsPerTrack}-sector, {SectorSizeBytes}-byte/sector track).");
        return new Jv1DiskImage(data, data.Length / trackSize);
    }

    public byte[] ReadSector(int track, int sector)
    {
        var result = new byte[SectorSizeBytes];
        Array.Copy(_data, Offset(track, sector), result, 0, SectorSizeBytes);
        return result;
    }

    public void WriteSector(int track, int sector, ReadOnlySpan<byte> data)
    {
        if (data.Length != SectorSizeBytes)
            throw new ArgumentException($"Sector data must be {SectorSizeBytes} bytes.", nameof(data));
        data.CopyTo(_data.AsSpan(Offset(track, sector)));
    }

    public byte[] ToArray() => (byte[])_data.Clone();

    private int Offset(int track, int sector)
    {
        if (track < 0 || track >= Tracks || sector < 0 || sector >= SectorsPerTrack)
            throw new ArgumentOutOfRangeException(nameof(track), $"Invalid JV1 sector {track}/{sector}.");
        return (track * SectorsPerTrack + sector) * SectorSizeBytes;
    }
}
