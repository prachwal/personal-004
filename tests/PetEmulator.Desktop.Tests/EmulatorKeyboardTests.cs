using Avalonia;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Views.Controls;

namespace PetEmulator.Desktop.Tests;

/// <summary>Ported from personal-002's Terminal.Avalonia.Demo.Tests - same framework, same
/// coverage, carried over with the port rather than re-derived. Exercises the real
/// <see cref="EmulatorKeyboardState"/> (internal to PetEmulator.Desktop; visible here via
/// InternalsVisibleTo), not a copy.</summary>
public sealed class EmulatorKeyboardTests
{
    [Test]
    public void Layout_RejectsInvalidKeys()
    {
        Action duplicate = () => new EmulatorKeyboardLayout("test", new Size(20, 20), [Key("A"), Key("A")]);
        Action outside = () => new EmulatorKeyboardLayout("test", new Size(20, 20), [Key("A", new Rect(15, 0, 10, 10))]);
        Action noSignals = () => new EmulatorKeyboardLayout("test", new Size(20, 20),
            [new EmulatorKeyDefinition("A", new Rect(0, 0, 10, 10), [], [])]);

        duplicate.Should().Throw<ArgumentException>();
        outside.Should().Throw<ArgumentOutOfRangeException>();
        noSignals.Should().Throw<ArgumentException>();
    }

    [Test]
    public void State_PressesAndReleasesSignalsInRequiredOrder_WithReferenceCounts()
    {
        var layout = new EmulatorKeyboardLayout("test", new Size(40, 20),
            [Key("one", new Rect(0, 0, 10, 10), ["Shift", "A"]), Key("two", new Rect(12, 0, 10, 10), ["Shift", "B"])]);
        var calls = new List<string>();
        var state = new EmulatorKeyboardState(layout, (signal, pressed) => calls.Add($"{signal}:{pressed}"));

        state.Press("one"); state.Press("two"); state.Release("one"); state.Release("two");

        calls.Should().Equal("Shift:True", "A:True", "B:True", "A:False", "B:False", "Shift:False");
        state.PressedKeyIds.Should().BeEmpty();
    }

    [Test]
    public void State_ToggleAndReleaseAll_ClearPressedKeys()
    {
        var layout = new EmulatorKeyboardLayout("test", new Size(30, 20),
            [Key("toggle", behavior: EmulatorKeyBehavior.Toggle), Key("key", new Rect(12, 0, 10, 10))]);
        var calls = new List<string>();
        var state = new EmulatorKeyboardState(layout, (signal, pressed) => calls.Add($"{signal}:{pressed}"));

        state.Press("toggle"); state.Press("key"); state.ReleaseAll();

        state.PressedKeyIds.Should().BeEmpty();
        calls.Should().Equal("toggle:True", "key:True", "key:False", "toggle:False");
    }

    [Test]
    public void Definition_PreservesAliasesAndAutomationMetadata()
    {
        EmulatorKeyDefinition key = new("A", new Rect(0, 0, 10, 10), [new("A", new Point(2, 2))], ["A"],
            AutomationName: "Letter A", HostAliases: ["A", "Key.A"]);

        key.AutomationName.Should().Be("Letter A");
        key.HostAliases.Should().Equal("A", "Key.A");
    }

    private static EmulatorKeyDefinition Key(string id, Rect? bounds = null, IReadOnlyList<string>? signals = null,
        EmulatorKeyBehavior behavior = EmulatorKeyBehavior.Momentary) =>
        new(id, bounds ?? new Rect(0, 0, 10, 10), [new(id, new Point(1, 1))], signals ?? [id], behavior);
}
