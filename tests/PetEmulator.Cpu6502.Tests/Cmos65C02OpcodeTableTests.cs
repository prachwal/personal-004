using PetEmulator.Cpu6502;
using NUnit.Framework;

namespace PetEmulator.Cpu6502.Tests;

[TestFixture]
public class Cmos65C02OpcodeTableTests
{
    [Test]
    public void Cmos65C02_table_has_one_entry_for_every_opcode()
    {
        var table = OpcodeTables.CreateCmos65C02Table().Definitions;
        Assert.AreEqual(256, table.Count());
    }

    [Test]
    public void Cmos65C02_derives_from_shared_Nmos_without_mutating_it()
    {
        // 0x04/0x1A/0x89 are NMOS illegal-NOP slots that the 65C02 table
        // replaces with TSB/INC A/BIT #imm. If Derive() leaked mutation
        // back into the shared Nmos singleton, these mnemonics would change.
        var nmosMnemonicsBefore = new[] { OpcodeTables.Nmos[0x04].Mnemonic, OpcodeTables.Nmos[0x1A].Mnemonic, OpcodeTables.Nmos[0x89].Mnemonic };

        _ = OpcodeTables.CreateCmos65C02Table();

        Assert.AreEqual(nmosMnemonicsBefore[0], OpcodeTables.Nmos[0x04].Mnemonic);
        Assert.AreEqual(nmosMnemonicsBefore[1], OpcodeTables.Nmos[0x1A].Mnemonic);
        Assert.AreEqual(nmosMnemonicsBefore[2], OpcodeTables.Nmos[0x89].Mnemonic);
        Assert.AreNotEqual("TSB", OpcodeTables.Nmos[0x04].Mnemonic);
        Assert.AreEqual(256, OpcodeTables.Nmos.Definitions.Count());
    }

    [Test]
    public void New_65C02_opcodes_are_defined()
    {
        var table = OpcodeTables.CreateCmos65C02Table();

        Assert.AreEqual("TSB", table[0x04].Mnemonic);
        Assert.AreEqual("INC", table[0x1A].Mnemonic);
        Assert.AreEqual("PHY", table[0x5A].Mnemonic);
        Assert.AreEqual("STZ", table[0x64].Mnemonic);
        Assert.AreEqual("BRA", table[0x80].Mnemonic);
        Assert.AreEqual("BIT", table[0x89].Mnemonic);
        Assert.AreEqual("PHX", table[0xDA].Mnemonic);
        Assert.AreEqual("PLX", table[0xFA].Mnemonic);
    }

    private static readonly byte[] Tranche2RemappedToNop =
    [
        0x03,0x07,0x0F,0x13,0x17,0x1B,0x1F,0x23,0x27,0x2F,0x33,0x37,0x3B,0x3F,
        0x43,0x47,0x4F,0x53,0x57,0x5B,0x5F,0x63,0x67,0x6F,0x73,0x77,0x7B,0x7F,
        0x83,0x87,0x8B,0x8F,0x93,0x97,0x9B,0x9F,0xA3,0xA7,0xAB,0xAF,0xB3,0xB7,0xBB,0xBF,
        0xC3,0xC7,0xCF,0xD3,0xD7,0xDB,0xDF,0xE3,0xE7,0xEB,0xEF,0xF3,0xF7,0xFB,0xFF,
        0x02,0x22,0x42,0x62
    ];

    [Test]
    public void Tranche2_illegal_opcodes_become_NOP_on_cmos_table_without_mutating_Nmos()
    {
        var table = OpcodeTables.CreateCmos65C02Table();
        foreach (var op in Tranche2RemappedToNop)
        {
            Assert.AreEqual("NOP", table[op].Mnemonic, $"0x{op:X2} should be NOP on 65C02 table");
            Assert.AreNotEqual("NOP", OpcodeTables.Nmos[op].Mnemonic, $"0x{op:X2} on shared Nmos must be unchanged");
        }
    }

    [Test]
    public void Tranche2_already_NOP_bytes_are_left_untouched_by_design()
    {
        // These opcodes are already NOP in the NMOS table and don't need remapping
        byte[] alreadyCorrect = [0x44, 0x54, 0xD4, 0xF4, 0xDC, 0xFC];
        var table = OpcodeTables.CreateCmos65C02Table();
        foreach (var op in alreadyCorrect)
            Assert.AreEqual("NOP", table[op].Mnemonic, $"0x{op:X2}");
    }

    [Test]
    public void Verify_0x5C_is_NOP_after_remap()
    {
        var table = OpcodeTables.CreateCmos65C02Table();
        Assert.AreEqual("NOP", table[0x5C].Mnemonic);
        Assert.AreEqual(8, table[0x5C].BaseCycles);
    }
}
