namespace PetEmulator.Vic20;

/// <summary>Read-only joystick state exposed to machine and host adapters.</summary>
public interface IJoystickSource
{
    event Action? StateChanged;

    bool Up { get; }
    bool Down { get; }
    bool Left { get; }
    bool Right { get; }
    bool Fire { get; }
}
