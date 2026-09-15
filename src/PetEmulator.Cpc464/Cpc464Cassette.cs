namespace PetEmulator.Cpc464;

/// <summary>A cassette supporting CDT pulse streams and the synthetic byte format used by the
/// CSAVE/CLOAD subsystem round-trip test. Synthetic bits are one HIGH pulse whose width encodes 0
/// or 1 (<see cref="ZeroBitTicks"/>/<see cref="OneBitTicks"/>), followed by a fixed LOW gap.
/// <para>One <see cref="Tick"/> call is one microsecond: <see cref="Cpc464Bus.Tick"/> already calls
/// it once per Gate Array clock (1 MHz), so callers driving a T-state budget must pass 4x the
/// desired tick count.</para></summary>
public sealed class Cpc464Cassette
{
    public const int ZeroBitTicks = 200;
    public const int OneBitTicks = 400;
    public const int GapTicks = 200;

    private byte[] _tape = [];
    private int _byteIndex, _bitIndex = 7, _remaining;
    private bool _playbackHigh, _started;
    private IReadOnlyList<int> _pulseTicks = [];
    private int _pulseIndex;
    private bool _pulsePlayback;

    private readonly List<(int Ticks, bool Level)> _recorded = [];
    private int _recordTicks;
    private bool _recordLevel;
    private bool _recording;

    public bool MotorOn { get; private set; }
    public bool AtEndOfTape => _pulsePlayback ? _pulseIndex >= _pulseTicks.Count : _byteIndex >= _tape.Length;
    public bool Signal { get; private set; }

    public void Reset()
    {
        MotorOn = false;
        _recorded.Clear();
        _recordTicks = 0;
        _recording = false;
        Rewind();
    }

    /// <summary>Mounts a tape for playback (a byte stream, MSB first per byte - the same shape a
    /// prior <see cref="TryGetRecordedTape"/> call returns).</summary>
    public void LoadTape(byte[] tape)
    {
        _tape = tape;
        _pulsePlayback = false;
        Rewind();
    }

    /// <summary>Mounts alternating pulse widths measured in one-microsecond cassette ticks.</summary>
    public void LoadPulses(IReadOnlyList<int> pulseTicks)
    {
        ArgumentNullException.ThrowIfNull(pulseTicks);
        if (pulseTicks.Any(ticks => ticks <= 0))
            throw new ArgumentOutOfRangeException(nameof(pulseTicks), "Pulse widths must be positive.");
        _pulseTicks = pulseTicks.ToArray();
        _pulsePlayback = true;
        Rewind();
    }

    private void Rewind()
    {
        _byteIndex = 0;
        _bitIndex = 7;
        _remaining = 0;
        _started = false;
        _playbackHigh = false;
        _pulseIndex = 0;
        if (_pulsePlayback && _pulseTicks.Count > 0)
            _remaining = _pulseTicks[0];
        Signal = false;
    }

    public void SetMotor(bool enabled)
    {
        MotorOn = enabled;
        if (!enabled && _recording) { FlushPulse(); _recording = false; }
    }

    public bool ReadSignal() => !MotorOn || AtEndOfTape ? true : Signal;

    /// <summary>Drives the write head (real Amstrad PPI Port C bit 5) while the motor is running;
    /// ignored otherwise, matching the real cassette relay cutting the write circuit.</summary>
    public void WriteData(bool level)
    {
        if (!MotorOn) return;
        if (!_recording) { _recorded.Clear(); _recordTicks = 0; _recordLevel = level; _recording = true; return; }
        if (level == _recordLevel) return;
        FlushPulse();
        _recordLevel = level;
    }

    private void FlushPulse()
    {
        if (_recordTicks > 0) _recorded.Add((_recordTicks, _recordLevel));
        _recordTicks = 0;
    }

    public void Tick()
    {
        if (_recording) _recordTicks++;
        if (!MotorOn || AtEndOfTape) return;
        if (_pulsePlayback)
        {
            if (--_remaining > 0) return;
            Signal = !Signal;
            _pulseIndex++;
            if (!AtEndOfTape) _remaining = _pulseTicks[_pulseIndex];
            return;
        }
        if (!_started) { _started = true; EnterBit(); }
        if (--_remaining > 0) return;
        Advance();
    }

    private void EnterBit()
    {
        var bit = (_tape[_byteIndex] >> _bitIndex) & 1;
        _playbackHigh = true;
        Signal = true;
        _remaining = bit == 1 ? OneBitTicks : ZeroBitTicks;
    }

    private void Advance()
    {
        if (_playbackHigh)
        {
            _playbackHigh = false;
            Signal = false;
            _remaining = GapTicks;
            return;
        }
        if (--_bitIndex < 0) { _bitIndex = 7; _byteIndex++; }
        if (!AtEndOfTape) EnterBit();
    }

    /// <summary>Reconstructs the bytes just recorded from the HIGH-pulse widths captured while the
    /// motor was on, or <see langword="false"/> if recording is still in progress, nothing was
    /// recorded, or the bit count isn't a whole number of bytes.</summary>
    public bool TryGetRecordedTape(out byte[] data)
    {
        data = [];
        if (_recording || _recorded.Count == 0) return false;

        const int Threshold = (ZeroBitTicks + OneBitTicks) / 2;
        var bits = new List<bool>();
        foreach (var (ticks, level) in _recorded)
            if (level) bits.Add(ticks >= Threshold);

        if (bits.Count == 0 || bits.Count % 8 != 0) return false;
        data = new byte[bits.Count / 8];
        for (var i = 0; i < data.Length; i++)
        for (var bit = 0; bit < 8; bit++)
            data[i] = (byte)((data[i] << 1) | (bits[i * 8 + bit] ? 1 : 0));
        return true;
    }
}
