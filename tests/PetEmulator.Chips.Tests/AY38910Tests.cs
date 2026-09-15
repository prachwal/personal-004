using NUnit.Framework;
using PetEmulator.Audio;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

[TestFixture]
public sealed class AY38910Tests
{
    [Test]
    public void RegisterSelectAndWriteApplyHardwareMasks()
    {
        var ay = new Ay38910();
        ay.WritePort(0xA0, 1);
        ay.WritePort(0xA1, 0xFF);
        Assert.That(ay.ReadSelected(), Is.EqualTo(0x0F));
    }

    [Test]
    public void ZeroPeriodsAreClampedSoFrequenciesStayFinite()
    {
        var ay = new Ay38910();
        Assert.That(ay.TonePeriod(0), Is.EqualTo(1));
        Assert.That(ay.NoisePeriod, Is.EqualTo(1));
        Assert.That(double.IsFinite(ay.ToneHz(0)), Is.True);
        Assert.That(double.IsFinite(ay.NoiseHz), Is.True);
    }

    [Test]
    public void RenderProducesANonSilentToneWhenAChannelIsOnAtFullVolume()
    {
        var ay = new Ay38910 { Clock = 1_000_000, SampleRate = 44_100 };
        ay.WritePort(0xA0, 0); ay.WritePort(0xA1, 100);   // R0: tone period low byte -> audible pitch
        ay.WritePort(0xA0, 1); ay.WritePort(0xA1, 0);     // R1: tone period high byte
        ay.WritePort(0xA0, 7); ay.WritePort(0xA1, 0b111110); // mixer: channel A tone enabled, all else disabled
        ay.WritePort(0xA0, 8); ay.WritePort(0xA1, 0x0F);  // R8: channel A volume = max, no envelope

        Span<AudioFrame> buffer = stackalloc AudioFrame[512];
        ay.Render(buffer);

        // Unipolar per-channel gating (see Render's own doc comment): a tone-only channel pulses
        // between 0 (gate closed) and +amplitude (gate open), never negative.
        Assert.That(buffer.ToArray().Any(f => f.Left > 0f), Is.True, "an enabled full-volume tone must not render silence");
        Assert.That(buffer.ToArray().Any(f => f.Left == 0f), Is.True, "a square wave's gate must also close within a large enough buffer");
        Assert.That(buffer.ToArray().All(f => f.Left >= 0f), Is.True);
    }

    [Test]
    public void RenderIsSilentWhenEveryChannelVolumeIsZero()
    {
        var ay = new Ay38910();
        ay.WritePort(0xA0, 7); ay.WritePort(0xA1, 0b111000); // mixer: tone enabled on all 3 channels, noise disabled
        ay.WritePort(0xA0, 8); ay.WritePort(0xA1, 0);
        ay.WritePort(0xA0, 9); ay.WritePort(0xA1, 0);
        ay.WritePort(0xA0, 10); ay.WritePort(0xA1, 0);

        Span<AudioFrame> buffer = stackalloc AudioFrame[64];
        ay.Render(buffer);

        Assert.That(buffer.ToArray().All(f => f.Left == 0f), Is.True);
    }

    [Test]
    public void WritingR13RestartsTheEnvelopeFromItsAttackEdge()
    {
        var ay = new Ay38910();
        ay.WritePort(0xA0, 11); ay.WritePort(0xA1, 1); // short envelope period
        ay.WritePort(0xA0, 13); ay.WritePort(0xA1, 0b0100); // attack, no continue/alternate/hold -> ramps 0..15 once

        Assert.That(ay.EnvelopeLevel, Is.EqualTo(0));
        Span<AudioFrame> buffer = stackalloc AudioFrame[16];
        ay.Render(buffer);
        Assert.That(ay.EnvelopeLevel, Is.GreaterThan(0), "the envelope must have advanced after rendering");

        ay.WritePort(0xA0, 13); ay.WritePort(0xA1, 0b0100); // re-writing R13 restarts even with the same value
        Assert.That(ay.EnvelopeLevel, Is.EqualTo(0));
    }
}
