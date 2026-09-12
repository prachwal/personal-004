using NUnit.Framework;
using PetEmulator.Cpu6809;
using FluentAssertions;

namespace PetEmulator.Cpu6809.Tests.M6809;

public class AddressingTests
{
    [Test]
    public void DirectAddressing_UsesDirectPageRegister()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.DP = 0x34;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0x96);
        memory.Write(0x1001, 0x56);
        memory.Write(0x3456, 0x42);

        cpu.Step();

        cpu.State.A.Should().Be(0x42);
    }

    [Test]
    public void ExtendedAddressing_Uses16bitAddress()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xB6);
        memory.Write(0x1001, 0x45);
        memory.Write(0x1002, 0x67);
        memory.Write(0x4567, 0x89);

        cpu.Step();

        cpu.State.A.Should().Be(0x89);
    }

    [Test]
    public void PostIncrement_AdvancesRegisterByOne()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.X = 0x2000;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x80);
        memory.Write(0x2000, 0x33);

        cpu.Step();

        cpu.State.X.Should().Be(0x2001);
    }

    [Test]
    public void DoublePostIncrement_AdvancesRegisterByTwo()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.X = 0x2000;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x81);
        memory.Write(0x2000, 0x44);

        cpu.Step();

        cpu.State.X.Should().Be(0x2002);
    }

    [Test]
    public void PreDecrement_DecrementsRegisterBeforeUse()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.X = 0x2000;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x82);
        memory.Write(0x1FFF, 0x55);

        cpu.Step();

        cpu.State.X.Should().Be(0x1FFF);
    }

    [Test]
    public void NoOffset_UsesRegisterDirectly()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.X = 0x3000;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x84);
        memory.Write(0x3000, 0x77);

        cpu.Step();

        cpu.State.X.Should().Be(0x3000);
    }

    [Test]
    public void BAccumulatorOffset_AddsBToRegister()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.X = 0x2000;
        cpu.State.B = 0x10;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x85);
        memory.Write(0x2010, 0x88);

        cpu.Step();

        cpu.State.X.Should().Be(0x2000);
    }

    [Test]
    public void AAccumulatorOffset_AddsAToRegister()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.X = 0x2000;
        cpu.State.A = 0x20;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x86);
        memory.Write(0x2020, 0x99);

        cpu.Step();

        cpu.State.X.Should().Be(0x2000);
    }

    [Test]
    public void Signed8bitOffset_AddsOffsetToRegister()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.X = 0x2000;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x88);
        memory.Write(0x1002, 0x50);
        memory.Write(0x2050, 0xAA);

        cpu.Step();

        cpu.State.X.Should().Be(0x2000);
    }

    [Test]
    public void Signed16bitOffset_AddsLargeOffsetToRegister()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.X = 0x2000;
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x89);
        memory.Write(0x1002, 0x01);
        memory.Write(0x1003, 0x00);
        memory.Write(0x2100, 0xBB);

        cpu.Step();

        cpu.State.X.Should().Be(0x2000);
    }

    [Test]
    public void PCRelativEightBit_UsesOffsetFromPC()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x8C);
        memory.Write(0x1002, 0x10);
        memory.Write(0x1013, 0xDD);

        cpu.Step();
    }

    [Test]
    public void PCRelativeSixteenBit_UsesLargeOffsetFromPC()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();
        cpu.State.PC = 0x1000;

        memory.Write(0x1000, 0xA6);
        memory.Write(0x1001, 0x8D);
        memory.Write(0x1002, 0x01);
        memory.Write(0x1003, 0x00);
        memory.Write(0x1104, 0xEE);

        cpu.Step();
    }
}
