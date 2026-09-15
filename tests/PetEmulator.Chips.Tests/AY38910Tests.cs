using NUnit.Framework;
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
    public void ZeroPeriodsAreClampedAndNoiseLfsrRemainsNonZero()
    {
        var ay = new Ay38910();
        Assert.That(ay.TonePeriod(0), Is.EqualTo(1));
        Assert.That(ay.NoisePeriod, Is.EqualTo(1));
        for (var i = 0; i < 20; i++) ay.Tick();
        Assert.That(ay.NoiseShift, Is.Not.EqualTo(0));
    }
}
