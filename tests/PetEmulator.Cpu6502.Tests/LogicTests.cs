using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class LogicTests
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
    public void AND_Imm_FF_and_0F()
    {
        // A=$FF, oper=$0F → A=$0F, Z=0, N=0
        cpu!.A = 0xFF;
        LoadProgram([0x29, 0x0F]);  // AND #$0F
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x0F));
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void AND_Imm_00_and_FF()
    {
        // A=$00, oper=$FF → A=$00, Z=1
        cpu!.A = 0x00;
        LoadProgram([0x29, 0xFF]);  // AND #$FF
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void AND_Imm_80_and_80()
    {
        // A=$80, oper=$80 → A=$80, N=1
        cpu!.A = 0x80;
        LoadProgram([0x29, 0x80]);  // AND #$80
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x80));
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void ORA_Imm_00_or_55()
    {
        // A=$00, oper=$55 → A=$55
        cpu!.A = 0x00;
        LoadProgram([0x09, 0x55]);  // ORA #$55
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
    }

    [Test]
    public void ORA_Imm_80_or_00()
    {
        // A=$80, oper=$00 → A=$80, N=1
        cpu!.A = 0x80;
        LoadProgram([0x09, 0x00]);  // ORA #$00
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x80));
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void EOR_Imm_FF_xor_AA()
    {
        // A=$FF, oper=$AA → A=$55
        cpu!.A = 0xFF;
        LoadProgram([0x49, 0xAA]);  // EOR #$AA
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
    }

    [Test]
    public void EOR_Imm_00_xor_00()
    {
        // A=$00, oper=$00 → A=$00, Z=1
        cpu!.A = 0x00;
        LoadProgram([0x49, 0x00]);  // EOR #$00
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void AND_C_V_flags_unchanged()
    {
        // Ustaw C=1, V=1 przed AND → nadal 1
        cpu!.SetFlag(Cpu6502.FlagC, true);
        cpu!.SetFlag(Cpu6502.FlagV, true);
        cpu!.A = 0xFF;
        LoadProgram([0x29, 0x0F]);  // AND #$0F
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
    }

    [Test]
    public void AND_Zp()
    {
        // Test AND Zero Page mode
        SetZp(0x10, 0x0F);
        cpu!.A = 0xFF;
        LoadProgram([0x25, 0x10]);  // AND $10
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x0F));
    }

    [Test]
    public void AND_Abs()
    {
        // Test AND Absolute mode
        SetAbs(0x1234, 0x0F);
        cpu!.A = 0xFF;
        LoadProgram([0x2D, 0x34, 0x12]);  // AND $1234
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x0F));
    }

    [Test]
    public void ORA_Zp()
    {
        // Test ORA Zero Page mode
        SetZp(0x10, 0x55);
        cpu!.A = 0x00;
        LoadProgram([0x05, 0x10]);  // ORA $10
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
    }

    [Test]
    public void ORA_Abs()
    {
        // Test ORA Absolute mode
        SetAbs(0x1234, 0x55);
        cpu!.A = 0x00;
        LoadProgram([0x0D, 0x34, 0x12]);  // ORA $1234
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
    }

    [Test]
    public void EOR_Zp()
    {
        // Test EOR Zero Page mode
        SetZp(0x10, 0xAA);
        cpu!.A = 0xFF;
        LoadProgram([0x45, 0x10]);  // EOR $10
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
    }

    [Test]
    public void EOR_Abs()
    {
        // Test EOR Absolute mode
        SetAbs(0x1234, 0xAA);
        cpu!.A = 0xFF;
        LoadProgram([0x4D, 0x34, 0x12]);  // EOR $1234
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
    }
}
