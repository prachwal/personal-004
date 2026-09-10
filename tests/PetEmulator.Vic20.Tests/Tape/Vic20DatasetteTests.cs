using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Chips;
using PetEmulator.Vic20.Tape;

namespace PetEmulator.Vic20.Tests.Tape;

/// <summary>Unit-level tests against <see cref="Vic20Datasette"/> and a bare <see cref="Via6522"/>
/// - no real ROM/CPU involved, mirrors <c>PetEmulator.Pet.Tests.Tape.PetDatasetteTests</c>'s shape
/// (same lifecycle: motor gating, edge timing, sense, eject/rewind). See
/// docs/vic20-tape.md for what IS and ISN'T verified against the real KERNAL beyond this level -
/// a real ROM's own byte-decode timing is a separate, not-yet-confirmed question this file does
/// not attempt to answer.</summary>
public sealed class Vic20DatasetteTests
{
    private const byte MotorOnPcr = 0x0C; // CA2 manual output, held low - active-low motor-on (see Vic20Datasette.MotorOn)
    private const byte MotorOffPcr = 0x0E; // CA2 manual output, held high - motor off

    private static bool Ca1FlagSet(Via6522 via) => (via.Read(Via6522.InterruptFlag) & Via6522.Ca1Interrupt) != 0;

    [Test]
    public void Motor_stays_off_and_no_edge_occurs_until_pcr_turns_it_on()
    {
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);
        datasette.LoadTape([100, 200]);
        datasette.PressPlay();

        for (var i = 0; i < 500; i++) datasette.Tick();

        datasette.MotorOn.Should().BeFalse();
        Ca1FlagSet(via).Should().BeFalse();
    }

    [Test]
    public void Forces_a_falling_edge_at_each_pulse_boundary_once_motor_is_on()
    {
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);
        datasette.LoadTape([100, 200, 50]);
        datasette.PressPlay();
        via.Write(Via6522.PeripheralControl, MotorOnPcr);

        datasette.MotorOn.Should().BeTrue();

        for (var i = 0; i < 99; i++) datasette.Tick();
        Ca1FlagSet(via).Should().BeFalse("the first pulse (100 cycles) hasn't elapsed yet");

        datasette.Tick(); // 100th tick: first pulse boundary
        Ca1FlagSet(via).Should().BeTrue();
        via.CA1.Should().BeTrue("the line always settles high after a boundary");
        via.Read(Via6522.OraWithoutHandshake); // reading Port A clears the CA1 flag, same as the real KERNAL ISR would

        for (var i = 0; i < 199; i++) datasette.Tick();
        Ca1FlagSet(via).Should().BeFalse("the second pulse (200 cycles) hasn't elapsed yet");

        datasette.Tick(); // second boundary
        Ca1FlagSet(via).Should().BeTrue("every boundary is a falling edge, not just every other one");
        via.Read(Via6522.OraWithoutHandshake);

        for (var i = 0; i < 49; i++) datasette.Tick();
        datasette.IsAtEnd.Should().BeFalse();
        datasette.Tick(); // third and final boundary
        Ca1FlagSet(via).Should().BeTrue();
        datasette.IsAtEnd.Should().BeTrue();

        via.Read(Via6522.OraWithoutHandshake);
        datasette.Tick(); // ticking past the end is a no-op
        Ca1FlagSet(via).Should().BeFalse();
    }

    [Test]
    public void Turning_the_motor_off_mid_tape_stops_advancing()
    {
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);
        datasette.LoadTape([10, 10, 10]);
        datasette.PressPlay();
        via.Write(Via6522.PeripheralControl, MotorOnPcr);
        for (var i = 0; i < 10; i++) datasette.Tick();
        Ca1FlagSet(via).Should().BeTrue();
        via.Read(Via6522.OraWithoutHandshake);

        via.Write(Via6522.PeripheralControl, MotorOffPcr);
        datasette.MotorOn.Should().BeFalse();
        for (var i = 0; i < 100; i++) datasette.Tick();
        Ca1FlagSet(via).Should().BeFalse("the motor is off, so no further edges should occur");

        via.Write(Via6522.PeripheralControl, MotorOnPcr);
        for (var i = 0; i < 9; i++) datasette.Tick();
        Ca1FlagSet(via).Should().BeFalse();
        datasette.Tick();
        Ca1FlagSet(via).Should().BeTrue("the second pulse's remaining count should resume, not restart");
    }

    [Test]
    public void Rewind_resets_playback_position_without_touching_the_motor()
    {
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);
        datasette.LoadTape([5, 5]);
        datasette.PressPlay();
        via.Write(Via6522.PeripheralControl, MotorOnPcr);
        for (var i = 0; i < 5; i++) datasette.Tick();
        Ca1FlagSet(via).Should().BeTrue();

        datasette.Rewind();
        datasette.IsAtEnd.Should().BeFalse();
        datasette.MotorOn.Should().BeTrue("rewinding is a tape-position operation, not a motor control");
    }

    [Test]
    public void Sense_reflects_whether_play_is_pressed_independent_of_tape_or_motor_state()
    {
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);

        datasette.Sense.Should().BeFalse("play hasn't been pressed yet");

        datasette.PressPlay();
        datasette.Tick(); // PA7 only updates when Tick() runs - see Vic20Datasette.Tick's doc comment
        datasette.Sense.Should().BeTrue("a real deck's sense switch closes on ANY transport button, even with no tape loaded");
        datasette.MotorOn.Should().BeFalse("pressing play doesn't turn the motor on by itself - that's software-driven");

        datasette.Stop();
        datasette.Sense.Should().BeFalse();
    }

    [Test]
    public void PressPlay_ClosesThePa7SenseLineOnVia1()
    {
        // The actual wiring PetMachine's PIA1 PA4 equivalent has for VIC-20: real KERNAL LOAD
        // reads VIA1 Port A bit 7 to detect PLAY - see docs/vic20-tape.md.
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);
        datasette.Tick();
        (via.PortAInput & 0x80).Should().Be(0x80, "PA7 idles high (not pressed) - active low");

        datasette.PressPlay();
        datasette.Tick();

        (via.PortAInput & 0x80).Should().Be(0, "PA7 goes low the instant PLAY is (emulated-)pressed");
    }

    [Test]
    public void LoadingATape_DoesNotPressPlayForYou()
    {
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);
        datasette.PressPlay();

        datasette.LoadTape([10]);

        datasette.Sense.Should().BeFalse("loading a fresh tape releases play, matching a real deck");
        datasette.HasTape.Should().BeTrue();
    }

    [Test]
    public void Eject_ClearsTheTapeAndReleasesPlay()
    {
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);
        datasette.LoadTape([10, 20], "game.tap");
        datasette.PressPlay();

        datasette.Eject();

        datasette.HasTape.Should().BeFalse();
        datasette.TapeName.Should().BeNull();
        datasette.Sense.Should().BeFalse();
    }

    [Test]
    [CancelAfter(5_000)]
    public void PlayPressed_ButMotorOff_DoesNotAdvanceTheTape()
    {
        var via = new Via6522();
        var datasette = new Vic20Datasette(via);
        datasette.LoadTape([10, 10]);
        datasette.PressPlay(); // motor never turned on via PCR

        for (var i = 0; i < 100; i++) datasette.Tick();

        Ca1FlagSet(via).Should().BeFalse("real tape only moves when both the motor is on AND play is pressed");
    }
}
