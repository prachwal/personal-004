namespace PetEmulator.Chips;

/// <summary>Read-only diagnostic view of one PIO port.</summary>
public sealed record Z80PioPortDebugInfo(
    int Port,
    Z80PioMode Mode,
    byte DirectionMask,
    byte OutputLatch,
    byte InputLatch,
    bool InputAvailable,
    bool InputOverrun,
    bool OutputPending,
    bool StrobeAsserted,
    bool Ready,
    byte InterruptVector,
    bool InterruptPending,
    Z80PioControlPhase ControlPhase);

/// <summary>Immutable diagnostic view suitable for debugger and monitoring code.</summary>
public sealed record Z80PioDebugSnapshot(
    Z80PioPortDebugInfo PortA,
    Z80PioPortDebugInfo PortB,
    bool InterruptInService,
    bool InterruptInputEnabled);
