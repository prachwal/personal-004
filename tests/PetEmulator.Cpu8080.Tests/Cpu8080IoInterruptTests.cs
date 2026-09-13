using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080IoInterruptTests
{
    [Test]
    public void In_and_out_use_the_optional_8_bit_port_bus()
    {
        var memory = new TestMemory { Bytes = { [0] = 0xDB, [1] = 0x12, [2] = 0xD3, [3] = 0x34 } };
        var ports = new TestPorts { Input = 0xA6 };
        var cpu = new Cpu8080(memory, ports: ports);

        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(0xA6);
        ports.LastPort.Should().Be(0x34);
        ports.LastValue.Should().Be(0xA6);
    }

    [Test]
    public void Ei_enables_interrupts_after_the_following_instruction()
    {
        var memory = new TestMemory { Bytes = { [0] = 0xFB, [1] = 0x00 } };
        var interruptBus = new TestInterruptBus { Opcode = 0xCF };
        var cpu = new Cpu8080(memory, interruptBus: interruptBus);
        cpu.CpuState.SP = 0x1000;

        cpu.StepInstruction();
        cpu.CpuState.InterruptsEnabled.Should().BeFalse();
        cpu.StepInstruction();
        cpu.CpuState.InterruptsEnabled.Should().BeTrue();

        cpu.SetIRQ(true);
        cpu.StepInstruction();

        cpu.CpuState.PC.Should().Be(0x0008);
        cpu.CpuState.SP.Should().Be(0x0FFE);
        cpu.CpuState.InterruptsEnabled.Should().BeFalse();
        interruptBus.AcknowledgeCount.Should().Be(1);
    }

    [Test]
    public void Di_cancels_pending_ei_and_blocks_interrupt_acceptance()
    {
        var memory = new TestMemory { Bytes = { [0] = 0xFB, [1] = 0xF3, [2] = 0x00 } };
        var interruptBus = new TestInterruptBus { Opcode = 0xFF };
        var cpu = new Cpu8080(memory, interruptBus: interruptBus);

        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.SetIRQ(true);
        cpu.StepInstruction();

        cpu.CpuState.PC.Should().Be(3);
        interruptBus.AcknowledgeCount.Should().Be(0);
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }

    private sealed class TestPorts : IPortBus
    {
        public byte Input { get; init; }
        public ushort LastPort { get; private set; }
        public byte LastValue { get; private set; }

        public byte Read(ushort port)
        {
            LastPort = port;
            return Input;
        }

        public void Write(ushort port, byte value)
        {
            LastPort = port;
            LastValue = value;
        }
    }

    private sealed class TestInterruptBus : ICpu8080InterruptBus
    {
        public byte Opcode { get; init; }
        public int AcknowledgeCount { get; private set; }

        public byte AcknowledgeInterrupt()
        {
            AcknowledgeCount++;
            return Opcode;
        }
    }
}
