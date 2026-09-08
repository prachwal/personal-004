using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class CompareBitTests
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
    public void CMP_Imm_Requal()
    {
        // A=$42, M=$42 → Z=1, C=1, N=0
        cpu!.A = 0x42;
        LoadProgram([0xC9, 0x42]);  // CMP #$42
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void CMP_Imm_AgreaterM()
    {
        // A=$80, M=$10 → Z=0, C=1, N=0
        cpu!.A = 0x80;
        LoadProgram([0xC9, 0x10]);  // CMP #$10
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void CMP_Imm_AlessM()
    {
        // A=$10, M=$80 → Z=0, C=0, N=1
        cpu!.A = 0x10;
        LoadProgram([0xC9, 0x80]);  // CMP #$80
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void CPX_Imm()
    {
        // X=$42, M=$42 → Z=1, C=1, N=0
        cpu!.X = 0x42;
        LoadProgram([0xE0, 0x42]);  // CPX #$42
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void CPY_Imm()
    {
        // Y=$42, M=$42 → Z=1, C=1, N=0
        cpu!.Y = 0x42;
        LoadProgram([0xC0, 0x42]);  // CPY #$42
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void BIT_Zp_Z1_A0_MFF()
    {
        // A=$00, M=$FF → Z=1 (A&M=0)
        cpu!.A = 0x00;
        SetZp(0x10, 0xFF);
        LoadProgram([0x24, 0x10]);  // BIT $10
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void BIT_Zp_Z0_AFF_MFF()
    {
        // A=$FF, M=$FF → Z=0
        cpu!.A = 0xFF;
        SetZp(0x10, 0xFF);
        LoadProgram([0x24, 0x10]);  // BIT $10
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
    }

    [Test]
    public void BIT_Zp_N_V_from_operand()
    {
        // M=$C0 → N=1 (bit 7), V=1 (bit 6)
        SetZp(0x10, 0xC0);
        LoadProgram([0x24, 0x10]);  // BIT $10
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
    }

    [Test]
    public void BIT_doesNotModifyA()
    {
        // A=$55 przed i po BIT
        cpu!.A = 0x55;
        SetZp(0x10, 0xFF);
        LoadProgram([0x24, 0x10]);  // BIT $10
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
    }

    [Test]
    public void CMP_Zp()
    {
        // Test CMP Zero Page mode
        SetZp(0x10, 0x42);
        cpu!.A = 0x42;
        LoadProgram([0xC5, 0x10]);  // CMP $10
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void CMP_Abs()
    {
        // Test CMP Absolute mode
        SetAbs(0x1234, 0x80);
        cpu!.A = 0x80;
        LoadProgram([0xCD, 0x34, 0x12]);  // CMP $1234
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void CPX_Zp()
    {
        // Test CPX Zero Page mode
        SetZp(0x10, 0x42);
        cpu!.X = 0x42;
        LoadProgram([0xE4, 0x10]);  // CPX $10
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void CPX_Abs()
    {
        // Test CPX Absolute mode
        SetAbs(0x1234, 0x80);
        cpu!.X = 0x80;
        LoadProgram([0xEC, 0x34, 0x12]);  // CPX $1234
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void CPY_Zp()
    {
        // Test CPY Zero Page mode
        SetZp(0x10, 0x42);
        cpu!.Y = 0x42;
        LoadProgram([0xC4, 0x10]);  // CPY $10
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void CPY_Abs()
    {
        // Test CPY Absolute mode
        SetAbs(0x1234, 0x80);
        cpu!.Y = 0x80;
        LoadProgram([0xCC, 0x34, 0x12]);  // CPY $1234
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void BIT_Abs()
    {
        // Test BIT Absolute mode
        SetAbs(0x1234, 0xC0);
        LoadProgram([0x2C, 0x34, 0x12]);  // BIT $1234
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
    }
}
