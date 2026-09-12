using NUnit.Framework;
using PetEmulator.Cpu6809;
using FluentAssertions;

namespace PetEmulator.Cpu6809.Tests.M6809;

public class Page0Tests
{
    [Test]
    public void NEG_NegatesAccumulatorA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x40);
        cpu.State.A = 0x05;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0xFB);
        cpu.State.Flags.N.Should().BeTrue();
        cpu.State.Flags.C.Should().BeTrue();
    }

    [Test]
    public void COM_ComplementsAccumulatorA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x43);
        cpu.State.A = 0x0F;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0xF0);
        cpu.State.Flags.N.Should().BeTrue();
        cpu.State.Flags.Z.Should().BeFalse();
        cpu.State.Flags.C.Should().BeTrue();
    }

    [Test]
    public void LSR_LogicalShiftRightAccumulatorA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x44);
        cpu.State.A = 0x81;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0x40);
        cpu.State.Flags.C.Should().BeTrue();
        cpu.State.Flags.N.Should().BeFalse();
    }

    [Test]
    public void TST_TestAccumulatorA_LeavesCarry()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x4D);
        cpu.State.A = 0x80;
        cpu.State.Flags.C = true;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.Flags.N.Should().BeTrue();
        cpu.State.Flags.Z.Should().BeFalse();
        cpu.State.Flags.V.Should().BeFalse();
        cpu.State.Flags.C.Should().BeTrue();
    }

    [Test]
    public void MUL_MultipleAccumulators()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x3D);
        cpu.State.A = 0x06;
        cpu.State.B = 0x07;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.D.Should().Be(0x2A);
    }

    [Test]
    public void LBRA_LongBranchAlways()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x16);
        memory.Write(0x1001, 0x01);
        memory.Write(0x1002, 0x00);
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.PC.Should().Be(0x1103);
    }

    [Test]
    public void BRA_BranchAlways()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x20);
        memory.Write(0x1001, 0x10);
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.PC.Should().Be(0x1012);
    }

    [Test]
    public void BEQ_BranchIfEqual_Taken()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x27);
        memory.Write(0x1001, 0x20);
        cpu.State.PC = 0x1000;
        cpu.State.Flags.Z = true;

        cpu.Step();

        cpu.State.PC.Should().Be(0x1022);
    }

    [Test]
    public void BEQ_BranchIfEqual_NotTaken()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x27);
        memory.Write(0x1001, 0x20);
        cpu.State.PC = 0x1000;
        cpu.State.Flags.Z = false;

        cpu.Step();

        cpu.State.PC.Should().Be(0x1002);
    }

    [Test]
    public void LEAX_LoadEffectiveAddressX()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x30);
        memory.Write(0x1001, 0x84);
        cpu.State.X = 0x2000;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.X.Should().Be(0x2000);
        cpu.State.Flags.Z.Should().BeFalse();
    }

    [Test]
    public void LEAS_LoadEffectiveAddressS_ArmsNMI()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x32);
        memory.Write(0x1001, 0x84);
        cpu.State.X = 0x3000;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.S.Should().Be(0x3000);
    }

    [Test]
    public void DAA_DecimalAdjustA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x19);
        cpu.State.A = 0x0A;
        cpu.State.Flags.C = false;
        cpu.State.Flags.H = true;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0x10);
    }

    [Test]
    public void ABX_AddBToX()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x3A);
        cpu.State.X = 0x2000;
        cpu.State.B = 0x50;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.X.Should().Be(0x2050);
    }

    [Test]
    public void TFR_TransferRegisterD_ToX()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x1F);
        memory.Write(0x1001, 0x01);
        cpu.State.D = 0x1234;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.X.Should().Be(0x1234);
    }

    [Test]
    public void TFR_TransferToPC_IsJump()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x1F);
        memory.Write(0x1001, 0x15);
        cpu.State.X = 0x5000;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.PC.Should().Be(0x5000);
    }

    [Test]
    public void ADDA_AddImmediateToA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x8B);
        memory.Write(0x1001, 0x05);
        cpu.State.A = 0x03;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0x08);
        cpu.State.Flags.N.Should().BeFalse();
        cpu.State.Flags.Z.Should().BeFalse();
        cpu.State.Flags.C.Should().BeFalse();
    }

    [Test]
    public void SUBA_SubtractImmediateFromA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x80);
        memory.Write(0x1001, 0x03);
        cpu.State.A = 0x08;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0x05);
    }

    [Test]
    public void LDA_LoadImmediateToA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x86);
        memory.Write(0x1001, 0x42);
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0x42);
        cpu.State.Flags.N.Should().BeFalse();
        cpu.State.Flags.Z.Should().BeFalse();
    }

    [Test]
    public void STA_StoreAToMemory()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0xB7);
        memory.Write(0x1001, 0x45);
        memory.Write(0x1002, 0x67);
        cpu.State.A = 0x88;
        cpu.State.PC = 0x1000;

        cpu.Step();

        memory.Read(0x4567).Should().Be(0x88);
    }

    [Test]
    public void CMPA_CompareAWithImmediate()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x81);
        memory.Write(0x1001, 0x05);
        cpu.State.A = 0x05;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.Flags.Z.Should().BeTrue();
        cpu.State.Flags.C.Should().BeFalse();
    }

    [Test]
    public void ANDA_ANDImmediateWithA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x84);
        memory.Write(0x1001, 0x0F);
        cpu.State.A = 0xF0;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0x00);
        cpu.State.Flags.Z.Should().BeTrue();
    }

    [Test]
    public void ORA_ORImmediateWithA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x8A);
        memory.Write(0x1001, 0x0F);
        cpu.State.A = 0xF0;
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.A.Should().Be(0xFF);
        cpu.State.Flags.N.Should().BeTrue();
    }

    [Test]
    public void RTS_ReturnFromSubroutine()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x39);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0x2000;
        memory.Write(0x2000, 0x50);
        memory.Write(0x2001, 0x00);

        cpu.Step();

        cpu.State.PC.Should().Be(0x5000);
    }

    [Test]
    public void PSHS_PushMultipleToS()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x34);
        memory.Write(0x1001, 0xFF);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0x2000;
        cpu.State.A = 0x11;
        cpu.State.B = 0x22;

        cpu.Step();

        cpu.State.S.Should().Be(0x1FF4);
        memory.Read(0x1FF5).Should().Be(0x11);
        memory.Read(0x1FF6).Should().Be(0x22);
    }

    [Test]
    public void PULS_PullMultipleFromS()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x35);
        memory.Write(0x1001, 0x03);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0x1FFE;
        memory.Write(0x1FFE, 0x66);
        memory.Write(0x1FFF, 0x55);

        cpu.Step();

        cpu.State.A.Should().Be(0x55);
        cpu.State.S.Should().Be(0x2000);
    }
}
