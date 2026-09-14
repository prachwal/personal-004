using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Views.Controls;
using PetEmulator.Kaypro.Keyboard;

namespace PetEmulator.Desktop.Tests;

public sealed class KayproKeyboardLayoutFactoryTests
{
    [Test]
    public void Build_ProducesAValidLayout()
    {
        var act = () => KayproKeyboardLayoutFactory.Build();

        act.Should().NotThrow();
    }

    [Test]
    public void Build_CoversEveryMainKeyArrowKeyAndNumericPadKey()
    {
        var layout = KayproKeyboardLayoutFactory.Build();
        var wiredBytes = layout.Keys
            .Select(key => KayproKeyboardLayoutFactory.TryParseSignal(key.SignalIds[0], out var value) ? value : (byte?)null)
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .ToHashSet();

        foreach (var (label, value) in KayproKeyMap.MainKeys)
            wiredBytes.Should().Contain(value, $"'{label}' (0x{value:X2}) should be reachable from the on-screen keyboard");
        foreach (var (label, value) in KayproKeyMap.ArrowKeys)
            wiredBytes.Should().Contain(value, $"arrow '{label}' (0x{value:X2}) should be reachable");
        // Numeric pad bytes duplicate main-key bytes by design (see KayproKeyMap doc comment) -
        // already covered by the MainKeys assertion above, but confirms the pad keys themselves
        // parse to a valid signal rather than silently landing outside the layout.
        foreach (var key in layout.Keys.Where(k => k.Id.StartsWith("pad:", StringComparison.Ordinal)))
            KayproKeyboardLayoutFactory.TryParseSignal(key.SignalIds[0], out _).Should().BeTrue();
    }

    [Test]
    public void PressingAnOnScreenKeyResolvesToTheExactByteItClaims()
    {
        var layout = KayproKeyboardLayoutFactory.Build();
        var enterKey = layout.Keys.Single(k => k.AutomationName == "RETURN");

        KayproKeyboardLayoutFactory.TryParseSignal(enterKey.SignalIds[0], out var value).Should().BeTrue();

        value.Should().Be(0x0D);
    }

    [TestCase("kaypro:0D", 0x0D)]
    [TestCase("kaypro:61", 0x61)]
    [TestCase("pet:4,0", 0)]
    public void TryParseSignal_RoundTripsOrRejectsCleanly(string signal, byte expected)
    {
        var ok = KayproKeyboardLayoutFactory.TryParseSignal(signal, out var value);

        if (signal.StartsWith("kaypro:", StringComparison.Ordinal))
        {
            ok.Should().BeTrue();
            value.Should().Be(expected);
        }
        else
        {
            ok.Should().BeFalse();
        }
    }
}
