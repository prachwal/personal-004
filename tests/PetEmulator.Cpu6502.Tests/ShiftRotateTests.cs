using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class ShiftRotateTests
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
    public void ASL_Acc_01_to_02()
    {
        // A=$01 → A=$02, C=0, N=0, Z=0
        cpu!.A = 0x01;
        LoadProgram([0x0A]);  // ASL A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x02));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
    }

    [Test]
    public void ASL_Acc_80_to_00()
    {
        // A=$80 → A=$00, C=1, Z=1
        cpu!.A = 0x80;
        LoadProgram([0x0A]);  // ASL A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void LSR_Acc_02_to_01()
    {
        // A=$02 → A=$01, C=0
        cpu!.A = 0x02;
        LoadProgram([0x4A]);  // LSR A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x01));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
    }

    [Test]
    public void LSR_Acc_01_to_00()
    {
        // A=$01 → A=$00, C=1, Z=1
        cpu!.A = 0x01;
        LoadProgram([0x4A]);  // LSR A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void ROL_Acc_C0_with_C0()
    {
        // C=0, A=$80 → A=$00, C=1
        cpu!.SetFlag(Cpu6502.FlagC, false);
        cpu!.A = 0x80;
        LoadProgram([0x2A]);  // ROL A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void ROL_Acc_C1_with_C1()
    {
        // C=1, A=$00 → A=$01, C=0
        cpu!.SetFlag(Cpu6502.FlagC, true);
        cpu!.A = 0x00;
        LoadProgram([0x2A]);  // ROL A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x01));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
    }

    [Test]
    public void ROR_Acc_C0_with_C0()
    {
        // C=0, A=$01 → A=$00, C=1
        cpu!.SetFlag(Cpu6502.FlagC, false);
        cpu!.A = 0x01;
        LoadProgram([0x6A]);  // ROR A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void ROR_Acc_C1_with_C1()
    {
        // C=1, A=$00 → A=$80, C=0
        cpu!.SetFlag(Cpu6502.FlagC, true);
        cpu!.A = 0x00;
        LoadProgram([0x6A]);  // ROR A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x80));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
    }

    [Test]
    public void ASL_Zp()
    {
        // ASL na pamięci: $01 → $02
        SetZp(0x10, 0x01);
        LoadProgram([0x06, 0x10]);  // ASL $10
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x02));
    }

    [Test]
    public void ASL_ZpX()
    {
        // ASL na pamięci z X: $01 → $02
        cpu!.X = 2;
        SetZp(0x12, 0x01);
        LoadProgram([0x16, 0x10]);  // ASL $10,X
        ExecuteOne();
        Assert.That(memory!.Read(0x12), Is.EqualTo(0x02));
    }

    [Test]
    public void ASL_Abs()
    {
        // ASL na pamięci absolutnej: $01 → $02
        SetAbs(0x1234, 0x01);
        LoadProgram([0x0E, 0x34, 0x12]);  // ASL $1234
        ExecuteOne();
        Assert.That(memory!.Read(0x1234), Is.EqualTo(0x02));
    }

    [Test]
    public void LSR_Zp()
    {
        // LSR na pamięci: $02 → $01
        SetZp(0x10, 0x02);
        LoadProgram([0x46, 0x10]);  // LSR $10
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x01));
    }

    [Test]
    public void ROL_Zp()
    {
        // ROL na pamięci: C=0, $80 → $00, C=1
        cpu!.SetFlag(Cpu6502.FlagC, false);
        SetZp(0x10, 0x80);
        LoadProgram([0x26, 0x10]);  // ROL $10
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void ROR_Zp()
    {
        // ROR na pamięci: C=0, $01 → $00, C=1
        cpu!.SetFlag(Cpu6502.FlagC, false);
        SetZp(0x10, 0x01);
        LoadProgram([0x66, 0x10]);  // ROR $10
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void ASL_N_flag()
    {
        // ASL A=$40 → A=$80, N=1
        cpu!.A = 0x40;
        LoadProgram([0x0A]);  // ASL A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x80));
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void LSR_N_flag()
    {
        // LSR A=$80 → A=$40, N=0
        cpu!.A = 0x80;
        LoadProgram([0x4A]);  // LSR A
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x40));
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }
}
