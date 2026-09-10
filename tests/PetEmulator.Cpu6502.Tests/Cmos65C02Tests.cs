using System;
using PetEmulator.Cpu6502;
using PetEmulator.Cpu6502.Variants;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Cpu6502.Tests;

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

    private static ulong RunAdcOrSbcZpCycles(Func<IMemoryBus, Cpu6502> makeCpu, byte opcode, bool decimalMode)
    {
        var mem = new FlatMemory();
        var c = makeCpu(mem);
        mem.Write(0xFFFC, 0x00);
        mem.Write(0xFFFD, 0x00);
        c.Reset();
        mem.Write(0x0050, 0x01);
        c.A = 0x01;
        c.SetFlag(0x08, decimalMode); // D flag
        mem.Write(0x8000, opcode);
        mem.Write(0x8001, 0x50);
        c.PC = 0x8000;
        var before = c.CycleCount;
        c.StepInstruction();
        return c.CycleCount - before;
    }

    [Test]
    public void ADC_zp_decimal_mode_on_65C02_costs_one_extra_cycle()
    {
        var binary = RunAdcOrSbcZpCycles(m => new Cpu6502Cmos65C02(m), 0x65, decimalMode: false);
        var decimal_ = RunAdcOrSbcZpCycles(m => new Cpu6502Cmos65C02(m), 0x65, decimalMode: true);
        Assert.AreEqual(3, (int)binary);
        Assert.AreEqual(4, (int)decimal_);
    }

    [Test]
    public void SBC_zp_decimal_mode_on_65C02_costs_one_extra_cycle()
    {
        var binary = RunAdcOrSbcZpCycles(m => new Cpu6502Cmos65C02(m), 0xE5, decimalMode: false);
        var decimal_ = RunAdcOrSbcZpCycles(m => new Cpu6502Cmos65C02(m), 0xE5, decimalMode: true);
        Assert.AreEqual(3, (int)binary);
        Assert.AreEqual(4, (int)decimal_);
    }

    [Test]
    public void ADC_zp_decimal_mode_on_NMOS_does_not_cost_extra_cycle()
    {
        var binary = RunAdcOrSbcZpCycles(m => new Cpu6502(m), 0x65, decimalMode: false);
        var decimal_ = RunAdcOrSbcZpCycles(m => new Cpu6502(m), 0x65, decimalMode: true);
        Assert.AreEqual(3, (int)binary);
        Assert.AreEqual(3, (int)decimal_);
    }

    [Test]
    public void NOP_0x03_implied_1byte_executes_and_advances_PC_by_1()
    {
        LoadProgram(new byte[] { 0x03 });
        var before = cpu!.CycleCount;
        cpu.StepInstruction();
        Assert.AreEqual(0x8001, cpu.PC);
        Assert.AreEqual(1, (int)(cpu.CycleCount - before));
    }

    [Test]
    public void NOP_0x9B_implied_does_not_touch_A_or_X_unlike_old_TAS()
    {
        cpu!.A = 0x12;
        cpu.X = 0x34;
        LoadProgram(new byte[] { 0x9B });
        cpu.StepInstruction();
        Assert.AreEqual(0x12, cpu.A);
        Assert.AreEqual(0x34, cpu.X);
    }

    [Test]
    public void NOP_0x02_immediate_2byte_no_longer_halts_the_cpu()
    {
        LoadProgram(new byte[] { 0x02, 0x99 });
        cpu!.StepInstruction();
        Assert.IsFalse(cpu.Halted, "65C02 must not JAM on 0x02");
        Assert.AreEqual(0x8002, cpu.PC);
    }

    [Test]
    public void NOP_0xEB_implied_does_not_perform_SBC_math_unlike_old_USBC()
    {
        cpu!.A = 0x10;
        LoadProgram(new byte[] { 0xEB, 0x05 });
        cpu.StepInstruction();
        Assert.AreEqual(0x10, cpu.A, "must not have subtracted anything");
        Assert.AreEqual(0x8001, cpu.PC);
    }

    [Test]
    public void NOP_0x5C_absolute_shaped_costs_8_cycles_and_advances_PC_by_3()
    {
        LoadProgram(new byte[] { 0x5C, 0x00, 0x20 });
        var before = cpu!.CycleCount;
        cpu.StepInstruction();
        Assert.AreEqual(0x8003, cpu.PC);
        Assert.AreEqual(8, (int)(cpu.CycleCount - before));
    }

    [Test]
    public void All_tranche2_nop_opcodes_execute_without_throwing()
    {
        byte[] impliedOpcodes = new byte[] {
            0x03,0x07,0x0F,0x13,0x17,0x1B,0x1F,0x23,0x27,0x2F,0x33,0x37,0x3B,0x3F,
            0x43,0x47,0x4F,0x53,0x57,0x5B,0x5F,0x63,0x67,0x6F,0x73,0x77,0x7B,0x7F,
            0x83,0x87,0x8B,0x8F,0x93,0x97,0x9B,0x9F,0xA3,0xA7,0xAB,0xAF,0xB3,0xB7,0xBB,0xBF,
            0xC3,0xC7,0xCF,0xD3,0xD7,0xDB,0xDF,0xE3,0xE7,0xEB,0xEF,0xF3,0xF7,0xFB,0xFF };

        foreach (var op in impliedOpcodes)
        {
            var mem = new FlatMemory();
            var c = new Cpu6502Cmos65C02(mem);
            mem.Write(0xFFFC, 0x00);
            mem.Write(0xFFFD, 0x00);
            c.Reset();
            mem.Write(0x8000, op);
            c.PC = 0x8000;
            Assert.DoesNotThrow(() => c.StepInstruction(), $"opcode 0x{op:X2} threw");
            Assert.AreEqual(0x8001, c.PC, $"opcode 0x{op:X2} should be 1 byte");
        }

        byte[] immediateOpcodes = new byte[] { 0x02, 0x22, 0x42, 0x62 };
        foreach (var op in immediateOpcodes)
        {
            var mem = new FlatMemory();
            var c = new Cpu6502Cmos65C02(mem);
            mem.Write(0xFFFC, 0x00);
            mem.Write(0xFFFD, 0x00);
            c.Reset();
            mem.Write(0x8000, op);
            c.PC = 0x8000;
            Assert.DoesNotThrow(() => c.StepInstruction(), $"opcode 0x{op:X2} threw");
            Assert.AreEqual(0x8002, c.PC, $"opcode 0x{op:X2} should be 2 bytes");
        }
    }
}
