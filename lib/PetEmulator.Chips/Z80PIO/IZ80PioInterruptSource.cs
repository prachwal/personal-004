namespace PetEmulator.Chips;

/// <summary>Interrupt contract shared by a Z80 PIO daisy-chain adapter.</summary>
public interface IZ80PioInterruptSource
{
    bool InterruptRequested { get; }
    bool InterruptInService { get; }
    bool InterruptInputEnabled { get; set; }
    bool InterruptOutputEnabled { get; }
    bool TryAcknowledgeInterrupt(out byte vector);
    void NotifyReti();
}
