using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Tests.Tape;

public sealed class PetDatasette2Tests
{
    [Test]
    public void Motor_UsesViaPortBBitFourAndSenseIsIndependent()
    {
        var pia = new MT6520();
        var via = new MOS6522();
        var datasette = new PetDatasette2(pia, via);

        datasette.PressPlay();
        datasette.MotorOn.Should().BeFalse("PB4 is not configured as an output yet");
        datasette.Sense.Should().BeTrue();

        via.Write(MOS6522.Ddrb, 0x10);
        via.Write(MOS6522.Orb, 0x00);
        datasette.MotorOn.Should().BeTrue("PB4 low drives cassette #2 motor on");

        via.Write(MOS6522.Orb, 0x10);
        datasette.MotorOn.Should().BeFalse("PB4 high releases cassette #2 motor");
    }

    [Test]
    public void TapeLifecycle_IsIndependentFromCassetteOneState()
    {
        var datasette = new PetDatasette2(new MT6520(), new MOS6522());
        datasette.LoadTape([100, 200], "second.tap");

        datasette.HasTape.Should().BeTrue();
        datasette.TapeName.Should().Be("second.tap");
        datasette.PlayPressed.Should().BeFalse();

        datasette.Eject();

        datasette.HasTape.Should().BeFalse();
        datasette.TapeName.Should().BeNull();
        datasette.Sense.Should().BeFalse();
    }
}
