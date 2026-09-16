namespace PetEmulator.CpcFdc;

/// <summary>Signals exposed by a floppy drive to an 8272-compatible controller.</summary>
public interface IFloppyDrive
{
    bool Ready { get; }
    bool WriteProtected { get; }
    bool TrackZero { get; }
    bool TwoSided { get; }
    bool HeadLoaded { get; }
    bool Index { get; }
    int Cylinder { get; }
    int Head { get; }

    bool Seek(int cylinder);
    void SelectHead(byte head);
    bool ReadSector(byte cylinder, byte head, byte sector, byte sizeCode, Span<byte> data);
    bool WriteSector(byte cylinder, byte head, byte sector, byte sizeCode, ReadOnlySpan<byte> data);
    bool FormatTrack(byte head, byte sizeCode, byte sectorCount, ReadOnlySpan<byte> ids, byte fill);
}
