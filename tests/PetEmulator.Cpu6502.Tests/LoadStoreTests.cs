using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class LoadStoreTests
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

    private void SetZp(byte addr, byte value) => memory!.Write(addr, value);
    private void SetAbs(ushort addr, byte value) => memory!.Write(addr, value);

    [Test]
    public void T1_1_LdaImmediate_LoadsA()
    {
        LoadProgram([0xA9, 0x42]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x42));
        Assert.That(cpu!.PC, Is.EqualTo(0x0102));
    }

    [Test]
    public void T1_2_LdaImmediate_SetsNZ_Zero()
    {
        LoadProgram([0xA9, 0x00]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.True);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.False);
    }

    [Test]
    public void T1_3_LdaImmediate_SetsNZ_Negative()
    {
        LoadProgram([0xA9, 0x80]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.False);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.True);
    }

    [Test]
    public void T1_4_LdaZeroPage_LoadsFromMemory()
    {
        SetZp(0x10, 0x55);
        LoadProgram([0xA5, 0x10]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x55));
        Assert.That(cpu!.PC, Is.EqualTo(0x0102));
    }

    [Test]
    public void T1_5_LdaZeroPageX_WrapsInPage()
    {
        // Program nie może zajmować adresów ZP używanych przez test
        // Umieść program wyżej, w $0100
        cpu!.X = 2;
        SetZp(0x01, 0x77);
        LoadProgram([0xB5, 0xFF]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x77));
    }

    [Test]
    public void T1_6_LdaAbsolute_LoadsFromFullAddress()
    {
        SetAbs(0x1234, 0x99);
        LoadProgram([0xAD, 0x34, 0x12]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x99));
        Assert.That(cpu!.PC, Is.EqualTo(0x0103));
    }

    [Test]
    public void T1_7_LdaAbsoluteX_CrossesPage()
    {
        cpu!.X = 2;
        SetAbs(0x1301, 0xBB);
        LoadProgram([0xBD, 0xFF, 0x12]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0xBB));
    }

    [Test]
    public void T1_8_LdaIndirectX_PreIndexed()
    {
        cpu!.X = 4;
        SetZp(0x24, 0xCD);
        SetZp(0x25, 0xAB);
        SetAbs(0xABCD, 0x42);
        LoadProgram([0xA1, 0x20]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x42));
    }

    [Test]
    public void T1_9_LdaIndirectY_PostIndexed()
    {
        cpu!.Y = 3;
        SetZp(0x20, 0x00);
        SetZp(0x21, 0x80);
        SetAbs(0x8003, 0x5A);
        LoadProgram([0xB1, 0x20]);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0x5A));
    }

    [Test]
    public void T1_10_LdxImmediate_LoadsX()
    {
        LoadProgram([0xA2, 0x7F]);
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x7F));
        Assert.That(cpu!.PC, Is.EqualTo(0x0102));
    }

    [Test]
    public void T1_11_LdyZeroPage_LoadsY()
    {
        SetZp(0x30, 0x33);
        LoadProgram([0xA4, 0x30]);
        ExecuteOne();
        Assert.That(cpu!.Y, Is.EqualTo(0x33));
    }

    [Test]
    public void T1_12_StaZeroPage_StoresA()
    {
        cpu!.A = 0xAB;
        LoadProgram([0x85, 0x50]);
        ExecuteOne();
        Assert.That(memory!.Read(0x50), Is.EqualTo(0xAB));
    }

    [Test]
    public void T1_13_StaAbsolute_StoresToFullAddress()
    {
        cpu!.A = 0xCD;
        LoadProgram([0x8D, 0x00, 0x20]);
        ExecuteOne();
        Assert.That(memory!.Read(0x2000), Is.EqualTo(0xCD));
    }

    [Test]
    public void T1_14_StaDoesNotAffectFlags()
    {
        cpu!.A = 0xAB;
        cpu!.SetFlag(Cpu6502.FlagN, true);
        cpu!.SetFlag(Cpu6502.FlagZ, true);
        LoadProgram([0x85, 0x40]);
        ExecuteOne();
        Assert.That(cpu!.GetFlag(Cpu6502.FlagN), Is.True);
        Assert.That(cpu!.GetFlag(Cpu6502.FlagZ), Is.True);
    }

    [Test]
    public void T1_15_StxAbsolute_StoresX()
    {
        cpu!.X = 0x42;
        LoadProgram([0x8E, 0x00, 0x30]);
        ExecuteOne();
        Assert.That(memory!.Read(0x3000), Is.EqualTo(0x42));
    }

    [Test]
    public void T1_16_StyZeroPage_StoresY()
    {
        cpu!.Y = 0x77;
        LoadProgram([0x84, 0x05]);
        ExecuteOne();
        Assert.That(memory!.Read(0x05), Is.EqualTo(0x77));
    }

    [Test]
    public void T1_17_AllLdaModes_PcAdvancesCorrectly()
    {
        // LDA Immediate (2 bajty)
        LoadProgram([0xA9, 0x42]);
        ExecuteOne();
        Assert.That(cpu!.PC, Is.EqualTo(0x0102));

        // LDA Zero Page (2 bajty)
        cpu!.A = 0; // reset
        LoadProgram([0xA5, 0x10]);
        ExecuteOne();
        Assert.That(cpu!.PC, Is.EqualTo(0x0102));

        // LDA Absolute (3 bajty)
        cpu!.A = 0; // reset
        LoadProgram([0xAD, 0x34, 0x12]);
        ExecuteOne();
        Assert.That(cpu!.PC, Is.EqualTo(0x0103));
    }

    [Test]
    public void T1_18_LdaIndirectX_ZeroPageWrap()
    {
        cpu!.X = 2;
        // Ustaw ZP PO załadowaniu programu, żeby nie zostało nadpisane
        LoadProgram([0xA1, 0xFF]);
        SetZp(0x01, 0x34);
        SetZp(0x02, 0x12);
        SetAbs(0x1234, 0xAA);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0xAA));
    }

    [Test]
    public void T1_19_LdaIndirectY_ZeroPageWrap()
    {
        cpu!.Y = 0;
        LoadProgram([0xB1, 0xFF]);
        SetZp(0xFF, 0xCD);
        SetZp(0x00, 0xAB);
        SetAbs(0xABCD, 0xBB);
        ExecuteOne();
        Assert.That(cpu!.A, Is.EqualTo(0xBB));
    }

    [Test]
    public void T1_20_StaWritesCorrectAddr_AllModes()
    {
        // Test STA Absolute
        cpu!.A = 0x11;
        LoadProgram([0x8D, 0x00, 0x40]);
        ExecuteOne();
        Assert.That(memory!.Read(0x4000), Is.EqualTo(0x11));

        // Test STA Zero Page
        cpu!.A = 0x22;
        LoadProgram([0x85, 0x60]);
        ExecuteOne();
        Assert.That(memory!.Read(0x60), Is.EqualTo(0x22));

        // Test STA Zero Page,X
        cpu!.A = 0x33;
        cpu!.X = 5;
        LoadProgram([0x95, 0x70]);
        ExecuteOne();
        Assert.That(memory!.Read(0x75), Is.EqualTo(0x33));

        // Test STA Absolute,X
        cpu!.A = 0x44;
        cpu!.X = 3;
        LoadProgram([0x9D, 0x00, 0x50]);
        ExecuteOne();
        Assert.That(memory!.Read(0x5003), Is.EqualTo(0x44));

        // Test STA Absolute,Y
        cpu!.A = 0x55;
        cpu!.Y = 7;
        LoadProgram([0x99, 0x00, 0x60]);
        ExecuteOne();
        Assert.That(memory!.Read(0x6007), Is.EqualTo(0x55));

        // Test STA (Indirect,X)
        cpu!.A = 0x66;
        cpu!.X = 0;
        LoadProgram([0x81, 0x20]);
        SetZp(0x20, 0x00);
        SetZp(0x21, 0x70);
        ExecuteOne();
        Assert.That(memory!.Read(0x7000), Is.EqualTo(0x66));

        // Test STA (Indirect),Y
        cpu!.A = 0x77;
        cpu!.Y = 4;
        LoadProgram([0x91, 0x30]);
        SetZp(0x30, 0x00);
        SetZp(0x31, 0x80);
        ExecuteOne();
        Assert.That(memory!.Read(0x8004), Is.EqualTo(0x77));
    }

    [Test]
    public void LdxZeroPage_LoadsX()
    {
        SetZp(0x10, 0x7F);
        LoadProgram([0xA6, 0x10]);
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x7F));
    }

    [Test]
    public void LdxZeroPageY_LoadsX()
    {
        cpu!.Y = 2;
        SetZp(0x12, 0x3F);
        LoadProgram([0xB6, 0x10]);
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x3F));
    }

    [Test]
    public void LdxAbsolute_LoadsX()
    {
        SetAbs(0x1234, 0x5A);
        LoadProgram([0xAE, 0x34, 0x12]);
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x5A));
    }

    [Test]
    public void LdxAbsoluteY_LoadsX()
    {
        cpu!.Y = 1;
        SetAbs(0x1235, 0x6B);
        LoadProgram([0xBE, 0x34, 0x12]);
        ExecuteOne();
        Assert.That(cpu!.X, Is.EqualTo(0x6B));
    }

    [Test]
    public void LdyImmediate_LoadsY()
    {
        LoadProgram([0xA0, 0x20]);
        ExecuteOne();
        Assert.That(cpu!.Y, Is.EqualTo(0x20));
    }

    [Test]
    public void LdyZeroPageX_LoadsY()
    {
        cpu!.X = 3;
        SetZp(0x13, 0x4D);
        LoadProgram([0xB4, 0x10]);
        ExecuteOne();
        Assert.That(cpu!.Y, Is.EqualTo(0x4D));
    }

    [Test]
    public void LdyAbsolute_LoadsY()
    {
        SetAbs(0x5678, 0xE0);
        LoadProgram([0xAC, 0x78, 0x56]);
        ExecuteOne();
        Assert.That(cpu!.Y, Is.EqualTo(0xE0));
    }

    [Test]
    public void LdyAbsoluteX_LoadsY()
    {
        cpu!.X = 2;
        SetAbs(0x567A, 0xF1);
        LoadProgram([0xBC, 0x78, 0x56]);
        ExecuteOne();
        Assert.That(cpu!.Y, Is.EqualTo(0xF1));
    }

    [Test]
    public void StxZeroPage_StoresX()
    {
        cpu!.X = 0x99;
        LoadProgram([0x86, 0x20]);
        ExecuteOne();
        Assert.That(memory!.Read(0x20), Is.EqualTo(0x99));
    }

    [Test]
    public void StxZeroPageY_StoresX()
    {
        cpu!.X = 0x88;
        cpu!.Y = 5;
        LoadProgram([0x96, 0x20]);
        ExecuteOne();
        Assert.That(memory!.Read(0x25), Is.EqualTo(0x88));
    }

    [Test]
    public void StyZeroPageX_StoresY()
    {
        cpu!.Y = 0x66;
        cpu!.X = 4;
        LoadProgram([0x94, 0x30]);
        ExecuteOne();
        Assert.That(memory!.Read(0x34), Is.EqualTo(0x66));
    }

    [Test]
    public void StyAbsolute_StoresY()
    {
        cpu!.Y = 0x55;
        LoadProgram([0x8C, 0x00, 0x40]);
        ExecuteOne();
        Assert.That(memory!.Read(0x4000), Is.EqualTo(0x55));
    }
}
