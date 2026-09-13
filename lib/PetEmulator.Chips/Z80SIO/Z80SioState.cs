namespace PetEmulator.Chips;

/// <summary>
/// Serializable value model of the state required to resume a Z80 SIO.
/// Collections are cloned by <see cref="Z80Sio.CaptureState"/> and
/// <see cref="Z80Sio.RestoreState"/>.
/// </summary>
public sealed record Z80SioState(
    long CurrentTState,
    int BaudRate,
    byte InterruptVector,
    bool InterruptInService,
    bool InterruptInputEnabled,
    IReadOnlyList<Z80SioChannelState> Channels);

public sealed record Z80SioChannelState(
    byte[] WriteRegisters,
    int RegisterPointer,
    bool Overrun,
    bool RxInterruptArmed,
    bool TxInterruptPending,
    bool ExtStatusInterruptPending,
    byte? TransmitByte,
    long TransmitReadyAtTState,
    IReadOnlyList<byte> TransmitBuffer,
    long ReadyAtTState,
    bool CtsAsserted,
    bool DcdAsserted,
    bool BreakDetected,
    Z80SioFrameMode Mode,
    ushort SyncWord,
    bool SyncAcquired,
    byte? SyncPendingByte,
    bool AutoEchoEnabled,
    bool LocalLoopbackEnabled,
    IReadOnlyList<Z80SioReceivedByte> ReceiveBuffer,
    IReadOnlyList<bool>? TransmitBits = null,
    int TransmitBitIndex = 0,
    IReadOnlyList<bool>? ReceiveBits = null,
    IReadOnlyList<bool>? SdlcReceiveBits = null,
    IReadOnlyList<bool>? TransmitFrameBits = null,
    int TransmitFrameBitIndex = 0);

public readonly record struct Z80SioReceivedByte(
    byte Value,
    bool ParityError,
    bool FramingError);
