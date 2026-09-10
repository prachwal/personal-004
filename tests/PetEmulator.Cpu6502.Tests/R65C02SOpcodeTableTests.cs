using System.Linq;
using PetEmulator.Cpu6502;
using PetEmulator.Cpu6502.Tests.TestHelpers;
using PetEmulator.Cpu6502.Variants;
using NUnit.Framework;

namespace PetEmulator.Cpu6502.Tests;

[TestFixture]
public class R65C02SOpcodeTableTests
{
    [Test]
    public void R65C02S_table_has_one_entry_for_every_opcode()
    {
        var table = OpcodeTables.CreateR65C02STable();
        Assert.AreEqual(256, table.Definitions.Count());
    }

    [Test]
    public void Plain_65C02_table_unaffected_by_R65C02S_derivation()
    {
        var plain = OpcodeTables.CreateCmos65C02Table();
        var plain0x07 = plain[0x07].Mnemonic;
        var plain0xCB = plain[0xCB].Mnemonic;
        var plain0xDB = plain[0xDB].Mnemonic;

        _ = OpcodeTables.CreateR65C02STable();

        Assert.AreEqual(plain0x07, plain[0x07].Mnemonic, "0x07 on 65C02 should be unchanged after R65C02S derivation");
        Assert.AreEqual(plain0xCB, plain[0xCB].Mnemonic, "0xCB on 65C02 should be unchanged after R65C02S derivation");
        Assert.AreEqual(plain0xDB, plain[0xDB].Mnemonic, "0xDB on 65C02 should be unchanged after R65C02S derivation");
    }

    [Test]
    public void RMB_SMB_BBR_BBS_WAI_STP_mnemonics_are_correct()
    {
        var t = OpcodeTables.CreateR65C02STable();
        Assert.AreEqual("RMB0", t[0x07].Mnemonic);
        Assert.AreEqual("RMB7", t[0x77].Mnemonic);
        Assert.AreEqual("SMB0", t[0x87].Mnemonic);
        Assert.AreEqual("SMB7", t[0xF7].Mnemonic);
        Assert.AreEqual("BBR0", t[0x0F].Mnemonic);
        Assert.AreEqual("BBR7", t[0x7F].Mnemonic);
        Assert.AreEqual("BBS0", t[0x8F].Mnemonic);
        Assert.AreEqual("BBS7", t[0xFF].Mnemonic);
        Assert.AreEqual("WAI", t[0xCB].Mnemonic);
        Assert.AreEqual("STP", t[0xDB].Mnemonic);
    }

    [Test]
    public void RMB_SMB_have_correct_lengths_and_cycles()
    {
        var t = OpcodeTables.CreateR65C02STable();
        for (byte i = 0; i < 8; i++)
        {
            var rmb = t[(byte)(0x07 + i * 0x10)];
            Assert.AreEqual(2, rmb.Length, $"RMB{i} should be 2 bytes");
            Assert.AreEqual(5, rmb.BaseCycles, $"RMB{i} should be 5 cycles");
            Assert.AreEqual(AddressingMode.ZeroPage, rmb.AddressingMode, $"RMB{i} should be ZeroPage addressing");

            var smb = t[(byte)(0x87 + i * 0x10)];
            Assert.AreEqual(2, smb.Length, $"SMB{i} should be 2 bytes");
            Assert.AreEqual(5, smb.BaseCycles, $"SMB{i} should be 5 cycles");
            Assert.AreEqual(AddressingMode.ZeroPage, smb.AddressingMode, $"SMB{i} should be ZeroPage addressing");
        }
    }

    [Test]
    public void BBR_BBS_have_correct_lengths_and_cycles()
    {
        var t = OpcodeTables.CreateR65C02STable();
        for (byte i = 0; i < 8; i++)
        {
            var bbr = t[(byte)(0x0F + i * 0x10)];
            Assert.AreEqual(3, bbr.Length, $"BBR{i} should be 3 bytes");
            Assert.AreEqual(5, bbr.BaseCycles, $"BBR{i} should be 5 cycles");
            Assert.AreEqual(AddressingMode.ZeroPageRelative, bbr.AddressingMode, $"BBR{i} should be ZeroPageRelative addressing");

            var bbs = t[(byte)(0x8F + i * 0x10)];
            Assert.AreEqual(3, bbs.Length, $"BBS{i} should be 3 bytes");
            Assert.AreEqual(5, bbs.BaseCycles, $"BBS{i} should be 5 cycles");
            Assert.AreEqual(AddressingMode.ZeroPageRelative, bbs.AddressingMode, $"BBS{i} should be ZeroPageRelative addressing");
        }
    }

