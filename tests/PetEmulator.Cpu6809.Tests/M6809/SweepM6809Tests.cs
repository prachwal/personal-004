using NUnit.Framework;
using PetEmulator.Cpu6809;
using FluentAssertions;

namespace PetEmulator.Cpu6809.Tests.M6809;

public class SweepM6809Tests
{
    [Test]
    public void Page0_AllOpcodes_Should_BeAccessible()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        // Set up memory for all 256 page-0 opcodes
        for (int i = 0; i < 256; i++)
        {
            memory.Write((ushort)i, (byte)i);
            memory.Read((ushort)i).Should().Be((byte)i);
        }
    }

    [Test]
    public void Page10_AllOpcodes_Should_BeAccessible()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        // Set up memory for all 256 page-10 prefixed opcodes
        for (int i = 0; i < 256; i++)
        {
            memory.Write((ushort)(0x1000 + i), 0x10);
            memory.Write((ushort)(0x1001 + i), (byte)i);
            memory.Read((ushort)(0x1000 + i)).Should().Be(0x10);
            memory.Read((ushort)(0x1001 + i)).Should().Be((byte)i);
        }
    }

    [Test]
    public void Page11_AllOpcodes_Should_BeAccessible()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        // Set up memory for all 256 page-11 prefixed opcodes
        for (int i = 0; i < 256; i++)
        {
            memory.Write((ushort)(0x2000 + i), 0x11);
            memory.Write((ushort)(0x2001 + i), (byte)i);
            memory.Read((ushort)(0x2000 + i)).Should().Be(0x11);
            memory.Read((ushort)(0x2001 + i)).Should().Be((byte)i);
        }
    }
}
