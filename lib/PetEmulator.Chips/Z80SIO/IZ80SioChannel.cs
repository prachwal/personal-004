namespace PetEmulator.Chips;

/// <summary>
/// External wiring of one Z80 SIO channel. Bytes represent the serial RxD/TxD
/// stream at the chip boundary; the channel clock frequencies drive frame timing.
/// </summary>
public interface IZ80SioChannel
{
    int RxClockFrequencyHz { get; }
    int TxClockFrequencyHz { get; }
    bool CtsAsserted { get; }
    bool DcdAsserted { get; }
    event Action<byte, bool, bool>? RxDReceived;
    event Action<bool>? CtsChanged;
    event Action<bool>? DcdChanged;
    void WriteTxD(byte value);
    void SetRts(bool asserted);
    void SetDtr(bool asserted);
}

/// <summary>
/// Optional bit-level wiring for channels that expose the serial line before
/// byte framing. The byte-level members remain available for simple adapters.
/// </summary>
public interface IZ80SioBitChannel : IZ80SioChannel
{
    event Action<bool>? RxDBitReceived;
    void WriteTxDBit(bool value);
}
