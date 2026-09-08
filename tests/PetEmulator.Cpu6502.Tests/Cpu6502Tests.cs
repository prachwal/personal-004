using Cpu6502.Variants;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Cpu6502.Tests;

[TestFixture]
public sealed class Cpu6502Tests
{
    [Test]
    public void Reset_loads_vector_and_executes_instructions()
    {
        var memory = new TestMemory();
        memory[0xFFFC] = 0x00;
        memory[0xFFFD] = 0x80;
        memory[0x8000] = 0xA9; // LDA #$42
        memory[0x8001] = 0x42;
        memory[0x8002] = 0x85; // STA $10
        memory[0x8003] = 0x10;

        var cpu = new Cpu6502Classic(memory);
        cpu.Reset();

        cpu.PC.Should().Be(0x8000);
        cpu.StepInstruction();
        cpu.A.Should().Be(0x42);
        cpu.CycleCount.Should().Be(2);

        cpu.StepInstruction();
        memory[0x0010].Should().Be(0x42);
        cpu.InstructionCount.Should().Be(2);
        cpu.CycleCount.Should().Be(5);
    }

    [Test]
    public void Cpu_exposes_the_core_processor_contract()
    {
        IProcessor processor = new Cpu6502Classic(new TestMemory());

        processor.Halted.Should().BeFalse();
        processor.InstructionCount.Should().Be(0);
        processor.CycleCount.Should().Be(0);
    }

    [Test]
    public void Registers_are_a_single_live_view_of_cpu_state()
    {
        var cpu = new Cpu6502Classic(new TestMemory());

        cpu.Registers.A = 0x12;
        cpu.Registers.X = 0x34;
        cpu.Registers.PC = 0x5678;

        cpu.A.Should().Be(0x12);
        cpu.X.Should().Be(0x34);
        cpu.PC.Should().Be(0x5678);
        cpu.Registers.Get("PC").Should().Be(0x5678);
    }

    [Test]
    public void Variant_initializes_hardware_quirks_without_constructor_mutation()
    {
        var classic = new Cpu6502Classic(new TestMemory());
        var nes = new Cpu6502Nes(new TestMemory());

        classic.Variant.Name.Should().Be("MOS 6502");
        classic.Variant.Quirks.Should().Be(global::Cpu6502.CpuQuirk.DecimalArithmetic | global::Cpu6502.CpuQuirk.JmpIndirectPageWrap);
        classic.DecimalModeEnabled.Should().BeTrue();
        classic.HasJmpIndirectBug.Should().BeTrue();
        nes.Variant.Name.Should().Be("Ricoh 2A03");
        nes.Variant.Quirks.Should().Be(global::Cpu6502.CpuQuirk.None);
        nes.DecimalModeEnabled.Should().BeFalse();
        nes.HasJmpIndirectBug.Should().BeFalse();
    }

    private sealed class TestMemory : IMemoryBus
    {
        private readonly byte[] _data = new byte[ushort.MaxValue + 1];

        public byte this[ushort address]
        {
            get => _data[address];
            set => _data[address] = value;
        }

        public byte Read(ushort address) => this[address];

        public void Write(ushort address, byte value) => this[address] = value;
    }
}
