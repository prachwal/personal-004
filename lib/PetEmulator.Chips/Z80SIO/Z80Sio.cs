namespace PetEmulator.Chips;

/// <summary>
/// Independent, port-mapped model of the Zilog Z80 SIO/O family.
/// Machine-specific wiring, port addresses and external line polarity belong
/// to the host machine adapter.
/// </summary>
public sealed class Z80Sio : IZ80SioInterruptSource
{
    public const int ChannelA = 0;
    public const int ChannelB = 1;
    public const int ChannelCount = 2;
    public const int RxBufferDepth = 2;
    public const int TxBufferDepth = 8;

    public const byte Rr0RxAvailable = 0x01;
    public const byte Rr0TxEmpty = 0x04;
    public const byte Rr0Dcd = 0x08;
    public const byte Rr0Cts = 0x20;
    public const byte Rr1AllSent = 0x01;
    public const byte Rr1ParityError = 0x10;
    public const byte Rr1OverrunError = 0x20;
    public const byte Rr1FramingError = 0x40;
    public const byte Rr1BreakDetected = 0x80;
    public const byte Rr3BRxInterrupt = 0x01;
    public const byte Rr3BTxInterrupt = 0x02;
    public const byte Rr3BExtStatusInterrupt = 0x04;
    public const byte Rr3ARxInterrupt = 0x08;
    public const byte Rr3ATxInterrupt = 0x10;
    public const byte Rr3AExtStatusInterrupt = 0x20;

    private const int BitsPerCharacter = 10;
    private const byte Wr1ExtStatusInterruptEnable = 0x01;
    private const byte Wr1TxInterruptEnable = 0x02;
    private const byte Wr1StatusAffectsVector = 0x04;
    private const byte Wr1RxInterruptMask = 0x18;
    private const int Wr1RxInterruptShift = 3;
    private const byte Wr3RxEnable = 0x01;
    private const byte Wr3RxBitsMask = 0xC0;
    private const byte Wr4ClockMultiplierMask = 0xC0;
    private const byte Wr4StopBitsMask = 0x0C;
    private const byte Wr4AsyncMode = 0x04;
    private const byte Wr4ParityEnable = 0x01;
    private const byte Wr4ParityEven = 0x02;
    private const byte Wr5Rts = 0x02;
    private const byte Wr5SendBreak = 0x10;
    private const byte Wr5TxEnable = 0x08;
    private const byte Wr5Dtr = 0x80;
    private const byte Wr5TxBitsMask = 0x60;

    private readonly Channel[] _channels = [new(), new()];
    private readonly ushort _basePort;
    private readonly int _clockFrequencyHz;
    private long _currentTState;
    private int _baudRate = 300;
    private byte _wr2;
    private bool _interruptInService;

    private sealed class Channel
    {
        public readonly Queue<RxByte> Rx = new();
        public readonly Queue<byte> Tx = new();
        public readonly byte[] Wr = new byte[8];
        public int Pointer;
        public bool Overrun;
        public bool RxInterruptArmed = true;
        public bool TxInterruptPending;
        public bool ExtStatusInterruptPending;
        public byte? TransmitByte;
        public IReadOnlyList<bool>? TransmitBits;
        public int TransmitBitIndex;
        public IReadOnlyList<bool>? TransmitFrameBits;
        public int TransmitFrameBitIndex;
        public readonly List<bool> ReceiveBits = [];
        public readonly List<bool> SdlcReceiveBits = [];
        public long TransmitReadyAtTState;
        public long ReadyAtTState;
        public bool CtsAsserted = true;
        public bool DcdAsserted = true;
        public bool BreakDetected;
        public Z80SioFrameMode Mode = Z80SioFrameMode.Async;
        public ushort SyncWord;
        public bool SyncAcquired = true;
        public byte? SyncPendingByte;
        public bool AutoEchoEnabled;
        public bool LocalLoopbackEnabled;
        public ChannelBinding? Binding;
    }

    private sealed class ChannelBinding(IZ80SioChannel channel)
    {
        public IZ80SioChannel Channel { get; } = channel;
        public Action<byte, bool, bool>? RxDReceived { get; set; }
        public Action<bool>? RxDBitReceived { get; set; }
        public Action<bool>? CtsChanged { get; set; }
        public Action<bool>? DcdChanged { get; set; }
    }

    private readonly record struct RxByte(byte Value, bool ParityError, bool FramingError);
    private enum InterruptKind { Receive, Transmit, ExternalStatus }

