using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080LifecycleTests
{
    [Test]
    public void Nop_fetches_from_memory_and_advances_pc_and_clock()
    {
        var memory = new TestMemory();
        memory.Bytes[0] = 0x00;
        var cpu = new Cpu8080(memory);

        cpu.StepInstruction();

        cpu.CpuState.PC.Should().Be(1);
        cpu.CycleCount.Should().Be(4);
        cpu.InstructionCount.Should().Be(1);
    }

    [Test]
    public void Opcode_table_contains_the_initial_base_opcode_metadata()
    {
        var cpu = new Cpu8080(new TestMemory());

        cpu.OpcodeDefinitions.Should().ContainSingle(definition =>
            definition.Key == OpcodeKey.Base(0x00)
            && definition.Mnemonic == "NOP"
            && definition.Length == 1
            && definition.BaseCycles == 4);
    }

    [Test]
    public void Reset_clears_cpu_counters_and_state()
    {
        var memory = new TestMemory();
        memory.Bytes[0] = 0x00;
        var cpu = new Cpu8080(memory);

        cpu.StepInstruction();
        cpu.Reset();

        cpu.CycleCount.Should().Be(0);
        cpu.InstructionCount.Should().Be(0);
        cpu.CpuState.PC.Should().Be(0);
        cpu.Halted.Should().BeFalse();
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}
