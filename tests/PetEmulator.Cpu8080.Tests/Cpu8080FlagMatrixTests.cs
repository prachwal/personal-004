using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080FlagMatrixTests
{
    [TestCase(0x00, 0x00, 0x00, 0x44)]
    [TestCase(0x7F, 0x01, 0x80, 0x90)]
    [TestCase(0x0F, 0x01, 0x10, 0x10)]
    [TestCase(0xFF, 0x01, 0x00, 0x55)]
    public void Add_sets_result_and_8080_flags(byte left, byte right, byte expected, byte expectedFlags)
    {
        var cpu = Create(0x3E, left, 0xC6, right);

        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(expected);
        (cpu.CpuState.Flags & 0xD5).Should().Be(expectedFlags);
    }

    [TestCase(0x00, 0x01, 0xFF, 0x95)]
    [TestCase(0x10, 0x01, 0x0F, 0x14)]
    [TestCase(0x80, 0x80, 0x00, 0x44)]
    public void Sub_sets_result_and_borrow_flags(byte left, byte right, byte expected, byte expectedFlags)
    {
        var cpu = Create(0x3E, left, 0xD6, right);

        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(expected);
        (cpu.CpuState.Flags & 0xD5).Should().Be(expectedFlags);
    }

    [Test]
    public void Increment_and_decrement_preserve_carry()
    {
        var cpu = Create(0x37, 0x3E, 0x0F, 0x3C, 0x3D);

        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.StepInstruction();

        (cpu.CpuState.Flags & 0x01).Should().Be(0x01);
        cpu.CpuState.A.Should().Be(0x0F);
    }

    [Test]
    public void Ana_uses_the_8080_auxiliary_carry_quirk()
    {
        var cpu = Create(0x3E, 0x08, 0xE6, 0x30);

        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(0);
        (cpu.CpuState.Flags & 0x10).Should().Be(0x10);
    }

    [TestCase(0x09, 0x01, 0x10, false)]
    [TestCase(0x99, 0x01, 0x00, true)]
    public void Daa_adjusts_bcd_result(byte left, byte right, byte expected, bool carry)
    {
        var cpu = Create(0x3E, left, 0xC6, right, 0x27);

        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(expected);
        (cpu.CpuState.Flags & 0x01).Should().Be(carry ? 0x01 : 0);
    }

    private static Cpu8080 Create(params byte[] program)
    {
        var memory = new TestMemory();
        program.CopyTo(memory.Bytes, 0);
        return new Cpu8080(memory);
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}
