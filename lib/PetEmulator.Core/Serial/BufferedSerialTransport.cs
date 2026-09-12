namespace PetEmulator.Core.Serial;

/// <summary>Deterministic byte queues for a machine endpoint or a terminal adapter.</summary>
public sealed class BufferedSerialTransport : ISerialTransport
{
    private readonly Queue<byte> _received = [];
    private readonly Queue<byte> _transmitted = [];

    public bool HasByteAvailable => _received.Count > 0;

    public bool TransmitReady => true;

    public bool IsConnected { get; set; }

    public event Action? ByteReceived;

    public bool TryReadByte(out byte value)
    {
        if (_received.TryDequeue(out value))
            return true;

        value = 0;
        return false;
    }

    public void WriteByte(byte value) => _transmitted.Enqueue(value);

    public void ReceiveFromHost(byte value)
    {
        _received.Enqueue(value);
        ByteReceived?.Invoke();
    }

    public bool TryReadTransmitted(out byte value) => _transmitted.TryDequeue(out value);

    public void Dispose()
    {
        _received.Clear();
        _transmitted.Clear();
    }
}
