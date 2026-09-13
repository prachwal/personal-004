using PetEmulator.Chips;

namespace PetEmulator.Kaypro;

/// <summary>
/// Kaypro II wiring adapter for the independent Z80 SIO chip.
/// It owns only the machine port map and keyboard/channel-B policy.
/// </summary>
public class KayproSioWiring
{
    public const ushort BasePort = 0x04;
    public const ushort ChannelAData = 0x04;
    public const ushort ChannelBData = 0x05;
    public const ushort ChannelAControl = 0x06;
    public const ushort ChannelBControl = 0x07;

    private readonly Z80Sio _chip = new(BasePort);

    public KayproSioWiring() => _chip.Transmitted += (_, value) => Transmitted?.Invoke(value);

    public event Action<byte>? Transmitted;

    public byte Read(ushort port) => _chip.ReadPort(port);

    public void Write(ushort port, byte value) => _chip.WritePort(port, value);

    public void EnqueueKeyboardByte(byte value) => _chip.EnqueueRxByte(Z80Sio.ChannelB, value);

    public void Tick(int tStates) => _chip.Tick(tStates);

    public bool TryConsumePendingInterrupt(out byte vector) => _chip.TryConsumePendingInterrupt(out vector);

    public bool InterruptRequested => _chip.InterruptRequested;

    public bool InterruptInService => _chip.InterruptInService;

    public bool TryAcknowledgeInterrupt(out byte vector) => _chip.TryAcknowledgeInterrupt(out vector);

    public void NotifyReti() => _chip.NotifyReti();

    public void Reset() => _chip.Reset();
}
