using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Cpu6502;

namespace PetEmulator.Cpu6502.Tests;

[TestFixture]
public sealed class CoreVariantOpcodeTableTests
{
    [Test]
    public void Cmos65C02_core_table_has_all_entries_and_does_not_mutate_nmos()
    {
        var nmos = OpcodeTables.NmosCore;
        var cmos = OpcodeTables.Cmos65C02Core;

        Assert.That(cmos.Entries, Has.Count.EqualTo(256));
        Assert.That(cmos.IsSealed, Is.True);
        Assert.That(nmos.Get(OpcodeKey.Base(0x04)).Mnemonic, Is.EqualTo("NOP"));
        Assert.That(cmos.Get(OpcodeKey.Base(0x04)).Mnemonic, Is.EqualTo("TSB"));
        Assert.That(cmos.Get(OpcodeKey.Base(0x1A)).Mnemonic, Is.EqualTo("INC"));
        Assert.That(cmos.Get(OpcodeKey.Base(0x80)).Mnemonic, Is.EqualTo("BRA"));
        Assert.That(cmos.Get(OpcodeKey.Base(0x5C)).BaseCycles, Is.EqualTo(8));
    }

    [Test]
    public void R65C02S_core_table_has_all_bit_and_wait_stop_opcodes()
    {
        var table = OpcodeTables.R65C02SCore;

        Assert.That(table.Entries, Has.Count.EqualTo(256));
        Assert.That(table.IsSealed, Is.True);
        for (var bit = 0; bit < 8; bit++)
        {
            Assert.That(table.Get(OpcodeKey.Base((byte)(0x07 + (bit << 4)))).Mnemonic, Is.EqualTo($"RMB{bit}"));
            Assert.That(table.Get(OpcodeKey.Base((byte)(0x87 + (bit << 4)))).Mnemonic, Is.EqualTo($"SMB{bit}"));
            Assert.That(table.Get(OpcodeKey.Base((byte)(0x0F + (bit << 4)))).Mnemonic, Is.EqualTo($"BBR{bit}"));
            Assert.That(table.Get(OpcodeKey.Base((byte)(0x8F + (bit << 4)))).Mnemonic, Is.EqualTo($"BBS{bit}"));
        }

        Assert.That(table.Get(OpcodeKey.Base(0xCB)).Mnemonic, Is.EqualTo("WAI"));
        Assert.That(table.Get(OpcodeKey.Base(0xDB)).Mnemonic, Is.EqualTo("STP"));
    }
}
