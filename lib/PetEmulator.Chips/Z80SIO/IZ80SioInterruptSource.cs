namespace PetEmulator.Chips;

/// <summary>
/// Interrupt-source contract suitable for a Z80 IEI/IEO daisy chain.
/// A source may acknowledge an interrupt only while IEI is passed to it.
/// </summary>
public interface IZ80SioInterruptSource
{
    bool InterruptRequested { get; }
    bool InterruptInService { get; }
    bool InterruptInputEnabled { get; set; }
    bool InterruptOutputEnabled { get; }
    bool TryAcknowledgeInterrupt(out byte vector);
    /// <summary>Signals the Z80 RETI boundary and releases the acknowledged source.</summary>
    void NotifyReti();
    void CompleteInterrupt();
}
