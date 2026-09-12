using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6800.Tests;

public class M6800StateTests
{
    [Test]
    public void Flags_RoundTrip_PreservesSix6800ConditionBits()
    {
        var flags = new M6800Flags
        {
            H = true,
            I = true,
            N = true,
            Z = true,
            V = true,
            C = true,
        };

        M6800Flags restored = M6800Flags.FromByte(flags.ToByte());

        restored.H.Should().BeTrue();
        restored.I.Should().BeTrue();
        restored.N.Should().BeTrue();
        restored.Z.Should().BeTrue();
        restored.V.Should().BeTrue();
        restored.C.Should().BeTrue();
    }

    [Test]
    public void Flags_ToByte_Leaves6800ReservedBitsClear()
    {
        new M6800Flags { I = true }.ToByte().Should().Be(0x10);
    }

    [Test]
    public void State_Reset_ClearsRegistersAndEnablesInterruptMask()
    {
        var state = new M6800State
        {
            A = 0xFF,
            B = 0xFF,
            X = 0xFFFF,
            StackPointer = 0xFFFF,
            PC = 0xFFFF,
            Cycles = 100,
            Halted = true,
        };

        state.Reset();

        state.A.Should().Be(0);
        state.B.Should().Be(0);
        state.X.Should().Be(0);
        state.StackPointer.Should().Be(0);
        state.PC.Should().Be(0);
        state.Cycles.Should().Be(0);
        state.Halted.Should().BeFalse();
        state.Flags.I.Should().BeTrue();
    }
}
