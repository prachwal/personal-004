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
    public void IRQ_PushesTheFullFrameInRealHardwareByteOrder()
    {
        // Regression pin for docs/pet/superpet-6809-boot-hang.md: PushFullFrame used to push
        // CC first and PC last, the exact reverse of real 6809 hardware (PC first, CC last) - so
        // after the push, CC ended up at the highest address and PC's own bytes landed where CC/A
        // belonged, with X and Y silently swapped in between. RTI (which pops in the documented
        // [CC,A,B,DP,X,Y,U,PC] order - see RTI_PullsFullFrameWhenESet above, which writes memory
        // in exactly that low-to-high order) would then resume at a garbage address instead of the
        // interrupted PC. This is exactly what happened live: the SuperPET 6809 Waterloo ROM's
        // first-ever IRQ corrupted its own return address and wandered into zeroed RAM, crashing
        // before it ever reached the code that prints the "Waterloo microSystems...Select:" menu -
        // fixing this one push order was the actual fix that made the menu render at all.
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFF8, 0x25);
        memory.Write(0xFFF9, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x2000;
        cpu.State.PC = 0x1234;
        cpu.State.A = 0x11;
        cpu.State.B = 0x22;
        cpu.State.DP = 0x33;
        cpu.State.X = 0x4455;
        cpu.State.Y = 0x6677;
        cpu.State.U = 0x8899;
        cpu.State.Flags.I = false;

        cpu.RequestIrq();
        cpu.Step();

        cpu.State.S.Should().Be(0x1FF4, "a full 12-byte frame was pushed");
        // Real 6809 hardware layout, low address to high: CC, A, B, DP, X, Y, U, PC - matching
        // RTI_PullsFullFrameWhenESet above, which writes memory in this exact order and expects
        // RTI to restore it correctly.
        memory.Read(0x1FF5).Should().Be(0x11, "A");
        memory.Read(0x1FF6).Should().Be(0x22, "B");
        memory.Read(0x1FF7).Should().Be(0x33, "DP");
        memory.Read(0x1FF8).Should().Be(0x44, "X high byte");
        memory.Read(0x1FF9).Should().Be(0x55, "X low byte");
        memory.Read(0x1FFA).Should().Be(0x66, "Y high byte");
        memory.Read(0x1FFB).Should().Be(0x77, "Y low byte");
        memory.Read(0x1FFC).Should().Be(0x88, "U high byte");
        memory.Read(0x1FFD).Should().Be(0x99, "U low byte");
        memory.Read(0x1FFE).Should().Be(0x12, "PC high byte");
        memory.Read(0x1FFF).Should().Be(0x34, "PC low byte - the interrupted return address, not swapped with anything");
    }

    [Test]
    public void FIRQ_PushesTheFastFrameInRealHardwareByteOrder()
    {
        // Same bug, 3-byte frame: PushFastFrame used to push CC before PC, so RTI's [CC,PC] pop
        // (see the E-clear branch exercised by RTI_PullsOnlyPCWhenEClear) would read PC's own high
        // byte as CC and get a 1-byte-short address for PC. Real order is PC then CC.
        var memory = new RamMemoryBus(0x10000);
        memory.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x00);
        memory.Write(0xFFF6, 0x35);
        memory.Write(0xFFF7, 0x00);
        var cpu = new M6809Cpu(memory);
        cpu.Reset();

        cpu.State.S = 0x2000;
        cpu.State.PC = 0x1234;
        cpu.State.Flags.F = false;

        cpu.RequestFirq();
        cpu.Step();

        cpu.State.S.Should().Be(0x1FFD, "a 3-byte frame was pushed");
        // PC is pushed first (ends up at the higher addresses), CC last (final/lowest S) -
        // 0x1FFD holds CC, not asserted here since its exact bits depend on flags entering the call.
        memory.Read(0x1FFE).Should().Be(0x12, "PC high byte");
        memory.Read(0x1FFF).Should().Be(0x34, "PC low byte");
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
