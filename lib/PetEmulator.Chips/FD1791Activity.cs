namespace PetEmulator.Chips;

/// <summary>Externally visible controller activity, separate from diagnostic tracing.</summary>
public enum FD1791ActivityKind
{
    Reset,
    CommandStarted,
    DataRequested,
    DataTransferred,
    CommandCompleted,
    Error
}

/// <summary>Stable activity snapshot suitable for board glue, LEDs and front ends.</summary>
public sealed class FD1791ActivityEventArgs : EventArgs
{
    public FD1791ActivityEventArgs(
        FD1791ActivityKind kind,
        long tStates,
        byte command,
        byte status,
        byte track,
        byte sector,
        int transferIndex,
        bool busy,
        bool drqAsserted,
        bool intrqAsserted)
    {
        Kind = kind;
        TStates = tStates;
        Command = command;
        Status = status;
        Track = track;
        Sector = sector;
        TransferIndex = transferIndex;
        Busy = busy;
        DrqAsserted = drqAsserted;
        IntrqAsserted = intrqAsserted;
    }

    public FD1791ActivityKind Kind { get; }
    public long TStates { get; }
    public byte Command { get; }
    public byte Status { get; }
    public byte Track { get; }
    public byte Sector { get; }
    public int TransferIndex { get; }
    public bool Busy { get; }
    public bool DrqAsserted { get; }
    public bool IntrqAsserted { get; }
}
