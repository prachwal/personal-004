using PetEmulator.CpuZ80.Bus;

namespace PetEmulator.Trs80;

public sealed class Trs80CassettePlayer : IIoBus
{
    private const byte MotorBit = 0x04;
    private const byte EarBit = 0x80;
    private readonly byte[] _tape;
    private readonly List<(int DurationTStates, byte Level)> _recordedPulses = [];
    private int _byteIndex, _bitIndex = 7, _segmentIndex, _remaining;
    private bool _pulsePending, _started, _recording;
    private int _recordDuration;
    private byte _recordLevel;

    public Trs80CassettePlayer(byte[]? tape = null) => _tape = tape ?? [];
    public bool MotorOn { get; private set; }
    public bool AtEndOfTape => _byteIndex >= _tape.Length;

    public void Tick(int tStates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tStates);
        if (_recording) _recordDuration += tStates;
        if (!MotorOn || AtEndOfTape) return;
        if (!_started) { _started = true; EnterSegment(CurrentSegments()[0], ExtraDelayMicros(0)); }
        while (tStates > 0 && !AtEndOfTape)
        {
            if (tStates < _remaining) { _remaining -= tStates; return; }
            tStates -= _remaining;
            AdvanceSegment();
        }
    }

    public byte Read(byte port) => port == Trs80MemoryMap.CassettePort
        ? (_pulsePending ? ClearPulse() : (byte)0) : (byte)0xFF;

    public void Write(byte port, byte value)
    {
        if (port != Trs80MemoryMap.CassettePort) return;
        MotorOn = (value & MotorBit) != 0;
        ObserveWriteLevel((byte)(value & 0x03));
    }

    public bool TryGetRecordedCas(out byte[] data)
    {
        data = [];
        if (_recording || _recordedPulses.Count == 0) return false;

        // A "0" bit's waveform (see Trs80CassetteEncoding.ZeroBit) has exactly one rising edge per
        // cell; a "1" bit (OneBit) has two - a short ~128us pulse then, after a short low, a second,
        // longer pulse - so every 0->1 transition is a rising edge, but not every rising edge starts
        // a new bit: a "1" bit's second edge is still part of the bit its first edge started.
        // Collect edge positions (cumulative T-states) first, then group them by the gap to the next
        // edge: a short gap means "this edge and the next one both belong to one '1' bit"; a long
        // gap means "this edge is a lone '0' bit".
        var edgePositions = new List<int>();
        var position = 0;
        for (var i = 0; i + 1 < _recordedPulses.Count; i++)
        {
            position += _recordedPulses[i].DurationTStates;
            if (_recordedPulses[i].Level == 0 && _recordedPulses[i + 1].Level == 1)
                edgePositions.Add(position);
        }

        if (edgePositions.Count == 0) return false;

        // Splits a short intra-cell gap (~128us between a "1" bit's two edges) from a long
        // inter-cell gap (~1871-1999us to the next bit's own leading edge) - the two are nowhere
        // near this boundary, so it doesn't need to track OneBit/ZeroBit's exact timing.
        var shortGapThreshold = Trs80CassetteEncoding.ToTStates(500);
        List<bool> bits = [];
        var edgeIndex = 0;
        while (edgeIndex < edgePositions.Count)
        {
            var hasNext = edgeIndex + 1 < edgePositions.Count;
            var isOneBit = hasNext && edgePositions[edgeIndex + 1] - edgePositions[edgeIndex] <= shortGapThreshold;
            bits.Add(isOneBit);
            edgeIndex += isOneBit ? 2 : 1;
        }

        if (bits.Count == 0 || bits.Count % 8 != 0) return false;
        data = new byte[bits.Count / 8];
        for (var i = 0; i < data.Length; i++)
            for (var bit = 0; bit < 8; bit++)
                data[i] = (byte)((data[i] << 1) | (bits[i * 8 + bit] ? 1 : 0));
        return true;
    }

    byte IIoBus.Read(ushort port) => Read((byte)port);
    void IIoBus.Write(ushort port, byte value) => Write((byte)port, value);

    private byte ClearPulse() { _pulsePending = false; return EarBit; }
    private void ObserveWriteLevel(byte level)
    {
        if (MotorOn && !_recording) { _recordedPulses.Clear(); _recordDuration = 0; _recordLevel = level; _recording = true; return; }
        if (_recording && MotorOn && level != _recordLevel) { FlushPulse(); _recordLevel = level; }
        if (!MotorOn && _recording) { FlushPulse(); _recording = false; }
    }
    private void FlushPulse() { if (_recordDuration > 0) _recordedPulses.Add((_recordDuration, _recordLevel)); _recordDuration = 0; }
    private (double DurationMicros, bool High)[] CurrentSegments() =>
        ((_tape[_byteIndex] >> _bitIndex) & 1) != 0 ? Trs80CassetteEncoding.OneBit : Trs80CassetteEncoding.ZeroBit;
    private double ExtraDelayMicros(int segment) =>
        _bitIndex == 0 && segment == CurrentSegments().Length - 1 && _tape[_byteIndex] == Trs80CassetteEncoding.Sync
            ? Trs80CassetteEncoding.PostSyncExtraDelayMicros : 0;
    private void EnterSegment((double DurationMicros, bool High) segment, double extra)
    {
        if (segment.High) _pulsePending = true;
        _remaining = Math.Max(1, Trs80CassetteEncoding.ToTStates(segment.DurationMicros + extra));
    }
    private void AdvanceSegment()
    {
        var segments = CurrentSegments();
        if (++_segmentIndex < segments.Length) { EnterSegment(segments[_segmentIndex], ExtraDelayMicros(_segmentIndex)); return; }
        _segmentIndex = 0;
        if (--_bitIndex < 0) { _bitIndex = 7; _byteIndex++; }
        if (!AtEndOfTape) EnterSegment(CurrentSegments()[0], ExtraDelayMicros(0));
    }
}
