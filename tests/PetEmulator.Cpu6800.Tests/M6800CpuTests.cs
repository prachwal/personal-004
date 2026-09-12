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

    private sealed class TestCpu(IMemoryBus memory) : M6800Cpu(memory, new M6800State(), CreateTable())
    {
        public byte LastOpcode { get; private set; }

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
