using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Chips;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Tests.Tape;

public sealed class PetDatasetteTests
{
    private const byte MotorOnControlB = 0x20 | 0x00; // CB2 output mode, value 0 = motor on (active low)
    private const byte MotorOffControlB = 0x20 | 0x08; // CB2 output mode, value 1 = motor off
    private const byte Ca1FlagBit = 0x80; // PIA control register bit set by an active CA1 edge

    /// <summary>CRA's reset value (0) makes CA1 falling-edge sensitive, matching the KERNAL's
    /// documented behavior of timing successive falling edges - so the control register's flag
    /// bit is the thing that actually matters to a real reader, not the raw line level.</summary>
    private static bool Ca1FlagSet(Pia pia) => (pia.Read(1) & Ca1FlagBit) != 0;

    [Test]
    public void Motor_stays_off_and_no_edge_occurs_until_cb2_turns_it_on()
    {
        var pia = new Pia();
        var datasette = new PetDatasette(pia);
        datasette.LoadTape([100, 200]);

        for (var i = 0; i < 500; i++) datasette.Tick();

        datasette.MotorOn.Should().BeFalse();
        Ca1FlagSet(pia).Should().BeFalse();
    }

    [Test]
    public void Forces_a_falling_edge_at_each_pulse_boundary_once_motor_is_on()
    {
        var pia = new Pia();
        pia.Write(1, 0x04); // select CRA's data register so reading Port A clears the CA1 flag, as real KERNAL code does
        var datasette = new PetDatasette(pia);
        datasette.LoadTape([100, 200, 50]);
        pia.Write(3, MotorOnControlB);

        datasette.MotorOn.Should().BeTrue();

        for (var i = 0; i < 99; i++) datasette.Tick();
        Ca1FlagSet(pia).Should().BeFalse("the first pulse (100 cycles) hasn't elapsed yet");

        datasette.Tick(); // 100th tick: first pulse boundary
        Ca1FlagSet(pia).Should().BeTrue();
        pia.CA1.Should().BeTrue("the line always settles high after a boundary");
        pia.Read(0); // reading Port A clears the CA1 flag, same as the real KERNAL ISR would

        for (var i = 0; i < 199; i++) datasette.Tick();
        Ca1FlagSet(pia).Should().BeFalse("the second pulse (200 cycles) hasn't elapsed yet");

        datasette.Tick(); // second boundary
        Ca1FlagSet(pia).Should().BeTrue("every boundary is a falling edge, not just every other one");
        pia.CA1.Should().BeTrue();
        pia.Read(0);

        for (var i = 0; i < 49; i++) datasette.Tick();
        datasette.IsAtEnd.Should().BeFalse();
        datasette.Tick(); // third and final boundary
        Ca1FlagSet(pia).Should().BeTrue();
        datasette.IsAtEnd.Should().BeTrue();

        pia.Read(0);
        datasette.Tick(); // ticking past the end is a no-op
        Ca1FlagSet(pia).Should().BeFalse();
    }

    [Test]
    public void Turning_the_motor_off_mid_tape_stops_advancing()
    {
        var pia = new Pia();
        pia.Write(1, 0x04); // select CRA's data register so reading Port A clears the CA1 flag
        var datasette = new PetDatasette(pia);
        datasette.LoadTape([10, 10, 10]);
        pia.Write(3, MotorOnControlB);
        for (var i = 0; i < 10; i++) datasette.Tick();
        Ca1FlagSet(pia).Should().BeTrue();
        pia.Read(0);

        pia.Write(3, MotorOffControlB);
        datasette.MotorOn.Should().BeFalse();
        for (var i = 0; i < 100; i++) datasette.Tick();
        Ca1FlagSet(pia).Should().BeFalse("the motor is off, so no further edges should occur");

        pia.Write(3, MotorOnControlB);
        for (var i = 0; i < 9; i++) datasette.Tick();
        Ca1FlagSet(pia).Should().BeFalse();
        datasette.Tick();
        Ca1FlagSet(pia).Should().BeTrue("the second pulse's remaining count should resume, not restart");
    }

    [Test]
    public void Rewind_resets_playback_position_without_touching_the_motor()
    {
        var pia = new Pia();
        var datasette = new PetDatasette(pia);
        datasette.LoadTape([5, 5]);
        pia.Write(3, MotorOnControlB);
        for (var i = 0; i < 5; i++) datasette.Tick();
        Ca1FlagSet(pia).Should().BeTrue();

        datasette.Rewind();
        datasette.IsAtEnd.Should().BeFalse();
        datasette.MotorOn.Should().BeTrue("rewinding is a tape-position operation, not a motor control");
    }

    [Test]
    public void Sense_reflects_whether_a_tape_is_loaded_independent_of_motor_state()
    {
        var pia = new Pia();
        var datasette = new PetDatasette(pia);

        datasette.Sense.Should().BeFalse("no tape is loaded yet");

        datasette.LoadTape([10]);
        datasette.Sense.Should().BeTrue();
        datasette.MotorOn.Should().BeFalse("loading a tape doesn't turn the motor on by itself");
    }
}
