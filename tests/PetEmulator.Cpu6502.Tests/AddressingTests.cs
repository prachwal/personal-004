using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class AddressingTests
{
    private FlatMemory? memory;
    private Cpu6502? cpu;

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

    [SetUp]
    public void Setup()
    {
        memory = new FlatMemory();
        cpu = new Cpu6502(memory);
        memory.Write(0xFFFC, 0x00);
        memory.Write(0xFFFD, 0x00);
        cpu.Reset();
    }

    [Test]
    public void AddrZp_ReturnsZeroPageAddress()
    {
        // Set up zero page value
        memory!.Write(0x42, 0xAA);
        
        // Load program that uses LDA zp
        LoadProgram(0xA5, 0x42); // LDA $42
        
        ExecuteOne();
        
        // Should load value from zero page
        Assert.That(cpu.A, Is.EqualTo(0xAA));
    }

    [Test]
    public void AddrZpX_WrapsWithinZeroPage()
    {
        // Set up zero page values - value should be at 0x7F (wrapped address)
        memory!.Write(0x7F, 0xBB);
        
        // Load program that uses LDA zp,X with X=0xFF (wraps around)
        LoadProgram(0xB5, 0x80); // LDA $80,X
        cpu.X = 0xFF; // X = 0xFF, so 0x80 + 0xFF = 0x7F (wraps)
        
        ExecuteOne();
        
        // Should wrap: 0x80 + 0xFF = 0x7F and load value 0xBB
        Assert.That(cpu.A, Is.EqualTo(0xBB));
    }

    [Test]
    public void AddrAbsX_PageCrossingDetected()
    {
        // Set up memory
        memory!.Write(0x01FF, 0xCC);
        memory.Write(0x0201, 0xDD); // Value should be at 0x0201 (0x01FF + 0x02)
        
        // Load program that uses LDA abs,X with page crossing
        LoadProgram(0xBD, 0xFF, 0x01); // LDA $01FF,X
        cpu.X = 0x02; // X = 0x02, so 0x01FF + 0x02 = 0x0201 (page cross)
        
        ExecuteOne();
        
        // Should load from 0x0201 and take extra cycle for page crossing
        Assert.That(cpu.A, Is.EqualTo(0xDD));
    }

    [Test]
    public void AddrAbsX_NoPageCrossing()
    {
        // Set up memory
        memory!.Write(0x01FF, 0xEE); // Value should be at 0x01FF (0x0180 + 0x7F)
        
        // Load program that uses LDA abs,X without page crossing
        LoadProgram(0xBD, 0x80, 0x01); // LDA $0180,X
        cpu.X = 0x7F; // X = 0x7F, so 0x0180 + 0x7F = 0x01FF (no page cross)
        
        ExecuteOne();
        
        // Should load from 0x01FF
        Assert.That(cpu.A, Is.EqualTo(0xEE));
    }

    [Test]
    public void AddrIndX_WrapsInZeroPage()
    {
        // Set up indirect address in zero page
        memory!.Write(0x10, 0x20);  // Low byte
        memory.Write(0x11, 0x03);   // High byte
        memory.Write(0x0320, 0xFF); // Target value
        
        // Load program that uses LDA (zp,X)
        LoadProgram(0xA1, 0x0F); // LDA ($0F,X)
        cpu.X = 0x01; // X = 0x01, so 0x0F + 0x01 = 0x10
        
        ExecuteOne();
        
        // Should load from indirect address 0x0320
        Assert.That(cpu.A, Is.EqualTo(0xFF));
    }

    [Test]
    public void AddrIndY_PageCrossingDetected()
    {
        // Set up indirect address
        memory!.Write(0x40, 0xFF);  // Low byte
        memory.Write(0x41, 0x01);   // High byte
        memory.Write(0x0201, 0x11); // Target value (0x01FF + 0x02 = 0x0201)
        
        // Load program that uses LDA (zp),Y with page crossing
        LoadProgram(0xB1, 0x40); // LDA ($40),Y
        cpu.Y = 0x02; // Y = 0x02, so 0x01FF + 0x02 = 0x0201 (page cross)
        
        ExecuteOne();
        
        // Should load from 0x0201 and take extra cycle
        Assert.That(cpu.A, Is.EqualTo(0x11));
    }

    [Test]
    public void AllPreviousInstructionsStillWork()
    {
        // Test a few instructions from previous phases to ensure no regression
        
        // Test LDA immediate
        LoadProgram(0xA9, 0x42); // LDA #$42
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x42));
        
        // Test ADC zero page
        memory!.Write(0x50, 0x10);
        LoadProgram(0x65, 0x50); // ADC $50
        cpu.A = 0x20;
        ExecuteOne();
        Assert.That(cpu.A, Is.EqualTo(0x30));
        
        // Test STA absolute
        LoadProgram(0x8D, 0x00, 0x04); // STA $0400
        cpu.A = 0x77;
        ExecuteOne();
        Assert.That(memory.Read(0x0400), Is.EqualTo(0x77));
    }
}