    public Z80Sio(
        ushort basePort = 0,
        int clockFrequencyHz = 2_500_000,
        Z80SioRevision revision = Z80SioRevision.Z8440)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(clockFrequencyHz);
        _basePort = basePort;
        _clockFrequencyHz = clockFrequencyHz;
        Profile = Z80SioProfile.For(revision);
        Ports = [basePort, (ushort)(basePort + 1), (ushort)(basePort + 2), (ushort)(basePort + 3)];
    }

    public IReadOnlyList<ushort> Ports { get; }
    public Z80SioProfile Profile { get; }
    public int BaudRate
    {
        get => _baudRate;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _baudRate = value;
        }
    }
    public event Action<int, byte>? Transmitted;
    public event Action<int, bool>? BreakChanged;

    public bool InterruptRequested => InterruptInputEnabled && !_interruptInService && FindHighestPendingInterrupt() is not null;
    public bool InterruptInService => _interruptInService;
    public bool InterruptInputEnabled { get; set; } = true;
    public bool InterruptOutputEnabled =>
        Profile.SupportsDaisyChain && InterruptInputEnabled && !_interruptInService && !InterruptRequested;

    public readonly record struct ChannelState(
        bool RxEnabled,
        bool TxEnabled,
        bool CtsAsserted,
        bool DcdAsserted,
        bool RtsAsserted,
        bool DtrAsserted,
        int ReceiveDataBits,
        int TransmitDataBits,
        int ClockMultiplier,
        bool AsyncMode,
        bool ParityEnabled,
        bool EvenParity,
        bool BreakAsserted,
        bool BreakDetected,
        Z80SioFrameMode Mode,
        ushort SyncWord,
        bool SyncAcquired,
        byte? SyncPendingByte,
        bool AutoEchoEnabled,
        bool LocalLoopbackEnabled);

    private void PauseUnavailableTiming()
    {
        foreach (var state in _channels)
        {
            if (!HasClock(state, transmit: false) && state.Rx.Count > 0)
                state.ReadyAtTState = long.MaxValue;
            if (!HasClock(state, transmit: true)
                && (state.TransmitByte is not null || state.TransmitFrameBits is not null))
                state.TransmitReadyAtTState = long.MaxValue;
        }
    }

    private void ResumePausedTiming()
    {
        foreach (var state in _channels)
        {
            if (state.Rx.Count > 0 && state.ReadyAtTState == long.MaxValue && HasClock(state, transmit: false))
                state.ReadyAtTState = ScheduleAt(_currentTState, CharacterTStates(state));

            if (state.TransmitFrameBits is not null && state.TransmitReadyAtTState == long.MaxValue && HasClock(state, transmit: true))
                state.TransmitReadyAtTState = ScheduleAt(_currentTState, BitTStates(state, transmit: true));
            else if (state.TransmitByte is not null && state.TransmitReadyAtTState == long.MaxValue && HasClock(state, transmit: true))
            {
                var delay = state.TransmitBits is not null
                    ? BitTStates(state, transmit: true)
                    : CharacterTStates(state, transmit: true);
                state.TransmitReadyAtTState = ScheduleAt(_currentTState, delay);
            }
        }
    }

    private bool HasClock(Channel state, bool transmit) =>
        BaudRate > 0 && (transmit
            ? state.Binding?.Channel.TxClockFrequencyHz ?? _clockFrequencyHz
            : state.Binding?.Channel.RxClockFrequencyHz ?? _clockFrequencyHz) > 0;

    private static long ScheduleAt(long origin, long delay) =>
        delay == long.MaxValue ? long.MaxValue : origin + delay;

    private long CharacterTStates(Channel state, bool transmit = false)
    {
        if (BaudRate <= 0)
            return long.MaxValue;

        var frameBits = (double)BitsPerCharacter;
        if ((state.Wr[4] & Wr4AsyncMode) != 0)
        {
            var dataBits = transmit
                ? DecodeDataBits((byte)((state.Wr[5] & Wr5TxBitsMask) >> 5))
                : DecodeDataBits((byte)(state.Wr[3] & Wr3RxBitsMask));
            var parityBits = (state.Wr[4] & Wr4ParityEnable) != 0 ? 1 : 0;
            var stopBits = DecodeStopBits((byte)((state.Wr[4] & Wr4StopBitsMask) >> 2));
            frameBits = 1 + dataBits + parityBits + stopBits;
        }

        var multiplier = DecodeClockMultiplier((byte)((state.Wr[4] & Wr4ClockMultiplierMask) >> 6));
        var clockFrequencyHz = transmit
            ? state.Binding?.Channel.TxClockFrequencyHz ?? _clockFrequencyHz
            : state.Binding?.Channel.RxClockFrequencyHz ?? _clockFrequencyHz;
        if (clockFrequencyHz <= 0)
            return long.MaxValue;
        return Math.Max(1, (long)Math.Ceiling(clockFrequencyHz * frameBits / (BaudRate * multiplier)));
    }

    private long BitTStates(Channel state, bool transmit = false)
    {
        if (BaudRate <= 0)
            return long.MaxValue;

        var multiplier = DecodeClockMultiplier((byte)((state.Wr[4] & Wr4ClockMultiplierMask) >> 6));
        var clockFrequencyHz = transmit
            ? state.Binding?.Channel.TxClockFrequencyHz ?? _clockFrequencyHz
            : state.Binding?.Channel.RxClockFrequencyHz ?? _clockFrequencyHz;
        if (clockFrequencyHz <= 0)
            return long.MaxValue;
        return Math.Max(1, (long)Math.Ceiling((double)clockFrequencyHz / (BaudRate * multiplier)));
    }

    public void Tick(int tStates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tStates);
        PauseUnavailableTiming();
        ResumePausedTiming();
        _currentTState += tStates;
        AdvanceTransmitters();
    }

    public void AttachChannel(int channel, IZ80SioChannel externalChannel)
    {
        ArgumentNullException.ThrowIfNull(externalChannel);
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, ChannelCount);
        ArgumentOutOfRangeException.ThrowIfNegative(externalChannel.RxClockFrequencyHz);
        ArgumentOutOfRangeException.ThrowIfNegative(externalChannel.TxClockFrequencyHz);

        DetachChannel(channel);
        var state = _channels[channel];
        var binding = new ChannelBinding(externalChannel)
        {
            RxDReceived = (value, parityError, framingError) => EnqueueRxByte(channel, value, parityError, framingError),
            CtsChanged = asserted => SetCts(channel, asserted),
            DcdChanged = asserted => SetDcd(channel, asserted),
        };
        if (externalChannel is IZ80SioBitChannel)
            binding.RxDBitReceived = bit => ReceiveBit(channel, bit);
        state.Binding = binding;
        externalChannel.RxDReceived += binding.RxDReceived;
        if (externalChannel is IZ80SioBitChannel bitChannel)
            bitChannel.RxDBitReceived += binding.RxDBitReceived;
        externalChannel.CtsChanged += binding.CtsChanged;
        externalChannel.DcdChanged += binding.DcdChanged;
        state.CtsAsserted = externalChannel.CtsAsserted;
        state.DcdAsserted = externalChannel.DcdAsserted;
        UpdateModemOutputs(channel, state);
    }

    public void DetachChannel(int channel)
    {
        var state = GetChannel(channel);
        if (state.Binding is not { } binding)
            return;

        binding.Channel.RxDReceived -= binding.RxDReceived;
        if (binding.Channel is IZ80SioBitChannel bitChannel)
            bitChannel.RxDBitReceived -= binding.RxDBitReceived;
        binding.Channel.CtsChanged -= binding.CtsChanged;
        binding.Channel.DcdChanged -= binding.DcdChanged;
        state.Binding = null;
    }

    public void EnqueueRxByte(int channel, byte value) => EnqueueRxByte(channel, value, false, false);

    public void EnqueueRxByte(int channel, byte value, bool parityError, bool framingError)
    {
        EnqueueRxByteCore(channel, value, parityError, framingError, allowAutoEcho: true);
    }

    private bool EnqueueRxByteCore(int channel, byte value, bool parityError, bool framingError, bool allowAutoEcho)
    {
        var state = GetChannel(channel);
        if (!AcceptsSynchronizedByte(state, value))
            return false;
        if (state.Rx.Count >= RxBufferDepth)
        {
            state.Overrun = true;
            return false;
        }

        state.Rx.Enqueue(new RxByte(value, parityError, framingError));
        if (state.Rx.Count == 1)
            state.ReadyAtTState = ScheduleAt(_currentTState, CharacterTStates(state));
        if (allowAutoEcho && state.AutoEchoEnabled)
            WriteData(channel, value);
        return true;
    }

    private void ReceiveBit(int channel, bool bit)
    {
        var state = GetChannel(channel);
        switch (state.Mode)
        {
            case Z80SioFrameMode.Async:
                state.ReceiveBits.Add(bit);
                var asyncFormat = GetAsyncFormat(state);
                var asyncResult = Z80SioBitCodec.DecodeAsync(state.ReceiveBits, asyncFormat);
                if (asyncResult.ConsumedBits > 0)
                {
                    state.ReceiveBits.RemoveRange(0, asyncResult.ConsumedBits);
                    EnqueueRxByteCore(channel, asyncResult.Value, asyncResult.ParityError, asyncResult.FramingError, allowAutoEcho: true);
                }
                break;
            case Z80SioFrameMode.Sync8:
            case Z80SioFrameMode.Sync16:
            case Z80SioFrameMode.ExternalSync:
                state.ReceiveBits.Add(bit);
                if (state.ReceiveBits.Count >= 8)
                {
                    var value = ToByte(state.ReceiveBits);
                    state.ReceiveBits.Clear();
                    EnqueueRxByteCore(channel, value, false, false, allowAutoEcho: true);
                }
                break;
            case Z80SioFrameMode.Sdlc:
                state.SdlcReceiveBits.Add(bit);
                var frame = Z80SioSdlcCodec.DecodeFrame(state.SdlcReceiveBits);
                if (frame.Aborted || frame.EndOfFrame)
                {
                    if (frame.Success)
                        foreach (var value in frame.Payload)
                            EnqueueRxByteCore(channel, value, false, false, allowAutoEcho: true);
                    state.SdlcReceiveBits.Clear();
                }
                break;
        }
    }

    public void ConfigureMode(int channel, Z80SioFrameMode mode, ushort? syncWord = null)
    {
        var state = GetChannel(channel);
        state.Mode = mode;
        if (syncWord is ushort configuredSyncWord)
            state.SyncWord = configuredSyncWord;
        state.SyncPendingByte = null;
        state.SyncAcquired = mode is Z80SioFrameMode.Async or Z80SioFrameMode.Sdlc;
    }

    public void SignalExternalSync(int channel)
    {
        var state = GetChannel(channel);
        if (state.Mode == Z80SioFrameMode.ExternalSync)
            state.SyncAcquired = true;
    }

    public void SetAutoEcho(int channel, bool enabled) => GetChannel(channel).AutoEchoEnabled = enabled;

    public void SetLocalLoopback(int channel, bool enabled) => GetChannel(channel).LocalLoopbackEnabled = enabled;

    public void TransmitSdlcFrame(int channel, IReadOnlyList<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var state = GetChannel(channel);
        if (state.Binding?.Channel is not IZ80SioBitChannel)
            throw new InvalidOperationException("SDLC frame transmission requires a bit-level channel.");
        if (!IsTxEnabled(state) || !state.CtsAsserted || state.TransmitByte is not null || state.TransmitFrameBits is not null)
            throw new InvalidOperationException("The SIO transmitter is not ready for an SDLC frame.");

        state.TransmitFrameBits = Z80SioSdlcCodec.EncodeFrame(payload);
        state.TransmitFrameBitIndex = 0;
        state.TransmitReadyAtTState = ScheduleAt(_currentTState, BitTStates(state, transmit: true));
    }

    public void TransmitSdlcAbort(int channel)
    {
        var state = GetChannel(channel);
        if (state.Binding?.Channel is not IZ80SioBitChannel)
            throw new InvalidOperationException("SDLC abort transmission requires a bit-level channel.");
        if (state.TransmitByte is not null || state.TransmitFrameBits is not null)
            throw new InvalidOperationException("The SIO transmitter is busy.");

        state.TransmitFrameBits = Z80SioSdlcCodec.EncodeAbort();
        state.TransmitFrameBitIndex = 0;
        state.TransmitReadyAtTState = ScheduleAt(_currentTState, BitTStates(state, transmit: true));
    }

    public int FlushReceiveBuffer(int channel)
    {
        var state = GetChannel(channel);
        var count = state.Rx.Count;
        state.Rx.Clear();
        state.Overrun = false;
        state.RxInterruptArmed = true;
        state.ReadyAtTState = _currentTState;
        return count;
    }

    public ChannelState GetChannelState(int channel)
    {
        var state = GetChannel(channel);
        return new ChannelState(
            IsRxEnabled(state),
            IsTxEnabled(state),
            state.CtsAsserted,
            state.DcdAsserted,
            (state.Wr[5] & Wr5Rts) != 0,
            (state.Wr[5] & Wr5Dtr) != 0,
            DecodeDataBits((byte)(state.Wr[3] & Wr3RxBitsMask)),
            DecodeDataBits((byte)((state.Wr[5] & Wr5TxBitsMask) >> 5)),
            DecodeClockMultiplier((byte)((state.Wr[4] & Wr4ClockMultiplierMask) >> 6)),
            (state.Wr[4] & Wr4AsyncMode) != 0,
            (state.Wr[4] & Wr4ParityEnable) != 0,
            (state.Wr[4] & Wr4ParityEven) != 0,
            (state.Wr[5] & Wr5SendBreak) != 0,
            state.BreakDetected,
            state.Mode,
            state.SyncWord,
            state.SyncAcquired,
            state.SyncPendingByte,
            state.AutoEchoEnabled,
            state.LocalLoopbackEnabled);
    }

    public void SetCts(int channel, bool asserted)
    {
        var state = GetChannel(channel);
        if (state.CtsAsserted != asserted && IsExtStatusInterruptEnabled(state))
            state.ExtStatusInterruptPending = true;
        state.CtsAsserted = asserted;
        if (asserted)
            StartNextTransmitter(state);
    }

    public void SetDcd(int channel, bool asserted)
    {
        var state = GetChannel(channel);
        if (state.DcdAsserted != asserted && IsExtStatusInterruptEnabled(state))
            state.ExtStatusInterruptPending = true;
        state.DcdAsserted = asserted;
    }

    public void SetRxBreak(int channel, bool detected)
    {
        var state = GetChannel(channel);
        if (state.BreakDetected != detected && IsExtStatusInterruptEnabled(state))
            state.ExtStatusInterruptPending = true;
        state.BreakDetected = detected;
    }

    public Z80SioState CaptureState() => new(
        _currentTState,
        BaudRate,
        _wr2,
        _interruptInService,
        InterruptInputEnabled,
        _channels.Select(state => new Z80SioChannelState(
            state.Wr.ToArray(),
            state.Pointer,
            state.Overrun,
            state.RxInterruptArmed,
            state.TxInterruptPending,
            state.ExtStatusInterruptPending,
            state.TransmitByte,
            state.TransmitReadyAtTState,
            state.Tx.ToArray(),
            state.ReadyAtTState,
            state.CtsAsserted,
            state.DcdAsserted,
            state.BreakDetected,
            state.Mode,
            state.SyncWord,
            state.SyncAcquired,
            state.SyncPendingByte,
            state.AutoEchoEnabled,
            state.LocalLoopbackEnabled,
            state.Rx.Select(rx => new Z80SioReceivedByte(
                rx.Value,
                rx.ParityError,
                rx.FramingError)).ToArray(),
            state.TransmitBits?.ToArray(),
            state.TransmitBitIndex,
            state.ReceiveBits.ToArray(),
            state.SdlcReceiveBits.ToArray(),
            state.TransmitFrameBits?.ToArray(),
            state.TransmitFrameBitIndex)).ToArray());

    public void RestoreState(Z80SioState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Channels is null || state.Channels.Count != ChannelCount)
            throw new ArgumentException("A Z80 SIO state must contain exactly two channels.", nameof(state));

        foreach (var channel in state.Channels)
        {
            if (channel is null || channel.WriteRegisters is null || channel.WriteRegisters.Length != 8)
                throw new ArgumentException("Each Z80 SIO channel state must contain eight write registers.", nameof(state));
            if (channel.ReceiveBuffer is null || channel.ReceiveBuffer.Count > RxBufferDepth)
                throw new ArgumentException("The Z80 SIO receive buffer is full or invalid.", nameof(state));
            if (channel.TransmitBuffer is null || channel.TransmitBuffer.Count > TxBufferDepth)
                throw new ArgumentException("The Z80 SIO transmit buffer is full or invalid.", nameof(state));
            if (channel.TransmitBitIndex < 0 || (channel.TransmitBits is not null && channel.TransmitBitIndex > channel.TransmitBits.Count))
                throw new ArgumentException("The Z80 SIO transmit bit state is invalid.", nameof(state));
            if (channel.TransmitFrameBitIndex < 0 || (channel.TransmitFrameBits is not null && channel.TransmitFrameBitIndex > channel.TransmitFrameBits.Count))
                throw new ArgumentException("The Z80 SIO SDLC transmit bit state is invalid.", nameof(state));
            if (channel.RegisterPointer is < 0 or > 7 || channel.ReadyAtTState < 0)
                throw new ArgumentException("The Z80 SIO channel state contains an invalid timing or register pointer.", nameof(state));
        }

        _currentTState = state.CurrentTState;
        BaudRate = state.BaudRate;
        _wr2 = state.InterruptVector;
        _interruptInService = state.InterruptInService;
        InterruptInputEnabled = state.InterruptInputEnabled;
        for (var index = 0; index < ChannelCount; index++)
        {
            var source = state.Channels[index];
            var target = _channels[index];
            Array.Copy(source.WriteRegisters, target.Wr, target.Wr.Length);
            target.Pointer = source.RegisterPointer;
            target.Overrun = source.Overrun;
            target.RxInterruptArmed = source.RxInterruptArmed;
            target.TxInterruptPending = source.TxInterruptPending;
            target.ExtStatusInterruptPending = source.ExtStatusInterruptPending;
            target.TransmitByte = source.TransmitByte;
            target.TransmitBits = source.TransmitBits?.ToArray();
            target.TransmitBitIndex = source.TransmitBitIndex;
            target.TransmitFrameBits = source.TransmitFrameBits?.ToArray();
            target.TransmitFrameBitIndex = source.TransmitFrameBitIndex;
            target.TransmitReadyAtTState = source.TransmitReadyAtTState;
            target.Tx.Clear();
            foreach (var transmitted in source.TransmitBuffer)
                target.Tx.Enqueue(transmitted);
            target.ReadyAtTState = source.ReadyAtTState;
            target.CtsAsserted = source.CtsAsserted;
            target.DcdAsserted = source.DcdAsserted;
            target.BreakDetected = source.BreakDetected;
            target.Mode = source.Mode;
            target.SyncWord = source.SyncWord;
            target.SyncAcquired = source.SyncAcquired;
            target.SyncPendingByte = source.SyncPendingByte;
            target.AutoEchoEnabled = source.AutoEchoEnabled;
            target.LocalLoopbackEnabled = source.LocalLoopbackEnabled;
            target.ReceiveBits.Clear();
            if (source.ReceiveBits is not null)
                target.ReceiveBits.AddRange(source.ReceiveBits);
            target.SdlcReceiveBits.Clear();
            if (source.SdlcReceiveBits is not null)
                target.SdlcReceiveBits.AddRange(source.SdlcReceiveBits);
            target.Rx.Clear();
            foreach (var received in source.ReceiveBuffer)
                target.Rx.Enqueue(new RxByte(received.Value, received.ParityError, received.FramingError));
        }
    }

    public byte GetStatus(int channel) => ComputeRr0(GetChannel(channel));

    public byte ReadPort(ushort port)
    {
        var offset = port - _basePort;
        return offset switch
        {
            0 => ReadData(ChannelA),
            1 => ReadData(ChannelB),
            2 => ReadControl(ChannelA),
            3 => ReadControl(ChannelB),
            _ => Profile.InvalidReadValue,
        };
    }

    public void WritePort(ushort port, byte value)
    {
        var offset = port - _basePort;
        switch (offset)
        {
            case 0: WriteData(ChannelA, value); break;
            case 1: WriteData(ChannelB, value); break;
            case 2: WriteControl(ChannelA, value); break;
            case 3: WriteControl(ChannelB, value); break;
        }
    }

    public bool TryConsumePendingInterrupt(out byte vectorByte)
    {
        var pending = FindHighestPendingInterrupt();
        if (!InterruptInputEnabled || _interruptInService || pending is not (int channel, var kind))
        {
            vectorByte = 0;
            return false;
        }

        vectorByte = ComputeVector(channel, kind);
        var state = _channels[channel];
        switch (kind)
        {
            case InterruptKind.Receive when RxInterruptMode(state) == 1:
                state.RxInterruptArmed = false;
                break;
            case InterruptKind.Transmit:
                state.TxInterruptPending = false;
                break;
            case InterruptKind.ExternalStatus:
                state.ExtStatusInterruptPending = false;
                break;
        }
        return true;
    }

    public bool TryAcknowledgeInterrupt(out byte vector)
    {
        vector = 0;
        if (!InterruptInputEnabled || _interruptInService || !TryConsumePendingInterrupt(out vector))
            return false;

        _interruptInService = true;
        return true;
    }

    public void NotifyReti() => _interruptInService = false;

    public void CompleteInterrupt() => NotifyReti();

    public void Reset()
    {
        foreach (var state in _channels)
        {
            state.Rx.Clear();
            state.Tx.Clear();
            Array.Clear(state.Wr);
            state.Pointer = 0;
            state.Overrun = false;
            state.RxInterruptArmed = true;
            state.TxInterruptPending = false;
            state.ExtStatusInterruptPending = false;
            state.TransmitByte = null;
            state.TransmitBits = null;
            state.TransmitBitIndex = 0;
            state.TransmitFrameBits = null;
            state.TransmitFrameBitIndex = 0;
            state.ReceiveBits.Clear();
            state.SdlcReceiveBits.Clear();
            state.TransmitReadyAtTState = 0;
            state.ReadyAtTState = 0;
            state.CtsAsserted = true;
            state.DcdAsserted = true;
            state.BreakDetected = false;
            state.Mode = Z80SioFrameMode.Async;
            state.SyncWord = 0;
            state.SyncAcquired = true;
            state.SyncPendingByte = null;
            state.AutoEchoEnabled = false;
            state.LocalLoopbackEnabled = false;
        }

        _wr2 = 0;
        _currentTState = 0;
        _interruptInService = false;
        InterruptInputEnabled = true;
    }

    private byte ReadData(int channel)
    {
        var state = _channels[channel];
        if (!IsHeadReady(state))
            return 0;

        var value = state.Rx.Dequeue().Value;
        if (state.Rx.Count > 0)
            state.ReadyAtTState = ScheduleAt(_currentTState, CharacterTStates(state));
        return value;
    }

    private void WriteData(int channel, byte value)
    {
        var state = _channels[channel];
        if (!IsTxEnabled(state) || !state.CtsAsserted)
            return;

        if (state.Tx.Count >= TxBufferDepth || state.TransmitFrameBits is not null)
            return;

        state.TxInterruptPending = false;
        state.Tx.Enqueue(value);
        StartNextTransmitter(state);
    }

    private byte ReadControl(int channel)
    {
        var state = _channels[channel];
        var register = state.Pointer;
        state.Pointer = 0;
        return register switch
        {
            0 => ComputeRr0(state),
            1 => ComputeRr1(state),
            2 => ComputeRr2(channel),
            3 => Profile.SupportsRr3 ? ComputeRr3() : Profile.UnsupportedRegisterReadValue,
            _ => Profile.UnsupportedRegisterReadValue,
        };
    }

    private void WriteControl(int channel, byte value)
    {
        var state = _channels[channel];
        if (state.Pointer == 0)
        {
            ExecuteWr0(state, (value >> 3) & 0x07);
            state.Pointer = value & 0x07;
            return;
        }

        if (state.Pointer == 2)
            _wr2 = value;
        else if (state.Pointer is >= 1 and <= 7)
            WriteRegister(channel, state, state.Pointer, value);
        state.Pointer = 0;
    }

    private void WriteRegister(int channel, Channel state, int register, byte value)
    {
        if (register == 5)
        {
            var wasBreakAsserted = (state.Wr[5] & Wr5SendBreak) != 0;
            var isBreakAsserted = (value & Wr5SendBreak) != 0;
            if (wasBreakAsserted != isBreakAsserted)
            {
                if (IsExtStatusInterruptEnabled(state))
                    state.ExtStatusInterruptPending = true;
                BreakChanged?.Invoke(channel, isBreakAsserted);
            }
        }

        state.Wr[register] = value;
        if (register == 6)
            state.SyncWord = (ushort)((value << 8) | state.Wr[7]);
        else if (register == 7)
            state.SyncWord = (ushort)((state.Wr[6] << 8) | value);
        if (register == 5)
            UpdateModemOutputs(channel, state);
    }

    private static void UpdateModemOutputs(int channel, Channel state)
    {
        state.Binding?.Channel.SetRts((state.Wr[5] & Wr5Rts) != 0);
        state.Binding?.Channel.SetDtr((state.Wr[5] & Wr5Dtr) != 0);
    }

    private void ExecuteWr0(Channel state, int command)
    {
        switch (command)
        {
            case 0b011:
                state.Rx.Clear();
                state.Tx.Clear();
                Array.Clear(state.Wr);
                state.Overrun = false;
                state.RxInterruptArmed = true;
                state.TxInterruptPending = false;
                state.ExtStatusInterruptPending = false;
                state.TransmitByte = null;
                state.TransmitBits = null;
                state.TransmitBitIndex = 0;
                state.TransmitFrameBits = null;
                state.TransmitFrameBitIndex = 0;
                state.ReceiveBits.Clear();
                state.SdlcReceiveBits.Clear();
                state.TransmitReadyAtTState = 0;
                state.BreakDetected = false;
                state.Mode = Z80SioFrameMode.Async;
                state.SyncWord = 0;
                state.SyncAcquired = true;
                state.SyncPendingByte = null;
                state.AutoEchoEnabled = false;
                state.LocalLoopbackEnabled = false;
                UpdateModemOutputs(Array.IndexOf(_channels, state), state);
                break;
            case 0b100:
                state.RxInterruptArmed = true;
                break;
            case 0b101:
                state.TxInterruptPending = false;
                break;
            case 0b110:
                state.Overrun = false;
                break;
        }
    }

    private byte ComputeRr0(Channel state)
    {
        var value = (byte)0;
        if (state.Tx.Count == 0)
            value |= Rr0TxEmpty;
        if (IsHeadReady(state))
            value |= Rr0RxAvailable;
        if (state.DcdAsserted)
            value |= Rr0Dcd;
        if (state.CtsAsserted)
            value |= Rr0Cts;
        return value;
    }

    private static byte ComputeRr1(Channel state)
    {
        var value = (byte)0;
        if (state.TransmitByte is null && state.Tx.Count == 0)
            value |= Rr1AllSent;
        if (state.Overrun)
            value |= Rr1OverrunError;
        if (state.BreakDetected)
            value |= Rr1BreakDetected;
        if (state.Rx.Count > 0)
        {
            var rx = state.Rx.Peek();
            if (rx.ParityError) value |= Rr1ParityError;
            if (rx.FramingError) value |= Rr1FramingError;
        }
        return value;
    }

    private byte ComputeRr2(int channel)
    {
        if (channel == ChannelA || (GetChannel(ChannelB).Wr[1] & Wr1StatusAffectsVector) == 0)
            return _wr2;

        var pending = FindHighestPendingInterrupt();
        return pending is (int source, var kind) ? ComputeVector(source, kind) : (byte)(_wr2 & 0xF1);
    }

    private bool IsHeadReady(Channel state) => IsRxEnabled(state)
        && state.Rx.Count > 0
        && _currentTState >= state.ReadyAtTState;

    private static bool IsRxEnabled(Channel state) => (state.Wr[3] & Wr3RxEnable) != 0;

    private static bool IsTxEnabled(Channel state) => (state.Wr[5] & Wr5TxEnable) != 0;

    private int RxInterruptMode(Channel state) => (state.Wr[1] & Wr1RxInterruptMask) >> Wr1RxInterruptShift;

    private bool IsRxInterruptPending(Channel state)
    {
        var mode = RxInterruptMode(state);
        return mode != 0 && IsHeadReady(state) && (mode != 1 || state.RxInterruptArmed);
    }

    private bool IsTxInterruptPending(Channel state) =>
        (state.Wr[1] & Wr1TxInterruptEnable) != 0 && state.TxInterruptPending;

    private void AdvanceTransmitters()
    {
        foreach (var state in _channels)
        {
            while (true)
            {
                if (state.TransmitFrameBits is { } frameBits && state.Binding?.Channel is IZ80SioBitChannel frameChannel)
                {
                    if (_currentTState < state.TransmitReadyAtTState)
                        break;

                    frameChannel.WriteTxDBit(frameBits[state.TransmitFrameBitIndex++]);
                    if (state.TransmitFrameBitIndex < frameBits.Count)
                    {
                        state.TransmitReadyAtTState = ScheduleAt(state.TransmitReadyAtTState, BitTStates(state, transmit: true));
                        continue;
                    }

                    state.TransmitFrameBits = null;
                    state.TransmitFrameBitIndex = 0;
                    continue;
                }

                if (state.TransmitByte is null || _currentTState < state.TransmitReadyAtTState)
                    break;

                if (state.TransmitBits is { } bits && state.Binding?.Channel is IZ80SioBitChannel bitChannel)
                {
                    bitChannel.WriteTxDBit(bits[state.TransmitBitIndex++]);
                    if (state.TransmitBitIndex < bits.Count)
                    {
                        state.TransmitReadyAtTState = ScheduleAt(state.TransmitReadyAtTState, BitTStates(state, transmit: true));
                        continue;
                    }
                }

                var completedAt = state.TransmitReadyAtTState;
                var value = state.TransmitByte.Value;
                state.TransmitByte = null;
                state.TransmitBits = null;
                state.TransmitBitIndex = 0;
                Transmitted?.Invoke(Array.IndexOf(_channels, state), value);
                if (state.Binding?.Channel is not IZ80SioBitChannel)
                    state.Binding?.Channel.WriteTxD(value);
                if (state.LocalLoopbackEnabled)
                    EnqueueRxByteCore(Array.IndexOf(_channels, state), value, false, false, allowAutoEcho: false);
                if (state.Tx.Count == 0 && (state.Wr[1] & Wr1TxInterruptEnable) != 0)
                    state.TxInterruptPending = true;
                StartNextTransmitter(state, completedAt);
            }
        }
    }

    private void StartNextTransmitter(Channel state, long? origin = null)
    {
        if (state.TransmitByte is not null || !state.CtsAsserted || state.Tx.Count == 0)
            return;

        state.TransmitByte = state.Tx.Dequeue();
        if (state.Binding?.Channel is IZ80SioBitChannel)
        {
            state.TransmitBits = state.Mode == Z80SioFrameMode.Async
                ? Z80SioBitCodec.EncodeAsync(state.TransmitByte.Value, GetAsyncFormat(state, transmit: true))
                : EncodeSynchronousByte(state.TransmitByte.Value);
            state.TransmitBitIndex = 0;
            state.TransmitReadyAtTState = ScheduleAt(origin ?? _currentTState, BitTStates(state, transmit: true));
        }
        else
        {
            state.TransmitBits = null;
            state.TransmitBitIndex = 0;
            state.TransmitReadyAtTState = ScheduleAt(origin ?? _currentTState, CharacterTStates(state, transmit: true));
        }
    }

    private static bool IsExtStatusInterruptEnabled(Channel state) =>
        (state.Wr[1] & Wr1ExtStatusInterruptEnable) != 0;

    private static bool AcceptsSynchronizedByte(Channel state, byte value)
    {
        switch (state.Mode)
        {
            case Z80SioFrameMode.Async:
            case Z80SioFrameMode.Sdlc:
                return true;
            case Z80SioFrameMode.ExternalSync:
                return state.SyncAcquired;
            case Z80SioFrameMode.Sync8:
                if (state.SyncAcquired) return true;
                state.SyncAcquired = value == (byte)state.SyncWord;
                return false;
            case Z80SioFrameMode.Sync16:
                if (state.SyncAcquired) return true;
                if (state.SyncPendingByte is not byte first)
                {
                    state.SyncPendingByte = value;
                    return false;
                }
                state.SyncPendingByte = value;
                state.SyncAcquired = (ushort)((first << 8) | value) == state.SyncWord;
                if (state.SyncAcquired) state.SyncPendingByte = null;
                return false;
            default:
                return true;
        }
    }

    private static bool IsExtStatusInterruptPending(Channel state) =>
        IsExtStatusInterruptEnabled(state) && state.ExtStatusInterruptPending;

    private static Z80SioAsyncFormat GetAsyncFormat(Channel state, bool transmit = false) => new(
        transmit
            ? DecodeDataBits((byte)((state.Wr[5] & Wr5TxBitsMask) >> 5))
            : DecodeDataBits((byte)(state.Wr[3] & Wr3RxBitsMask)),
        (state.Wr[4] & Wr4ParityEnable) != 0,
        (state.Wr[4] & Wr4ParityEven) != 0,
        DecodeStopBits((byte)((state.Wr[4] & Wr4StopBitsMask) >> 2)));

    private static IReadOnlyList<bool> EncodeSynchronousByte(byte value) =>
        Enumerable.Range(0, 8).Select(bit => (value & (1 << bit)) != 0).ToArray();

    private static byte ToByte(IReadOnlyList<bool> bits)
    {
        byte value = 0;
        for (var bit = 0; bit < 8; bit++)
            if (bits[bit]) value |= (byte)(1 << bit);
        return value;
    }

    private (int channel, InterruptKind kind)? FindHighestPendingInterrupt()
    {
        for (var channel = 0; channel < ChannelCount; channel++)
        {
            if (IsRxInterruptPending(_channels[channel])) return (channel, InterruptKind.Receive);
            if (IsTxInterruptPending(_channels[channel])) return (channel, InterruptKind.Transmit);
            if (IsExtStatusInterruptPending(_channels[channel])) return (channel, InterruptKind.ExternalStatus);
        }
        return null;
    }

    private byte ComputeVector(int channel, InterruptKind kind)
    {
        if ((_channels[ChannelB].Wr[1] & Wr1StatusAffectsVector) == 0)
            return _wr2;

        var source = channel == ChannelA ? 0b100 : 0;
        if (kind == InterruptKind.Receive)
            source |= 0b010;
        return (byte)((_wr2 & 0xF1) | (source << 1));
    }

    private byte ComputeRr3()
    {
        var value = (byte)0;
        if (IsRxInterruptPending(_channels[ChannelB])) value |= Rr3BRxInterrupt;
        if (IsTxInterruptPending(_channels[ChannelB])) value |= Rr3BTxInterrupt;
        if (IsExtStatusInterruptPending(_channels[ChannelB])) value |= Rr3BExtStatusInterrupt;
        if (IsRxInterruptPending(_channels[ChannelA])) value |= Rr3ARxInterrupt;
        if (IsTxInterruptPending(_channels[ChannelA])) value |= Rr3ATxInterrupt;
        if (IsExtStatusInterruptPending(_channels[ChannelA])) value |= Rr3AExtStatusInterrupt;
        return value;
    }

    private static int DecodeDataBits(byte value) => value switch
    {
        0b00 => 5,
        0b01 => 7,
        0b10 => 6,
        _ => 8,
    };

    private static int DecodeClockMultiplier(byte value) => value switch
    {
        0b00 => 1,
        0b01 => 16,
        0b10 => 32,
        _ => 64,
    };

    private static double DecodeStopBits(byte value) => value switch
    {
        0b01 => 1,
        0b10 => 1.5,
        0b11 => 2,
        _ => 1,
    };

    private Channel GetChannel(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, ChannelCount);
        return _channels[channel];
    }
}
