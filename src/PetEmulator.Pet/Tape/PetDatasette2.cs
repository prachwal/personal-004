using PetEmulator.Chips;

namespace PetEmulator.Pet.Tape;

/// <summary>
/// Independent PET cassette #2 transport. The second connector has its own PLAY/sense switch on
/// PIA1 PA5 and motor relay on VIA Port B bit 4. It deliberately does not reuse cassette #1's
/// PIA1 CA1 pulse reader: the repository's verified PET map only establishes the second deck's
/// sense and motor lines, so no guessed read-line wiring is introduced here.
/// </summary>
public sealed class PetDatasette2
{
    private readonly MT6520 _pia1;
    private readonly MOS6522 _via;
    private IReadOnlyList<int> _pulseCycles = [];

    public PetDatasette2(MT6520 pia1, MOS6522 via)
    {
        _pia1 = pia1 ?? throw new ArgumentNullException(nameof(pia1));
        _via = via ?? throw new ArgumentNullException(nameof(via));
    }

    public bool MotorOn => (_via.DDRB & 0x10) != 0 && (_via.PortBOutput & 0x10) == 0;

    public bool HasTape => _pulseCycles.Count > 0;

    public bool PlayPressed { get; private set; }

    public bool Sense => PlayPressed;

    public bool IsAtEnd => true;

    public string? TapeName { get; private set; }

    public void LoadTape(IReadOnlyList<int> pulseCycles, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(pulseCycles);
        _pulseCycles = pulseCycles;
        TapeName = name;
        PlayPressed = false;
    }

    public void Eject()
    {
        _pulseCycles = [];
        TapeName = null;
        PlayPressed = false;
    }

    public void PressPlay() => PlayPressed = true;

    public void Stop() => PlayPressed = false;

    public void Reset() => PlayPressed = false;

    public PetDatasette2Snapshot CaptureState() => new()
    {
        PulseCycles = _pulseCycles.ToArray(), PlayPressed = PlayPressed, TapeName = TapeName
    };

    public void RestoreState(PetDatasette2Snapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _pulseCycles = state.PulseCycles.ToArray();
        PlayPressed = state.PlayPressed;
        TapeName = state.TapeName;
    }
}
