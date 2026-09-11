namespace PetEmulator.Vic20;

/// <summary>Digital inputs exposed by the VIC-20 control port.</summary>
public enum Vic20JoystickInput
{
    Up,
    Down,
    Left,
    Right,
    Fire
}

/// <summary>
/// Holds the active-low VIC-20 joystick lines. The four directions and fire button are wired to
/// VIA1 PA2-PA5 and VIA2 PB7; the machine combines these masks with the existing IEC and tape
/// input lines before the CPU reads either VIA.
/// </summary>
public sealed class Vic20Joystick : IJoystickSource
{
    private bool _up;
    private bool _down;
    private bool _left;
    private bool _right;
    private bool _fire;

    public event Action? StateChanged;

    public bool Up => _up;
    public bool Down => _down;
    public bool Left => _left;
    public bool Right => _right;
    public bool Fire => _fire;

    /// <summary>Sets one switch. <paramref name="pressed"/> means the line is pulled low.</summary>
    public void Set(Vic20JoystickInput input, bool pressed)
    {
        var state = input switch
        {
            Vic20JoystickInput.Up => _up,
            Vic20JoystickInput.Down => _down,
            Vic20JoystickInput.Left => _left,
            Vic20JoystickInput.Right => _right,
            Vic20JoystickInput.Fire => _fire,
            _ => throw new ArgumentOutOfRangeException(nameof(input), input, null)
        };

        if (state == pressed)
            return;

        switch (input)
        {
            case Vic20JoystickInput.Up: _up = pressed; break;
            case Vic20JoystickInput.Down: _down = pressed; break;
            case Vic20JoystickInput.Left: _left = pressed; break;
            case Vic20JoystickInput.Right: _right = pressed; break;
            case Vic20JoystickInput.Fire: _fire = pressed; break;
            default: throw new ArgumentOutOfRangeException(nameof(input), input, null);
        }
        StateChanged?.Invoke();
    }

    /// <summary>Returns the active-low direction/fire bits for VIA1 Port A (PA2-PA5).</summary>
    internal byte Via1PortAInput
    {
        get
        {
            var value = (byte)0x3C;
            if (_up) value &= unchecked((byte)~0x04);
            if (_down) value &= unchecked((byte)~0x08);
            if (_left) value &= unchecked((byte)~0x10);
            if (_fire) value &= unchecked((byte)~0x20);
            return value;
        }
    }

    /// <summary>Returns the active-low right-direction bit for VIA2 Port B (PB7).</summary>
    internal byte Via2PortBInput => _right ? (byte)0x00 : (byte)0x80;
}
