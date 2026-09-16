namespace PetEmulator.Pet;

/// <summary>
/// External eight-bit PET User Port state. Direction and output are updated by the PET VIA's
/// Port A; <see cref="Input"/> and <see cref="HandshakeInput"/> are supplied by an attached
/// peripheral and default to the inactive pulled-high level.
/// </summary>
public sealed class PetUserPort
{
    private byte _input = 0xFF;
    private bool _handshakeInput = true;

    public event Action? InputChanged;

    public event Action? HandshakeInputChanged;

    public byte Input
    {
        get => _input;
        set
        {
            if (_input == value)
                return;

            _input = value;
            InputChanged?.Invoke();
        }
    }

    public bool HandshakeInput
    {
        get => _handshakeInput;
        set
        {
            if (_handshakeInput == value)
                return;

            _handshakeInput = value;
            HandshakeInputChanged?.Invoke();
        }
    }

    public byte Output { get; internal set; }

    public byte Direction { get; internal set; }

    public bool HandshakeOutput { get; internal set; }

    public PetUserPortSnapshot CaptureState() => new()
    {
        Input = Input, HandshakeInput = HandshakeInput, Output = Output,
        Direction = Direction, HandshakeOutput = HandshakeOutput
    };

    public void RestoreState(PetUserPortSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Input = state.Input;
        HandshakeInput = state.HandshakeInput;
        Output = state.Output;
        Direction = state.Direction;
        HandshakeOutput = state.HandshakeOutput;
    }
}
