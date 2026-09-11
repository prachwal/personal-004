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

    public byte Output { get; internal set; }

    public byte Direction { get; internal set; }
}
