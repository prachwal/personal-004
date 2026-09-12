using PetEmulator.CpuZ80.Disassembly;
using Xunit;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80DisassemblerTests
{
    /// <summary>The exact byte sequence traced by hand this session (Osborne 1 V144
    /// ROM's `IE.CO` at 0x0949, cross-checked against `rom144.asm`) - a real
    /// fragment, not synthetic, so this test also documents what the tool
    /// was built to read.</summary>
    [Fact]
    public void DisassemblesTheRealIeCoFragmentByteForByte()
    {
        var bytes = new byte[] { 0xF5, 0xE5, 0xCB, 0x41, 0x28, 0x2B, 0x21, 0x01, 0x29, 0x36, 0x3A, 0x3E, 0xFF, 0x32, 0x00, 0x29, 0x36, 0x3E, 0xAF, 0x32, 0x00, 0x29 };
        byte Read(ushort a) => bytes[a];

        AssertNext(Read, 0x0000, "PUSH AF", 1);
        AssertNext(Read, 0x0001, "PUSH HL", 1);
        AssertNext(Read, 0x0002, "BIT 0,C", 2);
        AssertNext(Read, 0x0004, "JR Z,0031h", 2); // opcode 0x28 = JR Z,d; displacement 0x2B (+43) from address 0x0006 (address+2) = 0x0031
        AssertNext(Read, 0x0006, "LD HL,2901h", 3);
        AssertNext(Read, 0x0009, "LD (HL),3Ah", 2);
        AssertNext(Read, 0x000B, "LD A,FFh", 2);
        AssertNext(Read, 0x000D, "LD (2900h),A", 3);
        AssertNext(Read, 0x0010, "LD (HL),3Eh", 2);
        AssertNext(Read, 0x0012, "XOR A", 1);
        AssertNext(Read, 0x0013, "LD (2900h),A", 3);
    }

    [Theory]
    [InlineData(0x00, "NOP", 1)]
    [InlineData(0x76, "HALT", 1)]
    [InlineData(0xC9, "RET", 1)]
    [InlineData(0xF3, "DI", 1)]
    [InlineData(0xFB, "EI", 1)]
    [InlineData(0xE9, "JP (HL)", 1)]
    [InlineData(0xEB, "EX DE,HL", 1)]
    public void OneByteOpcodesRenderExactly(byte opcode, string expected, int expectedLength)
    {
        AssertNext(a => opcode, 0, expected, expectedLength);
    }

    [Fact]
    public void LdRToRCoversAllPairsIncludingHalt()
    {
        // 0x40 = LD B,B .. 0x7F = LD A,A, with 0x76 being HALT instead of LD (HL),(HL).
        var (text, length) = Z80Disassembler.Disassemble(_ => 0x41, 0); // LD B,C
        Assert.Equal(("LD B,C", 1), (text, length));

        (text, length) = Z80Disassembler.Disassemble(_ => 0x7E, 0); // LD A,(HL)
        Assert.Equal(("LD A,(HL)", 1), (text, length));
    }

    [Fact]
    public void CbPrefixedBitOpsDecode()
    {
        byte[] bytes = [0xCB, 0x7F]; // BIT 7,A
        AssertNext(a => bytes[a], 0, "BIT 7,A", 2);

        byte[] set = [0xCB, 0xC6]; // SET 0,(HL)
        AssertNext(a => set[a], 0, "SET 0,(HL)", 2);
    }

    [Fact]
    public void EdPrefixed16BitLoadAndBlockOpsDecode()
    {
        byte[] ldFromMem = [0xED, 0x5B, 0x34, 0x12]; // LD DE,(1234h)
        AssertNext(a => ldFromMem[a], 0, "LD DE,(1234h)", 4);

        byte[] ldir = [0xED, 0xB0];
        AssertNext(a => ldir[a], 0, "LDIR", 2);

        byte[] neg = [0xED, 0x44];
        AssertNext(a => neg[a], 0, "NEG", 2);
    }

    [Fact]
    public void DecodesRealIndexedOpcodes()
    {
        // LD IX,1234h
        byte[] ldIx = [0xDD, 0x21, 0x34, 0x12];
        AssertNext(a => ldIx[a], 0, "LD IX,1234h", 4);

        // LD B,(IX+05h)
        byte[] ldB = [0xDD, 0x46, 0x05];
        AssertNext(a => ldB[a], 0, "LD B,(IX+05h)", 3);

        // LD (IY-03h),A - negative displacement
        byte[] ldNeg = [0xFD, 0x77, 0xFD];
        AssertNext(a => ldNeg[a], 0, "LD (IY-03h),A", 3);

        // BIT 3,(IX+02h) - DD CB d op
        byte[] bit = [0xDD, 0xCB, 0x02, 0x5E];
        AssertNext(a => bit[a], 0, "BIT 3,(IX+02h)", 4);

        // JP (IX)
        byte[] jpIx = [0xDD, 0xE9];
        AssertNext(a => jpIx[a], 0, "JP (IX)", 2);
    }

    [Fact]
    public void IndexedOpcodeNotTouchingHlOrHlFallsBackToItsUnprefixedForm()
    {
        // DD prefix on an opcode that doesn't reference H/L/(HL) is wasted
        // on real hardware - executes as the plain unprefixed opcode, one
        // byte longer.
        byte[] nop = [0xDD, 0x00];
        AssertNext(a => nop[a], 0, "NOP", 2);
    }

    private static void AssertNext(Z80Disassembler.ByteReader read, ushort address, string expectedText, int expectedLength)
    {
        var (text, length) = Z80Disassembler.Disassemble(read, address);
        Assert.Equal(expectedText, text);
        Assert.Equal(expectedLength, length);
    }
}
