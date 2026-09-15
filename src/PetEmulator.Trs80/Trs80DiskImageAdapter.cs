using PetEmulator.Chips;

namespace PetEmulator.Trs80;

public sealed class Trs80DiskImageAdapter : IFD1791DiskImage
{
    private readonly Jv1DiskImage _image;
    public Trs80DiskImageAdapter(Jv1DiskImage image) => _image = image ?? throw new ArgumentNullException(nameof(image));
    public bool WriteProtected { get; init; }
    public int SectorSize => Jv1DiskImage.SectorSizeBytes;
    public int SectorsOnTrack(int track) => track is >= 0 && track < _image.Tracks ? Jv1DiskImage.SectorsPerTrack : 0;
    public bool TryReadSector(int track, int sector, Span<byte> destination)
    {
        if (destination.Length < SectorSize || track < 0 || track >= _image.Tracks || sector < 0 || sector >= Jv1DiskImage.SectorsPerTrack) return false;
        _image.ReadSector(track, sector).CopyTo(destination);
        return true;
    }
    public bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source)
    {
        if (WriteProtected || source.Length != SectorSize || track < 0 || track >= _image.Tracks || sector < 0 || sector >= Jv1DiskImage.SectorsPerTrack) return false;
        _image.WriteSector(track, sector, source);
        return true;
    }
}

public sealed class Trs80DmkDiskImageAdapter : IFD1791SidedDiskImage
{
    private readonly DmkDiskImage _image;
    public Trs80DmkDiskImageAdapter(DmkDiskImage image) => _image = image ?? throw new ArgumentNullException(nameof(image));
    public bool WriteProtected => true;
    public int SectorSize => 256;
    public bool IsDoubleDensity => true;
    public int SectorsOnTrack(int track) => track is >= 0 and < 96 ? _image.SectorsPerSide : 0;
    public bool TryReadSector(int track, int sector, Span<byte> destination) => TryReadSector(track, 0, sector, destination);
    public bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source) => false;
    public bool TryReadSector(int track, int side, int sector, Span<byte> destination)
    {
        if (track < 0 || track >= _image.Tracks || side < 0 || side >= _image.Sides || sector < 0) return false;
        byte[] data;
        try { data = _image.ReadSector(track, side, sector); }
        catch (ArgumentOutOfRangeException) { return false; }
        // The real sector length comes from the DMK image's own IDAM length code (128/256/512/1024
        // - see DmkDiskImage.TryReadSector) and isn't guaranteed to match the fixed SectorSize=256
        // this adapter advertises. Too large to fit the caller's buffer is a real failure; too small
        // is zero-padded rather than left as whatever FD1791's transfer buffer happened to contain.
        if (data.Length > destination.Length) return false;
        data.CopyTo(destination);
        if (data.Length < destination.Length) destination[data.Length..].Clear();
        return true;
    }
    public bool TryWriteSector(int track, int side, int sector, ReadOnlySpan<byte> source) => false;
}
