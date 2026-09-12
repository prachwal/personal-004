namespace PetEmulator.Vic20;

/// <summary>Receives digital joystick switch changes from host adapters.</summary>
public interface IJoystickInputSink
{
    void Set(Vic20JoystickInput input, bool pressed);
}
