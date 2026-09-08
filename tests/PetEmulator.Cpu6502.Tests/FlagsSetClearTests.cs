using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class FlagsSetClearTests
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
    public void CLC_ClearsCarry()
    {
        // Ustaw C=1, wykonaj CLC → C=0
        cpu!.SetFlag(Cpu6502.FlagC, true);
        cpu.SetFlag(Cpu6502.FlagN, true);  // Inna flaga powinna zostać
        LoadProgram([0x18]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void SEC_SetsCarry()
    {
        // Ustaw C=0, wykonaj SEC → C=1
        cpu!.SetFlag(Cpu6502.FlagC, false);
        LoadProgram([0x38]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.True);
    }

    [Test]
    public void CLC_DoesNotAffectOtherFlags()
    {
        // Sprawdź że CLC nie zmienia innych flag
        cpu!.SetFlag(Cpu6502.FlagC, true);
        cpu.SetFlag(Cpu6502.FlagN, true);
        cpu.SetFlag(Cpu6502.FlagZ, true);
        cpu.SetFlag(Cpu6502.FlagV, true);
        cpu.SetFlag(Cpu6502.FlagD, true);
        cpu.SetFlag(Cpu6502.FlagI, true);
        LoadProgram([0x18]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagD), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagI), Is.True);
    }

    [Test]
    public void SEC_SetsCarryDoesNotAffectOtherFlags()
    {
        // Sprawdź że SEC nie zmienia innych flag
        cpu!.SetFlag(Cpu6502.FlagC, false);
        cpu.SetFlag(Cpu6502.FlagN, false);
        cpu.SetFlag(Cpu6502.FlagZ, false);
        cpu.SetFlag(Cpu6502.FlagV, false);
        cpu.SetFlag(Cpu6502.FlagD, false);
        cpu.SetFlag(Cpu6502.FlagI, false);
        LoadProgram([0x38]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagV), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagD), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagI), Is.False);
    }

    [Test]
    public void CLD_ClearsDecimal()
    {
        // Ustaw D=1, wykonaj CLD → D=0
        cpu!.SetFlag(Cpu6502.FlagD, true);
        LoadProgram([0xD8]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagD), Is.False);
    }

    [Test]
    public void SED_SetsDecimal()
    {
        // Ustaw D=0, wykonaj SED → D=1
        cpu!.SetFlag(Cpu6502.FlagD, false);
        LoadProgram([0xF8]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagD), Is.True);
    }

    [Test]
    public void CLI_ClearsInterrupt()
    {
        // Ustaw I=1, wykonaj CLI → I=0
        cpu!.SetFlag(Cpu6502.FlagI, true);
        LoadProgram([0x58]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagI), Is.False);
    }

    [Test]
    public void SEI_SetsInterrupt()
    {
        // Ustaw I=0, wykonaj SEI → I=1
        cpu!.SetFlag(Cpu6502.FlagI, false);
        LoadProgram([0x78]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagI), Is.True);
    }

    [Test]
    public void CLV_ClearsOverflow()
    {
        // Ustaw V=1, wykonaj CLV → V=0
        cpu!.SetFlag(Cpu6502.FlagV, true);
        LoadProgram([0xB8]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagV), Is.False);
    }

    [Test]
    public void CLV_DoesNotAffectOtherFlags()
    {
        // Sprawdź że CLV nie zmienia innych flag
        cpu!.SetFlag(Cpu6502.FlagV, true);
        cpu.SetFlag(Cpu6502.FlagN, true);
        cpu.SetFlag(Cpu6502.FlagZ, true);
        cpu.SetFlag(Cpu6502.FlagC, true);
        cpu.SetFlag(Cpu6502.FlagD, true);
        cpu.SetFlag(Cpu6502.FlagI, true);
        LoadProgram([0xB8]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagV), Is.False);
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagC), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagD), Is.True);
        Assert.That(cpu.GetFlag(Cpu6502.FlagI), Is.True);
    }
}
