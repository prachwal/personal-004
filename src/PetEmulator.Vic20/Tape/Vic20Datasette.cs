using PetEmulator.Pet.Chips;

namespace PetEmulator.Vic20.Tape;

/// <summary>
/// Drives VIA1's cassette port from a decoded pulse-cycle stream: CA1 (input, cassette read line)
/// is pulsed at each pulse boundary, PA7 (input, cassette switch sense) reflects whether PLAY is
/// physically held, gated by CA2 (output, cassette motor control). Mirrors
/// <c>PetEmulator.Pet.Tape.PetDatasette</c>'s shape exactly (same Tick/LoadTape/PressPlay
/// contract, same reuse of <c>PetEmulator.Pet.Tape</c>'s cassette pulse format - see
/// <see cref="PetEmulator.Pet.Tape.PetTapeCassetteFormat"/>'s doc comment: every cassette-based
/// Commodore machine shares the identical encoding, just via a different chip (VIA here, PIA on
/// PET) and different pin numbers.
///
/// Real wiring confirmed via WebSearch (c64-wiki/RetroIsle VIC-20 memory maps) plus
/// <c>BusObserver</c> tracing a real LOAD against <c>docs/vic20-disassembly/kernal.asm</c> (PCR/IER
/// writes at $911C/$911E matching a CA1-edge-interrupt setup): VIA1 CA1 = cassette read, VIA1 PA7
/// = cassette switch sense (active low, same convention as PET's PIA1 PA4), VIA1 CA2 = cassette
/// motor control (active low, same convention as PET's PIA1 CB2). See
/// docs/vic20-tape.md for the investigation.
/// </summary>
public sealed class Vic20Datasette
{
    private readonly Via6522 _via1;
    private IReadOnlyList<int> _pulseCycles = [];
    private int _pulseIndex;
    private int _cyclesUntilNextEdge;

    public Vic20Datasette(Via6522 via1) => _via1 = via1 ?? throw new ArgumentNullException(nameof(via1));

    /// <summary>CA2 configured as a manual-output line (PCR bits 1-3 select an output mode,
    /// $08-$0E) and held low - active-low motor-on, same convention as PET's PIA1 CB2.</summary>
    public bool MotorOn => (_via1.PCR & 0x0E) >= 0x08 && !_via1.CA2Output;

    public bool HasTape => _pulseCycles.Count > 0;

    /// <summary>Whether the (emulated) physical PLAY button is currently held down - see
    /// <c>PetDatasette.PlayPressed</c>'s doc comment for the real-hardware reasoning this mirrors.</summary>
    public bool PlayPressed { get; private set; }

    /// <summary>Cassette switch sense (VIA1 PA7, active low): closes whenever a transport button
    /// is physically held down, regardless of whether a tape is even loaded - matching real
    /// hardware. Same idiom as <c>PetDatasette.Sense</c>; kept as its own property (rather than
    /// inlining <see cref="PlayPressed"/> everywhere) so a caller reads intent, not mechanism.</summary>
    public bool Sense => PlayPressed;

    /// <summary>True once every pulse in the loaded tape has been played past.</summary>
    public bool IsAtEnd => _pulseIndex >= _pulseCycles.Count;

    /// <summary>Display name of the currently loaded tape, or null when none was given.</summary>
    public string? TapeName { get; private set; }

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

    /// <summary>Advances the tape by one CPU cycle. No-op if the motor is off, PLAY isn't pressed,
    /// no tape is loaded, or the loaded tape has already played to the end.</summary>
    public void Tick()
    {
        // PA7 reflects the physical PLAY sense switch every cycle, independent of the motor/tape
        // state below (a real switch closes the moment PLAY is pressed, tape moving or not).
        _via1.PortAInput = (byte)(PlayPressed ? 0x00 : 0x80);

        if (!MotorOn || !PlayPressed || IsAtEnd)
            return;

        if (--_cyclesUntilNextEdge > 0)
            return;

        _via1.CA1 = true; // ensure a high baseline first (no-op if already high) so the next line is a guaranteed transition
        _via1.CA1 = false;
        _via1.CA1 = true;
        _pulseIndex++;
        if (!IsAtEnd)
            _cyclesUntilNextEdge = _pulseCycles[_pulseIndex];
    }
}
