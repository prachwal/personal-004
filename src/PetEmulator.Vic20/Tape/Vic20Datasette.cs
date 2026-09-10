using PetEmulator.Pet.Chips;

namespace PetEmulator.Vic20.Tape;

/// <summary>
/// Drives the VIC-20's cassette port from a decoded pulse-cycle stream (playback/LOAD). SAVE is
/// handled at a higher level (<see cref="Vic20Machine"/> snapshots the real KERNAL's own header
/// buffer and program bytes once it detects a real SAVE dispatch, then hands the encoded result
/// straight to <see cref="LoadTape"/> - see <c>Vic20Machine</c>'s doc comment and
/// docs/vic20-tape.md's "Write (SAVE)" section for why: this class's first pass tried a literal
/// analog capture of VIA2 PB3's real write pulses, which genuinely worked at the wiring level but
/// never reliably round-tripped back through LOAD - real emulated interrupt-dispatch jitter on
/// each individual bit-toggle interrupt was enough to blur the two-level (~192/~352-cycle) FM
/// encoding <c>TPTOGLE</c> actually produces. Snapshotting the logical content (header + payload
/// bytes) and re-encoding it through the same <c>PetTapeCassetteFormat</c> pulse train LOAD
/// already decodes reliably sidesteps that entirely, at the cost of not literally reproducing the
/// real ROM's analog write timing bit-for-bit - a reasonable trade for "SAVE then LOAD it back
/// reliably works", which is what actually matters here.
///
/// Mirrors <c>PetEmulator.Pet.Tape.PetDatasette</c>'s playback contract (same Tick/LoadTape/
/// PressPlay shape, same reuse of <c>PetEmulator.Pet.Tape.PetTapeCassetteFormat</c>'s pulse
/// encoding - confirmed compatible with real VIC-20 hardware, not just PET: its short/medium/long
/// cycle-width ranges (352/512/672) fall squarely inside the 296-424/440-576/600-744 microsecond
/// ranges Commodore's own KERNAL source documents for these four pulse kinds).
///
/// Real wiring (see docs/vic20-tape.md for how this was found - a labeled KERNAL disassembly, not
/// a guess): VIA2 CA1 ($912C PCR bit 0) = cassette READ line (NOT VIA1 - VIA1's CA1 is the
/// [RESTORE] key). VIA1 PA6 ($9111/$911F bit 6, active low) = cassette switch sense (NOT PA7).
/// VIA1 CA2 (PCR bits 1-3, manual-output mode, bit 1 = level) = cassette motor control, active
/// low. VIA2 PB3 ($9120 bit 3, output) = cassette WRITE line - real hardware fact, not used
/// directly by this class (see above).
/// </summary>
public sealed class Vic20Datasette
{
    private readonly Via6522 _via1;
    private readonly Via6522 _via2;
    private IReadOnlyList<int> _pulseCycles = [];
    private int _pulseIndex;
    private int _cyclesUntilNextEdge;

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

    /// <summary>Display name of the currently loaded tape, or null when none was given. A tape
    /// "in the deck" for GUI/status purposes - including a freshly created blank one (see
    /// <see cref="NewBlankTape"/>) - always has a non-null name, even with zero pulses; see
    /// <see cref="Vic20DatasetteStatus"/>, which uses this (not <see cref="HasTape"/>) to decide
    /// whether to show "No tape".</summary>
    public string? TapeName { get; private set; }

    public void LoadTape(IReadOnlyList<int> pulseCycles, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(pulseCycles);
        _pulseCycles = pulseCycles;
        TapeName = name;
        PlayPressed = false; // loading a fresh tape doesn't press play for you - matches a real deck
        Rewind();
    }

    /// <summary>Puts a fresh, empty, writable tape "in the deck" - zero pulses, a real name (see
    /// <see cref="TapeName"/>'s doc comment), ready to receive a real SAVE (see
    /// <c>Vic20Machine</c>'s doc comment) and then be <see cref="LoadTape"/>-ed straight back.</summary>
    public void NewBlankTape(string name) => LoadTape([], name);

    /// <summary>Replaces the tape's content with what a real SAVE just wrote, without releasing
    /// PLAY - unlike <see cref="LoadTape"/>, which models a human ejecting and inserting a
    /// physically different tape (real hardware would need PLAY pressed again for that). A real
    /// SAVE writes onto the SAME tape that's already in the deck with PLAY still held down the
    /// whole time; <see cref="Vic20Machine"/> calls this once SAVE completes, not
    /// <see cref="LoadTape"/>, so a caller can immediately LOAD the same content straight back
    /// without needing to press PLAY again.</summary>
    internal void ReplaceContentAfterSave(IReadOnlyList<int> pulseCycles, string name)
    {
        ArgumentNullException.ThrowIfNull(pulseCycles);
        _pulseCycles = pulseCycles;
        TapeName = name;
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

    /// <summary>Advances the tape by one CPU cycle: plays back the loaded tape's pulses onto VIA2
    /// CA1. No-op if the motor is off, PLAY isn't pressed, no tape is loaded, or the loaded tape
    /// has already played to the end.</summary>
    public void Tick()
    {
        // PA6 reflects the physical PLAY sense switch every cycle, independent of the motor/tape
        // state below (a real switch closes the moment PLAY is pressed, tape moving or not).
        _via1.PortAInput = (byte)(PlayPressed ? (_via1.PortAInput & ~0x40) : (_via1.PortAInput | 0x40));

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
