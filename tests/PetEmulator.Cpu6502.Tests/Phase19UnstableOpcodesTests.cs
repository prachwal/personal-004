using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class Phase19UnstableOpcodesTests
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

    private void LoadProgram(params byte[] program)
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

    #region ANE (XAA) - $8B Tests

    [Test]
    public void ANE_Immediate_SetsAToXAndOperand()
    {
        cpu!.A = 0xFF;
        cpu!.X = 0x0F;
        LoadProgram(new byte[] { 0x8B, 0xFF });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x0F));
        Assert.That(cpu.P & 0x80, Is.EqualTo(0x00));
        Assert.That(cpu.P & 0x02, Is.EqualTo(0x00));
    }

    [Test]
    public void ANE_ZeroResult_SetsZeroFlag()
    {
        cpu!.A = 0x00;
        cpu!.X = 0x00;
        LoadProgram(new byte[] { 0x8B, 0xFF });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x00));
        Assert.That(cpu.P & 0x02, Is.EqualTo(0x02));
    }

    [Test]
    public void ANE_NegativeResult_SetsNegativeFlag()
    {
        cpu!.A = 0xFF;
        cpu!.X = 0x80;
        LoadProgram(new byte[] { 0x8B, 0xFF });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x80));
        Assert.That(cpu.P & 0x80, Is.EqualTo(0x80));
    }

    #endregion

    #region LXA (LAX Immediate) - $AB Tests

    [Test]
    public void LXA_Immediate_LoadsAAndX()
    {
        cpu!.A = 0xFF;
        LoadProgram(new byte[] { 0xAB, 0x42 });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x42));
        Assert.That(cpu.X, Is.EqualTo(0x42));
        Assert.That(cpu.P & 0x80, Is.EqualTo(0x00));
        Assert.That(cpu.P & 0x02, Is.EqualTo(0x00));
    }

    [Test]
    public void LXA_Immediate_Zero_SetsZeroFlag()
    {
        cpu!.A = 0xFF;
        LoadProgram(new byte[] { 0xAB, 0x00 });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x00));
        Assert.That(cpu.X, Is.EqualTo(0x00));
        Assert.That(cpu.P & 0x02, Is.EqualTo(0x02));
    }

    #endregion

    #region USBC - $EB Tests

    [Test]
    public void USBC_BehavesLikeSBC_NoBorrow()
    {
        cpu!.A = 0x50;
        cpu.SetFlag(Cpu6502.FlagC, true);  // C=1 (no borrow)
        LoadProgram(new byte[] { 0xEB, 0x10 });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x40));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void USBC_WithBorrow_ClearsCarry()
    {
        cpu!.A = 0x10;
        cpu.SetFlag(Cpu6502.FlagC, true);  // C=1 (no borrow initially)
        LoadProgram(new byte[] { 0xEB, 0x20 });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0xF0));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
    }

    #endregion

    #region NOP Tests

    [Test]
    public void NOP_Zp_DoesNotModifyRegisters()
    {
        cpu!.A = 0x42;
        cpu!.X = 0x13;
        cpu!.Y = 0x99;
        memory!.Write(0x10, 0xFF);
        LoadProgram(new byte[] { 0x04, 0x10 });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x42));
        Assert.That(cpu.X, Is.EqualTo(0x13));
        Assert.That(cpu.Y, Is.EqualTo(0x99));
    }

    [Test]
    public void NOP_Imm_DoesNotModifyRegisters()
    {
        cpu!.A = 0x42;
        cpu!.P = 0xFF;
        LoadProgram(new byte[] { 0x80, 0xFF });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x42));
        Assert.That(cpu.P, Is.EqualTo(0xFF));
    }

    [Test]
    public void NOP_Impl_DoesNotModifyRegisters()
    {
        cpu!.A = 0x42;
        cpu!.P = 0xFF;
        LoadProgram(new byte[] { 0x1A });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x42));
        Assert.That(cpu.P, Is.EqualTo(0xFF));
    }

    [Test]
    public void NOP_AbsX_DoesNotModifyRegisters()
    {
        cpu!.A = 0x42;
        cpu!.X = 0x01;
        cpu!.P = 0xFF;
        memory!.Write(0x1001, 0xFF);
        LoadProgram(new byte[] { 0x1C, 0x00, 0x10 });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x42));
        Assert.That(cpu.X, Is.EqualTo(0x01));
        Assert.That(cpu.P, Is.EqualTo(0xFF));
    }

    #endregion

    #region KIL Tests

    [Test]
    public void KIL_02_HaltsCPU()
    {
        cpu!.A = 0x42;
        LoadProgram(new byte[] { 0x02, 0xEA, 0xEA });
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x42));
        Assert.That(cpu.PC, Is.EqualTo(0x0101));

        cpu.Tick();
        cpu.Tick();
        cpu.Tick();
        Assert.That(cpu.A, Is.EqualTo(0x42));
        Assert.That(cpu.PC, Is.EqualTo(0x0101));
    }

    [Test]
    public void KIL_12_HaltsCPU()
    {
        cpu!.A = 0x42;
        LoadProgram(new byte[] { 0x12 });
        ExecuteOne();
        cpu.Tick();
        cpu.Tick();
        Assert.That(cpu.PC, Is.EqualTo(0x0101));
    }

    [Test]
    public void KIL_42_HaltsCPU()
    {
        cpu!.A = 0x42;
        LoadProgram(new byte[] { 0x42 });
        ExecuteOne();
        cpu.Tick();
        cpu.Tick();
        Assert.That(cpu.PC, Is.EqualTo(0x0101));
    }

    [Test]
    public void KIL_F2_HaltsCPU()
    {
        cpu!.A = 0x42;
        LoadProgram(new byte[] { 0xF2 });
        ExecuteOne();
        cpu.Tick();
        cpu.Tick();
        Assert.That(cpu.PC, Is.EqualTo(0x0101));
    }

    #endregion
}
