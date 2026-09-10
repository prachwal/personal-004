using PetEmulator.Pet.Chips;

namespace PetEmulator.Vic20.Tape;

/// <summary>
/// Drives the VIC-20's cassette port from a decoded pulse-cycle stream (playback/LOAD) and
/// captures the real KERNAL's own output pulses during SAVE (recording/WRITE). Mirrors
/// <c>PetEmulator.Pet.Tape.PetDatasette</c>'s playback contract (same Tick/LoadTape/PressPlay
/// shape, same reuse of <c>PetEmulator.Pet.Tape.PetTapeCassetteFormat</c>'s pulse encoding - its
/// short/medium/long cycle-width ranges (352/512/672) fall squarely inside the 296-424/440-576/
/// 600-744 microsecond ranges Commodore's own KERNAL source documents for these four pulse kinds,
/// confirming the SAME physical encoding really is shared across the cassette-based machines,
/// just measured in each one's own native CPU cycles).
///
/// Real wiring - corrected from an earlier, wrong guess (see docs/vic20-tape.md's "wiring
/// corrected" section) against a REAL, labeled VIC-20 KERNAL/BASIC ROM disassembly (Lee Davison,
/// 2005-2012, with Simon Rowe's enhancements - a full symbol table, not blind register-address
/// guessing):
///
/// - VIA2 CA1 ($912C PCR bit 0) = cassette READ line - NOT VIA1 (VIA1's CA1 is the [RESTORE] key,
///   confirmed by the same source; every read-pulse edge this class raised there before the fix
///   was landing on an unrelated key line, never reaching the real tape-read ISR at all - the
///   root cause behind this session's earlier "plays fully through, KERNAL never decodes it"
///   result).
/// - VIA1 PA6 ($9111/$911F bit 6, active low) = cassette switch sense - NOT PA7 (confirmed by the
///   real IRQ handler: <c>LDA VIA1PA2; AND #$40; BEQ ...</c> branches on bit 6 being clear).
/// - VIA1 CA2 (PCR bits 1-3, manual-output mode, bit 1 = level) = cassette motor control, active
///   low - this one was already right.
/// - VIA2 PB3 ($9120 bit 3, output) = cassette WRITE line - real SAVE toggles it under VIA2 Timer2
///   -interrupt timing, not a level a caller polls; this class doesn't replicate that write
///   algorithm, it just watches the pin (see <see cref="Tick"/>) and records every edge's
///   cycle-gap, exactly like a real deck's read head would off a live WRITE signal.
/// </summary>
public sealed class Vic20Datasette
{
    private readonly Via6522 _via1;
    private readonly Via6522 _via2;
    private IReadOnlyList<int> _pulseCycles = [];
    private int _pulseIndex;
    private int _cyclesUntilNextEdge;

    private readonly List<int> _recordedPulses = [];
    private bool _recording;
    private bool _lastPb3;
    private long _cyclesSinceLastPb3Edge;

    public Vic20Datasette(Via6522 via1, Via6522 via2)
    {
        _via1 = via1 ?? throw new ArgumentNullException(nameof(via1));
        _via2 = via2 ?? throw new ArgumentNullException(nameof(via2));
    }

    /// <summary>CA2 configured as a manual-output line (PCR bits 1-3 select an output mode,
    /// $08-$0E) and held low - active-low motor-on, same convention as PET's PIA1 CB2.</summary>
    public bool MotorOn => (_via1.PCR & 0x0E) >= 0x08 && !_via1.CA2Output;

    public bool HasTape => _pulseCycles.Count > 0;

    /// <summary>Whether the (emulated) physical PLAY button is currently held down - see
    /// <c>PetDatasette.PlayPressed</c>'s doc comment for the real-hardware reasoning this mirrors.</summary>
    public bool PlayPressed { get; private set; }

    /// <summary>Cassette switch sense (VIA1 PA6, active low): closes whenever a transport button
    /// is physically held down, regardless of whether a tape is even loaded - matching real
    /// hardware. Same idiom as <c>PetDatasette.Sense</c>; kept as its own property (rather than
    /// inlining <see cref="PlayPressed"/> everywhere) so a caller reads intent, not mechanism.</summary>
    public bool Sense => PlayPressed;

    /// <summary>True once every pulse in the loaded tape has been played past.</summary>
    public bool IsAtEnd => _pulseIndex >= _pulseCycles.Count;

    /// <summary>Display name of the currently loaded tape, or null when none was given.</summary>
    public string? TapeName { get; private set; }

    /// <summary>True while <see cref="BeginRecording"/> has been called and <see cref="StopRecording"/>
    /// hasn't (yet) - see those methods.</summary>
    public bool IsRecording => _recording;