    [Test]
    public void WAI_STP_have_correct_properties()
    {
        var t = OpcodeTables.CreateR65C02STable();
        var wai = t[0xCB];
        Assert.AreEqual(1, wai.Length, "WAI should be 1 byte");
        Assert.AreEqual(3, wai.BaseCycles, "WAI should be 3 cycles");
        Assert.AreEqual(AddressingMode.Implied, wai.AddressingMode, "WAI should be Implied addressing");

        var stp = t[0xDB];
        Assert.AreEqual(1, stp.Length, "STP should be 1 byte");
        Assert.AreEqual(3, stp.BaseCycles, "STP should be 3 cycles");
        Assert.AreEqual(AddressingMode.Implied, stp.AddressingMode, "STP should be Implied addressing");
    }

    [Test]
    public void All_r65c02s_new_opcodes_execute_without_throwing()
    {
        byte[] rmbSmb = { 0x07,0x17,0x27,0x37,0x47,0x57,0x67,0x77, 0x87,0x97,0xA7,0xB7,0xC7,0xD7,0xE7,0xF7 };
        foreach (var op in rmbSmb)
        {
            var mem = new FlatMemory();
            var c = new Cpu6502WdcR65C02S(mem);
            mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
            c.Reset();
            mem.Write(0x8000, op);
            mem.Write(0x0010, 0x00);
            c.PC = 0x8000;
            Assert.DoesNotThrow(() => c.StepInstruction(), $"0x{op:X2} threw");
            Assert.AreEqual(0x8002, c.PC, $"0x{op:X2} should be 2 bytes");
        }

        byte[] bbrBbs = { 0x0F,0x1F,0x2F,0x3F,0x4F,0x5F,0x6F,0x7F, 0x8F,0x9F,0xAF,0xBF,0xCF,0xDF,0xEF,0xFF };
        foreach (var op in bbrBbs)
        {
            var mem = new FlatMemory();
            var c = new Cpu6502WdcR65C02S(mem);
            mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
            c.Reset();
            mem.Write(0x8000, op);
            mem.Write(0x8001, 0x00); // zp operand
            mem.Write(0x8002, 0x00); // branch offset = 0
            mem.Write(0x0000, 0x00);
            c.PC = 0x8000;
            Assert.DoesNotThrow(() => c.StepInstruction(), $"0x{op:X2} threw");
        }
    }

    [Test]
    public void WAI_freezes_until_unmasked_interrupt_arrives()
    {
        var mem = new FlatMemory();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        mem.Write(0xFFFE, 0x00); mem.Write(0xFFFF, 0x90); // IRQ vector -> 0x9000
        c.Reset();
        c.SetFlag(0x04, false); // I flag clear (unmasked)
        mem.Write(0x8000, 0xCB); // WAI
        c.PC = 0x8000;
        c.StepInstruction();
        var pcAfterWai = c.PC;
        c.StepInstruction(); // should still be frozen, no pending IRQ yet
        Assert.AreEqual(pcAfterWai, c.PC, "WAI must freeze with no pending interrupt");
        c.SetIRQ(true);
        c.StepInstruction(); // now should wake and vector to IRQ handler
        Assert.AreEqual(0x9000, c.PC, "WAI must wake and vector on unmasked IRQ");
    }

    [Test]
    public void STP_freezes_permanently_until_reset()
    {
        var mem = new FlatMemory();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        c.Reset();
        mem.Write(0x8000, 0xDB); // STP
        c.PC = 0x8000;
        c.StepInstruction();
        var pcAfterStp = c.PC;
        c.StepInstruction();
        c.StepInstruction();
        Assert.AreEqual(pcAfterStp, c.PC, "STP must freeze the CPU");
        Assert.IsTrue(c.Halted);
        c.Reset();
        Assert.IsFalse(c.Halted, "Reset must clear STP halt");
    }

    [Test]
    public void RMB_clears_specified_bit()
    {
        var mem = new FlatMemory();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        c.Reset();

        // Test RMB0 (0x07) - clear bit 0
        mem.Write(0x0010, 0xFF); // all bits set
        mem.Write(0x8000, 0x07); // RMB0
        mem.Write(0x8001, 0x10); // zp operand (address of target byte)
        c.PC = 0x8000;
        c.StepInstruction();
        Assert.AreEqual(0xFE, mem.Read(0x0010), "RMB0 should clear bit 0");

        // Test RMB7 (0x77) - clear bit 7
        mem.Write(0x0011, 0xFF);
        mem.Write(0x8002, 0x77); // RMB7
        mem.Write(0x8003, 0x11); // zp operand (address of target byte)
        c.PC = 0x8002;
        c.StepInstruction();
        Assert.AreEqual(0x7F, mem.Read(0x0011), "RMB7 should clear bit 7");
    }

