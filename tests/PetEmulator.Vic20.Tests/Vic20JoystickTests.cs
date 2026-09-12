using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Vic20.Tests;

[TestFixture]
public sealed class Vic20JoystickTests
{
    [Test]
    public void Set_TracksPressedStateAndRaisesOnlyOnChange()
    {
        var joystick = new Vic20Joystick();
        var changes = 0;
        joystick.StateChanged += () => changes++;

        joystick.Set(Vic20JoystickInput.Up, true);
        joystick.Set(Vic20JoystickInput.Up, true);
        joystick.Set(Vic20JoystickInput.Up, false);

        joystick.Up.Should().BeFalse();
        changes.Should().Be(2);
    }

    [Test]
    public void ImplementsReadOnlyJoystickSource()
    {
        IJoystickSource source = new Vic20Joystick();

        source.Up.Should().BeFalse();
        source.Down.Should().BeFalse();
        source.Left.Should().BeFalse();
        source.Right.Should().BeFalse();
        source.Fire.Should().BeFalse();
    }
}
