namespace PetEmulator.Chips;

public sealed record Z80PioPortState(
    Z80PioMode Mode,
    byte DirectionMask,
    byte OutputLatch,
    byte InputLatch,
    bool InputAvailable,
    bool InputOverrun,
    bool OutputPending,
    bool BidirectionalInput,
    byte InterruptMask,
    byte InterruptControl,
    bool InterruptEnabled,
    bool InterruptPending,
    bool StrobeAsserted,
    bool Ready,
    Z80PioControlPhase ControlPhase,
    byte InterruptVector);

public sealed record Z80PioState(
    Z80PioPortState PortA,
    Z80PioPortState PortB,
    byte InterruptVector,
    bool InterruptInService,
    bool InterruptInputEnabled);
