namespace PetEmulator.Vic20.Tape;

/// <summary>
/// Optional diagnostic sampler for the physical VIC-20 cassette WRITE line. It records the
/// number of sampled CPU cycles between PB3 transitions. The result is intentionally not fed
/// into LOAD: the real ROM's interrupt-dispatched PB3 waveform still needs a proven decoder.
/// </summary>
public sealed class Vic20CassetteWriteRecorder
{
    private readonly Func<bool> _readLevel;
    private readonly List<int> _pulseCycles = [];
    private bool _lastLevel;
    private int _cyclesSinceTransition;

    public Vic20CassetteWriteRecorder(Func<bool> readLevel)
    {
        _readLevel = readLevel ?? throw new ArgumentNullException(nameof(readLevel));
    }

    public bool IsRecording { get; private set; }

    /// <summary>Complete transition intervals captured since the last <see cref="Begin"/>.</summary>
    public IReadOnlyList<int> PulseCycles => _pulseCycles;

    public void Begin()
    {
        _pulseCycles.Clear();
        _lastLevel = _readLevel();
        _cyclesSinceTransition = 0;
        IsRecording = true;
    }

    /// <summary>Samples PB3 once. Call once per emulated CPU cycle.</summary>
    public void Tick()
    {
        if (!IsRecording)
            return;

        _cyclesSinceTransition++;
        var level = _readLevel();
        if (level == _lastLevel)
            return;

        _pulseCycles.Add(_cyclesSinceTransition);
        _cyclesSinceTransition = 0;
        _lastLevel = level;
    }

    public IReadOnlyList<int> End()
    {
        IsRecording = false;
        return PulseCycles;
    }
}
