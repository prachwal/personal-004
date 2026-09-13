namespace PetEmulator.Chips;

/// <summary>
/// Independent Z80 PIO baseline. Data transfer and handshake behavior are
/// deliberately added in later layers; this type owns the chip contract,
/// programming parser, reset and state serialization.
/// </summary>
public sealed class Z80PioDevice : IZ80PioInterruptSource
{
    public const int PortA = 0;
    public const int PortB = 1;
    public const int PortCount = 2;

    private readonly PioPortState[] _ports = [new(), new()];
    private readonly PortBinding?[] _bindings = new PortBinding?[PortCount];
    private readonly ushort _basePort;
    private bool _interruptInService;
    private bool _interruptInputEnabled = true;
    private byte _interruptVector;

    private sealed class PioPortState
    {
        public Z80PioMode Mode = Z80PioMode.Input;
        public byte DirectionMask = 0xFF;
        public byte OutputLatch;
        public byte InputLatch;
        public bool InputAvailable;
        public bool InputOverrun;
        public bool OutputPending;
        public bool BidirectionalInput = true;
        public byte InterruptMask;
        public byte InterruptControl;
        public bool InterruptEnabled;
        public bool InterruptPending;
        public bool StrobeAsserted;
        public bool Ready;
        public Z80PioControlPhase ControlPhase = Z80PioControlPhase.Ready;
        public byte InterruptVector;
    }

    private sealed class PortBinding(
        IZ80PioPort port,
        Action<byte> outputChanged,
        Action<byte> inputChanged,
        Action<bool> strobeChanged,
        Action<bool> readyChanged)
    {
        public IZ80PioPort Port { get; } = port;
        public Action<byte> OutputChanged { get; } = outputChanged;
        public Action<byte> InputChanged { get; } = inputChanged;
        public Action<bool> StrobeChanged { get; } = strobeChanged;
        public Action<bool> ReadyChanged { get; } = readyChanged;
    }

    public Z80PioDevice(
        ushort basePort = 0,
        Z80PioRevision revision = Z80PioRevision.Z8420)
    {
        _basePort = basePort;
        Profile = Z80PioProfile.For(revision);
        Ports = [basePort, (ushort)(basePort + 1), (ushort)(basePort + 2), (ushort)(basePort + 3)];
    }

    public IReadOnlyList<ushort> Ports { get; }
    public Z80PioProfile Profile { get; }
    public bool InterruptRequested =>
        _interruptInputEnabled && !_interruptInService && (_ports[0].InterruptPending || _ports[1].InterruptPending);
    public bool InterruptInService => _interruptInService;
    public bool InterruptInputEnabled
    {
        get => _interruptInputEnabled;
        set => _interruptInputEnabled = value;
    }
    public bool InterruptOutputEnabled =>
        _interruptInputEnabled && !_interruptInService && !InterruptRequested;

    public void AttachPort(int port, IZ80PioPort externalPort)
    {
        ArgumentNullException.ThrowIfNull(externalPort);
        ValidatePort(port);
        DetachPort(port);
        Action<byte> outputChanged = _ => { };
        Action<byte> inputChanged = value => DriveInput(port, value, _ports[port].StrobeAsserted);
        Action<bool> strobeChanged = asserted => SetStrobe(port, asserted);
        Action<bool> readyChanged = asserted => SetPeripheralReady(port, asserted);
        externalPort.InputChanged += inputChanged;
        externalPort.StrobeChanged += strobeChanged;
        externalPort.OutputChanged += outputChanged;
        externalPort.ReadyChanged += readyChanged;
        _bindings[port] = new PortBinding(externalPort, outputChanged, inputChanged, strobeChanged, readyChanged);
        _ports[port].InputLatch = externalPort.InputValue;
        _ports[port].StrobeAsserted = externalPort.StrobeAsserted;
        _ports[port].Ready = externalPort.Ready;
    }

