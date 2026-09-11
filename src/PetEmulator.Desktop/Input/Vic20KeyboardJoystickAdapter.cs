using Avalonia.Input;
using PetEmulator.Vic20;

namespace PetEmulator.Desktop.Input;

/// <summary>Maps Desktop keyboard keys to the VIC-20 joystick input vocabulary.</summary>
public static class Vic20KeyboardJoystickAdapter
{
    public static bool TryMap(Key key, out Vic20JoystickInput input)
    {
        input = key switch
        {
            Key.NumPad8 => Vic20JoystickInput.Up,
            Key.NumPad2 => Vic20JoystickInput.Down,
            Key.NumPad4 => Vic20JoystickInput.Left,
            Key.NumPad6 => Vic20JoystickInput.Right,
            Key.NumPad0 => Vic20JoystickInput.Fire,
            _ => default
        };

        return key is Key.NumPad8 or Key.NumPad2 or Key.NumPad4 or Key.NumPad6 or Key.NumPad0;
    }
}
