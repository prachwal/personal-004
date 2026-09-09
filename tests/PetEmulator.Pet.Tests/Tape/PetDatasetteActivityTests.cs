using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Chips;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Tests.Tape;

/// <summary>Proves PetDatasette.Activity fires for motor start/stop and each played pulse
/// boundary - not just that CA1 toggled.</summary>
public sealed class PetDatasetteActivityTests
{
    private const byte MotorOnControlB = 0x20 | 0x00; // CB2 output mode, value 0 = motor on (active low)
    private const byte MotorOffControlB = 0x20 | 0x08; // CB2 output mode, value 1 = motor off

    [Test]
    public void MotorOn_RaisesMotorOnActivity()
    {
        var pia = new Pia();
        var datasette = new PetDatasette(pia);
        datasette.LoadTape([100]);
        var activity = new List<DatasetteActivity>();
        datasette.Activity += activity.Add;

        pia.Write(3, MotorOnControlB);
        datasette.Tick();

        activity.Should().ContainSingle()
            .Which.Should().Be(new DatasetteActivity("motor", "on"));
    }

    [Test]
    public void MotorOff_RaisesMotorOffActivity()
    {
        var pia = new Pia();
        var datasette = new PetDatasette(pia);
        datasette.LoadTape([10, 10]);
        pia.Write(3, MotorOnControlB);
        datasette.Tick(); // consumes the initial off->on transition

        var activity = new List<DatasetteActivity>();
        datasette.Activity += activity.Add;

        pia.Write(3, MotorOffControlB);
        datasette.Tick();

        activity.Should().ContainSingle()
            .Which.Should().Be(new DatasetteActivity("motor", "off"));
    }

    [Test]
    public void PulseBoundary_RaisesPulseActivityWithWidthAndIndex()
    {
        var pia = new Pia();
        var datasette = new PetDatasette(pia);
        datasette.LoadTape([100, 200]);
        pia.Write(3, MotorOnControlB);
        datasette.Tick(); // motor-on tick, index still 0

        var activity = new List<DatasetteActivity>();
        datasette.Activity += activity.Add;

        for (var i = 0; i < 99; i++) datasette.Tick();

        activity.Should().ContainSingle()
            .Which.Should().Be(new DatasetteActivity("pulse", "boundary 0 width=100"));
    }

    [Test]
    public void NoSubscriber_DoesNotThrow()
    {
        var pia = new Pia();
        var datasette = new PetDatasette(pia);
        datasette.LoadTape([10]);
        var act = () =>
        {
            pia.Write(3, MotorOnControlB);
            for (var i = 0; i < 10; i++) datasette.Tick();
        };
        act.Should().NotThrow();
    }
}
