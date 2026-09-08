using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class IncDecTests
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
    public void INC_Zp_IncrementsMemory()
    {
        // INC $10 - memory value $05 → $06, N=0, Z=0
        SetZp(0x10, 0x05);
        LoadProgram([0xE6, 0x10]);  // INC $10
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x06));
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void INC_Zp_WrapAround_FF_to_00()
    {
        // INC $10 - memory value $FF → $00, Z=1, N=0
        SetZp(0x10, 0xFF);
        LoadProgram([0xE6, 0x10]);  // INC $10
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x00));
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void INC_ZpX_IncrementsMemory()
    {
        // INC $10,X - X=2, address=$12, value=$03 → $04
        cpu!.X = 2;
        SetZp(0x12, 0x03);
        LoadProgram([0xF6, 0x10]);  // INC $10,X
        ExecuteOne();
        Assert.That(memory!.Read(0x12), Is.EqualTo(0x04));
    }

    [Test]
    public void INC_Abs_IncrementsMemory()
    {
        // INC $1234 - memory value $07 → $08
        SetAbs(0x1234, 0x07);
        LoadProgram([0xEE, 0x34, 0x12]);  // INC $1234
        ExecuteOne();
        Assert.That(memory!.Read(0x1234), Is.EqualTo(0x08));
    }

    [Test]
    public void INC_AbsX_IncrementsMemory()
    {
        // INC $1234,X - X=3, address=$1237, value=$09 → $0A
        cpu!.X = 3;
        SetAbs(0x1237, 0x09);
        LoadProgram([0xFE, 0x34, 0x12]);  // INC $1234,X
        ExecuteOne();
        Assert.That(memory!.Read(0x1237), Is.EqualTo(0x0A));
    }

    [Test]
    public void DEC_Zp_DecrementsMemory()
    {
        // DEC $10 - memory value $05 → $04
        SetZp(0x10, 0x05);
        LoadProgram([0xC6, 0x10]);  // DEC $10
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0x04));
    }

    [Test]
    public void DEC_Zp_WrapAround_00_to_FF()
    {
        // DEC $10 - memory value $00 → $FF, N=1, Z=0
        SetZp(0x10, 0x00);
        LoadProgram([0xC6, 0x10]);  // DEC $10
        ExecuteOne();
        Assert.That(memory!.Read(0x10), Is.EqualTo(0xFF));
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
    }

    [Test]
    public void DEC_ZpX_DecrementsMemory()
    {
        // DEC $10,X - X=1, address=$11, value=$08 → $07
        cpu!.X = 1;
        SetZp(0x11, 0x08);
        LoadProgram([0xD6, 0x10]);  // DEC $10,X
        ExecuteOne();
        Assert.That(memory!.Read(0x11), Is.EqualTo(0x07));
    }

    [Test]
    public void DEC_Abs_DecrementsMemory()
    {
        // DEC $1234 - memory value $0F → $0E
        SetAbs(0x1234, 0x0F);
        LoadProgram([0xCE, 0x34, 0x12]);  // DEC $1234
        ExecuteOne();
        Assert.That(memory!.Read(0x1234), Is.EqualTo(0x0E));
    }

    [Test]
    public void DEC_AbsX_DecrementsMemory()
    {
        // DEC $1234,X - X=2, address=$1236, value=$10 → $0F
        cpu!.X = 2;
        SetAbs(0x1236, 0x10);
        LoadProgram([0xDE, 0x34, 0x12]);  // DEC $1234,X
        ExecuteOne();
        Assert.That(memory!.Read(0x1236), Is.EqualTo(0x0F));
    }

    [Test]
    public void INX_IncrementsX()
    {
        // INX - X=$05 → $06, N=0, Z=0
        cpu!.X = 0x05;
        LoadProgram([0xE8]);  // INX
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x06));
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void INX_WrapAround_FF_to_00()
    {
        // INX - X=$FF → $00, Z=1
        cpu!.X = 0xFF;
        LoadProgram([0xE8]);  // INX
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x00));
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void DEX_DecrementsX()
    {
        // DEX - X=$05 → $04
        cpu!.X = 0x05;
        LoadProgram([0xCA]);  // DEX
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x04));
    }

    [Test]
    public void DEX_WrapAround_00_to_FF()
    {
        // DEX - X=$00 → $FF, N=1
        cpu!.X = 0x00;
        LoadProgram([0xCA]);  // DEX
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0xFF));
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void INY_IncrementsY()
    {
        // INY - Y=$03 → $04
        cpu!.Y = 0x03;
        LoadProgram([0xC8]);  // INY
        ExecuteOne();
        Assert.That(cpu!.Y, Is.EqualTo(0x04));
    }

    [Test]
    public void DEY_DecrementsY()
    {
        // DEY - Y=$03 → $02
        cpu!.Y = 0x03;
        LoadProgram([0x88]);  // DEY
        ExecuteOne();
        Assert.That(cpu!.Y, Is.EqualTo(0x02));
    }

    [Test]
    public void INC_FlagC_Unchanged()
    {
        // INC nie modyfikuje flagi C
        cpu!.SetFlag(Cpu6502.FlagC, true);
        SetZp(0x10, 0x01);
        LoadProgram([0xE6, 0x10]);  // INC $10
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void DEC_FlagC_Unchanged()
    {
        // DEC nie modyfikuje flagi C
        cpu!.SetFlag(Cpu6502.FlagC, false);
        SetZp(0x10, 0x05);
        LoadProgram([0xC6, 0x10]);  // DEC $10
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.False);
    }
}
