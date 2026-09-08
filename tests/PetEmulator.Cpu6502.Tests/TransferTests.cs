using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class TransferTests
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
    public void TAX_CopiesAToX()
    {
        // Test 1: TAX kopiuje A do X
        cpu!.A = 0x42;
        LoadProgram([0xAA]);
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x42));
    }

    [Test]
    public void TAX_SetsNZ_Zero()
    {
        // Test 2: TAX ustawia Z=1 gdy A=0
        cpu!.A = 0x00;
        LoadProgram([0xAA]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void TAX_SetsNZ_Negative()
    {
        // Test 3: TAX ustawia N=1 gdy A>=0x80
        cpu!.A = 0x80;
        LoadProgram([0xAA]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.False);
    }

    [Test]
    public void TAY_CopiesAToY()
    {
        // Test 4: TAY kopiuje A do Y
        cpu!.A = 0x55;
        LoadProgram([0xA8]);
        ExecuteOne();
        Assert.That(cpu!.Y, Is.EqualTo(0x55));
    }

    [Test]
    public void TSX_CopiesSPtoX()
    {
        // Test 5: TSX kopiuje SP do X
        cpu!.SP = 0xFD;
        LoadProgram([0xBA]);
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0xFD));
    }

    [Test]
    public void TXA_CopiesXtoA()
    {
        // Test 6: TXA kopiuje X do A
        cpu!.X = 0x33;
        LoadProgram([0x8A]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x33));
    }

    [Test]
    public void TXS_CopiesXtoSP()
    {
        // Test 7: TXS kopiuje X do SP
        cpu!.X = 0xFF;
        LoadProgram([0x9A]);
        ExecuteOne();
        Assert.That(cpu!.SP, Is.EqualTo(0xFF));
    }

    [Test]
    public void TXS_DoesNotAffectFlags()
    {
        // Test 8: TXS nie modyfikuje flag
        cpu!.X = 0x80;
        cpu!.SetFlag(Cpu6502.FlagN, true);
        cpu!.SetFlag(Cpu6502.FlagZ, true);
        LoadProgram([0x9A]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void TYA_CopiesYtoA()
    {
        // Test 9: TYA kopiuje Y do A
        cpu!.Y = 0x7F;
        LoadProgram([0x98]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x7F));
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.False);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.False);
    }
}
