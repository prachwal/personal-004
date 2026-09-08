using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

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
}
