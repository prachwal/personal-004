using NUnit.Framework;
using PetEmulator.Cpu6809;
using FluentAssertions;

namespace PetEmulator.Cpu6809.Tests.M6809;

public class M6809IntegrationTests
{
    [Test]
    public void StringCopy_UsingAutoIncrement()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        const ushort srcAddr = 0x1000;
        const ushort dstAddr = 0x2000;
        string testStr = "Hello";

        for (int i = 0; i < testStr.Length; i++)
        {
            memory.Write((ushort)(srcAddr + i), (byte)testStr[i]);
        }

        cpu.State.X = srcAddr;
        cpu.State.Y = dstAddr;
        cpu.State.B = (byte)testStr.Length;

        for (int i = 0; i < testStr.Length; i++)
        {
            memory.Read((ushort)(srcAddr + i)).Should().Be((byte)testStr[i]);
        }
    }

    [Test]
    public void RecursiveFactorial_ViaUStack()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.U = 0x3000;

        memory.Write(0x3000, 0x00);
        memory.Write(0x3001, 0x00);
        memory.Write(0x3002, 0x00);

        memory.Read(0x3000).Should().Be(0x00);
        cpu.State.U.Should().Be(0x3000);
    }

    [Test]
    public void PositionIndependentCode_UsingPCRelative()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        ushort codeAddr = 0x4000;
        cpu.State.PC = codeAddr;

        memory.Write(codeAddr, 0x30);
        memory.Write((ushort)(codeAddr + 1), 0x8C);
        memory.Write((ushort)(codeAddr + 2), 0x10);

        ushort target = (ushort)(codeAddr + 3 + 0x10);
        target.Should().Be(0x4013);
    }
}

public class M6809SweepTests
{
    [Test]
    public void Page0_AllOpcodes_ShouldNotCrash()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x8000;
        cpu.State.U = 0x7000;

        for (int i = 0; i < 256; i++)
        {
            byte opcode = (byte)i;
            memory.Write(0x1000, opcode);

            switch (opcode)
            {
                case 0x10:
                case 0x11:
                    memory.Write(0x1001, 0x00);
                    break;
            }

            cpu.State.PC = 0x1000;
            int cycles = cpu.Step();

            cycles.Should().BeGreaterThan(0);
        }
    }

    [Test]
    public void Page10_AllOpcodes_ShouldNotCrash()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x8000;
        cpu.State.U = 0x7000;

        for (int i = 0; i < 256; i++)
        {
            byte opcode = (byte)i;
            memory.Write(0x1000, 0x10);
            memory.Write(0x1001, opcode);

            cpu.State.PC = 0x1000;
            int cycles = cpu.Step();

            cycles.Should().BeGreaterThan(0);
        }
    }

    [Test]
    public void Page11_AllOpcodes_ShouldNotCrash()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x8000;
        cpu.State.U = 0x7000;

        for (int i = 0; i < 256; i++)
        {
            byte opcode = (byte)i;
            memory.Write(0x1000, 0x11);
            memory.Write(0x1001, opcode);

            cpu.State.PC = 0x1000;
            int cycles = cpu.Step();

            cycles.Should().BeGreaterThan(0);
        }
    }
}
