using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class SkeletonTests
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
    public void CreateCpuDoesNotThrow()
    {
        // Arrange
        var mem = new FlatMemory();

        // Act
        var result = new Cpu6502(mem);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Reset_LoadsVectorAndSetsSP()
    {
        // Arrange
        memory!.Write(0xFFFC, 0x00);
        memory!.Write(0xFFFD, 0xC0);

        // Act
        cpu!.Reset();

        // Assert
        Assert.That(cpu!.PC, Is.EqualTo(0xC000));
        Assert.That(cpu!.SP, Is.EqualTo(0xFD));
        Assert.That(cpu!.GetFlag(Cpu6502.FlagI), Is.True);
    }

    [Test]
    public void Reset_SetsFlagI()
    {
        // Arrange
        memory!.Write(0xFFFC, 0x00);
        memory!.Write(0xFFFD, 0x00);

        // Act
        cpu!.Reset();

        // Assert
        Assert.That(cpu!.GetFlag(Cpu6502.FlagI), Is.True);
        // Flag U (bit 5) jest zawsze ustawiona na 1 w prawdziwym 6502
        Assert.That(cpu!.GetFlag(Cpu6502.FlagU), Is.True);
    }

    [Test]
    public void Reset_ClearsOtherFlags()
    {
        // Arrange
        memory!.Write(0xFFFC, 0x00);
        memory!.Write(0xFFFD, 0x00);

        // Act
        cpu!.Reset();

        // Assert
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagD), Is.False);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagB), Is.False);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagV), Is.False);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.False);
        // Flag U jest zawsze ustawiona na 1 w prawdziwym 6502
        Assert.That(cpu!.GetFlag(Cpu6502.FlagU), Is.True);
    }

    [Test]
    public void Reset_ClearsRegisters()
    {
        // Arrange
        memory!.Write(0xFFFC, 0x00);
        memory!.Write(0xFFFD, 0x00);

        // Act
        cpu!.Reset();

        // Assert
        Assert.That(cpu!.GetState().A, Is.EqualTo(0x00));
        Assert.That(cpu!.GetState().X, Is.EqualTo(0x00));
        Assert.That(cpu!.GetState().Y, Is.EqualTo(0x00));
    }

    [Test]
    public void Tick_DoesNotThrow_AfterReset()
    {
        // Arrange
        memory!.Write(0xFFFC, 0x00);
        memory!.Write(0xFFFD, 0x00);
        cpu!.Reset();

        // Act & Assert - Tick nie rzuca wyjątku, bo wszystkie opkody są mapowane na NOP lub implementacje
        Assert.DoesNotThrow(() => cpu!.Tick());
    }

    [Test]
    public void FlatMemory_ReadWrite()
    {
        // Arrange
        var mem = new FlatMemory();
        var testValue = (byte)0xAB;

        // Act
        mem.Write(0x1234, testValue);
        var result = mem.Read(0x1234);

        // Assert
        Assert.That(result, Is.EqualTo(testValue));
    }

    [Test]
    public void FlatMemory_LoadRom()
    {
        // Arrange
        var mem = new FlatMemory();
        var romData = new byte[] { 0x4C, 0x00, 0x80, 0xEA };

        // Act
        mem.LoadRom(0x8000, romData);

        // Assert
        Assert.That(mem.Read(0x8000), Is.EqualTo(0x4C));
        Assert.That(mem.Read(0x8001), Is.EqualTo(0x00));
        Assert.That(mem.Read(0x8002), Is.EqualTo(0x80));
        Assert.That(mem.Read(0x8003), Is.EqualTo(0xEA));
    }
}
