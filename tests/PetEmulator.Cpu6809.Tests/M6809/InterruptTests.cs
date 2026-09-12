using NUnit.Framework;
using PetEmulator.Cpu6809;
using FluentAssertions;

namespace PetEmulator.Cpu6809.Tests.M6809;

public class InterruptTests
{
    [Test]
    public void NMI_PushesFullFrame_AfterArming()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFFC, 0x30);
        memory.Write(0xFFFD, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        // LDS (page 0x10) to arm NMI
        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0xCE);
        memory.Write(0x1002, 0x20);
        memory.Write(0x1003, 0x00);
        cpu.State.PC = 0x1000;
        cpu.State.A = 0x11;
        cpu.State.B = 0x22;
        cpu.State.DP = 0x33;
        cpu.State.X = 0x4455;
        cpu.State.Y = 0x6677;
        cpu.State.U = 0x8899;

        cpu.Step();
        cpu.State.S.Should().Be(0x2000);

        cpu.RequestNmi();
        cpu.Step();

        cpu.State.PC.Should().Be(0x3000);
        cpu.State.Flags.I.Should().BeTrue();
        cpu.State.Flags.F.Should().BeTrue();
    }

    [Test]
    public void IRQ_PushesFullFrame()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFF8, 0x25);
        memory.Write(0xFFF9, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x2000;
        cpu.State.PC = 0x1000;
        cpu.State.Flags.I = false;

        cpu.RequestIrq();
        cpu.Step();

        cpu.State.PC.Should().Be(0x2500);
        cpu.State.Flags.I.Should().BeTrue();
    }

    [Test]
    public void FIRQ_PushesFastFrame()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFF6, 0x35);
        memory.Write(0xFFF7, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x2000;
        cpu.State.PC = 0x1000;
        cpu.State.Flags.F = false;

        cpu.RequestFirq();
        cpu.Step();

        cpu.State.PC.Should().Be(0x3500);
        cpu.State.Flags.E.Should().BeFalse();
        cpu.State.Flags.F.Should().BeTrue();
        cpu.State.Flags.I.Should().BeTrue();
    }

    [Test]
    public void RTI_PullsFullFrameWhenESet()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x1FE8;
        cpu.State.PC = 0x1000;

        memory.Write(0x1FE8, 0x80);
        memory.Write(0x1FE9, 0x11);
        memory.Write(0x1FEA, 0x22);
        memory.Write(0x1FEB, 0x33);
        memory.Write(0x1FEC, 0x44);
        memory.Write(0x1FED, 0x55);
        memory.Write(0x1FEE, 0x66);
        memory.Write(0x1FEF, 0x77);
        memory.Write(0x1FF0, 0x88);
        memory.Write(0x1FF1, 0x99);
        memory.Write(0x1FF2, 0xAA);
        memory.Write(0x1FF3, 0xBB);

        memory.Write(0x1000, 0x3B);
        cpu.Step();

        cpu.State.S.Should().Be(0x1FF4);
    }

    [Test]
    public void RTI_PullsOnlyPCWhenEClear()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x1FF8;
        cpu.State.PC = 0x1000;

        memory.Write(0x1FF8, 0x00);
        memory.Write(0x1FF9, 0x45);
        memory.Write(0x1FFA, 0x67);

        memory.Write(0x1000, 0x3B);
        cpu.Step();

        cpu.State.PC.Should().Be(0x4567);
        cpu.State.S.Should().Be(0x1FFB);
    }

    [Test]
    public void SWI_VectorsTo0xFFFA()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFFA, 0x40);
        memory.Write(0xFFFB, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x3F);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0x2000;

        cpu.Step();

        cpu.State.PC.Should().Be(0x4000);
        cpu.State.Flags.I.Should().BeTrue();
        cpu.State.Flags.F.Should().BeTrue();
    }

    [Test]
    public void SWI2_VectorsTo0xFFF4()
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
        cpu.State.Flags.I.Should().BeTrue();
        cpu.State.Flags.F.Should().BeTrue();
    }

    [Test]
    public void SWI3_VectorsTo0xFFF2()
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
        cpu.State.Flags.I.Should().BeTrue();
        cpu.State.Flags.F.Should().BeTrue();
    }

    [Test]
    public void CWAI_WaitAndPushFrame()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x3C);
        memory.Write(0x1001, 0x00);
        cpu.State.PC = 0x1000;
        cpu.State.S = 0x2000;

        cpu.Step();

        cpu.State.Halted.Should().BeTrue();
        cpu.State.Flags.E.Should().BeTrue();
    }

    [Test]
    public void SYNC_WaitForInterrupt()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x13);
        cpu.State.PC = 0x1000;

        cpu.Step();

        cpu.State.Halted.Should().BeTrue();
    }

    [Test]
    public void NMI_NotPendingUntilSWritten()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.RequestNmi();
        cpu.Step();

        cpu.State.PC.Should().Be(0x0002);
    }

    [Test]
    public void LDS_ArmsNMI_ThenInterruptDelivered()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFFC, 0x40);
        memory.Write(0xFFFD, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x10);
        memory.Write(0x1001, 0xCE);
        memory.Write(0x1002, 0x80);
        memory.Write(0x1003, 0x00);
        cpu.State.PC = 0x1000;

        cpu.RequestNmi();
        cpu.Step();

        cpu.State.S.Should().Be(0x8000);
        cpu.RequestNmi();
        cpu.Step();

        cpu.State.PC.Should().Be(0x4000);
    }

    [Test]
    public void IRQ_BlockedByIFlag()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x12);
        cpu.State.PC = 0x1000;
        cpu.State.Flags.I = true;

        cpu.RequestIrq();
        cpu.Step();

        cpu.State.PC.Should().Be(0x1001);
    }

    [Test]
    public void FIRQ_BlockedByFFlag()
    {
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        memory.Write(0x1000, 0x12);
        cpu.State.PC = 0x1000;
        cpu.State.Flags.F = true;

        cpu.RequestFirq();
        cpu.Step();

        cpu.State.PC.Should().Be(0x1001);
    }
}
