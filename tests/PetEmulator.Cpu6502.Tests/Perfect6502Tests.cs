using System.Linq;
using Cpu6502.Variants;
using Cpu6502.Tests.TestHelpers;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class Perfect6502Tests
{
    private BusTraceMemoryBus _memory = null!;
    private Cpu6502 _cpu = null!;

    [SetUp]
    public void SetUp()
    {
        _memory = new BusTraceMemoryBus();
        _cpu = new Cpu6502Classic(_memory);
        _memory[0xFFFC] = 0x00;
        _memory[0xFFFD] = 0x80;
        _memory[0x8000] = 0xEA;
        _cpu.Reset();
        _memory.ClearTrace();
    }

    [Test]
    public void LdaImmediate_ConsumesOpcodeAndOperand()
    {
        _memory.Load(0x8000, 0xA9, 0x44);
        _cpu.StepInstruction();

        Assert.That(_cpu.A, Is.EqualTo(0x44));
        Assert.That(Addresses(), Is.EqualTo(new ushort[] { 0x8000, 0x8001 }));
    }

    [Test]
    public void StaZeroPage_ProducesSingleWrite()
    {
        _memory.Load(0x8000, 0x85, 0x20);
        _cpu.A = 0x7A;
        _cpu.StepInstruction();

        Assert.That(_memory[0x0020], Is.EqualTo(0x7A));
        Assert.That(Writes(), Is.EqualTo(new ushort[] { 0x0020 }));
    }

    [Test]
    public void Reset_UsesResetVector()
    {
        _cpu.Reset();
        Assert.That(Addresses().Take(2), Is.EqualTo(new ushort[] { 0xFFFC, 0xFFFD }));
        Assert.That(_cpu.GetState().PC, Is.EqualTo(0x8000));
    }

    [Test]
    public void Brk_UsesInterruptVectorAndPushesPcAndStatus()
    {
        _memory.Load(0x8000, 0x00);
        _memory[0xFFFE] = 0x34;
        _memory[0xFFFF] = 0x12;

        _cpu.StepInstruction();

        Assert.That(_cpu.GetState().PC, Is.EqualTo(0x1234));
        Assert.That(Writes().Length, Is.EqualTo(3));
    }

    [Test]
    public void Jsr_PushesReturnAddress()
    {
        _memory.Load(0x8000, 0x20, 0x05, 0x80, 0xEA);

        _cpu.StepInstruction();

        Assert.That(_cpu.GetState().PC, Is.EqualTo(0x8005));
        Assert.That(Writes().Length, Is.EqualTo(2));
    }

    [Test]
    public void Rts_PullsReturnAddress()
    {
        _memory.Load(0x8000, 0x20, 0x05, 0x80, 0xEA, 0xEA, 0x60);

        _cpu.StepInstruction();
        _cpu.StepInstruction();

        Assert.That(_cpu.GetState().PC, Is.EqualTo(0x8003));
    }

    [Test]
    public void BranchTaken_ChangesPc()
    {
        _memory.Load(0x8000, 0xD0, 0x02);
        _cpu.SetFlag(Cpu6502.FlagZ, false);

        _cpu.StepInstruction();

        Assert.That(_cpu.GetState().PC, Is.EqualTo(0x8004));
    }

    [Test]
    public void JmpIndirect_UsesPointer()
    {
        _memory.Load(0x8000, 0x6C, 0x00, 0x90);
        _memory[0x9000] = 0x78;
        _memory[0x9001] = 0x56;

        _cpu.StepInstruction();

        Assert.That(_cpu.GetState().PC, Is.EqualTo(0x5678));
    }

    [Test]
    public void RmwAbsolute_ProducesWriteBackSequence()
    {
        _memory.Load(0x8000, 0x0E, 0x34, 0x12);
        _memory[0x1234] = 0x01;

        _cpu.StepInstruction();

        Assert.That(_memory[0x1234], Is.EqualTo(0x02));
        Assert.That(Writes(), Does.Contain((ushort)0x1234));
    }

    [Test]
    public void Nmi_UsesNmiVector()
    {
        _memory[0xFFFA] = 0x78;
        _memory[0xFFFB] = 0x56;

        _cpu.SetNMI(true);
        _cpu.SetNMI(false);
        _cpu.StepInstruction();

        Assert.That(_cpu.GetState().PC, Is.EqualTo(0x5678));
    }

    private ushort[] Addresses()
    {
        return _memory.Accesses.Select(a => a.Address).ToArray();
    }

    private ushort[] Writes()
    {
        return _memory.Accesses.Where(a => a.IsWrite).Select(a => a.Address).ToArray();
    }
}
