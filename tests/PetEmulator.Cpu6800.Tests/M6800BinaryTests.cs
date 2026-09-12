using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6800.Tests;

public sealed class M6800BinaryTests
{
    [Test]
    public void Mc6800FunctionalBinary_CompletesAndReportsNoFailure()
    {
        var memory = new RamMemoryBus();
        var binaryPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "mc6800-functional-test.bin");
        var binary = File.ReadAllBytes(binaryPath);

        for (var offset = 0; offset < binary.Length; offset++)
            memory.Write((ushort)(0x0100 + offset), binary[offset]);

        memory.Write(0xFFFE, 0x01);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6800Cpu(memory);
        cpu.Reset();

        for (var step = 0; step < 200 && !cpu.Halted; step++)
            cpu.StepInstruction();

        cpu.Halted.Should().BeTrue("the binary ends with WAI");
        memory.Read(0x0200).Should().Be(0, "the failure code must remain clear");
        memory.Read(0x0201).Should().Be(0x10, "DAA must convert 09 + 01 to 10");
        memory.Read(0x0202).Should().Be(0x42, "indexed store/load must preserve A");
        memory.Read(0x0203).Should().Be(0x42, "PSHA/PULA must preserve A");
        memory.Read(0x0204).Should().Be(0, "extended INC must wrap FF to 00");
        memory.Read(0x0205).Should().Be(0);
        memory.Read(0x0206).Should().Be(0x55, "TAB/TBA must preserve the value");
        memory.Read(0x0207).Should().Be(0xAA, "ASLA must shift 55 to AA");
    }

    private sealed class RamMemoryBus : IMemoryBus
    {
        private readonly byte[] memory = new byte[65536];

        public byte Read(ushort address) => memory[address];

        public void Write(ushort address, byte value) => memory[address] = value;
    }
}
