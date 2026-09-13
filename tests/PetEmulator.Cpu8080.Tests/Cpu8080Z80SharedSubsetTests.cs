using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Cpu8080;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080Z80SharedSubsetTests
{
    [Test]
    public void Shared_base_subset_has_equal_register_and_memory_semantics()
    {
        var program = new byte[]
        {
            0x3E, 0x12,       // MVI A,12 / LD A,12
            0x06, 0x05,       // MVI B,05 / LD B,05
            0x80,             // ADD B
            0x04,             // INR B / INC B
            0x21, 0x00, 0x20, // LXI H,2000 / LD HL,2000
            0x36, 0x7F,       // MVI M,7F / LD (HL),7F
            0x46,             // MOV B,M / LD B,(HL)
        };
        var memory8080 = new TestMemory();
        program.CopyTo(memory8080.Bytes, 0);
        var memoryZ80 = new TestZ80Bus();
        program.CopyTo(memoryZ80.Memory, 0);

        var cpu8080 = new Cpu8080(memory8080);
        var cpuZ80 = new Z80Cpu(memoryZ80, new InterruptLines());

        for (var step = 0; step < 7; step++)
        {
            cpu8080.StepInstruction();
            cpuZ80.StepInstruction();

            cpu8080.CpuState.A.Should().Be(cpuZ80.Registers.A, $"A after shared step {step}");
            cpu8080.CpuState.B.Should().Be(cpuZ80.Registers.B, $"B after shared step {step}");
            cpu8080.CpuState.H.Should().Be(cpuZ80.Registers.H, $"H after shared step {step}");
            cpu8080.CpuState.L.Should().Be(cpuZ80.Registers.L, $"L after shared step {step}");
            memory8080.Bytes[0x2000].Should().Be(memoryZ80.Memory[0x2000], $"memory after shared step {step}");
        }
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];
        public byte Read(ushort address) => Bytes[address];
        public void Write(ushort address, byte value) => Bytes[address] = value;
    }

    private sealed class TestZ80Bus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }
    }
}
