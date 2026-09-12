using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Vic20.Tape;

namespace PetEmulator.Vic20.Tests.Tape;

public sealed class Vic20CassetteLinesTests
{
    [Test]
    public void Motor_is_active_low_on_via1_ca2_manual_output()
    {
        var via1 = new MOS6522();
        var lines = new Vic20CassetteLines(via1, new MOS6522());

        via1.Write(MOS6522.PeripheralControl, 0x0C);
        lines.MotorOn.Should().BeTrue();

        via1.Write(MOS6522.PeripheralControl, 0x0E);
        lines.MotorOn.Should().BeFalse();
    }

    [Test]
    public void Play_sense_is_active_low_on_via1_pa6_and_preserves_other_bits()
    {
        var via1 = new MOS6522 { PortAInput = 0xE5 };
        var lines = new Vic20CassetteLines(via1, new MOS6522());

        lines.SetPlaySense(true);
        via1.PortAInput.Should().Be(0xA5);
        lines.Sense.Should().BeTrue();

        lines.SetPlaySense(false);
        via1.PortAInput.Should().Be(0xE5);
        lines.Sense.Should().BeFalse();
    }

    [Test]
    public void Pulse_read_generates_a_ca1_interrupt_transition()
    {
        var via2 = new MOS6522();
        var lines = new Vic20CassetteLines(new MOS6522(), via2);

        lines.PulseRead();

        (via2.Read(MOS6522.InterruptFlag) & MOS6522.Ca1Interrupt).Should().NotBe(0);
        via2.CA1.Should().BeTrue();
    }

    [Test]
    public void Write_level_is_read_from_via2_pb3_output()
    {
        var via2 = new MOS6522();
        var lines = new Vic20CassetteLines(new MOS6522(), via2);

        lines.WriteLevel.Should().BeFalse();
        via2.Write(MOS6522.Ddrb, 0x08);
        via2.Write(MOS6522.Orb, 0x08);
        lines.WriteLevel.Should().BeTrue();

        via2.Write(MOS6522.Ddrb, 0x00);
        lines.WriteLevel.Should().BeFalse();
    }

    [Test]
    public void Write_recorder_captures_complete_pb3_transition_intervals()
    {
        var via2 = new MOS6522();
        var lines = new Vic20CassetteLines(new MOS6522(), via2);
        var recorder = new Vic20CassetteWriteRecorder(() => lines.WriteLevel);

        via2.Write(MOS6522.Ddrb, 0x08);
        recorder.Begin();
        for (var i = 0; i < 3; i++) recorder.Tick();
        via2.Write(MOS6522.Orb, 0x08);
        recorder.Tick();
        for (var i = 0; i < 4; i++) recorder.Tick();
        via2.Write(MOS6522.Orb, 0x00);
        recorder.Tick();
        recorder.End();

        recorder.PulseCycles.Should().Equal([4, 5]);
    }

    [Test]
    public void Write_recorder_is_not_advanced_when_not_recording()
    {
        var recorder = new Vic20CassetteWriteRecorder(() => true);

        recorder.Tick();

        recorder.IsRecording.Should().BeFalse();
        recorder.PulseCycles.Should().BeEmpty();
    }
}
