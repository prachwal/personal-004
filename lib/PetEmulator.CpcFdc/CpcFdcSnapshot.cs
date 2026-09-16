namespace PetEmulator.CpcFdc;

/// <summary>Versioned, owned state for an I8272 controller snapshot.</summary>
public sealed class I8272Snapshot
{
    public const byte CurrentVersion = 2;

    public int Version { get; init; } = CurrentVersion;
    public I8272Chip.Phase Phase { get; init; }
    public byte[] CommandBuffer { get; init; } = [];
    public byte[] ResultBuffer { get; init; } = [];
    public int BufferIndex { get; init; }
    public int ResultCount { get; init; }
    public byte Data { get; init; }
    public ushort Cylinder { get; init; }
    public int TransferBytesRemaining { get; init; }
    public byte StepRateHeadUnload { get; init; }
    public byte HeadLoadNonDma { get; init; }
    public int Tracks { get; init; }
    public int Heads { get; init; }
    public int SectorsPerTrack { get; init; }
    public int SectorSize { get; init; }
    public byte[][][]? DiskData { get; init; }
    public bool InterruptPending { get; init; }
    public byte[] TransferBuffer { get; init; } = [];
    public bool UsingDrive { get; init; }
    public bool TransferError { get; init; }
    public int FormatIdIndex { get; init; }
    public byte InterruptStatus { get; init; }
    public int ExecutionCyclesUntilOverrun { get; init; }
    public DskFloppyDriveSnapshot? Drive0 { get; init; }
    public DskFloppyDriveSnapshot? Drive1 { get; init; }
}
public sealed class DskFloppyDriveSnapshot
{
    public int Version { get; init; } = 1;
    public DskDiskImageSnapshot Image { get; init; } = new();
    public bool MotorOn { get; init; }
    public bool WriteProtected { get; init; }
    public int Cylinder { get; init; }
    public int Head { get; init; }
}

public sealed class DskDiskImageSnapshot
{
    public int Version { get; init; } = 1;
    public int TrackCount { get; init; }
    public int HeadCount { get; init; }
    public bool Extended { get; init; }
    public DskTrackSnapshot[] Tracks { get; init; } = [];
}

public sealed class DskTrackSnapshot
{
    public int Version { get; init; } = 1;
    public int Cylinder { get; init; }
    public int Head { get; init; }
    public DskSectorSnapshot[] Sectors { get; init; } = [];
}

public sealed class DskSectorSnapshot
{
    public byte Id { get; init; }
    public int Version { get; init; } = 1;
    public byte SizeCode { get; init; }
    public byte Status1 { get; init; }
    public byte Status2 { get; init; }
    public byte[] Data { get; init; } = [];
}
