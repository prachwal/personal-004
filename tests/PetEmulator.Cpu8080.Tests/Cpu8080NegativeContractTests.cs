using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080NegativeContractTests
{
    [Test]
    public void In_without_port_bus_returns_ff_and_out_is_ignored()
    {
        var memory = new TestMemory
        {
            Bytes = { [0] = 0xDB, [1] = 0x12, [2] = 0xD3, [3] = 0x34 },
        };
        var cpu = new Cpu8080(memory);

        cpu.StepInstruction();
        cpu.StepInstruction();

        cpu.CpuState.A.Should().Be(0xFF);
        cpu.CpuState.PC.Should().Be(4);
    }

    [Test]
    public void Pending_interrupt_waits_until_ei_delay_has_completed()
    {
        var memory = new TestMemory
        {
            Bytes = { [0] = 0xF3, [1] = 0xFB, [2] = 0x00 },
        };
        var interruptBus = new TestInterruptBus { Opcode = 0xCF };
        var cpu = new Cpu8080(memory, interruptBus: interruptBus);
        cpu.CpuState.SP = 0x1000;
        cpu.SetIRQ(true);

        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.CpuState.PC.Should().Be(2);
        interruptBus.AcknowledgeCount.Should().Be(0);

        cpu.StepInstruction();
        cpu.CpuState.PC.Should().Be(3);
        cpu.CpuState.InterruptsEnabled.Should().BeTrue();
        interruptBus.AcknowledgeCount.Should().Be(0);

        cpu.StepInstruction();
        cpu.CpuState.PC.Should().Be(0x0008);
        cpu.CpuState.SP.Should().Be(0x0FFE);
        cpu.CpuState.InterruptsEnabled.Should().BeFalse();
        interruptBus.AcknowledgeCount.Should().Be(1);
    }

    [Test]
    public void Invalid_interrupt_acknowledge_is_rejected_before_fetch()
    {
        var memory = new TestMemory { Bytes = { [0] = 0x00 } };
        var cpu = new Cpu8080(memory, interruptBus: new TestInterruptBus { Opcode = 0x00 });
        cpu.CpuState.InterruptsEnabled = true;
        cpu.CpuState.SP = 0x1000;
        cpu.SetIRQ(true);

        var act = () => cpu.StepInstruction();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*non-RST opcode*");
        cpu.CpuState.PC.Should().Be(0);
        cpu.CpuState.SP.Should().Be(0x1000);
        cpu.InstructionCount.Should().Be(0);
    }

    [Test]
    public void Restore_snapshot_rejects_missing_architectural_register()
    {
        var state = new Cpu8080State();
        var snapshot = new CpuStateSnapshot(
            new Dictionary<string, ulong> { ["A"] = 0x42 },
            Halted: false);

        var act = () => state.RestoreSnapshot(snapshot);

        act.Should().Throw<KeyNotFoundException>();
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

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; init; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}
