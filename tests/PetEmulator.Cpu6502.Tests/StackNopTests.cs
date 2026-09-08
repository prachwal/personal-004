using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class StackNopTests
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

    [Test]
    public void PHA_pushes_A()
    {
        // PHA z A=$42 → SP=$FC, stos[$01FD]=$42
        cpu!.A = 0x42;
        LoadProgram([0x48]);  // PHA
        ExecuteOne();
        Assert.That(cpu!.SP, Is.EqualTo(0xFC));
        Assert.That(memory!.Read(0x01FD), Is.EqualTo(0x42));
    }

    [Test]
    public void PHP_pushes_P_with_B_flag()
    {
        // PHP z P=$00 → SP=$FC, stos[$01FD]=$30 (B=1, bit5=1)
        cpu!.SetFlag(Cpu6502.FlagN, false);
        cpu!.SetFlag(Cpu6502.FlagZ, false);
        cpu!.SetFlag(Cpu6502.FlagC, false);
        cpu!.SetFlag(Cpu6502.FlagV, false);
        LoadProgram([0x08]);  // PHP
        ExecuteOne();
        byte pushedValue = memory!.Read(0x01FD);
        Assert.That(cpu!.SP, Is.EqualTo(0xFC));
        Assert.That((pushedValue & 0x30), Is.EqualTo(0x30), "B flag and bit5 should be set");
    }

    [Test]
    public void PLA_pulls_A()
    {
        // PLA z A=$00, stos[$01FD]=$42 → A=$42, SP=$FD
        cpu!.A = 0x00;
        memory!.Write(0x01FD, 0x42);
        cpu!.SP = 0xFC; // SP points to $01FD before pull
        LoadProgram([0x68]);  // PLA
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x42));
        Assert.That(cpu!.SP, Is.EqualTo(0xFD));
    }

    [Test]
    public void PLA_sets_NZ_flags()
    {
        // PLA z A=$00, stos[$01FD]=$80 → A=$80, N=1, Z=0
        cpu!.A = 0x00;
        memory!.Write(0x01FD, 0x80);
        cpu!.SP = 0xFC;
        LoadProgram([0x68]);  // PLA
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x80));
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
    }

    [Test]
    public void PLP_pulls_P()
    {
        // PLP z P=$00, stos[$01FD]=$FF → P=$FF
        cpu!.SetFlag(Cpu6502.FlagN, false);
        cpu!.SetFlag(Cpu6502.FlagZ, false);
        cpu!.SetFlag(Cpu6502.FlagC, false);
        cpu!.SetFlag(Cpu6502.FlagV, false);
        memory!.Write(0x01FD, 0xFF);
        cpu!.SP = 0xFC;
        LoadProgram([0x28]);  // PLP
        ExecuteOne();
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
    }

    [Test]
    public void NOP_does_nothing()
    {
        // NOP nie zmienia stanu CPU
        cpu!.A = 0x42;
        cpu!.X = 0x13;
        cpu!.Y = 0x99;
        cpu!.SetFlag(Cpu6502.FlagN, true);
        cpu!.SetFlag(Cpu6502.FlagZ, false);
        LoadProgram([0xEA]);  // NOP
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x42));
        Assert.That(cpu!.X, Is.EqualTo(0x13));
        Assert.That(cpu!.Y, Is.EqualTo(0x99));
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
    }

    [Test]
    public void SP_wrap_0_to_FF()
    {
        // Push przy SP=0 → SP=$FF
        cpu!.SP = 0x00;
        cpu!.A = 0x55;
        LoadProgram([0x48]);  // PHA
        ExecuteOne();
        Assert.That(cpu!.SP, Is.EqualTo(0xFF));
        Assert.That(memory!.Read(0x0100), Is.EqualTo(0x55));
    }

    [Test]
    public void SP_wrap_FF_to_0()
    {
        // Pull przy SP=$FF → SP=0
        cpu!.SP = 0xFF;
        // Write value to stack page (0x0100 + 0xFF + 1 = 0x0100 after wrap)
        memory!.Write(0x0100, 0x33);
        // Write PLA opcode to different location
        cpu!.PC = 0x0200;
        var state = cpu.GetState();
        state.Sync = true;
        cpu.SetState(state);
        memory!.Write(0x0200, 0x68);  // PLA opcode
        ExecuteOne();
        Assert.That(cpu!.SP, Is.EqualTo(0x00));
        Assert.That(cpu!.A, Is.EqualTo(0x33));
    }
}
