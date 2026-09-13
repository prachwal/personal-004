using PetEmulator.Chips;

namespace PetEmulator.Kaypro;

/// <summary>
/// Kaypro II board wiring for its two independent Z80 PIO devices.
/// The chip instances remain generic; this class owns only port mapping and
/// the system/printer-side connections.
/// </summary>
public sealed class KayproPioWiring
{
    public const ushort PioGBasePort = 0x08;
    public const ushort PioSBasePort = 0x1C;

    private readonly KayproSystemPort _systemPort = new();

    public KayproPioWiring()
    {
        PioG = new Z80PioDevice();
        PioS = new Z80PioDevice();
        PioS.AttachPort(Z80PioDevice.PortA, _systemPort);
        PioS.WritePort(2, 0x0F); // Mode 0 output for the system latch.
        _systemPort.OutputChanged += value => SystemPortChanged?.Invoke(value);
        PioS.WritePort(0, 0x80);
    }

    public Z80PioDevice PioG { get; }
    public Z80PioDevice PioS { get; }
    public IZ80PioPort SystemPort => _systemPort;
    public event Action<byte>? SystemPortChanged;

    public void ConnectPrinterPort(IZ80PioPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        PioG.AttachPort(Z80PioDevice.PortA, port);
    }

    public byte Read(ushort port)
    {
        return port switch
        {
            >= PioGBasePort and < PioGBasePort + 4 => PioG.ReadPort((ushort)(port - PioGBasePort)),
            >= PioSBasePort and < PioSBasePort + 4 => PioS.ReadPort((ushort)(port - PioSBasePort)),
            _ => 0xFF,
        };
    }

    public void Write(ushort port, byte value)
    {
        switch (port)
        {
            case >= PioGBasePort and < PioGBasePort + 4:
                PioG.WritePort((ushort)(port - PioGBasePort), value);
                break;
            case >= PioSBasePort and < PioSBasePort + 4:
                PioS.WritePort((ushort)(port - PioSBasePort), value);
                break;
        }
    }

    public void Reset()
    {
        PioG.Reset();
        PioS.Reset();
        _systemPort.Reset();
        _systemPort.SetReady(true);
        PioS.WritePort(2, 0x0F);
        PioS.WritePort(0, 0x80);
    }

    private sealed class KayproSystemPort : IZ80PioPort
    {
        private byte _inputValue;
        private byte _outputValue;
        private bool _ready = true;

        public byte InputValue => _inputValue;
        public bool StrobeAsserted { get; private set; }
        public bool Ready => _ready;
        public event Action<byte>? InputChanged;
        public event Action<bool>? StrobeChanged;
        public event Action<byte>? OutputChanged;
        public event Action<bool>? ReadyChanged;

        public void DriveInput(byte value)
        {
            _inputValue = value;
            InputChanged?.Invoke(value);
        }

        public void SetStrobe(bool asserted)
        {
            StrobeAsserted = asserted;
            StrobeChanged?.Invoke(asserted);
        }

        public void WriteOutput(byte value)
        {
            _outputValue = value;
            _inputValue = value;
            OutputChanged?.Invoke(value);
        }

        public void SetReady(bool asserted)
        {
            _ready = asserted;
            ReadyChanged?.Invoke(asserted);
        }

        public void Reset()
        {
            _inputValue = 0x80;
            _outputValue = 0x80;
            StrobeAsserted = false;
            _ready = true;
        }
    }
}
