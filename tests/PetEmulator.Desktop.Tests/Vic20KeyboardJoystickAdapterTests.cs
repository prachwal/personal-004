using Avalonia.Input;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Input;
using PetEmulator.Vic20;

namespace PetEmulator.Desktop.Tests;

public sealed class Vic20KeyboardJoystickAdapterTests
{
    [TestCase(Key.NumPad8, Vic20JoystickInput.Up)]
    [TestCase(Key.NumPad2, Vic20JoystickInput.Down)]
    [TestCase(Key.NumPad4, Vic20JoystickInput.Left)]
    [TestCase(Key.NumPad6, Vic20JoystickInput.Right)]
    [TestCase(Key.NumPad0, Vic20JoystickInput.Fire)]
    public void MapsNumpadKey(Key key, Vic20JoystickInput expected)
    {
        Vic20KeyboardJoystickAdapter.TryMap(key, out var input).Should().BeTrue();
        input.Should().Be(expected);
    }

    [Test]
    public void RejectsNonJoystickKey()
    {
        Vic20KeyboardJoystickAdapter.TryMap(Key.A, out _).Should().BeFalse();
    }

    [Test]
    public void AppliesMappedKeyToAnInputSink()
    {
        var sink = new RecordingSink();

        Vic20KeyboardJoystickAdapter.TryApply(Key.NumPad0, true, sink).Should().BeTrue();

        sink.Input.Should().Be(Vic20JoystickInput.Fire);
        sink.Pressed.Should().BeTrue();
    }

    private sealed class RecordingSink : IJoystickInputSink
    {
        public Vic20JoystickInput Input { get; private set; }
        public bool Pressed { get; private set; }

        public void Set(Vic20JoystickInput input, bool pressed)
        {
            Input = input;
            Pressed = pressed;
        }
    }
}
