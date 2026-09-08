using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class IllegalOpcodesTests
{
    private FlatMemory? memory;
    private Cpu6502? cpu;

    [SetUp]
    public void Setup()
    {
        memory = new FlatMemory();
        cpu = new Cpu6502(memory);
        memory.Write(0xFFFC, 0x00);
        memory.Write(0xFFFD, 0x00);
        cpu.Reset();
    }

    private void LoadProgram(byte[] program)
    {
        ushort baseAddr = 0x0100;
        for (int i = 0; i < program.Length; i++)
            memory!.Write((ushort)(baseAddr + i), program[i]);
        cpu!.PC = baseAddr;
        var state = cpu.GetState();
        state.Sync = true;
        cpu.SetState(state);
    }

    private void ExecuteOne()
    {
        do
        {
            cpu!.Tick();
        }
        while (!cpu!.GetState().Sync);
    }

    private void SetZp(byte addr, byte value) => memory!.Write(addr, value);
    private void SetAbs(ushort addr, byte value) => memory!.Write(addr, value);

    #region LAX Tests

    [Test]
    public void LAX_Zp_LoadsAAndX_Simple()
    {
        SetZp(0x10, 0x42);
        LoadProgram([0xA7, 0x10, 0xEA]); // LAX Zero Page, then NOP
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x42));
        Assert.That(cpu!.X, Is.EqualTo(0x42));
    }

    [Test]
    public void LAX_Zp_LoadsAAndX()
    {
        SetZp(0x10, 0x42);
        LoadProgram([0xA7, 0x10]); // LAX Zero Page
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x42));
        Assert.That(cpu!.X, Is.EqualTo(0x42));
        Assert.That(cpu!.PC, Is.EqualTo(0x0102));
        Assert.That(cpu!.P & 0x80, Is.EqualTo(0x00)); // N=0
        Assert.That(cpu!.P & 0x02, Is.EqualTo(0x00)); // Z=0
    }

    [Test]
    public void LAX_Zp_SetsZeroFlag()
    {
        SetZp(0x10, 0x00);
        LoadProgram([0xA7, 0x10]); // LAX Zero Page
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu!.X, Is.EqualTo(0x00));
        Assert.That(cpu!.P & 0x02, Is.EqualTo(0x02)); // Z=1
    }

    [Test]
    public void LAX_Zp_SetsNegativeFlag()
    {
        SetZp(0x10, 0x80);
        LoadProgram([0xA7, 0x10]); // LAX Zero Page
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x80));
        Assert.That(cpu!.X, Is.EqualTo(0x80));
        Assert.That(cpu!.P & 0x80, Is.EqualTo(0x80)); // N=1
    }

    [Test]
    public void LDA_Abs_LoadsA()
    {
        SetAbs(0x1234, 0x55);
        
        LoadProgram([0xAD, 0x34, 0x12]); // LDA Absolute
        // Verify program bytes
        Assert.That(memory!.Read(0x0100), Is.EqualTo(0xAD));
        Assert.That(memory!.Read(0x0101), Is.EqualTo(0x34));
        Assert.That(memory!.Read(0x0102), Is.EqualTo(0x12));
        Assert.That(memory!.Read(0x1234), Is.EqualTo(0x55));
        
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
    }

    [Test]
    public void LAX_Abs_LoadsAAndX()
    {
        SetAbs(0x1234, 0x55);
        
        LoadProgram([0xAF, 0x34, 0x12]); // LAX Absolute
        // Verify program bytes
        Assert.That(memory!.Read(0x0100), Is.EqualTo(0xAF));
        Assert.That(memory!.Read(0x0101), Is.EqualTo(0x34));
        Assert.That(memory!.Read(0x0102), Is.EqualTo(0x12));
        Assert.That(memory!.Read(0x1234), Is.EqualTo(0x55));
        
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
        Assert.That(cpu!.X, Is.EqualTo(0x55));
    }

    [Test]
    public void LAX_AbsY_LoadsAAndX()
    {
        cpu!.Y = 0x05;
        SetAbs(0x1234 + 0x05, 0xAA);
        LoadProgram([0xBF, 0x34, 0x12]); // LAX Absolute,Y
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0xAA));
        Assert.That(cpu!.X, Is.EqualTo(0xAA));
    }

    #endregion

    #region SAX Tests

    [Test]
    public void SAX_Zp_StoresAAndX()
    {
        cpu!.A = 0xFF;
        cpu!.X = 0x0F;
        LoadProgram([0x87, 0x10]); // SAX Zero Page
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x0F)); // A & X = 0xFF & 0x0F = 0x0F
    }

    [Test]
    public void SAX_Abs_StoresAAndX()
    {
        cpu!.A = 0xAA;
        cpu!.X = 0x55;
        LoadProgram([0x8F, 0x34, 0x12]); // SAX Absolute
        ExecuteOne();
        Assert.That(memory!.Read(0x1234), Is.EqualTo(0x00)); // 0xAA & 0x55 = 0x00
    }

    [Test]
    public void SAX_ZpY_StoresAAndX()
    {
        cpu!.A = 0xFF;
        cpu!.X = 0xFF;
        cpu!.Y = 0x03;
        LoadProgram([0x97, 0x10]); // SAX Zero Page,Y
        ExecuteOne();
        Assert.That(memory!.Read((byte)(0x10 + 0x03)), Is.EqualTo(0xFF));
    }

    #endregion

    #region DCP Tests (DEC + CMP)

    [Test]
    public void DCP_Zp_DecrementsMemoryAndCompares()
    {
        cpu!.A = 0x10;
        SetZp(0x10, 0x11);
        LoadProgram([0xC7, 0x10]); // DCP Zero Page
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x10)); // 0x11 - 1 = 0x10
        // CMP: A=0x10, memory=0x10, so A == memory -> C=1, Z=1, N=0
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x01)); // C=1
        Assert.That(cpu!.P & 0x02, Is.EqualTo(0x02)); // Z=1
    }

    [Test]
    public void DCP_Zp_SetsCarryWhenUnderflow()
    {
        cpu!.A = 0x05;
        SetZp(0x10, 0x01);
        LoadProgram([0xC7, 0x10]); // DCP Zero Page
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x00));
        // CMP: A=0x05, memory=0x00, so A > memory -> C=1, Z=0
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x01)); // C=1
        Assert.That(cpu!.P & 0x02, Is.EqualTo(0x00)); // Z=0
    }

    [Test]
    public void DCP_Zp_SetsCarryWhenBorrow()
    {
        cpu!.A = 0x05;
        SetZp(0x10, 0x0A);
        LoadProgram([0xC7, 0x10]); // DCP Zero Page
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x09));
        // CMP: A=0x05, memory=0x09, so A < memory -> C=0
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x00)); // C=0
    }

    #endregion

    #region ISC Tests (INC + SBC)

    [Test]
    public void ISC_Zp_IncrementsMemoryAndSubtracts_WithCarry()
    {
        cpu!.A = 0x20;
        SetZp(0x10, 0x05);
        // Set flag C=1 for SBC
        var state = cpu.GetState();
        state.P = 0x01; // C=1
        cpu.SetState(state);
        
        LoadProgram([0xE7, 0x10]); // ISC Zero Page
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x06)); // 0x05 + 1 = 0x06
        // SBC with C=1: A = A - operand (no borrow)
        // 0x20 - 0x06 = 0x1A
        Assert.That(cpu!.A, Is.EqualTo(0x1A));
    }

    [Test]
    public void ISC_Zp_NoCarry()
    {
        cpu!.A = 0x20;
        SetZp(0x10, 0x05);
        // Set flag C=0 for SBC (borrow)
        var state = cpu.GetState();
        state.P = 0x00; // C=0
        cpu.SetState(state);
        
        LoadProgram([0xE7, 0x10]); // ISC Zero Page
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x06));
        // SBC with C=0: A = A - operand - 1 (with borrow)
        // 0x20 - 0x06 - 1 = 0x19
        Assert.That(cpu!.A, Is.EqualTo(0x19));
    }

    #endregion

    #region RLA Tests (ROL + AND)

    [Test]
    public void RLA_Zp_RotatesLeftAndANDs()
    {
        cpu!.A = 0xFF;
        SetZp(0x10, 0x55); // 0x55 = 01010101
        // Set C=0
        var state = cpu.GetState();
        state.P = 0x00;
        cpu.SetState(state);
        
        LoadProgram([0x27, 0x10]); // RLA Zero Page
        ExecuteOne();
        // ROL 0x55 with C=0: 01010101 << 1 = 10101010 = 0xAA
        Assert.That(memory!.Read(0x10), Is.EqualTo(0xAA));
        // A & 0xAA = 0xFF & 0xAA = 0xAA
        Assert.That(cpu!.A, Is.EqualTo(0xAA));
        Assert.That(cpu!.P & 0x80, Is.EqualTo(0x80)); // N=1
    }

    [Test]
    public void RLA_Zp_WithCarryIn()
    {
        cpu!.A = 0xFF;
        SetZp(0x10, 0x55);
        // Set C=1
        var state = cpu.GetState();
        state.P = 0x01;
        cpu.SetState(state);
        
        LoadProgram([0x27, 0x10]); // RLA Zero Page
        ExecuteOne();
        // ROL 0x55 with C=1: 01010101 << 1 with C=1 = 10101011 = 0xAB
        Assert.That(memory!.Read(0x10), Is.EqualTo(0xAB));
        // A & 0xAB = 0xFF & 0xAB = 0xAB
        Assert.That(cpu!.A, Is.EqualTo(0xAB));
    }

    #endregion

    #region RRA Tests (ROR + ADC)

    [Test]
    public void RRA_Zp_RotatesRightAndADCs()
    {
        cpu!.A = 0x10;
        SetZp(0x10, 0x04); // 0x04 = 00000100
        // Set C=0
        var state = cpu.GetState();
        state.P = 0x00;
        cpu.SetState(state);
        
        LoadProgram([0x67, 0x10]); // RRA Zero Page
        ExecuteOne();
        // ROR 0x04 with C=0: 00000100 >> 1 = 00000010 = 0x02
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x02));
        // ADC: 0x10 + 0x02 + 0 = 0x12
        Assert.That(cpu!.A, Is.EqualTo(0x12));
    }

    [Test]
    public void RRA_Zp_WithCarryIn()
    {
        cpu!.A = 0x10;
        SetZp(0x10, 0x04);
        // Set C=1
        var state = cpu.GetState();
        state.P = 0x01;
        cpu.SetState(state);
        
        LoadProgram([0x67, 0x10]); // RRA Zero Page
        ExecuteOne();
        // ROR 0x04 with C=1: bit 0 goes to C, C goes to bit 7 = 10000010 = 0x82
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x82));
        // ADC: 0x10 + 0x82 + 0 = 0x92
        Assert.That(cpu!.A, Is.EqualTo(0x92));
    }

    #endregion

    #region SLO Tests (ASL + ORA)

    [Test]
    public void SLO_Zp_ShiftsLeftAndORs()
    {
        cpu!.A = 0x0F;
        SetZp(0x10, 0x11); // 0x11 = 00010001
        LoadProgram([0x07, 0x10]); // SLO Zero Page
        ExecuteOne();
        // ASL 0x11: 00010001 << 1 = 00100010 = 0x22
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x22));
        // A | 0x22 = 0x0F | 0x22 = 0x2F
        Assert.That(cpu!.A, Is.EqualTo(0x2F));
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x00)); // C=0 (bit 7 of 0x11 was 0)
    }

    [Test]
    public void SLO_Zp_SetsCarry()
    {
        cpu!.A = 0x00;
        SetZp(0x10, 0x80); // 0x80 = 10000000
        LoadProgram([0x07, 0x10]); // SLO Zero Page
        ExecuteOne();
        // ASL 0x80: 10000000 << 1 = 00000000, C=1
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x00));
        // A | 0x00 = 0x00
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x01)); // C=1
        Assert.That(cpu!.P & 0x02, Is.EqualTo(0x02)); // Z=1
    }

    #endregion

    #region SRE Tests (LSR + EOR)

    [Test]
    public void SRE_Zp_ShiftsRightAndEORs()
    {
        cpu!.A = 0xFF;
        SetZp(0x10, 0x0A); // 0x0A = 00001010
        LoadProgram([0x47, 0x10]); // SRE Zero Page
        ExecuteOne();
        // LSR 0x0A: 00001010 >> 1 = 00000101 = 0x05, C=0
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x05));
        // A ^ 0x05 = 0xFF ^ 0x05 = 0xFA
        Assert.That(cpu!.A, Is.EqualTo(0xFA));
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x00)); // C=0
    }

    [Test]
    public void SRE_Zp_SetsCarry()
    {
        cpu!.A = 0x00;
        SetZp(0x10, 0x05); // 0x05 = 00000101
        LoadProgram([0x47, 0x10]); // SRE Zero Page
        ExecuteOne();
        // LSR 0x05: 00000101 >> 1 = 00000010 = 0x02, C=1
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x02));
        // A ^ 0x02 = 0x00 ^ 0x02 = 0x02
        Assert.That(cpu!.A, Is.EqualTo(0x02));
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x01)); // C=1
    }

    #endregion

    #region ANC Tests

    [Test]
    public void ANC_Imm_ANDsAndSetsC()
    {
        cpu!.A = 0xFF;
        LoadProgram([0x0B, 0x80]); // ANC Immediate
        ExecuteOne();
        // A & 0x80 = 0x80
        Assert.That(cpu!.A, Is.EqualTo(0x80));
        Assert.That(cpu!.P & 0x80, Is.EqualTo(0x80)); // N=1
        Assert.That(cpu!.P & 0x02, Is.EqualTo(0x00)); // Z=0
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x01)); // C=1 (bit 7 set)
    }

    [Test]
    public void ALR_Imm_ANDsAndShiftsRight()
    {
        cpu!.A = 0xFF;
        // Set C=0
        var state = cpu.GetState();
        state.P = 0x00;
        cpu.SetState(state);
        
        LoadProgram([0x4B, 0xAA]); // ALR Immediate
        ExecuteOne();
        // A & 0xAA = 0xAA
        // LSR 0xAA: 10101010 >> 1 = 01010101 = 0x55, C=0
        Assert.That(cpu!.A, Is.EqualTo(0x55));
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x00)); // C=0
    }

    [Test]
    public void ARR_Imm_ANDsAndRotatesRight()
    {
        cpu!.A = 0xFF;
        // Set C=1
        var state = cpu.GetState();
        state.P = 0x01;
        cpu.SetState(state);
        
        LoadProgram([0x6B, 0xAA]); // ARR Immediate
        ExecuteOne();
        // A & 0xAA = 0xAA
        // ROR 0xAA with C=1: bit 0=0, C=1 -> bit 7=1, result=10101010 | 10000000 = 10101010... 
        // Actually ROR: 10101010 >> 1 with C=1 -> 11010101 (C out = 0, C in = 1)
        // Wait, let me think: ROR shifts right, bit 0 goes to C, C goes to bit 7
        // 10101010: bit 0 = 0, so C out = 0. C in = 1, so bit 7 = 1.
        // Result: 11010101 = 0xD5
        Assert.That(cpu!.A, Is.EqualTo(0xD5));
    }

    [Test]
    public void SBX_Imm_SubtractsFromAAndX()
    {
        cpu!.A = 0xFF;
        cpu!.X = 0x08;
        // Verify initial state
        Assert.That(cpu!.A, Is.EqualTo(0xFF));
        Assert.That(cpu!.X, Is.EqualTo(0x08));
        
        LoadProgram([0xCB, 0x03]); // SBX Immediate
        ExecuteOne();
        // (A & X) = 0xFF & 0x08 = 0x08
        // X = 0x08 - 0x03 = 0x05
        Assert.That(cpu!.X, Is.EqualTo(0x05));
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x01)); // C=1
    }

    [Test]
    public void SBX_Imm_WithBorrow()
    {
        cpu!.A = 0x10;
        cpu!.X = 0x02;
        LoadProgram([0xCB, 0x03]); // SBX Immediate
        ExecuteOne();
        // (A & X) = 0x10 & 0x02 = 0x00
        // X = 0x00 - 0x03 = 0xFD (underflow, since it's byte subtraction)
        Assert.That(cpu!.X, Is.EqualTo(0xFD));
        Assert.That(cpu!.P & 0x01, Is.EqualTo(0x00)); // C=0 (0x00 < 0x03)
    }

    #endregion
}
