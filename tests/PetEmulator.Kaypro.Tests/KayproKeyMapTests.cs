using NUnit.Framework;
using PetEmulator.Kaypro.Keyboard;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproKeyMapTests
{
    [Test]
    public void LettersSendLowercaseAscii()
    {
        var a = KayproKeyMap.MainKeys.Single(k => k.Label == "A");
        var z = KayproKeyMap.MainKeys.Single(k => k.Label == "Z");

        Assert.That(a.Value, Is.EqualTo((byte)'a'));
        Assert.That(z.Value, Is.EqualTo((byte)'z'));
    }

    [Test]
    public void DigitsSendTheirOwnAsciiCode()
    {
        foreach (var digit in "0123456789")
        {
            var key = KayproKeyMap.MainKeys.Single(k => k.Label == digit.ToString());
            Assert.That(key.Value, Is.EqualTo((byte)digit));
        }
    }

    [Test]
    public void ArrowKeysUseTheAdm3aCursorConvention()
    {
        Assert.That(KayproKeyMap.CursorUp, Is.EqualTo(0x0B));
        Assert.That(KayproKeyMap.CursorDown, Is.EqualTo(0x0A));
        Assert.That(KayproKeyMap.CursorLeft, Is.EqualTo(0x08));
        Assert.That(KayproKeyMap.CursorRight, Is.EqualTo(0x0C));
        Assert.That(KayproKeyMap.ArrowKeys, Has.Count.EqualTo(4));
    }

    [Test]
    public void WarmBootIsControlC()
    {
        var key = KayproKeyMap.MainKeys.Single(k => k.Label == "CTRL-C");
        Assert.That(key.Value, Is.EqualTo(0x03));
        Assert.That(KayproKeyMap.WarmBoot, Is.EqualTo(0x03));
    }

    [Test]
    public void NoLabelIsRepeatedWithinTheSameGroup()
    {
        Assert.That(KayproKeyMap.MainKeys.Select(k => k.Label).Distinct().Count(), Is.EqualTo(KayproKeyMap.MainKeys.Count));
        Assert.That(KayproKeyMap.NumericPad.Select(k => k.Label).Distinct().Count(), Is.EqualTo(KayproKeyMap.NumericPad.Count));
    }

    [Test]
    public void EveryByteIsPrintableOrAKnownControlCode()
    {
        var knownControls = new byte[] { 0x03, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x1B };
        foreach (var (label, value) in KayproKeyMap.MainKeys.Concat(KayproKeyMap.NumericPad).Concat(KayproKeyMap.ArrowKeys))
            Assert.That(value is >= 0x20 and <= 0x7E || knownControls.Contains(value),
                $"'{label}' sends 0x{value:X2}, not a printable 7-bit ASCII byte or a recognized control code");
    }
}
