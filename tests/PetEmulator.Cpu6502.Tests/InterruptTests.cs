using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class InterruptTests
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
        // Ładuj program od $0100, aby nie nadpisywać Zero Page ($00-$FF)
        ushort baseAddr = 0x0100;
        for (int i = 0; i < program.Length; i++)
            memory!.Write((ushort)(baseAddr + i), program[i]);
        cpu!.PC = baseAddr;
        // Wymuś sync aby Tick() pobrał nowy opcode z PC
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
    public void Brk_PushesPCPlus2()
    {
        LoadProgram(0x00, 0x12, 0x34); // BRK at $0100, signature byte $12
        cpu!.SP = 0xFF;
        
        // Set IRQ vector to $3412
        memory!.Write(0xFFFE, 0x12);
        memory.Write(0xFFFF, 0x34);
        
        ExecuteOne(); // Execute BRK
        
        // After BRK: PC should be at $FFFE/$FFFF vector
        Assert.That(cpu.PC, Is.EqualTo(0x3412));
        
        // Stack should have: PCH, PCL, P (with B=1)
        // SP starts at 0xFF, after 3 pushes: SP = 0xFC
        Assert.That(cpu.SP, Is.EqualTo(0xFC));
        
        // Verify pushed PC = 0x0102 (address after BRK + signature byte)
        // Stack layout: SP=0xFC (top), 0xFD=P, 0xFE=PCL, 0xFF=PCH
        byte pushedPch = memory!.Read(0x01FF); // PCH (first push)
        byte pushedPcl = memory.Read(0x01FE); // PCL (second push)
        Assert.That((ushort)((pushedPch << 8) | pushedPcl), Is.EqualTo(0x0102));
    }

    [Test]
    public void Brk_PushesPWithBFlagSet()
    {
        LoadProgram(0x00); // BRK
        cpu!.SP = 0xFF;
        cpu.P = 0x00; // Clear all flags
        
        ExecuteOne(); // Execute BRK
        
        // Pushed P should have B=1 (bit 4) and bit5=1
        byte pushedP = memory!.Read((ushort)(0x0100 + cpu.SP + 1));
        Assert.That((pushedP & 0x30), Is.EqualTo(0x30)); // B=1 and bit5=1
    }

    [Test]
    public void Brk_SetsInterruptFlag()
    {
        LoadProgram(0x00); // BRK
        cpu!.P = 0x00; // Clear all flags
        
        ExecuteOne(); // Execute BRK
        
        // I flag should be set after BRK
        Assert.That((cpu.P & 0x04), Is.EqualTo(0x04)); // I=1
    }

    [Test]
    public void Brk_JumpsToInterruptVector()
    {
        LoadProgram(0x00); // BRK
        
        // Set IRQ vector to $4000
        memory!.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x40);
        
        ExecuteOne(); // Execute BRK
        
        // PC should be at $4000
        Assert.That(cpu.PC, Is.EqualTo(0x4000));
    }

    [Test]
    public void Rti_RestoresPCAndP()
    {
        LoadProgram(0x40); // RTI
        cpu!.SP = 0xFC; // Stack pointer after BRK
        
        // Set up stack: P, PCL, PCH
        memory!.Write(0x01FD, 0x24); // P (with some flags)
        memory.Write(0x01FE, 0x34); // PCL
        memory.Write(0x01FF, 0x12); // PCH
        
        ExecuteOne(); // Execute RTI
        
        // PC should be restored to $1234
        Assert.That(cpu.PC, Is.EqualTo(0x1234));
        
        // P should be restored to 0x24
        Assert.That(cpu.P, Is.EqualTo(0x24));
    }

    [Test]
    public void Rti_WithBFlagZeroOnStack()
    {
        LoadProgram(0x40); // RTI
        cpu!.SP = 0xFC;
        
        // Set up stack with B=0
        memory!.Write(0x01FD, 0x04); // P with B=0
        memory.Write(0x01FE, 0x34); // PCL
        memory.Write(0x01FF, 0x12); // PCH
        
        ExecuteOne(); // Execute RTI
        
        // P should be restored with B=0; bit 5 (U) is kept set in the visible status byte.
        Assert.That(cpu.P, Is.EqualTo(0x24));
    }

    [Test]
    public void Irq_WhenIFlagClear_TriggersInterrupt()
    {
        LoadProgram(0xEA); // NOP
        cpu!.P = 0x00; // Clear all flags (I=0)
        
        // Set IRQ vector to $5000
        memory!.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x50);
        
        // Trigger IRQ
        cpu.SetIRQ(true);
        
        // Execute NOP (should trigger IRQ at end)
        ExecuteOne();
        
        // PC should jump to IRQ vector
        Assert.That(cpu.PC, Is.EqualTo(0x5000));
        
        // I flag should be set
        Assert.That((cpu.P & 0x04), Is.EqualTo(0x04));
    }

    [Test]
    public void Irq_WhenIFlagSet_IsIgnored()
    {
        LoadProgram(0xEA); // NOP
        cpu!.P = 0x04; // Set I flag (I=1)
        
        // Trigger IRQ
        cpu.SetIRQ(true);
        
        // Execute NOP (should NOT trigger IRQ)
        ExecuteOne();
        
        // PC should be at next instruction (NOP + 1)
        Assert.That(cpu.PC, Is.EqualTo(0x0101));
    }

    [Test]
    public void Irq_PushesPCAndPWithBFlagZero()
    {
        LoadProgram(0xEA); // NOP
        cpu!.SP = 0xFF;
        cpu.P = 0x00; // Clear all flags
        
        // Set IRQ vector to $6000
        memory!.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x60);
        
        // Trigger IRQ
        cpu.SetIRQ(true);
        
        // Execute NOP (should trigger IRQ)
        ExecuteOne();
        
        // Stack should have: PCH, PCL, P (with B=0)
        byte pushedP = memory!.Read((ushort)(0x0100 + cpu.SP + 1));
        Assert.That((pushedP & 0x10), Is.EqualTo(0x00)); // B=0
        Assert.That((pushedP & 0x20), Is.EqualTo(0x20)); // bit5=1
    }

    [Test]
    public void Nmi_TriggersOnFallingEdge()
    {
        LoadProgram(0xEA); // NOP
        
        // Set NMI vector to $7000
        memory!.Write(0xFFFA, 0x00);
        memory.Write(0xFFFB, 0x70);
        
        // Set NMI pin high then low (falling edge)
        cpu.SetNMI(true);  // high
        cpu.SetNMI(false); // falling edge - should latch
        
        // Execute NOP (should trigger NMI)
        ExecuteOne();
        
        // PC should jump to NMI vector
        Assert.That(cpu.PC, Is.EqualTo(0x7000));
    }

    [Test]
    public void Nmi_IgnoresIFlag()
    {
        LoadProgram(0xEA); // NOP
        cpu!.P = 0x04; // Set I flag (I=1)
        
        // Set NMI vector to $8000
        memory!.Write(0xFFFA, 0x00);
        memory.Write(0xFFFB, 0x80);
        
        // Trigger NMI
        cpu.SetNMI(true);
        cpu.SetNMI(false);
        
        // Execute NOP (should trigger NMI despite I=1)
        ExecuteOne();
        
        // PC should jump to NMI vector
        Assert.That(cpu.PC, Is.EqualTo(0x8000));
    }

    [Test]
    public void Nmi_PushesPCAndPWithBFlagZero()
    {
        LoadProgram(0xEA); // NOP
        cpu!.SP = 0xFF;
        cpu.P = 0x00; // Clear all flags
        
        // Set NMI vector to $9000
        memory!.Write(0xFFFA, 0x00);
        memory.Write(0xFFFB, 0x90);
        
        // Trigger NMI
        cpu.SetNMI(true);
        cpu.SetNMI(false);
        
        // Execute NOP (should trigger NMI)
        ExecuteOne();
        
        // Stack should have: PCH, PCL, P (with B=0)
        byte pushedP = memory!.Read((ushort)(0x0100 + cpu.SP + 1));
        Assert.That((pushedP & 0x10), Is.EqualTo(0x00)); // B=0
        Assert.That((pushedP & 0x20), Is.EqualTo(0x20)); // bit5=1
    }

    [Test]
    public void Cli_DelaysIrqByOneInstruction()
    {
        LoadProgram(0x58, 0xEA); // CLI followed by NOP
        cpu!.P = 0x04; // Set I flag (I=1)
        
        // Set IRQ vector to $A000
        memory!.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0xA0);
        
        // Trigger IRQ
        cpu.SetIRQ(true);
        
        // Execute CLI (should clear I flag)
        ExecuteOne();
        
        // I flag should be clear now
        Assert.That((cpu.P & 0x04), Is.EqualTo(0x00));
        
        // PC should be at NOP (next instruction, CLI is 1 byte)
        Assert.That(cpu.PC, Is.EqualTo(0x0101));
        
        // Execute NOP (next instruction completes because of CLI delay)
        ExecuteOne();
        
        // PC should be past NOP (NOP is 1 byte)
        Assert.That(cpu.PC, Is.EqualTo(0x0102));
        
        // Next Tick: IRQ should fire before the next instruction
        ExecuteOne();
        
        // PC should jump to IRQ vector
        Assert.That(cpu.PC, Is.EqualTo(0xA000));
    }
}
