using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class BcdTests
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
    public void ADC_BCD_09Plus01Equals10()
    {
        // Test: $09 + $01 = $10 in BCD mode
        LoadProgram(0x69, 0x01); // ADC #$01
        cpu!.A = 0x09;
        cpu.P = Cpu6502.FlagD; // Set decimal mode
        
        ExecuteOne(); // Execute ADC
        
        // Should result in $10, no carry
        Assert.That(cpu.A, Is.EqualTo(0x10));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void ADC_BCD_99Plus01Equals00WithCarry()
    {
        // Test: $99 + $01 = $00 with carry in BCD mode
        LoadProgram(0x69, 0x01); // ADC #$01
        cpu!.A = 0x99;
        cpu.P = Cpu6502.FlagD; // Set decimal mode
        
        ExecuteOne(); // Execute ADC
        
        // Should result in $00 with carry
        Assert.That(cpu.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void ADC_BCD_99Plus99PlusCarryEquals99WithCarry()
    {
        LoadProgram(0x69, 0x99); // ADC #$99
        cpu!.A = 0x99;
        cpu.P = Cpu6502.FlagD | Cpu6502.FlagC;

        ExecuteOne();

        Assert.That(cpu.A, Is.EqualTo(0x99));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void ADC_BCD_50Plus50Equals00WithCarryAndOverflow()
    {
        // Test: $50 + $50 = $00 with carry and overflow in BCD mode
        LoadProgram(0x69, 0x50); // ADC #$50
        cpu!.A = 0x50;
        cpu.P = Cpu6502.FlagD; // Set decimal mode
        
        ExecuteOne(); // Execute ADC
        
        // Should result in $00 with carry and overflow
        Assert.That(cpu.A, Is.EqualTo(0x00));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void SBC_BCD_10Minus01Equals09()
    {
        // Test: $10 - $01 = $09 in BCD mode
        LoadProgram(0xE9, 0x01); // SBC #$01
        cpu!.A = 0x10;
        cpu.P = Cpu6502.FlagD | Cpu6502.FlagC; // Set decimal mode and carry (no borrow)
        
        ExecuteOne(); // Execute SBC
        
        // Should result in $09, carry remains set
        Assert.That(cpu.A, Is.EqualTo(0x09));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void SBC_BCD_00Minus01Equals99WithBorrow()
    {
        // Test: $00 - $01 = $99 with borrow in BCD mode
        LoadProgram(0xE9, 0x01); // SBC #$01
        cpu!.A = 0x00;
        cpu.P = Cpu6502.FlagD | Cpu6502.FlagC; // Set decimal mode and carry (no borrow initially)
        
        ExecuteOne(); // Execute SBC
        
        // Should result in $99, carry cleared (borrow)
        Assert.That(cpu.A, Is.EqualTo(0x99));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True); // Negative result
    }

    [Test]
    public void ADC_BinaryModeUnchanged()
    {
        // Test: Binary mode still works (not affected by BCD implementation)
        LoadProgram(0x69, 0x42); // ADC #$42
        cpu!.A = 0x10;
        cpu.P = 0x00; // Clear all flags (binary mode)
        
        ExecuteOne(); // Execute ADC
        
        // Should result in $52 in binary
        Assert.That(cpu.A, Is.EqualTo(0x52));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void SBC_BinaryModeUnchanged()
    {
        // Test: Binary mode still works (not affected by BCD implementation)
        LoadProgram(0xE9, 0x10); // SBC #$10
        cpu!.A = 0x30;
        cpu.P = Cpu6502.FlagC; // Set carry (binary mode, no decimal flag)
        
        ExecuteOne(); // Execute SBC
        
        // Should result in $20 in binary
        Assert.That(cpu.A, Is.EqualTo(0x20));
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void CLD_ClearsDecimalFlag()
    {
        // Test: CLD instruction clears decimal flag
        LoadProgram(0xD8); // CLD
        cpu!.P = 0xFF; // Set all flags
        
        ExecuteOne(); // Execute CLD
        
        // Decimal flag should be cleared
        Assert.That(cpu.GetFlag(Cpu6502.FlagD), Is.False);
    }

    [Test]
    public void SED_SetsDecimalFlag()
    {
        // Test: SED instruction sets decimal flag
        LoadProgram(0xF8); // SED
        cpu!.P = 0x00; // Clear all flags
        
        ExecuteOne(); // Execute SED
        
        // Decimal flag should be set
        Assert.That(cpu.GetFlag(Cpu6502.FlagD), Is.True);
    }
}
