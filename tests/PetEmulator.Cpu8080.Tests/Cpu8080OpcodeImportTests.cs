using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080OpcodeImportTests
{
    [Test]
    public void Imported_table_contains_all_256_base_opcode_entries()
    {
        var cpu = new Cpu8080(new TestMemory());

        cpu.OpcodeDefinitions.Should().HaveCount(256);
        cpu.OpcodeDefinitions.Select(definition => definition.Key.Opcode)
            .Should().OnlyHaveUniqueItems()
            .And.HaveCount(256);
    }

    [Test]
    public void Every_imported_opcode_dispatches_without_an_unimplemented_opcode_error()
    {
        for (var opcode = 0; opcode < 256; opcode++)
        {
            var memory = new TestMemory { Bytes = { [0] = (byte)opcode } };
            var cpu = new Cpu8080(memory)
            {
                // Conditional returns/calls and stack instructions need a safe stack area.
            };
            cpu.CpuState.SP = 0x1000;

            var step = () => cpu.StepInstruction();

            step.Should().NotThrow<NotSupportedException>($"opcode 0x{opcode:X2} should have an imported handler");
        }
    }

    [Test]
    public void Imported_metadata_preserves_length_and_timing_for_representative_groups()
    {
        var cpu = new Cpu8080(new TestMemory());

        AssertDefinition(Definition(cpu, 0x01), "LXI", 3, 10);
        AssertDefinition(Definition(cpu, 0x36), "8080", 2, 10);
        AssertDefinition(Definition(cpu, 0xC3), "JMP", 3, 10);
        AssertDefinition(Definition(cpu, 0xCD), "CALL", 3, 17);
        AssertDefinition(Definition(cpu, 0x76), "HLT", 1, 7);
    }

    [Test]
    public void Imported_data_transfer_opcodes_execute_through_core_table()
    {
        var memory = new TestMemory { Bytes = { [0] = 0x3E, [1] = 0x5A, [2] = 0x47 } };
        var cpu = new Cpu8080(memory);

        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(0x5A);
        cpu.CpuState.B.Should().Be(0x5A);
        cpu.CycleCount.Should().Be(12);
    }

    [Test]
    public void Imported_alu_opcodes_update_result_and_flags()
    {
        var memory = new TestMemory { Bytes = { [0] = 0x3E, [1] = 0xFF, [2] = 0xC6, [3] = 1 } };
        var cpu = new Cpu8080(memory);

        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(0);
        (cpu.CpuState.Flags & 0x45).Should().Be(0x45);
    }

    [Test]
    public void Imported_control_flow_and_stack_opcodes_preserve_return_address()
    {
        var memory = new TestMemory { Bytes = { [0] = 0xCD, [1] = 5, [2] = 0, [5] = 0xC9 } };
        var cpu = new Cpu8080(memory);
        cpu.CpuState.SP = 0x1000;

        cpu.StepInstruction();
        cpu.CpuState.PC.Should().Be(5);
        cpu.StepInstruction();

        cpu.CpuState.PC.Should().Be(3);
        cpu.CpuState.SP.Should().Be(0x1000);
    }

    [Test]
    public void Imported_halt_opcode_stops_fetch_until_reset()
    {
        var memory = new TestMemory { Bytes = { [0] = 0x76, [1] = 0x00 } };
        var cpu = new Cpu8080(memory);

        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.Halted.Should().BeTrue();
        cpu.CpuState.PC.Should().Be(1);
        cpu.InstructionCount.Should().Be(1);
        cpu.CycleCount.Should().Be(11);
    }

    private static OpcodeDefinition<Cpu8080State> Definition(Cpu8080 cpu, byte opcode)
        => cpu.OpcodeDefinitions.Single(definition => definition.Key == OpcodeKey.Base(opcode));

    private static void AssertDefinition(OpcodeDefinition<Cpu8080State> definition, string mnemonic, byte length, byte cycles)
    {
        definition.Mnemonic.Should().Be(mnemonic);
        definition.Length.Should().Be(length);
        definition.BaseCycles.Should().Be(cycles);
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; set; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}
