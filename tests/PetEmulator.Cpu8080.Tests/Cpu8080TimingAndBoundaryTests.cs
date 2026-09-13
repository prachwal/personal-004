using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080TimingAndBoundaryTests
{
    [Test]
    public void Fetch_at_ffff_wraps_program_counter()
    {
        var memory = new TestMemory();
        memory.Bytes[0xFFFF] = 0x00;
        var cpu = new Cpu8080(memory);
        cpu.CpuState.PC = 0xFFFF;

        cpu.StepInstruction();

        cpu.CpuState.PC.Should().Be(0);
        cpu.CycleCount.Should().Be(4);
    }

    [Test]
    public void Immediate_word_at_memory_end_wraps_to_address_zero()
    {
        var memory = new TestMemory();
        memory.Bytes[0xFFFE] = 0x01;
        memory.Bytes[0xFFFF] = 0x34;
        memory.Bytes[0x0000] = 0x12;
        var cpu = new Cpu8080(memory);
        cpu.CpuState.PC = 0xFFFE;

        cpu.StepInstruction();

        cpu.CpuState.BC.Should().Be(0x1234);
        cpu.CpuState.PC.Should().Be(1);
        cpu.CycleCount.Should().Be(10);
    }

    [Test]
    public void Conditional_call_not_taken_still_consumes_its_operand()
    {
        var memory = new TestMemory { Bytes = { [0] = 0xC4, [1] = 0x34, [2] = 0x12 } };
        var cpu = new Cpu8080(memory);
        cpu.CpuState.Flags = 0x40; // Z: CNZ is false.
        cpu.CpuState.SP = 0x1000;

        cpu.StepInstruction();

        cpu.CpuState.PC.Should().Be(3);
        cpu.CpuState.SP.Should().Be(0x1000);
        cpu.CycleCount.Should().Be(11);
    }

    [Test]
    public void Conditional_call_taken_adds_the_internal_six_cycles()
    {
        var memory = new TestMemory { Bytes = { [0] = 0xC4, [1] = 0x34, [2] = 0x12 } };
        var cpu = new Cpu8080(memory);
        cpu.CpuState.SP = 0x1000;

        cpu.StepInstruction();

        cpu.CpuState.PC.Should().Be(0x1234);
        cpu.CpuState.SP.Should().Be(0x0FFE);
        cpu.CycleCount.Should().Be(17);
    }

    [Test]
    public void Push_at_zero_wraps_stack_address_and_pop_restores_pair()
    {
        var memory = new TestMemory { Bytes = { [0] = 0xC5, [1] = 0xC1 } };
        var cpu = new Cpu8080(memory);
        cpu.CpuState.BC = 0x1234;
        cpu.CpuState.SP = 0;

        cpu.StepInstruction();
        cpu.CpuState.BC = 0;
        cpu.StepInstruction();

        cpu.CpuState.BC.Should().Be(0x1234);
        cpu.CpuState.SP.Should().Be(0);
        memory.Bytes[0xFFFF].Should().Be(0x12);
        memory.Bytes[0xFFFE].Should().Be(0x34);
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}
