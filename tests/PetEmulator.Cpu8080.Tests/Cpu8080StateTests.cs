using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080StateTests
{
    [Test]
    public void Register_pairs_round_trip_through_their_byte_registers()
    {
        var state = new Cpu8080State { BC = 0x1234, DE = 0x5678, HL = 0x9ABC };

        state.B.Should().Be(0x12);
        state.C.Should().Be(0x34);
        state.DE.Should().Be(0x5678);
        state.HL.Should().Be(0x9ABC);
    }

    [Test]
    public void Reset_clears_8080_state_and_interrupt_enable()
    {
        var state = new Cpu8080State
        {
            A = 0xFF,
            PC = 0x1234,
            SP = 0x5678,
            Flags = 0xD7,
            InterruptsEnabled = true,
            Halted = true,
        };

        state.Reset();

        state.GetRegisters().Values.Should().OnlyContain(value => value == 0);
        state.Halted.Should().BeFalse();
    }

    [Test]
    public void Snapshot_restore_preserves_registers_and_lifecycle_state()
    {
        var state = new Cpu8080State
        {
            A = 0x42,
            BC = 0x1234,
            DE = 0x5678,
            HL = 0x9ABC,
            PC = 0x1357,
            SP = 0x2468,
            Flags = 0x95,
            InterruptsEnabled = true,
            Halted = true,
        };
        var snapshot = state.CaptureSnapshot();

        state.Reset();
        state.RestoreSnapshot(snapshot);

        state.Registers.Should().Be(new Cpu8080Registers(0x42, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0x1357, 0x2468, 0x95));
        state.InterruptsEnabled.Should().BeTrue();
        state.Halted.Should().BeTrue();
    }
}
