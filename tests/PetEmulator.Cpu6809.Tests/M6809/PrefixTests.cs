using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Cpu6809;
using FluentAssertions;

namespace PetEmulator.Cpu6809.Tests.M6809;

public class PrefixTests
{
    [Test]
    public void OpcodeMetadata_DistinguishesImplementedAndUnsupportedPrefixedEntries()
    {
        var cpu = new M6809Cpu(new RamMemoryBus(0x10000));

        cpu.Opcodes.Get(new OpcodeKey(0x00, 0x13)).Should().NotBeNull();
        cpu.Opcodes.Get(new OpcodeKey(0x10, 0x20)).Mnemonic.Should().Be("OP $20");
        cpu.Opcodes.Get(new OpcodeKey(0x10, 0x00)).Mnemonic.Should().Be("OP $00");
        cpu.Opcodes.Get(new OpcodeKey(0x11, 0x00)).Mnemonic.Should().Be("OP $00");
    }

    [Test]
    public void LBcc_Page10Prefix_LongBranchCarryClear_Taken()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0x24);
        memory.Write(0x1002, 0x01);
        memory.Write(0x1003, 0x00);
        cpu.State.PC = 0x1000;
        cpu.State.Flags.C = false;

        cpu.Step();

        cpu.State.PC.Should().Be(0x1104);
    }

    [Test]
    public void LBcc_Page10Prefix_LongBranchCarryClear_NotTaken()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0x24);
        memory.Write(0x1002, 0x01);
        memory.Write(0x1003, 0x00);
        cpu.State.PC = 0x1000;
        cpu.State.Flags.C = true;

        cpu.Step();

        cpu.State.PC.Should().Be(0x1004);
    }

    [Test]
    public void CMPD_Page10_CompareDImmediateWithD()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0x83);
        memory.Write(0x1002, 0x12);
        memory.Write(0x1003, 0x34);
        cpu.State.PC = 0x1000;
        cpu.State.D = 0x1234;

        cpu.Step();

        cpu.State.Flags.Z.Should().BeTrue();
    }

    [Test]
    public void CMPY_Page10_CompareYImmediateWithY()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0x8C);
        memory.Write(0x1002, 0x56);
        memory.Write(0x1003, 0x78);
        cpu.State.PC = 0x1000;
        cpu.State.Y = 0x5678;

        cpu.Step();

        cpu.State.Flags.Z.Should().BeTrue();
    }

    [Test]
    public void LDY_Page10_LoadYImmediate()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0x8E);
        memory.Write(0x1002, 0xAB);
        memory.Write(0x1003, 0xCD);
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.Y.Should().Be(0xABCD);
        cpu.State.Flags.N.Should().BeTrue();
        cpu.State.Flags.Z.Should().BeFalse();
    }

    [Test]
    public void STY_Page10_StoreYToMemory()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0xBF);
        memory.Write(0x1002, 0x34);
        memory.Write(0x1003, 0x56);
        cpu.State.PC = 0x1000;
        cpu.State.Y = 0x1122;

        cpu.Step();

        memory.Read(0x3456).Should().Be(0x11);
        memory.Read(0x3457).Should().Be(0x22);
    }

    [Test]
    public void LDS_Page10_LoadSImmediate_ArmsNMI()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0xCE);
        memory.Write(0x1002, 0x80);
        memory.Write(0x1003, 0x00);
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.S.Should().Be(0x8000);
    }

    [Test]
    public void STS_Page10_StoreSToMemory()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0xFF);
        memory.Write(0x1002, 0x20);
        memory.Write(0x1003, 0x00);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0x3344;

        cpu.Step();

        memory.Read(0x2000).Should().Be(0x33);
        memory.Read(0x2001).Should().Be(0x44);
    }

    [Test]
    public void SWI2_Page10_SoftwareInterrupt2()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFF4, 0x50);
        memory.Write(0xFFF5, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0x3F);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0x2000;

        cpu.Step();

        cpu.State.PC.Should().Be(0x5000);
        cpu.State.Flags.E.Should().BeTrue();
    }

    [Test]
    public void CMPU_Page11_CompareUImmediateWithU()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x11);
        memory.Write(0x1001, 0x83);
        memory.Write(0x1002, 0xAA);
        memory.Write(0x1003, 0xBB);
        cpu.State.PC = 0x1000;
        cpu.State.U = 0xAABB;

        cpu.Step();

        cpu.State.Flags.Z.Should().BeTrue();
    }

    [Test]
    public void CMPS_Page11_CompareSImmediateWithS()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x11);
        memory.Write(0x1001, 0x8C);
        memory.Write(0x1002, 0xCC);
        memory.Write(0x1003, 0xDD);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0xCCDD;

        cpu.Step();

        cpu.State.Flags.Z.Should().BeTrue();
    }

    [Test]
    public void SWI3_Page11_SoftwareInterrupt3()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFF2, 0x60);
        memory.Write(0xFFF3, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x11);
        memory.Write(0x1001, 0x3F);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0x2000;

        cpu.Step();

        cpu.State.PC.Should().Be(0x6000);
        cpu.State.Flags.E.Should().BeTrue();
    }

    [Test]
    public void PrefixedNOP_UnlistedPageOpcode_IsNOP()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0x00);
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.PC.Should().Be(0x1002);
    }

    [Test]
    public void LDY_Page10Direct_LoadYFromDirectPage()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0x9E);
        memory.Write(0x1002, 0x50);
        cpu.State.PC = 0x1000;
        cpu.State.DP = 0x12;
        memory.Write(0x1250, 0x34);
        memory.Write(0x1251, 0x56);

        cpu.Step();

        cpu.State.Y.Should().Be(0x3456);
    }
}
