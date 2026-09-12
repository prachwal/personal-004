using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6800.Tests;

public class M6800CpuTests
{
    [Test]
    public void StepInstruction_FetchesOpcodeAndAccountsCycles()
    {
        var memory = new TestMemoryBus();
        memory.Write(0xFFFE, 0x10);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0x1000, 0x42);
        var cpu = new TestCpu(memory);

        cpu.Reset();
        cpu.StepInstruction();

        cpu.LastOpcode.Should().Be(0x42);
        cpu.State.PC.Should().Be(0x1001);
        cpu.CycleCount.Should().Be(2);
        cpu.InstructionCount.Should().Be(1);
    }

    [Test]
    public void CommonAddressing_ReadsDirectAndExtendedOperands()
    {
        var memory = new TestMemoryBus();
        var cpu = new TestCpu(memory);
        memory.Write(0x0012, 0x34);
        memory.Write(0x0040, 0x12);
        memory.Write(0x0041, 0x34);
        cpu.State.PC = 0x0100;
        memory.Write(0x0100, 0x12);
        memory.Write(0x0101, 0x00);
        memory.Write(0x0102, 0x40);
        memory.Write(0x0103, 0x00);

        cpu.FetchDirect().Should().Be(0x34);
        cpu.Ld16Extended().Should().Be(0x1234);
    }

    [Test]
    public void CommonAlu_UsesBaseStateAndFlags()
    {
        var cpu = new TestCpu(new TestMemoryBus());
        cpu.State.A = 0x7F;

        cpu.ExecuteAddA(0x01).Should().Be(2);

        cpu.State.A.Should().Be(0x80);
        cpu.State.Flags.N.Should().BeTrue();
        cpu.State.Flags.V.Should().BeTrue();
        cpu.State.Flags.C.Should().BeFalse();
    }

    [Test]
    public void Mc6800OpcodeMetadata_DefinesCompleteByteMap()
    {
        var cpu = new M6800Processor(new TestMemoryBus());

        cpu.OpcodeMetadata.Definitions.Should().HaveCount(256);
        cpu.OpcodeMetadata.Definitions.Count(definition => definition.IsImplemented).Should().BeGreaterThan(100);
    }

    [Test]
    public void Mc6800ImplementedOpcodes_ExecuteWithoutDispatchFailures()
    {
        var memory = new TestMemoryBus();
        var cpu = new M6800Processor(memory);

        foreach (var definition in cpu.OpcodeMetadata.Definitions.Where(definition => definition.IsImplemented))
        {
            memory.Write(0xFFFE, 0x01);
            memory.Write(0xFFFF, 0x00);
            memory.Write(0x0100, definition.Opcode);
            cpu.Reset();

            var action = () => cpu.StepInstruction();
            action.Should().NotThrow($"opcode 0x{definition.Opcode:X2} ({definition.Mnemonic})");
        }
    }

    [Test]
    public void Mc6800ImmediateAndIndexedInstructions_Use6800Addressing()
    {
        var memory = new TestMemoryBus();
        var cpu = new M6800Processor(memory);
        memory.Write(0xFFFE, 0x01);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0x0100, 0x86); // LDAA #$42
        memory.Write(0x0101, 0x42);
        memory.Write(0x0102, 0xA6); // LDAA 1,X
        memory.Write(0x0103, 0x01);
        memory.Write(0x0201, 0x77);

        cpu.Reset();
        cpu.State.X = 0x0200;
        cpu.StepInstruction();
        cpu.State.A.Should().Be(0x42);
        cpu.StepInstruction();
        cpu.State.A.Should().Be(0x77);
    }

    private sealed class TestCpu(IMemoryBus memory) : M6800Cpu(memory, new M6800State(), CreateTable())
    {
        public byte LastOpcode { get; private set; }

        public int ExecuteAddA(byte value) => AddA(value);

        protected override int ExecuteOpcode(byte opcode)
        {
            LastOpcode = opcode;
            return 2;
        }

        private static M6800OpcodeTable CreateTable() => new();
    }

    private sealed class TestMemoryBus : IMemoryBus
    {
        private readonly byte[] _memory = new byte[65536];

        public byte Read(ushort address) => _memory[address];
        public void Write(ushort address, byte value) => _memory[address] = value;
    }
}