    public void DetachPort(int port)
    {
        ValidatePort(port);
        if (_bindings[port] is not { } binding)
            return;

        binding.Port.OutputChanged -= binding.OutputChanged;
        binding.Port.InputChanged -= binding.InputChanged;
        binding.Port.StrobeChanged -= binding.StrobeChanged;
        binding.Port.ReadyChanged -= binding.ReadyChanged;
        _bindings[port] = null;
    }

    public byte ReadPort(ushort port)
    {
        if (!TryDecode(port, out var channel, out var control))
            return Profile.InvalidReadValue;

        var state = _ports[channel];
        if (control)
            return Profile.UnsupportedRegisterReadValue;

        var value = state.Mode switch
        {
            Z80PioMode.Output => state.OutputLatch,
            Z80PioMode.BitControl => ReadBitControlValue(state),
            Z80PioMode.Bidirectional when !state.BidirectionalInput => state.OutputLatch,
            _ => state.InputLatch,
        };
        if (state.Mode == Z80PioMode.Input)
            state.InputAvailable = false;
        return value;
    }

    public void WritePort(ushort port, byte value)
    {
        if (!TryDecode(port, out var channel, out var control))
            return;

        if (control)
            WriteControl(channel, value);
        else
            WriteData(channel, value);
    }

    public void Tick(int tStates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tStates);
    }

    public void NotifyReti() => _interruptInService = false;

    /// <summary>Drives an input value from an attached peripheral or adapter.</summary>
    public void DriveInput(int port, byte value, bool strobeAsserted = true)
    {
        ValidatePort(port);
        var state = _ports[port];
        if (state.InputAvailable &&
            (state.Mode == Z80PioMode.Input ||
             (state.Mode == Z80PioMode.Bidirectional && state.BidirectionalInput)))
        {
            if (strobeAsserted)
                state.InputOverrun = true;
            return;
        }

        state.InputLatch = value;
        state.StrobeAsserted = strobeAsserted;
        if (strobeAsserted || state.Mode == Z80PioMode.BitControl)
            AcceptInput(port);
    }

    /// <summary>Changes the external strobe line without inventing a clock.</summary>
    public void SetStrobe(int port, bool asserted)
    {
        ValidatePort(port);
        if (_ports[port].StrobeAsserted == asserted)
            return;
        _ports[port].StrobeAsserted = asserted;
        if (asserted)
            AcceptInput(port);
    }

    /// <summary>Changes the peripheral acceptance/ready line.</summary>
    public void SetPeripheralReady(int port, bool asserted)
    {
        ValidatePort(port);
        var state = _ports[port];
        state.Ready = asserted;
        if (asserted && state.OutputPending)
            CommitOutput(port);
    }

    /// <summary>Changes the active direction of the bidirectional Port A.</summary>
    public void SetBidirectionalDirection(int port, bool input)
    {
        ValidatePort(port);
        var state = _ports[port];
        if (port != PortA || state.Mode != Z80PioMode.Bidirectional)
            return;

        if (input && !state.BidirectionalInput)
        {
            state.InputAvailable = false;
            state.InputOverrun = false;
        }

        state.BidirectionalInput = input;
        if (!input && state.Ready && state.OutputPending)
            CommitOutput(port);
    }

    public bool TryAcknowledgeInterrupt(out byte vector)
    {
        vector = 0;
        if (!InterruptRequested)
            return false;

        _interruptInService = true;
        var interruptingPort = _ports[0].InterruptPending ? PortA : PortB;
        vector = _ports[interruptingPort].InterruptVector;
        if (_ports[0].InterruptPending)
            _ports[0].InterruptPending = false;
        else
            _ports[1].InterruptPending = false;
        return true;
    }

    public Z80PioState CaptureState() => new(
        CapturePort(_ports[0]),
        CapturePort(_ports[1]),
        _interruptVector,
        _interruptInService,
        _interruptInputEnabled);

    public Z80PioDebugSnapshot CaptureDebugSnapshot() => new(
        CaptureDebugPort(PortA, _ports[PortA]),
        CaptureDebugPort(PortB, _ports[PortB]),
        _interruptInService,
        _interruptInputEnabled);

    public void RestoreState(Z80PioState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        RestorePort(_ports[0], state.PortA);
        RestorePort(_ports[1], state.PortB);
        _interruptVector = state.InterruptVector;
        _interruptInService = state.InterruptInService;
        _interruptInputEnabled = state.InterruptInputEnabled;
    }

    public void Reset()
    {
        foreach (var state in _ports)
        {
            state.Mode = Z80PioMode.Input;
            state.DirectionMask = 0xFF;
            state.OutputLatch = 0;
            state.InputLatch = 0;
            state.InputAvailable = false;
            state.InputOverrun = false;
            state.OutputPending = false;
            state.BidirectionalInput = true;
            state.InterruptMask = 0;
            state.InterruptControl = 0;
            state.InterruptEnabled = false;
            state.InterruptPending = false;
            state.StrobeAsserted = false;
            state.Ready = false;
            state.ControlPhase = Z80PioControlPhase.Ready;
            state.InterruptVector = 0;
        }

        _interruptVector = 0;
        _interruptInService = false;
        _interruptInputEnabled = true;
    }

    private void WriteControl(int channel, byte value)
    {
        var state = _ports[channel];
        switch (state.ControlPhase)
        {
            case Z80PioControlPhase.Mode3IoSelect:
                state.DirectionMask = value;
                state.ControlPhase = Z80PioControlPhase.Ready;
                return;
            case Z80PioControlPhase.InterruptMask:
                state.InterruptMask = value;
                state.ControlPhase = Z80PioControlPhase.Ready;
                return;
        }

        if ((value & 0x01) == 0)
        {
            _interruptVector = (byte)(value & 0xFE);
            state.InterruptVector = _interruptVector;
            return;
        }

        if ((value & 0x0F) == 0x0F)
        {
            var requestedMode = (Z80PioMode)((value >> 6) & 0x03);
            state.Mode = requestedMode == Z80PioMode.Bidirectional && channel != PortA
                ? Z80PioMode.Input
                : requestedMode;
            state.BidirectionalInput = true;
            state.ControlPhase = state.Mode == Z80PioMode.BitControl
                ? Z80PioControlPhase.Mode3IoSelect
                : Z80PioControlPhase.Ready;
            return;
        }

        if ((value & 0x0F) == 0x07)
        {
            state.InterruptControl = value;
            state.InterruptEnabled = (value & 0x80) != 0;
            state.ControlPhase = (value & 0x10) != 0
                ? Z80PioControlPhase.InterruptMask
                : Z80PioControlPhase.Ready;
            return;
        }

        if ((value & 0x0F) == 0x03)
            state.InterruptEnabled = false;
    }

    private Z80PioPortState CapturePort(PioPortState state) => new(
        state.Mode,
        state.DirectionMask,
        state.OutputLatch,
        state.InputLatch,
        state.InputAvailable,
        state.InputOverrun,
        state.OutputPending,
        state.BidirectionalInput,
        state.InterruptMask,
        state.InterruptControl,
        state.InterruptEnabled,
        state.InterruptPending,
        state.StrobeAsserted,
        state.Ready,
        state.ControlPhase,
        state.InterruptVector);

    private static Z80PioPortDebugInfo CaptureDebugPort(int port, PioPortState state) => new(
        port,
        state.Mode,
        state.DirectionMask,
        state.OutputLatch,
        state.InputLatch,
        state.InputAvailable,
        state.InputOverrun,
        state.OutputPending,
        state.StrobeAsserted,
        state.Ready,
        state.InterruptVector,
        state.InterruptPending,
        state.ControlPhase);

    private static void RestorePort(PioPortState target, Z80PioPortState source)
    {
        target.Mode = source.Mode;
        target.DirectionMask = source.DirectionMask;
        target.OutputLatch = source.OutputLatch;
        target.InputLatch = source.InputLatch;
        target.InputAvailable = source.InputAvailable;
        target.InputOverrun = source.InputOverrun;
        target.OutputPending = source.OutputPending;
        target.BidirectionalInput = source.BidirectionalInput;
        target.InterruptMask = source.InterruptMask;
        target.InterruptControl = source.InterruptControl;
        target.InterruptEnabled = source.InterruptEnabled;
        target.InterruptPending = source.InterruptPending;
        target.StrobeAsserted = source.StrobeAsserted;
        target.Ready = source.Ready;
        target.ControlPhase = source.ControlPhase;
        target.InterruptVector = source.InterruptVector;
    }

    private bool TryDecode(ushort port, out int channel, out bool control)
    {
        var offset = port - _basePort;
        if (port < _basePort || offset > 3)
        {
            channel = 0;
            control = false;
            return false;
        }

        channel = offset & 1;
        control = (offset & 2) != 0;
        return true;
    }

    private static void ValidatePort(int port)
    {
        if (port is < 0 or >= PortCount)
            throw new ArgumentOutOfRangeException(nameof(port));
    }

    private void WriteData(int channel, byte value)
    {
        var state = _ports[channel];

        if (state.Mode == Z80PioMode.BitControl)
        {
            state.OutputLatch = (byte)((state.OutputLatch & state.DirectionMask) |
                (value & ~state.DirectionMask));
            _bindings[channel]?.Port.WriteOutput(ReadBitControlValue(state));
            if (state.InterruptEnabled)
                EvaluateBitInterrupt(state);
            return;
        }

        state.OutputLatch = value;
        if (state.Mode == Z80PioMode.Input)
            return;

        if (state.Mode == Z80PioMode.Bidirectional && state.BidirectionalInput)
        {
            state.OutputPending = true;
            return;
        }

        state.OutputPending = true;
        if (state.Ready)
            CommitOutput(channel);
    }

    private void CommitOutput(int channel)
    {
        var state = _ports[channel];
        if (!state.OutputPending ||
            (state.Mode != Z80PioMode.Output &&
             !(state.Mode == Z80PioMode.Bidirectional && !state.BidirectionalInput)))
            return;

        _bindings[channel]?.Port.WriteOutput(state.OutputLatch);
        state.OutputPending = false;
        if (state.InterruptEnabled)
            state.InterruptPending = true;
    }

    private void AcceptInput(int channel)
    {
        var state = _ports[channel];
        if (state.Mode is not (Z80PioMode.Input or Z80PioMode.Bidirectional or Z80PioMode.BitControl))
            return;
        if (state.Mode == Z80PioMode.Bidirectional && !state.BidirectionalInput)
        {
            state.InputOverrun = true;
            return;
        }
        if (state.InputAvailable)
        {
            state.InputOverrun = true;
            return;
        }

        state.InputAvailable = true;
        if (state.InterruptEnabled)
        {
            if (state.Mode == Z80PioMode.BitControl)
                EvaluateBitInterrupt(state);
            else
                state.InterruptPending = true;
        }
    }

    private static byte ReadBitControlValue(PioPortState state) =>
        (byte)((state.InputLatch & state.DirectionMask) |
            (state.OutputLatch & ~state.DirectionMask));

    private static void EvaluateBitInterrupt(PioPortState state)
    {
        var mask = state.InterruptMask;
        if (mask == 0)
        {
            state.InterruptPending = false;
            return;
        }

        var sample = ReadBitControlValue(state);
        var activeHigh = (state.InterruptControl & 0x20) != 0;
        var andMode = (state.InterruptControl & 0x40) != 0;
        var matching = activeHigh ? (byte)(sample & mask) : (byte)(~sample & mask);
        state.InterruptPending = andMode
            ? matching == mask
            : matching != 0;
    }
}
