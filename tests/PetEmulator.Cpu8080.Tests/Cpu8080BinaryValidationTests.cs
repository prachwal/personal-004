using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080BinaryValidationTests
{
    [Test]
    public void Com_binary_at_0100_executes_arithmetic_branch_and_halt()
    {
        var memory = new TestMemory();
        var program =
        new byte[]
        {
            0x21, 0x00, 0x02,       // LXI H,0200
            0x36, 0x05,             // MVI M,05
            0x3E, 0x03,             // MVI A,03
            0x86,                   // ADD M => 08
            0x32, 0x01, 0x02,       // STA 0201
            0xFE, 0x08,             // CPI 08
            0xC2, 0x16, 0x01,       // JNZ failure
            0x3E, 0xA5,             // MVI A,A5
            0x32, 0x02, 0x02,       // STA 0202
            0x76,                   // HLT
            0x3E, 0xEE,             // failure: MVI A,EE
            0x32, 0x02, 0x02,       // STA 0202
            0x76,                   // HLT
        };
        program.CopyTo(memory.Bytes, 0x0100);
        var cpu = new Cpu8080(memory);
        cpu.CpuState.PC = 0x0100;

        for (var instruction = 0; instruction < 32 && !cpu.Halted; instruction++)
            cpu.StepInstruction();

        cpu.Halted.Should().BeTrue();
        memory.Bytes[0x0200].Should().Be(0x05);
        memory.Bytes[0x0201].Should().Be(0x08);
        memory.Bytes[0x0202].Should().Be(0xA5);
        cpu.CpuState.PC.Should().Be(0x0116);
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}
