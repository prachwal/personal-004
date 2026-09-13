namespace PetEmulator.Chips;

/// <summary>External eight-bit port and its handshake lines.</summary>
public interface IZ80PioPort
{
    byte InputValue { get; }
    bool StrobeAsserted { get; }
    bool Ready { get; }

    event Action<byte>? InputChanged;
    event Action<bool>? StrobeChanged;
    event Action<byte>? OutputChanged;
    event Action<bool>? ReadyChanged;

    void DriveInput(byte value);
    void SetStrobe(bool asserted);
    void WriteOutput(byte value);
    void SetReady(bool asserted);
}
