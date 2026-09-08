using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class ResetTests
{
    private FlatMemory? memory;
    private Cpu6502? cpu;

    [SetUp]
    public void Setup()
    {
        memory = new FlatMemory();
        cpu = new Cpu6502(memory);
    }

    [Test]
    public void Reset_LoadsPCFromResetVector()
    {
        // Test: Reset loads PC from $FFFC/$FFFD
        memory!.Write(0xFFFC, 0x00);  // Low byte
        memory!.Write(0xFFFD, 0xC0);  // High byte
        
        cpu!.Reset();
        
        // PC should be $C000
        Assert.That(cpu.PC, Is.EqualTo(0xC000));
    }

    [Test]
    public void Reset_SPEqualsFd()
    {
        // Test: SP = $FD after reset
        cpu!.Reset();
        
        // SP should be $FD
        Assert.That(cpu.SP, Is.EqualTo(0xFD));
    }

    [Test]
    public void Reset_IFlagSet()
    {
        // Test: I = 1 after reset (interrupt disable)
        cpu!.Reset();
        
        // I flag should be set
        Assert.That(cpu.GetFlag(Cpu6502.FlagI), Is.True);
    }

    [Test]
    public void Reset_DFlagClear()
    {
        // Test: D = 0 after reset (decimal mode disabled)
        cpu!.Reset();
        
        // D flag should be clear
        Assert.That(cpu.GetFlag(Cpu6502.FlagD), Is.False);
    }

    [Test]
    public void Reset_RegistersZeroed()
    {
        // Test: A=0, X=0, Y=0 after reset
        cpu!.Reset();
        
        // All registers should be zero
        Assert.That(cpu.A, Is.EqualTo(0x00));
        Assert.That(cpu.X, Is.EqualTo(0x00));
        Assert.That(cpu.Y, Is.EqualTo(0x00));
    }

    [Test]
    public void Reset_FirstInstructionExecution()
    {
        // Test: First instruction executes correctly after reset
        // Set up reset vector to point to NOP instruction
        memory!.Write(0xFFFC, 0x00);  // Low byte
        memory!.Write(0xFFFD, 0x01);  // High byte
        memory!.Write(0x0100, 0xEA);  // NOP instruction
        
        cpu!.Reset();
        
        // PC should be $0100
        Assert.That(cpu.PC, Is.EqualTo(0x0100));
        
        // Execute first instruction (NOP)
        do
        {
            cpu.Tick();
        }
        while (!cpu.GetState().Sync);
        
        // PC should advance to $0101
        Assert.That(cpu.PC, Is.EqualTo(0x0101));
    }

    [Test]
    public void Reset_MultipleResetsConsistent()
    {
        // Test: Multiple resets produce consistent state
        memory!.Write(0xFFFC, 0x34);  // Low byte
        memory!.Write(0xFFFD, 0x12);  // High byte
        
        cpu!.Reset();
        var state1 = cpu.GetState();
        
        cpu.Reset();
        var state2 = cpu.GetState();
        
        // Both states should be identical
        Assert.That(state2.A, Is.EqualTo(state1.A));
        Assert.That(state2.X, Is.EqualTo(state1.X));
        Assert.That(state2.Y, Is.EqualTo(state1.Y));
        Assert.That(state2.SP, Is.EqualTo(state1.SP));
        Assert.That(state2.PC, Is.EqualTo(state1.PC));
        Assert.That(state2.P, Is.EqualTo(state1.P));
    }
}
