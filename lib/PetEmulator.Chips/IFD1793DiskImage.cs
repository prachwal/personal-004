namespace PetEmulator.Chips;

/// <summary>Minimal disk-image contract required by <see cref="FD1793"/>.</summary>
public interface IFD1793DiskImage
{
    bool WriteProtected { get; }

    int SectorSize { get; }

    /// <summary>True when the medium uses MFM/double-density encoding.</summary>
    bool IsDoubleDensity => false;

    /// <summary>First sector identifier reported by READ ADDRESS.</summary>
    int FirstSectorId => 0;

    /// <summary>Number of address marks on a track, or zero when not modeled.</summary>
    int SectorsOnTrack(int track) => 0;

    bool TryReadSector(int track, int sector, Span<byte> destination);

    bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source);

    /// <summary>Whether READ SECTOR should report the deleted-data-mark status bit.</summary>
    bool IsDeletedDataMark(int track, int sector) => false;
}

/// <summary>Optional extension for disk images with independently addressable sides.</summary>
public interface IFD1793SidedDiskImage : IFD1793DiskImage
{
    bool TryReadSector(int track, int side, int sector, Span<byte> destination);

    bool TryWriteSector(int track, int side, int sector, ReadOnlySpan<byte> source);
}
