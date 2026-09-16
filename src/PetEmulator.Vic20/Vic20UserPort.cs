namespace PetEmulator.Vic20;

/// <summary>
/// External eight-bit VIC-20 User Port state. Direction and output are updated by VIA1's DDRB
/// and Port B writes; <see cref="Input"/> is supplied by an attached peripheral and defaults to
/// the inactive pulled-high level.
/// </summary>
public sealed class Vic20UserPort
{
    private byte _input = 0xFF;

    public event Action? InputChanged;

    /// <summary>Raised after the VIA changes the byte driven onto User Port pins.</summary>
    public event Action? OutputChanged;

    /// <summary>Raised after the VIA changes the User Port data-direction register.</summary>
    public event Action? DirectionChanged;

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

    public byte Output
    {
        get;
        internal set
        {
            if (field == value)
                return;

            field = value;
            OutputChanged?.Invoke();
        }
    }

    public byte Direction
    {
        get;
        internal set
        {
            if (field == value)
                return;

            field = value;
            DirectionChanged?.Invoke();
        }
    }

    public Vic20UserPortSnapshot CaptureState() => new() { Input = Input, Output = Output, Direction = Direction };

    public void RestoreState(Vic20UserPortSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Input = state.Input;
        Output = state.Output;
        Direction = state.Direction;
    }
}
