using Cpu6502;
using Cpu6502.Variants;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class Cmos65C02Tests
{
    private FlatMemory? memory;
    private Cpu6502Cmos65C02? cpu;

    [SetUp]
    public void Setup()
    {
        memory = new FlatMemory();
        cpu = new Cpu6502Cmos65C02(memory);
        memory.Write(0xFFFC, 0x00);
        memory.Write(0xFFFD, 0x00);
        cpu.Reset();
    }

    private void LoadProgram(byte[] program)
    {
        ushort baseAddr = 0x8000;
        for (int i = 0; i < program.Length; i++)
            memory!.Write((ushort)(baseAddr + i), program[i]);
        cpu!.PC = baseAddr;
    }

    [Test]
    public void BRA_always_branches()
    {
        LoadProgram(new byte[] { 0x80, 0x05 });
        cpu!.StepInstruction();
        Assert.AreEqual(0x8007, cpu.PC);
    }

    [Test]
    public void PHX_pushes_X_to_stack()
    {
        cpu!.X = 0x42;
        cpu.SP = 0xFF;
        LoadProgram(new byte[] { 0xDA });
        cpu.StepInstruction();
        Assert.AreEqual(0x42, memory!.Read(0x01FF));
        Assert.AreEqual(0xFE, cpu.SP);
    }

    [Test]
    public void PLX_pulls_X_from_stack()
    {
        memory!.Write(0x01FF, 0x42);
        cpu!.SP = 0xFE;
        LoadProgram(new byte[] { 0xFA });
        cpu.StepInstruction();
        Assert.AreEqual(0x42, cpu.X);
        Assert.AreEqual(0xFF, cpu.SP);
    }

    [Test]
    public void HasJmpIndirectBug_is_false_for_65C02()
    {
        Assert.IsFalse(cpu!.HasJmpIndirectBug);
    }

    [Test]
    public void PHY_pushes_Y_to_stack()
    {
        cpu!.Y = 0x77;
        cpu.SP = 0xFF;
        LoadProgram(new byte[] { 0x5A });
        cpu.StepInstruction();
        Assert.AreEqual(0x77, memory!.Read(0x01FF));
        Assert.AreEqual(0xFE, cpu.SP);
    }

    [Test]
    public void PLY_pulls_Y_from_stack()
    {
        memory!.Write(0x01FF, 0x77);
        cpu!.SP = 0xFE;
        LoadProgram(new byte[] { 0x7A });
        cpu.StepInstruction();
        Assert.AreEqual(0x77, cpu.Y);
        Assert.AreEqual(0xFF, cpu.SP);
    }

    [Test]
    public void INC_A_wraps_and_sets_flags()
    {
        cpu!.A = 0xFF;
        LoadProgram(new byte[] { 0x1A });
        cpu.StepInstruction();
        Assert.AreEqual(0x00, cpu.A);
        Assert.IsTrue(cpu.GetFlag(0x02)); // Zero
    }

    [Test]
    public void DEC_A_wraps_and_sets_flags()
    {
        cpu!.A = 0x00;
        LoadProgram(new byte[] { 0x3A });
        cpu.StepInstruction();
        Assert.AreEqual(0xFF, cpu.A);
        Assert.IsTrue(cpu.GetFlag(0x80)); // Negative
    }

    [Test]
    public void STZ_zp_writes_zero_without_touching_flags()
    {
        memory!.Write(0x0010, 0xFF);
        LoadProgram(new byte[] { 0x64, 0x10 });
        cpu!.StepInstruction();
        Assert.AreEqual(0x00, memory.Read(0x0010));
    }

    [Test]
    public void STZ_absX_writes_zero()
    {
        cpu!.X = 0x05;
        memory!.Write(0x2005, 0xAB);
        LoadProgram(new byte[] { 0x9E, 0x00, 0x20 });
        cpu.StepInstruction();
        Assert.AreEqual(0x00, memory.Read(0x2005));
    }

    [Test]
    public void TSB_zp_sets_bits_and_zero_flag()
    {
        cpu!.A = 0x0F;
        memory!.Write(0x0020, 0x00);
        LoadProgram(new byte[] { 0x04, 0x20 });
        cpu.StepInstruction();
        Assert.AreEqual(0x0F, memory.Read(0x0020));
        Assert.IsTrue(cpu.GetFlag(0x02)); // A & M(before) == 0 -> Zero set
    }

    [Test]
    public void TRB_zp_clears_bits_and_zero_flag()
    {
        cpu!.A = 0x0F;
        memory!.Write(0x0021, 0xFF);
        LoadProgram(new byte[] { 0x14, 0x21 });
        cpu.StepInstruction();
        Assert.AreEqual(0xF0, memory.Read(0x0021));
        Assert.IsFalse(cpu.GetFlag(0x02)); // A & M(before) = 0x0F != 0 -> Zero clear
    }

    [Test]
    public void BIT_immediate_sets_only_zero_flag()
    {
        cpu!.A = 0xFF;
        cpu.P = 0x00; // N,V,Z all clear beforehand
        LoadProgram(new byte[] { 0x89, 0xC0 }); // operand has N and V bits set
        cpu.StepInstruction();
        Assert.IsFalse(cpu.GetFlag(0x80), "BIT #imm must not touch N");
        Assert.IsFalse(cpu.GetFlag(0x40), "BIT #imm must not touch V");
        Assert.IsFalse(cpu.GetFlag(0x02), "A & operand != 0, Z must be clear");
    }

    [Test]
    public void BIT_zpX_sets_N_V_Z_like_classic_BIT()
    {
        cpu!.X = 0x01;
        cpu.A = 0x00;
        memory!.Write(0x0031, 0xC0); // bit7=1 (N), bit6=1 (V)
        LoadProgram(new byte[] { 0x34, 0x30 });
        cpu.StepInstruction();
        Assert.IsTrue(cpu.GetFlag(0x80), "N must be set from operand bit 7");
        Assert.IsTrue(cpu.GetFlag(0x40), "V must be set from operand bit 6");
        Assert.IsTrue(cpu.GetFlag(0x02), "A & M == 0 -> Z set");
    }

    private void SetZpIndirectPointer(byte zp, ushort target)
    {
        memory!.Write(zp, (byte)(target & 0xFF));
        memory.Write((byte)(zp + 1), (byte)(target >> 8));
    }

    [Test]
    public void ORA_zp_indirect_reads_through_pointer()
    {
        SetZpIndirectPointer(0x40, 0x3000);
        memory!.Write(0x3000, 0x0F);
        cpu!.A = 0xF0;
        LoadProgram(new byte[] { 0x12, 0x40 });
        cpu.StepInstruction();
        Assert.AreEqual(0xFF, cpu.A);
    }

    [Test]
    public void AND_zp_indirect_reads_through_pointer()
    {
        SetZpIndirectPointer(0x41, 0x3001);
        memory!.Write(0x3001, 0x0F);
        cpu!.A = 0xFF;
        LoadProgram(new byte[] { 0x32, 0x41 });
        cpu.StepInstruction();
        Assert.AreEqual(0x0F, cpu.A);
    }

    [Test]
    public void EOR_zp_indirect_reads_through_pointer()
    {
        SetZpIndirectPointer(0x42, 0x3002);
        memory!.Write(0x3002, 0xFF);
        cpu!.A = 0xFF;
        LoadProgram(new byte[] { 0x52, 0x42 });
        cpu.StepInstruction();
        Assert.AreEqual(0x00, cpu.A);
    }

    [Test]
    public void ADC_zp_indirect_reads_through_pointer()
    {
        SetZpIndirectPointer(0x43, 0x3003);
        memory!.Write(0x3003, 0x01);
        cpu!.A = 0x01;
        LoadProgram(new byte[] { 0x72, 0x43 });
        cpu.StepInstruction();
        Assert.AreEqual(0x02, cpu.A);
    }

    [Test]
    public void SBC_zp_indirect_reads_through_pointer()
    {
        SetZpIndirectPointer(0x44, 0x3004);
        memory!.Write(0x3004, 0x01);
        cpu!.A = 0x05;
        cpu.SetFlag(0x01, true); // carry set (no borrow)
        LoadProgram(new byte[] { 0xF2, 0x44 });
        cpu.StepInstruction();
        Assert.AreEqual(0x04, cpu.A);
    }

    [Test]
    public void STA_zp_indirect_writes_through_pointer()
    {
        SetZpIndirectPointer(0x45, 0x3005);
        cpu!.A = 0x55;
        LoadProgram(new byte[] { 0x92, 0x45 });
        cpu.StepInstruction();
        Assert.AreEqual(0x55, memory!.Read(0x3005));
    }

    [Test]
    public void LDA_zp_indirect_reads_through_pointer()
    {
        SetZpIndirectPointer(0x46, 0x3006);
        memory!.Write(0x3006, 0x99);
        LoadProgram(new byte[] { 0xB2, 0x46 });
        cpu!.StepInstruction();
        Assert.AreEqual(0x99, cpu.A);
    }

    [Test]
    public void CMP_zp_indirect_reads_through_pointer()
    {
        SetZpIndirectPointer(0x47, 0x3007);
        memory!.Write(0x3007, 0x10);
        cpu!.A = 0x10;
        LoadProgram(new byte[] { 0xD2, 0x47 });
        cpu.StepInstruction();
        Assert.IsTrue(cpu.GetFlag(0x02)); // equal -> Zero set
    }

    [Test]
    public void JMP_absX_resolves_indexed_indirect_address()
    {
        cpu!.X = 0x02;
        // pointer table at 0x4000; indexed by X -> reads target from 0x4002/0x4003
        memory!.Write(0x4002, 0x00);
        memory.Write(0x4003, 0x90);
        LoadProgram(new byte[] { 0x7C, 0x00, 0x40 });
        cpu.StepInstruction();
        Assert.AreEqual(0x9000, cpu.PC);
    }
}