    /// <summary>Every falling edge captured off VIA2 PB3 since the last <see cref="BeginRecording"/>,
    /// as cycle gaps - the same shape <see cref="LoadTape"/> takes, so a caller can play a
    /// captured recording straight back with no conversion. KNOWN GAP (see docs/vic20-tape.md):
    /// PB3 is also the keyboard column-3 line (a real, shared-pin hardware quirk, not a bug here),
    /// so recording started before the real SAVE command's own <c>SEI</c> takes effect captures
    /// genuine but spurious keyboard-scan noise as large leading entries - a caller currently has
    /// to trim those (e.g. drop everything through the last implausibly-large gap) before this is
    /// safe to play back; round-tripping a captured recording through a fresh LOAD hasn't been
    /// proven end-to-end yet, unlike the header/payload LOAD path (see
    /// <c>roms/vic20/test-tapes/hello-vic.tap</c>).</summary>
    public IReadOnlyList<int> RecordedPulseCycles => _recordedPulses;

    public void LoadTape(IReadOnlyList<int> pulseCycles, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(pulseCycles);
        _pulseCycles = pulseCycles;
        TapeName = name;
        PlayPressed = false; // loading a fresh tape doesn't press play for you - matches a real deck
        Rewind();
    }

    /// <summary>Ejects whatever tape is loaded: clears its pulses/name, releases play, and rewinds.</summary>
    public void Eject()
    {
        _pulseCycles = [];
        TapeName = null;
        PlayPressed = false;
        Rewind();
    }

    public void PressPlay() => PlayPressed = true;

    public void Stop() => PlayPressed = false;

    public void Rewind()
    {
        _pulseIndex = 0;
        _cyclesUntilNextEdge = _pulseCycles.Count > 0 ? _pulseCycles[0] : 0;
    }

    public void Reset() => Rewind();

    /// <summary>Starts capturing VIA2 PB3 edges into <see cref="RecordedPulseCycles"/> - the
    /// caller still has to <see cref="PressPlay"/> (real SAVE waits on the same sense line LOAD
    /// does) and type the real SAVE command for anything to actually appear. Clears any
    /// previously recorded pulses.</summary>
    public void BeginRecording()
    {
        _recordedPulses.Clear();
        _recording = true;
        _lastPb3 = Pb3Level;
        _cyclesSinceLastPb3Edge = 0;
    }

    public void StopRecording() => _recording = false;

    private bool Pb3Level => (_via2.PortBOutput & 0x08) != 0;

    /// <summary>Advances the tape by one CPU cycle: plays back the loaded tape's pulses onto VIA2
    /// CA1 (LOAD) and/or captures VIA2 PB3 edges into <see cref="RecordedPulseCycles"/>
    /// (SAVE) - either, both, or neither can be active at once (mirrors two independent physical
    /// signals a real deck carries at the same time).</summary>
    public void Tick()
    {
        // PA6 reflects the physical PLAY sense switch every cycle, independent of the motor/tape
        // state below (a real switch closes the moment PLAY is pressed, tape moving or not).
        _via1.PortAInput = (byte)(PlayPressed ? (_via1.PortAInput & ~0x40) : (_via1.PortAInput | 0x40));

        if (_recording)
        {
            _cyclesSinceLastPb3Edge++;
            var level = Pb3Level;
            if (level != _lastPb3)
            {
                // Only FALLING edges are meaningful pulse boundaries - matches CA1's own
                // convention on the read side (real hardware/KERNAL only times successive
                // falling edges, per the KERNAL's own doc block on the four pulse symbols; a
                // rising edge is just the brief strobe settling back high, not a real symbol
                // boundary - see this class's own playback Tick() below, which produces the exact
                // same brief high-low-high shape per pulse, not a sustained half-cycle level).
                // The counter is therefore only ever reset on a falling edge - a rising edge in
                // between must NOT reset it, or the recorded value would only span the brief low
                // phase instead of the full falling-to-falling period a real pulse actually is.
                if (!level)
                {
                    _recordedPulses.Add((int)_cyclesSinceLastPb3Edge);
                    _cyclesSinceLastPb3Edge = 0;
                }
                _lastPb3 = level;
            }
        }

        if (!MotorOn || !PlayPressed || IsAtEnd)
            return;

        if (--_cyclesUntilNextEdge > 0)
            return;

        _via2.CA1 = true; // ensure a high baseline first (no-op if already high) so the next line is a guaranteed transition
        _via2.CA1 = false;
        _via2.CA1 = true;
        _pulseIndex++;
        if (!IsAtEnd)
            _cyclesUntilNextEdge = _pulseCycles[_pulseIndex];
    }
}
