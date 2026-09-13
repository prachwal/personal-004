namespace PetEmulator.Chips;

public enum FD1791DiagnosticKind
{
    Reset,
    DiskInserted,
    RegisterRead,
    RegisterWrite,
    CommandAccepted,
    DataRequested,
    DataRequestCleared,
    DataRead,
    DataWritten,
    CommandCompleted,
    LostData
}

/// <summary>Immutable snapshot emitted at externally observable FD1791 transfer points.</summary>
public readonly record struct FD1791DiagnosticEvent(
    FD1791DiagnosticKind Kind,
    long TStates,
    byte Register,
    byte Value,
    byte Command,
    byte Status,
    byte Track,
    byte Sector,
    byte Data,
    int TransferIndex,
    int DataDeadlineTStates,
    bool DrqAsserted,
    bool IntrqAsserted);
