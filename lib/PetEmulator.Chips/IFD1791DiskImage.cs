namespace PetEmulator.Chips;

/// <summary>Common logical disk-image contract for FD1791-family controllers.</summary>
public interface IFD1791DiskImage
{
    bool WriteProtected { get; }

    int SectorSize { get; }

    bool IsDoubleDensity => false;

    int FirstSectorId => 0;

    int SectorsOnTrack(int track) => 0;

    bool TryReadSector(int track, int sector, Span<byte> destination);

    bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source);

    bool IsDeletedDataMark(int track, int sector) => false;
}

/// <summary>Optional side-aware extension of the common disk-image contract.</summary>
public interface IFD1791SidedDiskImage : IFD1791DiskImage
{
    bool TryReadSector(int track, int side, int sector, Span<byte> destination);

    bool TryWriteSector(int track, int side, int sector, ReadOnlySpan<byte> source);
}
