using PetEmulator.Chips;

namespace PetEmulator.Pet.Tape;

/// <summary>One protocol-level activity on a <see cref="PetDatasette"/>: a motor state change
/// (Kind="motor", Detail="on"/"off") or a played pulse boundary (Kind="pulse", e.g.
/// Detail="boundary 0 width=100"). For a future debug surface to observe *why* the tape state
/// changed, not just that CA1 toggled.</summary>
public readonly record struct DatasetteActivity(string Kind, string Detail);

/// <summary>
/// Drives PIA1's cassette #1 port from a decoded pulse-cycle stream: CA1 (input, cassette read
/// line) is pulsed at each pulse boundary, gated by CB2 (output, cassette motor control - PET
/// wiring is active-low: false/0 = motor on, true/1 = off).
///
/// Call <see cref="Tick"/> once per CPU cycle while a tape is loaded, the motor is on, and PLAY
/// is pressed (see <see cref="PlayPressed"/>).
///
/// Motor state is read directly from PIA1's CB2 on every tick (not cached from a change event):
/// the motor is only ever "on" once the ROM has actually configured CB2 as an output AND driven
/// it low.
///
/// Each pulse boundary forces CA1 high (idempotent baseline), then low, then high again, rather
/// than simply toggling it: the KERNAL only times successive *falling* edges, so a plain
/// alternating toggle would only produce a real falling edge on every other pulse-array entry
/// once CA1's edge polarity is fixed - silently halving the effective data rate. Forcing a known
/// baseline first guarantees the following low assignment is always a genuine transition (MT6520's
/// edge detection is a no-op when asked to set a line to its current value), so every entry
/// produces exactly one falling edge regardless of CA1's prior level.
///
/// NOTE (cross-dependency): <see cref="MT6520"/> is ported into PetEmulator.Chips by a parallel
/// effort; this file assumes the same member names as personal-001's Emulator.Chips.MT6520 (CA1,
/// CB2, IsCb2Output). If that port lands with a different shape, this file will need a small
/// follow-up fix.
///
/// Ported verbatim from personal-001: the source class implements no device interface (no
/// Emulator.Core.Abstractions.IDevice), so it is kept here as a plain class with its own
/// no-arg <see cref="Tick"/> (one CPU cycle per call), not PetEmulator.Core.IDevice's
/// Tick(ulong cycles) - see the porting report for the reasoning.
/// </summary>
public sealed class PetDatasette
{
    private readonly MT6520 _pia1;
    private IReadOnlyList<int> _pulseCycles = [];
    private int _pulseIndex;
    private int _cyclesUntilNextEdge;
    private bool _lastMotorOn;

    public PetDatasette(MT6520 pia1) => _pia1 = pia1 ?? throw new ArgumentNullException(nameof(pia1));

    /// <summary>Fires for motor start/stop and each played pulse boundary. Optional (nullable
    /// multicast delegate) - zero-cost and behavior-neutral when nobody subscribes.</summary>
    public event Action<DatasetteActivity>? Activity;

    public bool MotorOn => _pia1.IsCb2Output && !_pia1.CB2; // active low: CB2 = 0 means the motor is on

    public bool HasTape => _pulseCycles.Count > 0;

    /// <summary>Whether the (emulated) physical PLAY button is currently held down - real hardware
    /// has a switch under the transport buttons, independent of the software-controlled motor
    /// relay: the KERNAL's LOAD routine turns the motor on via <see cref="MotorOn"/> but then
    /// blocks printing "PRESS PLAY ON TAPE #1" until this switch closes too, exactly like a human
    /// needing to physically press play on a real datasette before tape actually moves. See
    /// <see cref="PressPlay"/>/<see cref="Stop"/>.</summary>
    public bool PlayPressed { get; private set; }

    /// <summary>Cassette #1 sense line (PIA1 PA4, active low): closes whenever a transport button
    /// is physically held down, regardless of whether a tape is even loaded - matching real
    /// hardware (you can press play on an empty deck; it just won't produce any pulses).</summary>
    public bool Sense => PlayPressed;

    /// <summary>True once every pulse in the loaded tape has been played past.</summary>
    public bool IsAtEnd => _pulseIndex >= _pulseCycles.Count;

    /// <summary>Display name of the currently loaded tape (e.g. a .tap file's name), or null when
    /// none was given - purely cosmetic, for a status display (see <c>PetEmulator.Pet.Devices</c>);
    /// nothing about tape playback itself depends on it.</summary>
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

    /// <summary>Presses the (emulated) PLAY button - see <see cref="PlayPressed"/>. Always
    /// available, even with no tape loaded (a real button doesn't know or care), so a caller never
    /// needs to check <see cref="HasTape"/> first.</summary>
    public void PressPlay() => PlayPressed = true;

    /// <summary>Releases the PLAY button (or STOP, on a real deck) - see <see cref="PlayPressed"/>.</summary>
    public void Stop() => PlayPressed = false;

    public void Rewind()
    {
        _pulseIndex = 0;
        _cyclesUntilNextEdge = _pulseCycles.Count > 0 ? _pulseCycles[0] : 0;
    }

    public void Reset() => Rewind();

    public PetDatasetteSnapshot CaptureState() => new()
    {
        PulseCycles = _pulseCycles.ToArray(), PulseIndex = _pulseIndex,
        CyclesUntilNextEdge = _cyclesUntilNextEdge, PlayPressed = PlayPressed,
        LastMotorOn = _lastMotorOn, TapeName = TapeName
    };

    public void RestoreState(PetDatasetteSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.PulseIndex < 0 || state.PulseIndex > state.PulseCycles.Length || state.CyclesUntilNextEdge < 0)
            throw new ArgumentException("Invalid PET datasette position.", nameof(state));
        _pulseCycles = state.PulseCycles.ToArray();
        _pulseIndex = state.PulseIndex;
        _cyclesUntilNextEdge = state.CyclesUntilNextEdge;
        PlayPressed = state.PlayPressed;
        _lastMotorOn = state.LastMotorOn;
        TapeName = state.TapeName;
    }

    /// <summary>Advances the tape by one CPU cycle. No-op if the motor is off, PLAY isn't pressed,
    /// no tape is loaded, or the loaded tape has already played to the end.</summary>
    public void Tick()
    {
        var motorOn = MotorOn;
        if (motorOn != _lastMotorOn)
        {
            _lastMotorOn = motorOn;
            Activity?.Invoke(new DatasetteActivity("motor", motorOn ? "on" : "off"));
        }

        if (!motorOn || !PlayPressed || IsAtEnd)
            return;

        if (--_cyclesUntilNextEdge > 0)
            return;

        _pia1.CA1 = true; // ensure a high baseline first (no-op if already high) so the next line is a guaranteed transition
        _pia1.CA1 = false;
        _pia1.CA1 = true;
        Activity?.Invoke(new DatasetteActivity("pulse", $"boundary {_pulseIndex} width={_pulseCycles[_pulseIndex]}"));
        _pulseIndex++;
        if (!IsAtEnd)
            _cyclesUntilNextEdge = _pulseCycles[_pulseIndex];
    }
}
