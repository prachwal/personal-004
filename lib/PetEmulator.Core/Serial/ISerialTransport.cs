namespace PetEmulator.Core.Serial;

/// <summary>Byte-oriented host endpoint used by an ACIA/UART without coupling the chip to a socket or UI.</summary>
public interface ISerialTransport : IDisposable
{
    bool HasByteAvailable { get; }

    bool TransmitReady { get; }

    bool TryReadByte(out byte value);

    void WriteByte(byte value);

    event Action? ByteReceived;

    bool IsConnected { get; }
}
