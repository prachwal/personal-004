using PetEmulator.Core;
using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6809;

public partial class M6809Cpu
{
private void FillOpcodeTable()
    {
        OpcodeTable = new OpcodeRegistrationTable(this, 0x00);
        Page10OpcodeTable = new OpcodeRegistrationTable(this, 0x10);
        Page11OpcodeTable = new OpcodeRegistrationTable(this, 0x11);

        // Keep the historical 2-cycle compatibility behavior, but expose it as unsupported metadata.
        for (int i = 0; i < 256; i++)
        {
            Page10OpcodeTable[(byte)i] = UnsupportedOpcode;
            Page11OpcodeTable[(byte)i] = UnsupportedOpcode;
        }

        // Fill Page10 prefix table (0x10 0xNN)
        Page10OpcodeTable[0x20] = () => Lbra(Fetch16());
        Page10OpcodeTable[0x21] = () => Lbrn(Fetch16());
        Page10OpcodeTable[0x22] = () => Lbhi(Fetch16());
        Page10OpcodeTable[0x23] = () => Lbls(Fetch16());
        Page10OpcodeTable[0x24] = () => Lbcc(Fetch16());
        Page10OpcodeTable[0x25] = () => Lbcs(Fetch16());
        Page10OpcodeTable[0x26] = () => Lbne(Fetch16());
        Page10OpcodeTable[0x27] = () => Lbeq(Fetch16());
        Page10OpcodeTable[0x28] = () => Lbvc(Fetch16());
        Page10OpcodeTable[0x29] = () => Lbvs(Fetch16());
        Page10OpcodeTable[0x2A] = () => Lbpl(Fetch16());
        Page10OpcodeTable[0x2B] = () => Lbmi(Fetch16());
        Page10OpcodeTable[0x2C] = () => Lbge(Fetch16());
        Page10OpcodeTable[0x2D] = () => Lblt(Fetch16());
        Page10OpcodeTable[0x2E] = () => Lbgt(Fetch16());
        Page10OpcodeTable[0x2F] = () => Lble(Fetch16());
        Page10OpcodeTable[0x83] = () => CmpD(Fetch16());
        Page10OpcodeTable[0x93] = () => CmpD(Ld16Direct());
        Page10OpcodeTable[0xA3] = () => CmpD(Ld16Indexed(out int exA3)) + exA3;
        Page10OpcodeTable[0xB3] = () => CmpD(Ld16Extended());
        Page10OpcodeTable[0x8C] = () => CmpY(Fetch16());
        Page10OpcodeTable[0x9C] = () => CmpY(Ld16Direct());
        Page10OpcodeTable[0xAC] = () => CmpY(Ld16Indexed(out int exAC)) + exAC;
        Page10OpcodeTable[0xBC] = () => CmpY(Ld16Extended());
        Page10OpcodeTable[0x8E] = () => LdyI(Fetch16());
        Page10OpcodeTable[0x9E] = () => LdyD();
        Page10OpcodeTable[0xAE] = () => LdyIdx();
        Page10OpcodeTable[0xBE] = () => LdyExt();
        Page10OpcodeTable[0x9F] = () => StyD();
        Page10OpcodeTable[0xAF] = () => StyIdx();
        Page10OpcodeTable[0xBF] = () => StyExt();
        Page10OpcodeTable[0xCE] = () => LdsI(Fetch16());
        Page10OpcodeTable[0xDE] = () => LdsD();
        Page10OpcodeTable[0xEE] = () => LdsIdx();
        Page10OpcodeTable[0xFE] = () => LdsExt();
        Page10OpcodeTable[0xDF] = () => StsD();
        Page10OpcodeTable[0xEF] = () => StsIdx();
        Page10OpcodeTable[0xFF] = () => StsExt();
        Page10OpcodeTable[0x3F] = () => Swi2();

        // Fill Page11 prefix table (0x11 0xNN)
        Page11OpcodeTable[0x83] = () => CmpU(Fetch16());
        Page11OpcodeTable[0x93] = () => CmpU(Ld16Direct());
        Page11OpcodeTable[0xA3] = () => CmpU(Ld16Indexed(out int exA3P11)) + exA3P11;
        Page11OpcodeTable[0xB3] = () => CmpU(Ld16Extended());
        Page11OpcodeTable[0x8C] = () => CmpS(Fetch16());
        Page11OpcodeTable[0x9C] = () => CmpS(Ld16Direct());
        Page11OpcodeTable[0xAC] = () => CmpS(Ld16Indexed(out int exACP11)) + exACP11;
        Page11OpcodeTable[0xBC] = () => CmpS(Ld16Extended());
        Page11OpcodeTable[0x3F] = () => Swi3();

        OpcodeTable[0x00] = () => base.Neg(FetchDirect());
        OpcodeTable[0x01] = () => 2;
        OpcodeTable[0x02] = () => 2;
        OpcodeTable[0x03] = () => base.Com(FetchDirect());
        OpcodeTable[0x04] = () => base.Lsr(FetchDirect());
        OpcodeTable[0x05] = () => 2;
        OpcodeTable[0x06] = () => base.Ror(FetchDirect());
        OpcodeTable[0x07] = () => base.Asr(FetchDirect());
        OpcodeTable[0x08] = () => base.Asl(FetchDirect());
        OpcodeTable[0x09] = () => base.Rol(FetchDirect());
        OpcodeTable[0x0A] = () => base.Dec(FetchDirect());
        OpcodeTable[0x0B] = () => 2;
        OpcodeTable[0x0C] = () => base.Inc(FetchDirect());
        OpcodeTable[0x0D] = () => base.Tst(FetchDirect());
        OpcodeTable[0x0E] = () => Jmp(FetchExtended());
        OpcodeTable[0x0F] = () => base.Clr(FetchDirect());
        OpcodeTable[0x10] = () => Page10OpcodeTable[Fetch()]();
        OpcodeTable[0x11] = () => Page11OpcodeTable[Fetch()]();
        OpcodeTable[0x12] = () => 2;
        OpcodeTable[0x13] = Sync;
        OpcodeTable[0x14] = () => 2;
        OpcodeTable[0x15] = () => 2;
        OpcodeTable[0x16] = () => Lbra(Fetch16());
        OpcodeTable[0x17] = () => Lbsr(Fetch16());
        OpcodeTable[0x18] = () => 2;
        OpcodeTable[0x19] = Daa;
        OpcodeTable[0x1A] = () => Orcc(Fetch());
        OpcodeTable[0x1B] = () => 2;
        OpcodeTable[0x1C] = () => Andcc(Fetch());
        OpcodeTable[0x1D] = Sex;
        OpcodeTable[0x1E] = () => Exg(Fetch());
        OpcodeTable[0x1F] = () => Tfr(Fetch());
        OpcodeTable[0x20] = () => base.Bra(FetchSigned());
        OpcodeTable[0x21] = () => base.Brn(FetchSigned());
        OpcodeTable[0x22] = () => base.Bhi(FetchSigned());
        OpcodeTable[0x23] = () => base.Bls(FetchSigned());
        OpcodeTable[0x24] = () => base.Bcc(FetchSigned());
        OpcodeTable[0x25] = () => base.Bcs(FetchSigned());
        OpcodeTable[0x26] = () => base.Bne(FetchSigned());
        OpcodeTable[0x27] = () => base.Beq(FetchSigned());
        OpcodeTable[0x28] = () => base.Bvc(FetchSigned());
        OpcodeTable[0x29] = () => base.Bvs(FetchSigned());
        OpcodeTable[0x2A] = () => base.Bpl(FetchSigned());
        OpcodeTable[0x2B] = () => base.Bmi(FetchSigned());
        OpcodeTable[0x2C] = () => base.Bge(FetchSigned());
        OpcodeTable[0x2D] = () => base.Blt(FetchSigned());
        OpcodeTable[0x2E] = () => base.Bgt(FetchSigned());
        OpcodeTable[0x2F] = () => base.Ble(FetchSigned());
        OpcodeTable[0x30] = () => Leax(FetchIndexed(out int ex30)) + ex30;
        OpcodeTable[0x31] = () => Leay(FetchIndexed(out int ex31)) + ex31;
        OpcodeTable[0x32] = () => Leas(FetchIndexed(out int ex32)) + ex32;
        OpcodeTable[0x33] = () => Leau(FetchIndexed(out int ex33)) + ex33;
        OpcodeTable[0x34] = () => Pshs(Fetch());
        OpcodeTable[0x35] = () => Puls(Fetch());
        OpcodeTable[0x36] = () => Pshu(Fetch());
        OpcodeTable[0x37] = () => Pulu(Fetch());
        OpcodeTable[0x38] = () => 2;
        OpcodeTable[0x39] = base.Rts;
        OpcodeTable[0x3A] = Abx;
        OpcodeTable[0x3B] = Rti;
        OpcodeTable[0x3C] = () => Cwai(Fetch());
        OpcodeTable[0x3D] = Mul;
        OpcodeTable[0x3E] = () => 2;
        OpcodeTable[0x3F] = Swi;

        // RMW A (0x40-0x4F)
        OpcodeTable[0x40] = base.NegA; OpcodeTable[0x41] = () => 2; OpcodeTable[0x42] = () => 2; OpcodeTable[0x43] = base.ComA;
        OpcodeTable[0x44] = base.LsrA; OpcodeTable[0x45] = () => 2; OpcodeTable[0x46] = base.RorA; OpcodeTable[0x47] = base.AsrA;
        OpcodeTable[0x48] = base.AslA; OpcodeTable[0x49] = base.RolA; OpcodeTable[0x4A] = base.DecA; OpcodeTable[0x4B] = () => 2;
        OpcodeTable[0x4C] = base.IncA; OpcodeTable[0x4D] = base.TstA; OpcodeTable[0x4E] = () => 2; OpcodeTable[0x4F] = base.ClrA;

        // RMW B (0x50-0x5F)
        OpcodeTable[0x50] = base.NegB; OpcodeTable[0x51] = () => 2; OpcodeTable[0x52] = () => 2; OpcodeTable[0x53] = base.ComB;
        OpcodeTable[0x54] = base.LsrB; OpcodeTable[0x55] = () => 2; OpcodeTable[0x56] = base.RorB; OpcodeTable[0x57] = base.AsrB;
        OpcodeTable[0x58] = base.AslB; OpcodeTable[0x59] = base.RolB; OpcodeTable[0x5A] = base.DecB; OpcodeTable[0x5B] = () => 2;
        OpcodeTable[0x5C] = base.IncB; OpcodeTable[0x5D] = base.TstB; OpcodeTable[0x5E] = () => 2; OpcodeTable[0x5F] = base.ClrB;

        // Indexed RMW (0x60-0x6F)
        OpcodeTable[0x60] = () => base.Neg(FetchIndexed(out int ex60)) + ex60;
        OpcodeTable[0x61] = () => 2; OpcodeTable[0x62] = () => 2;
        OpcodeTable[0x63] = () => base.Com(FetchIndexed(out int ex63)) + ex63;
        OpcodeTable[0x64] = () => base.Lsr(FetchIndexed(out int ex64)) + ex64;
        OpcodeTable[0x65] = () => 2;
        OpcodeTable[0x66] = () => base.Ror(FetchIndexed(out int ex66)) + ex66;
        OpcodeTable[0x67] = () => base.Asr(FetchIndexed(out int ex67)) + ex67;
        OpcodeTable[0x68] = () => base.Asl(FetchIndexed(out int ex68)) + ex68;
        OpcodeTable[0x69] = () => base.Rol(FetchIndexed(out int ex69)) + ex69;
        OpcodeTable[0x6A] = () => base.Dec(FetchIndexed(out int ex6A)) + ex6A;
        OpcodeTable[0x6B] = () => 2;
        OpcodeTable[0x6C] = () => base.Inc(FetchIndexed(out int ex6C)) + ex6C;
        OpcodeTable[0x6D] = () => base.Tst(FetchIndexed(out int ex6D)) + ex6D;
        OpcodeTable[0x6E] = () => Jmp(FetchIndexed(out int ex6E)) + ex6E;
        OpcodeTable[0x6F] = () => base.Clr(FetchIndexed(out int ex6F)) + ex6F;

        // Extended RMW (0x70-0x7F)
        OpcodeTable[0x70] = () => base.Neg(FetchExtended()); OpcodeTable[0x71] = () => 2; OpcodeTable[0x72] = () => 2;
        OpcodeTable[0x73] = () => base.Com(FetchExtended()); OpcodeTable[0x74] = () => base.Lsr(FetchExtended());
        OpcodeTable[0x75] = () => 2; OpcodeTable[0x76] = () => base.Ror(FetchExtended());
        OpcodeTable[0x77] = () => base.Asr(FetchExtended()); OpcodeTable[0x78] = () => base.Asl(FetchExtended());
        OpcodeTable[0x79] = () => base.Rol(FetchExtended()); OpcodeTable[0x7A] = () => base.Dec(FetchExtended());
        OpcodeTable[0x7B] = () => 2; OpcodeTable[0x7C] = () => base.Inc(FetchExtended());
        OpcodeTable[0x7D] = () => base.Tst(FetchExtended()); OpcodeTable[0x7E] = () => Jmp(FetchExtended());
        OpcodeTable[0x7F] = () => base.Clr(FetchExtended());

        // A-column ALU (0x80-0x8F)
        OpcodeTable[0x80] = () => base.SubA(Fetch()); OpcodeTable[0x81] = () => base.CmpA(Fetch());
        OpcodeTable[0x82] = () => base.SbcA(Fetch()); OpcodeTable[0x83] = () => SubD(Fetch16());
        OpcodeTable[0x84] = () => base.AndA(Fetch()); OpcodeTable[0x85] = () => base.BitA(Fetch());
        OpcodeTable[0x86] = () => base.LdaI(Fetch()); OpcodeTable[0x87] = () => 2;
        OpcodeTable[0x88] = () => base.EorA(Fetch()); OpcodeTable[0x89] = () => base.AdcA(Fetch());
        OpcodeTable[0x8A] = () => base.OraA(Fetch()); OpcodeTable[0x8B] = () => base.AddA(Fetch());
        OpcodeTable[0x8C] = () => base.CmpX(Fetch16()); OpcodeTable[0x8D] = () => base.Bsr(FetchSigned());
        OpcodeTable[0x8E] = () => base.LdxI(Fetch16()); OpcodeTable[0x8F] = () => 2;

        // A-column dir (0x90-0x9F)
        OpcodeTable[0x90] = () => base.SubA(LdDirect()); OpcodeTable[0x91] = () => base.CmpA(LdDirect());
        OpcodeTable[0x92] = () => base.SbcA(LdDirect()); OpcodeTable[0x93] = () => SubD(Ld16Direct());
        OpcodeTable[0x94] = () => base.AndA(LdDirect()); OpcodeTable[0x95] = () => base.BitA(LdDirect());
        OpcodeTable[0x96] = () => base.LoadA(FetchDirectAddress(), 4); OpcodeTable[0x97] = () => base.StoreA(FetchDirectAddress(), 4);
        OpcodeTable[0x98] = () => base.EorA(LdDirect()); OpcodeTable[0x99] = () => base.AdcA(LdDirect());
        OpcodeTable[0x9A] = () => base.OraA(LdDirect()); OpcodeTable[0x9B] = () => base.AddA(LdDirect());
        OpcodeTable[0x9C] = () => base.CmpX(Ld16Direct()); OpcodeTable[0x9D] = JsrD;
        OpcodeTable[0x9E] = () => base.LoadX(FetchDirectAddress(), 5); OpcodeTable[0x9F] = () => base.StoreX(FetchDirectAddress(), 5);

        // A-column indexed (0xA0-0xAF)
        OpcodeTable[0xA0] = () => base.SubA(LdIndexed(out int exA0)) + exA0;
        OpcodeTable[0xA1] = () => base.CmpA(LdIndexed(out int exA1)) + exA1;
        OpcodeTable[0xA2] = () => base.SbcA(LdIndexed(out int exA2)) + exA2;
        OpcodeTable[0xA3] = () => SubD(Ld16Indexed(out int exA3)) + exA3;
        OpcodeTable[0xA4] = () => base.AndA(LdIndexed(out int exA4)) + exA4;
        OpcodeTable[0xA5] = () => base.BitA(LdIndexed(out int exA5)) + exA5;
        OpcodeTable[0xA6] = () => base.LoadA(FetchIndexed(out int exA6), 4 + exA6);
        OpcodeTable[0xA7] = () => base.StoreA(FetchIndexed(out int exA7), 4 + exA7);
        OpcodeTable[0xA8] = () => base.EorA(LdIndexed(out int exA8)) + exA8;
        OpcodeTable[0xA9] = () => base.AdcA(LdIndexed(out int exA9)) + exA9;
        OpcodeTable[0xAA] = () => base.OraA(LdIndexed(out int exAA)) + exAA;
        OpcodeTable[0xAB] = () => base.AddA(LdIndexed(out int exAB)) + exAB;
        OpcodeTable[0xAC] = () => base.CmpX(Ld16Indexed(out int exAC)) + exAC;
        OpcodeTable[0xAD] = JsrIdx;
        OpcodeTable[0xAE] = () => base.LoadX(FetchIndexed(out int exAE), 5 + exAE);
        OpcodeTable[0xAF] = () => base.StoreX(FetchIndexed(out int exAF), 5 + exAF);

        // A-column extended (0xB0-0xBF)
        OpcodeTable[0xB0] = () => base.SubA(LdExtended()); OpcodeTable[0xB1] = () => base.CmpA(LdExtended());
        OpcodeTable[0xB2] = () => base.SbcA(LdExtended()); OpcodeTable[0xB3] = () => SubD(Ld16Extended());
        OpcodeTable[0xB4] = () => base.AndA(LdExtended()); OpcodeTable[0xB5] = () => base.BitA(LdExtended());
        OpcodeTable[0xB6] = () => base.LoadA(FetchExtended(), 5); OpcodeTable[0xB7] = () => base.StoreA(FetchExtended(), 5);
        OpcodeTable[0xB8] = () => base.EorA(LdExtended()); OpcodeTable[0xB9] = () => base.AdcA(LdExtended());
        OpcodeTable[0xBA] = () => base.OraA(LdExtended()); OpcodeTable[0xBB] = () => base.AddA(LdExtended());
        OpcodeTable[0xBC] = () => base.CmpX(Ld16Extended()); OpcodeTable[0xBD] = JsrExt;
        OpcodeTable[0xBE] = () => base.LoadX(FetchExtended(), 6); OpcodeTable[0xBF] = () => base.StoreX(FetchExtended(), 6);

        // B-column ALU (0xC0-0xCF)
        OpcodeTable[0xC0] = () => base.SubB(Fetch()); OpcodeTable[0xC1] = () => base.CmpB(Fetch());
        OpcodeTable[0xC2] = () => base.SbcB(Fetch()); OpcodeTable[0xC3] = () => AddD(Fetch16());
        OpcodeTable[0xC4] = () => base.AndB(Fetch()); OpcodeTable[0xC5] = () => base.BitB(Fetch());
        OpcodeTable[0xC6] = () => base.LdbI(Fetch()); OpcodeTable[0xC7] = () => 2;
        OpcodeTable[0xC8] = () => base.EorB(Fetch()); OpcodeTable[0xC9] = () => base.AdcB(Fetch());
        OpcodeTable[0xCA] = () => base.OraB(Fetch()); OpcodeTable[0xCB] = () => base.AddB(Fetch());
        OpcodeTable[0xCC] = () => LddI(Fetch16()); OpcodeTable[0xCD] = () => 2;
        OpcodeTable[0xCE] = () => LduI(Fetch16()); OpcodeTable[0xCF] = () => 2;

        // B-column dir (0xD0-0xDF)
        OpcodeTable[0xD0] = () => base.SubB(LdDirect()); OpcodeTable[0xD1] = () => base.CmpB(LdDirect());
        OpcodeTable[0xD2] = () => base.SbcB(LdDirect()); OpcodeTable[0xD3] = () => AddD(Ld16Direct());
        OpcodeTable[0xD4] = () => base.AndB(LdDirect()); OpcodeTable[0xD5] = () => base.BitB(LdDirect());
        OpcodeTable[0xD6] = () => base.LoadB(FetchDirectAddress(), 4); OpcodeTable[0xD7] = () => base.StoreB(FetchDirectAddress(), 4);
        OpcodeTable[0xD8] = () => base.EorB(LdDirect()); OpcodeTable[0xD9] = () => base.AdcB(LdDirect());
        OpcodeTable[0xDA] = () => base.OraB(LdDirect()); OpcodeTable[0xDB] = () => base.AddB(LdDirect());
        OpcodeTable[0xDC] = LddD; OpcodeTable[0xDD] = StdD;
        OpcodeTable[0xDE] = LduD; OpcodeTable[0xDF] = StuD;

        // B-column indexed (0xE0-0xEF)
        OpcodeTable[0xE0] = () => base.SubB(LdIndexed(out int exE0)) + exE0;
        OpcodeTable[0xE1] = () => base.CmpB(LdIndexed(out int exE1)) + exE1;
        OpcodeTable[0xE2] = () => base.SbcB(LdIndexed(out int exE2)) + exE2;
        OpcodeTable[0xE3] = () => AddD(Ld16Indexed(out int exE3)) + exE3;
        OpcodeTable[0xE4] = () => base.AndB(LdIndexed(out int exE4)) + exE4;
        OpcodeTable[0xE5] = () => base.BitB(LdIndexed(out int exE5)) + exE5;
        OpcodeTable[0xE6] = () => base.LoadB(FetchIndexed(out int exE6), 4 + exE6);
        OpcodeTable[0xE7] = () => base.StoreB(FetchIndexed(out int exE7), 4 + exE7);
        OpcodeTable[0xE8] = () => base.EorB(LdIndexed(out int exE8)) + exE8;
        OpcodeTable[0xE9] = () => base.AdcB(LdIndexed(out int exE9)) + exE9;
        OpcodeTable[0xEA] = () => base.OraB(LdIndexed(out int exEA)) + exEA;
        OpcodeTable[0xEB] = () => base.AddB(LdIndexed(out int exEB)) + exEB;
        OpcodeTable[0xEC] = LddIdx;
        OpcodeTable[0xED] = StdIdx;
        OpcodeTable[0xEE] = LduIdx;
        OpcodeTable[0xEF] = StuIdx;

        // B-column extended (0xF0-0xFF)
        OpcodeTable[0xF0] = () => base.SubB(LdExtended()); OpcodeTable[0xF1] = () => base.CmpB(LdExtended());
        OpcodeTable[0xF2] = () => base.SbcB(LdExtended()); OpcodeTable[0xF3] = () => AddD(Ld16Extended());
        OpcodeTable[0xF4] = () => base.AndB(LdExtended()); OpcodeTable[0xF5] = () => base.BitB(LdExtended());
        OpcodeTable[0xF6] = () => base.LoadB(FetchExtended(), 5); OpcodeTable[0xF7] = () => base.StoreB(FetchExtended(), 5);
        OpcodeTable[0xF8] = () => base.EorB(LdExtended()); OpcodeTable[0xF9] = () => base.AdcB(LdExtended());
        OpcodeTable[0xFA] = () => base.OraB(LdExtended()); OpcodeTable[0xFB] = () => base.AddB(LdExtended());
        OpcodeTable[0xFC] = LddExt; OpcodeTable[0xFD] = StdExt;
        OpcodeTable[0xFE] = LduExt; OpcodeTable[0xFF] = StuExt;
    }

    private int UnsupportedOpcode() => 2;

    protected sealed class OpcodeRegistrationTable(M6809Cpu cpu, byte page)
    {
        private readonly Func<int>?[] handlers = new Func<int>?[256];

        public Func<int> this[byte opcode]
        {
            get => handlers[opcode] ?? throw new InvalidOperationException($"Opcode 0x{opcode:X2} is not registered.");
            set
            {
                handlers[opcode] = value;
                cpu.RegisterOpcode(
                    new OpcodeKey(page, opcode),
                    $"OP ${opcode:X2}",
                    1,
                    0,
                    M6800AddressingMode.Unknown.ToString(),
                    value);
            }
        }
    }
}
