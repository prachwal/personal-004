using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class ArithmeticTests
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

    [Test]
    public void ADC_Imm_0plus0_C0()
    {
        // A=0, M=0, C=0 → A=0, C=0, Z=1, N=0, V=0
        cpu!.A = 0x00;
        LoadProgram([0x69, 0x00]);  // ADC #0
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.False);
    }

    [Test]
    public void ADC_Imm_0plus0_C1()
    {
        // A=0, M=0, C=1 → A=1, C=0, Z=0
        cpu!.A = 0x00;
        cpu.SetFlag(Cpu6502.FlagC, true);
        LoadProgram([0x69, 0x00]);  // ADC #0
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x01));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
    }

    [Test]
    public void ADC_Imm_7Fplus1_C0_Overflow()
    {
        // A=$7F, M=$01, C=0 → A=$80, V=1, N=1 (overflow)
        cpu!.A = 0x7F;
        LoadProgram([0x69, 0x01]);  // ADC #1
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x80));
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void ADC_Imm_80plus80_C0()
    {
        // A=$80, M=$80, C=0 → A=$00, V=1, C=1, Z=1
        cpu!.A = 0x80;
        LoadProgram([0x69, 0x80]);  // ADC #$80
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void ADC_Imm_FFplus1_C0()
    {
        // A=$FF, M=$01, C=0 → A=$00, C=1, Z=1
        cpu!.A = 0xFF;
        LoadProgram([0x69, 0x01]);  // ADC #1
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void SBC_Imm_05minus03_C1()
    {
        // A=$05, M=$03, C=1 → A=$02, C=1, Z=0
        cpu!.A = 0x05;
        cpu.SetFlag(Cpu6502.FlagC, true);  // C=1 (no borrow)
        LoadProgram([0xE9, 0x03]);  // SBC #3
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x02));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
    }

    [Test]
    public void SBC_Imm_00minus01_C1()
    {
        // A=$00, M=$01, C=1 → A=$FF, C=0, N=1 (borrow)
        cpu!.A = 0x00;
        cpu.SetFlag(Cpu6502.FlagC, true);  // C=1 (no borrow)
        LoadProgram([0xE9, 0x01]);  // SBC #1
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0xFF));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void SBC_Imm_80minus01_C1_Overflow()
    {
        // A=$80, M=$01, C=1 → A=$7F, V=1 (overflow)
        cpu!.A = 0x80;
        cpu.SetFlag(Cpu6502.FlagC, true);
        LoadProgram([0xE9, 0x01]);  // SBC #1
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x7F));
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
    }

    [Test]
    public void ADC_Zp()
    {
        // Test ADC Zero Page mode
        SetZp(0x10, 0x05);
        cpu!.A = 0x03;
        LoadProgram([0x65, 0x10]);  // ADC $10
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x08));
    }

    [Test]
    public void ADC_Abs()
    {
        // Test ADC Absolute mode
        SetAbs(0x1234, 0x07);
        cpu!.A = 0x02;
        LoadProgram([0x6D, 0x34, 0x12]);  // ADC $1234
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x09));
    }

    [Test]
    public void ADC_AbsX()
    {
        // Test ADC Absolute,X mode
        cpu!.X = 2;
        SetAbs(0x1236, 0x04);
        cpu!.A = 0x03;
        LoadProgram([0x7D, 0x34, 0x12]);  // ADC $1234,X
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x07));
    }

    [Test]
    public void ADC_AbsY()
    {
        // Test ADC Absolute,Y mode
        cpu!.Y = 3;
        SetAbs(0x1237, 0x05);
        cpu!.A = 0x02;
        LoadProgram([0x79, 0x34, 0x12]);  // ADC $1234,Y
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x07));
    }

    [Test]
    public void ADC_IndX()
    {
        // Test ADC (Indirect,X) mode
        cpu!.X = 4;
        SetZp(0x24, 0x10);  // Low byte of address
        SetZp(0x25, 0x30);  // High byte of address
        SetAbs(0x3010, 0x06);
        cpu!.A = 0x03;
        LoadProgram([0x61, 0x20]);  // ADC ($20,X)
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x09));
    }

    [Test]
    public void ADC_IndY()
    {
        // Test ADC (Indirect),Y mode
        cpu!.Y = 2;
        SetZp(0x20, 0x10);
        SetZp(0x21, 0x40);
        SetAbs(0x4012, 0x04);
        cpu!.A = 0x02;
        LoadProgram([0x71, 0x20]);  // ADC ($20),Y
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x06));
    }

    [Test]
    public void SBC_Zp()
    {
        // Test SBC Zero Page mode
        SetZp(0x20, 0x03);
        cpu!.A = 0x05;
        cpu.SetFlag(Cpu6502.FlagC, true);
        LoadProgram([0xE5, 0x20]);  // SBC $20
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x02));
    }

    [Test]
    public void SBC_Abs()
    {
        // Test SBC Absolute mode
        SetAbs(0x2222, 0x02);
        cpu!.A = 0x07;
        cpu.SetFlag(Cpu6502.FlagC, true);
        LoadProgram([0xED, 0x22, 0x22]);  // SBC $2222
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x05));
    }

    [Test]
    public void SBC_AbsX()
    {
        // Test SBC Absolute,X mode
        cpu!.X = 1;
        SetAbs(0x3333, 0x04);
        cpu!.A = 0x09;
        cpu.SetFlag(Cpu6502.FlagC, true);
        LoadProgram([0xFD, 0x32, 0x33]);  // SBC $3332,X
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x05));
    }

    [Test]
    public void SBC_AbsY()
    {
        // Test SBC Absolute,Y mode
        cpu!.Y = 2;
        SetAbs(0x4444, 0x03);
        cpu!.A = 0x08;
        cpu.SetFlag(Cpu6502.FlagC, true);
        LoadProgram([0xF9, 0x42, 0x44]);  // SBC $4442,Y
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x05));
    }

    [Test]
    public void SBC_IndX()
    {
        // Test SBC (Indirect,X) mode
        cpu!.X = 2;
        SetZp(0x32, 0x50);
        SetZp(0x33, 0x50);
        SetAbs(0x5050, 0x05);
        cpu!.A = 0x0A;
        cpu.SetFlag(Cpu6502.FlagC, true);
        LoadProgram([0xE1, 0x30]);  // SBC ($30,X)
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x05));
    }

    [Test]
    public void SBC_IndY()
    {
        // Test SBC (Indirect),Y mode
        cpu!.Y = 1;
        SetZp(0x40, 0x60);
        SetZp(0x41, 0x60);
        SetAbs(0x6061, 0x06);
        cpu!.A = 0x0C;
        cpu.SetFlag(Cpu6502.FlagC, true);
        LoadProgram([0xF1, 0x40]);  // SBC ($40),Y
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x06));
    }
}