    [Test]
    public void SMB_sets_specified_bit()
    {
        var mem = new FlatMemory();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        c.Reset();

        // Test SMB0 (0x87) - set bit 0
        mem.Write(0x0010, 0x00); // all bits clear
        mem.Write(0x8000, 0x87); // SMB0
        mem.Write(0x8001, 0x10); // zp operand (address of target byte)
        c.PC = 0x8000;
        c.StepInstruction();
        Assert.AreEqual(0x01, mem.Read(0x0010), "SMB0 should set bit 0");

        // Test SMB7 (0xF7) - set bit 7
        mem.Write(0x0011, 0x00);
        mem.Write(0x8002, 0xF7); // SMB7
        mem.Write(0x8003, 0x11); // zp operand (address of target byte)
        c.PC = 0x8002;
        c.StepInstruction();
        Assert.AreEqual(0x80, mem.Read(0x0011), "SMB7 should set bit 7");
    }

    [Test]
    public void BBR_branches_when_bit_clear()
    {
        var mem = new FlatMemory();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        c.Reset();

        // BBR0 when bit 0 is clear - should branch
        mem.Write(0x0010, 0xFE); // bit 0 is clear
        mem.Write(0x8000, 0x0F); // BBR0
        mem.Write(0x8001, 0x10); // zp address
        mem.Write(0x8002, 0x10); // branch offset = +16
        c.PC = 0x8000;
        c.StepInstruction();
        Assert.AreEqual(0x8013, c.PC, "BBR0 should branch when bit 0 is clear");
    }

    [Test]
    public void BBS_branches_when_bit_set()
    {
        var mem = new FlatMemory();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        c.Reset();

        // BBS0 when bit 0 is set - should branch
        mem.Write(0x0010, 0x01); // bit 0 is set
        mem.Write(0x8000, 0x8F); // BBS0
        mem.Write(0x8001, 0x10); // zp address
        mem.Write(0x8002, 0x10); // branch offset = +16
        c.PC = 0x8000;
        c.StepInstruction();
        Assert.AreEqual(0x8013, c.PC, "BBS0 should branch when bit 0 is set");
    }

    [Test]
    public void WAI_wakes_and_vectors_on_NMI()
    {
        var mem = new FlatMemory();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        mem.Write(0xFFFA, 0x00); mem.Write(0xFFFB, 0xA0); // NMI vector -> 0xA000
        c.Reset();
        c.SetFlag(0x04, true); // I flag SET - NMI must still wake WAI regardless
        mem.Write(0x8000, 0xCB); // WAI
        c.PC = 0x8000;
        c.StepInstruction();
        var pcAfterWai = c.PC;
        c.StepInstruction();
        Assert.AreEqual(pcAfterWai, c.PC, "WAI must stay frozen with no pending NMI");
        c.SetNMI(true);
        c.SetNMI(false); // NMI latches on the falling edge, not the level
        c.StepInstruction();
        Assert.AreEqual(0xA000, c.PC, "WAI must wake and vector on NMI even with I set");
    }

    [Test]
    public void WAI_wakes_without_vectoring_when_IRQ_masked()
    {
        var mem = new FlatMemory();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        c.Reset();
        c.SetFlag(0x04, true); // I flag SET - masked
        mem.Write(0x8000, 0xCB); // WAI
        mem.Write(0x8001, 0xEA); // NOP, the instruction right after WAI
        c.PC = 0x8000;
        c.StepInstruction(); // executes WAI, freezes
        c.SetIRQ(true);
        c.StepInstruction(); // masked IRQ must wake WAI WITHOUT vectoring
        Assert.AreEqual(0x8002, c.PC, "masked WAI wake must resume at the instruction after WAI, not vector");
    }

    [Test]
    public void RMB_writes_original_value_before_modified_value()
    {
        var mem = new BusTraceMemoryBus();
        var c = new Cpu6502WdcR65C02S(mem);
        mem.Write(0xFFFC, 0x00); mem.Write(0xFFFD, 0x00);
        c.Reset();
        mem.Load(0x0010, 0xFF);
        mem.Load(0x8000, 0x07, 0x10); // RMB0 $10
        c.PC = 0x8000;
        c.StepInstruction();

        var writes = mem.Accesses.Where(a => a.IsWrite && a.Address == 0x0010).ToList();
        Assert.AreEqual(2, writes.Count, "RMB must perform a dummy write then a real write");
        Assert.AreEqual(0xFF, writes[0].Value, "first write must carry the original (unmodified) value");
        Assert.AreEqual(0xFE, writes[1].Value, "second write must carry the modified value");
    }

    [Test]
    public void HasRockwellBitOps_is_true_for_R65C02S_and_false_for_plain_65C02()
    {
        var r65c02s = new Cpu6502WdcR65C02S(new FlatMemory());
        var plain65c02 = new Cpu6502Cmos65C02(new FlatMemory());
        Assert.IsTrue(r65c02s.HasRockwellBitOps);
        Assert.IsFalse(plain65c02.HasRockwellBitOps);
    }
}
