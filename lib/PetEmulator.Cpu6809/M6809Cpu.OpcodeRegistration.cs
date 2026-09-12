using PetEmulator.Core;
using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6809;

public partial class M6809Cpu
{
    private void FillOpcodeTable()
    {
        // Keep the historical 2-cycle compatibility behavior for undefined prefixed opcodes.
        for (int i = 0; i < 256; i++)
        {
            RegisterOpcode(new OpcodeKey(0x10, (byte)i), $"OP ${i:X2}", 1, 0, M6800AddressingMode.Unknown.ToString(), UnsupportedOpcode);
            RegisterOpcode(new OpcodeKey(0x11, (byte)i), $"OP ${i:X2}", 1, 0, M6800AddressingMode.Unknown.ToString(), UnsupportedOpcode);
        }

        // Fill Page10 prefix table (0x10 0xNN)
        RegisterOpcode(new OpcodeKey(0x10, 0x20), "OP $20", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbra(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x21), "OP $21", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbrn(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x22), "OP $22", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbhi(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x23), "OP $23", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbls(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x24), "OP $24", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbcc(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x25), "OP $25", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbcs(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x26), "OP $26", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbne(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x27), "OP $27", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbeq(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x28), "OP $28", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbvc(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x29), "OP $29", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbvs(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x2A), "OP $2A", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbpl(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x2B), "OP $2B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbmi(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x2C), "OP $2C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbge(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x2D), "OP $2D", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lblt(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x2E), "OP $2E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbgt(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x2F), "OP $2F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lble(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x83), "OP $83", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpD(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x93), "OP $93", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpD(Ld16Direct()));
        RegisterOpcode(new OpcodeKey(0x10, 0xA3), "OP $A3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpD(Ld16Indexed(out int exA3)) + exA3);
        RegisterOpcode(new OpcodeKey(0x10, 0xB3), "OP $B3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpD(Ld16Extended()));
        RegisterOpcode(new OpcodeKey(0x10, 0x8C), "OP $8C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpY(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x9C), "OP $9C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpY(Ld16Direct()));
        RegisterOpcode(new OpcodeKey(0x10, 0xAC), "OP $AC", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpY(Ld16Indexed(out int exAC)) + exAC);
        RegisterOpcode(new OpcodeKey(0x10, 0xBC), "OP $BC", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpY(Ld16Extended()));
        RegisterOpcode(new OpcodeKey(0x10, 0x8E), "OP $8E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LdyI(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0x9E), "OP $9E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LdyD());
        RegisterOpcode(new OpcodeKey(0x10, 0xAE), "OP $AE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LdyIdx());
        RegisterOpcode(new OpcodeKey(0x10, 0xBE), "OP $BE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LdyExt());
        RegisterOpcode(new OpcodeKey(0x10, 0x9F), "OP $9F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => StyD());
        RegisterOpcode(new OpcodeKey(0x10, 0xAF), "OP $AF", 1, 0, M6800AddressingMode.Unknown.ToString(), () => StyIdx());
        RegisterOpcode(new OpcodeKey(0x10, 0xBF), "OP $BF", 1, 0, M6800AddressingMode.Unknown.ToString(), () => StyExt());
        RegisterOpcode(new OpcodeKey(0x10, 0xCE), "OP $CE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LdsI(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x10, 0xDE), "OP $DE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LdsD());
        RegisterOpcode(new OpcodeKey(0x10, 0xEE), "OP $EE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LdsIdx());
        RegisterOpcode(new OpcodeKey(0x10, 0xFE), "OP $FE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LdsExt());
        RegisterOpcode(new OpcodeKey(0x10, 0xDF), "OP $DF", 1, 0, M6800AddressingMode.Unknown.ToString(), () => StsD());
        RegisterOpcode(new OpcodeKey(0x10, 0xEF), "OP $EF", 1, 0, M6800AddressingMode.Unknown.ToString(), () => StsIdx());
        RegisterOpcode(new OpcodeKey(0x10, 0xFF), "OP $FF", 1, 0, M6800AddressingMode.Unknown.ToString(), () => StsExt());
        RegisterOpcode(new OpcodeKey(0x10, 0x3F), "OP $3F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Swi2());

        // Fill Page11 prefix table (0x11 0xNN)
        RegisterOpcode(new OpcodeKey(0x11, 0x83), "OP $83", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpU(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x11, 0x93), "OP $93", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpU(Ld16Direct()));
        RegisterOpcode(new OpcodeKey(0x11, 0xA3), "OP $A3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpU(Ld16Indexed(out int exA3P11)) + exA3P11);
        RegisterOpcode(new OpcodeKey(0x11, 0xB3), "OP $B3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpU(Ld16Extended()));
        RegisterOpcode(new OpcodeKey(0x11, 0x8C), "OP $8C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpS(Fetch16()));
        RegisterOpcode(new OpcodeKey(0x11, 0x9C), "OP $9C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpS(Ld16Direct()));
        RegisterOpcode(new OpcodeKey(0x11, 0xAC), "OP $AC", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpS(Ld16Indexed(out int exACP11)) + exACP11);
        RegisterOpcode(new OpcodeKey(0x11, 0xBC), "OP $BC", 1, 0, M6800AddressingMode.Unknown.ToString(), () => CmpS(Ld16Extended()));
        RegisterOpcode(new OpcodeKey(0x11, 0x3F), "OP $3F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Swi3());

        RegisterOpcode(OpcodeKey.Base(0x00), "OP $00", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Neg(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x01), "OP $01", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x02), "OP $02", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x03), "OP $03", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Com(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x04), "OP $04", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Lsr(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x05), "OP $05", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x06), "OP $06", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Ror(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x07), "OP $07", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Asr(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x08), "OP $08", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Asl(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x09), "OP $09", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Rol(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x0A), "OP $0A", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Dec(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x0B), "OP $0B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x0C), "OP $0C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Inc(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x0D), "OP $0D", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Tst(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x0E), "OP $0E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Jmp(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x0F), "OP $0F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Clr(FetchDirect()));
        RegisterOpcode(OpcodeKey.Base(0x10), "OP $", 1, 0, M6800AddressingMode.Unknown.ToString(), () => ExecutePrefixedOpcode(0x10));
        RegisterOpcode(OpcodeKey.Base(0x11), "OP $", 1, 0, M6800AddressingMode.Unknown.ToString(), () => ExecutePrefixedOpcode(0x11));
        RegisterOpcode(OpcodeKey.Base(0x12), "OP $12", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x13), "OP $13", 1, 0, M6800AddressingMode.Unknown.ToString(), Sync);
        RegisterOpcode(OpcodeKey.Base(0x14), "OP $14", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x15), "OP $15", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x16), "OP $16", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbra(Fetch16()));
        RegisterOpcode(OpcodeKey.Base(0x17), "OP $17", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Lbsr(Fetch16()));
        RegisterOpcode(OpcodeKey.Base(0x18), "OP $18", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x19), "OP $19", 1, 0, M6800AddressingMode.Unknown.ToString(), Daa);
        RegisterOpcode(OpcodeKey.Base(0x1A), "OP $1A", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Orcc(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x1B), "OP $1B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x1C), "OP $1C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Andcc(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x1D), "OP $1D", 1, 0, M6800AddressingMode.Unknown.ToString(), Sex);
        RegisterOpcode(OpcodeKey.Base(0x1E), "OP $1E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Exg(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x1F), "OP $1F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Tfr(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x20), "OP $20", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bra(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x21), "OP $21", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Brn(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x22), "OP $22", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bhi(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x23), "OP $23", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bls(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x24), "OP $24", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bcc(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x25), "OP $25", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bcs(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x26), "OP $26", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bne(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x27), "OP $27", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Beq(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x28), "OP $28", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bvc(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x29), "OP $29", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bvs(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x2A), "OP $2A", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bpl(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x2B), "OP $2B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bmi(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x2C), "OP $2C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bge(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x2D), "OP $2D", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Blt(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x2E), "OP $2E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bgt(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x2F), "OP $2F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Ble(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x30), "OP $30", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Leax(FetchIndexed(out int ex30)) + ex30);
        RegisterOpcode(OpcodeKey.Base(0x31), "OP $31", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Leay(FetchIndexed(out int ex31)) + ex31);
        RegisterOpcode(OpcodeKey.Base(0x32), "OP $32", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Leas(FetchIndexed(out int ex32)) + ex32);
        RegisterOpcode(OpcodeKey.Base(0x33), "OP $33", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Leau(FetchIndexed(out int ex33)) + ex33);
        RegisterOpcode(OpcodeKey.Base(0x34), "OP $34", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Pshs(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x35), "OP $35", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Puls(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x36), "OP $36", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Pshu(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x37), "OP $37", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Pulu(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x38), "OP $38", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x39), "OP $39", 1, 0, M6800AddressingMode.Unknown.ToString(), base.Rts);
        RegisterOpcode(OpcodeKey.Base(0x3A), "OP $3A", 1, 0, M6800AddressingMode.Unknown.ToString(), Abx);
        RegisterOpcode(OpcodeKey.Base(0x3B), "OP $3B", 1, 0, M6800AddressingMode.Unknown.ToString(), Rti);
        RegisterOpcode(OpcodeKey.Base(0x3C), "OP $3C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Cwai(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x3D), "OP $3D", 1, 0, M6800AddressingMode.Unknown.ToString(), Mul);
        RegisterOpcode(OpcodeKey.Base(0x3E), "OP $3E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x3F), "OP $3F", 1, 0, M6800AddressingMode.Unknown.ToString(), Swi);

        // RMW A (0x40-0x4F)
        RegisterOpcode(OpcodeKey.Base(0x40), "OP $40", 1, 0, M6800AddressingMode.Unknown.ToString(), base.NegA);
        RegisterOpcode(OpcodeKey.Base(0x41), "OP $41", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x42), "OP $42", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x43), "OP $43", 1, 0, M6800AddressingMode.Unknown.ToString(), base.ComA);
        RegisterOpcode(OpcodeKey.Base(0x44), "OP $44", 1, 0, M6800AddressingMode.Unknown.ToString(), base.LsrA);
        RegisterOpcode(OpcodeKey.Base(0x45), "OP $45", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x46), "OP $46", 1, 0, M6800AddressingMode.Unknown.ToString(), base.RorA);
        RegisterOpcode(OpcodeKey.Base(0x47), "OP $47", 1, 0, M6800AddressingMode.Unknown.ToString(), base.AsrA);
        RegisterOpcode(OpcodeKey.Base(0x48), "OP $48", 1, 0, M6800AddressingMode.Unknown.ToString(), base.AslA);
        RegisterOpcode(OpcodeKey.Base(0x49), "OP $49", 1, 0, M6800AddressingMode.Unknown.ToString(), base.RolA);
        RegisterOpcode(OpcodeKey.Base(0x4A), "OP $4A", 1, 0, M6800AddressingMode.Unknown.ToString(), base.DecA);
        RegisterOpcode(OpcodeKey.Base(0x4B), "OP $4B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x4C), "OP $4C", 1, 0, M6800AddressingMode.Unknown.ToString(), base.IncA);
        RegisterOpcode(OpcodeKey.Base(0x4D), "OP $4D", 1, 0, M6800AddressingMode.Unknown.ToString(), base.TstA);
        RegisterOpcode(OpcodeKey.Base(0x4E), "OP $4E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x4F), "OP $4F", 1, 0, M6800AddressingMode.Unknown.ToString(), base.ClrA);

        // RMW B (0x50-0x5F)
        RegisterOpcode(OpcodeKey.Base(0x50), "OP $50", 1, 0, M6800AddressingMode.Unknown.ToString(), base.NegB);
        RegisterOpcode(OpcodeKey.Base(0x51), "OP $51", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x52), "OP $52", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x53), "OP $53", 1, 0, M6800AddressingMode.Unknown.ToString(), base.ComB);
        RegisterOpcode(OpcodeKey.Base(0x54), "OP $54", 1, 0, M6800AddressingMode.Unknown.ToString(), base.LsrB);
        RegisterOpcode(OpcodeKey.Base(0x55), "OP $55", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x56), "OP $56", 1, 0, M6800AddressingMode.Unknown.ToString(), base.RorB);
        RegisterOpcode(OpcodeKey.Base(0x57), "OP $57", 1, 0, M6800AddressingMode.Unknown.ToString(), base.AsrB);
        RegisterOpcode(OpcodeKey.Base(0x58), "OP $58", 1, 0, M6800AddressingMode.Unknown.ToString(), base.AslB);
        RegisterOpcode(OpcodeKey.Base(0x59), "OP $59", 1, 0, M6800AddressingMode.Unknown.ToString(), base.RolB);
        RegisterOpcode(OpcodeKey.Base(0x5A), "OP $5A", 1, 0, M6800AddressingMode.Unknown.ToString(), base.DecB);
        RegisterOpcode(OpcodeKey.Base(0x5B), "OP $5B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x5C), "OP $5C", 1, 0, M6800AddressingMode.Unknown.ToString(), base.IncB);
        RegisterOpcode(OpcodeKey.Base(0x5D), "OP $5D", 1, 0, M6800AddressingMode.Unknown.ToString(), base.TstB);
        RegisterOpcode(OpcodeKey.Base(0x5E), "OP $5E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x5F), "OP $5F", 1, 0, M6800AddressingMode.Unknown.ToString(), base.ClrB);

        // Indexed RMW (0x60-0x6F)
        RegisterOpcode(OpcodeKey.Base(0x60), "OP $60", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Neg(FetchIndexed(out int ex60)) + ex60);
        RegisterOpcode(OpcodeKey.Base(0x61), "OP $61", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x62), "OP $62", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x63), "OP $63", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Com(FetchIndexed(out int ex63)) + ex63);
        RegisterOpcode(OpcodeKey.Base(0x64), "OP $64", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Lsr(FetchIndexed(out int ex64)) + ex64);
        RegisterOpcode(OpcodeKey.Base(0x65), "OP $65", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x66), "OP $66", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Ror(FetchIndexed(out int ex66)) + ex66);
        RegisterOpcode(OpcodeKey.Base(0x67), "OP $67", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Asr(FetchIndexed(out int ex67)) + ex67);
        RegisterOpcode(OpcodeKey.Base(0x68), "OP $68", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Asl(FetchIndexed(out int ex68)) + ex68);
        RegisterOpcode(OpcodeKey.Base(0x69), "OP $69", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Rol(FetchIndexed(out int ex69)) + ex69);
        RegisterOpcode(OpcodeKey.Base(0x6A), "OP $6A", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Dec(FetchIndexed(out int ex6A)) + ex6A);
        RegisterOpcode(OpcodeKey.Base(0x6B), "OP $6B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x6C), "OP $6C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Inc(FetchIndexed(out int ex6C)) + ex6C);
        RegisterOpcode(OpcodeKey.Base(0x6D), "OP $6D", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Tst(FetchIndexed(out int ex6D)) + ex6D);
        RegisterOpcode(OpcodeKey.Base(0x6E), "OP $6E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Jmp(FetchIndexed(out int ex6E)) + ex6E);
        RegisterOpcode(OpcodeKey.Base(0x6F), "OP $6F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Clr(FetchIndexed(out int ex6F)) + ex6F);

        // Extended RMW (0x70-0x7F)
        RegisterOpcode(OpcodeKey.Base(0x70), "OP $70", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Neg(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x71), "OP $71", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x72), "OP $72", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x73), "OP $73", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Com(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x74), "OP $74", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Lsr(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x75), "OP $75", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x76), "OP $76", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Ror(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x77), "OP $77", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Asr(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x78), "OP $78", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Asl(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x79), "OP $79", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Rol(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x7A), "OP $7A", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Dec(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x7B), "OP $7B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x7C), "OP $7C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Inc(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x7D), "OP $7D", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Tst(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x7E), "OP $7E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => Jmp(FetchExtended()));
        RegisterOpcode(OpcodeKey.Base(0x7F), "OP $7F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Clr(FetchExtended()));

        // A-column ALU (0x80-0x8F)
        RegisterOpcode(OpcodeKey.Base(0x80), "OP $80", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SubA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x81), "OP $81", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x82), "OP $82", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SbcA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x83), "OP $83", 1, 0, M6800AddressingMode.Unknown.ToString(), () => SubD(Fetch16()));
        RegisterOpcode(OpcodeKey.Base(0x84), "OP $84", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AndA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x85), "OP $85", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.BitA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x86), "OP $86", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LdaI(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x87), "OP $87", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0x88), "OP $88", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.EorA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x89), "OP $89", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AdcA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x8A), "OP $8A", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.OraA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x8B), "OP $8B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AddA(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0x8C), "OP $8C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpX(Fetch16()));
        RegisterOpcode(OpcodeKey.Base(0x8D), "OP $8D", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.Bsr(FetchSigned()));
        RegisterOpcode(OpcodeKey.Base(0x8E), "OP $8E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LdxI(Fetch16()));
        RegisterOpcode(OpcodeKey.Base(0x8F), "OP $8F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);

        // A-column dir (0x90-0x9F)
        RegisterOpcode(OpcodeKey.Base(0x90), "OP $90", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SubA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x91), "OP $91", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x92), "OP $92", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SbcA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x93), "OP $93", 1, 0, M6800AddressingMode.Unknown.ToString(), () => SubD(Ld16Direct()));
        RegisterOpcode(OpcodeKey.Base(0x94), "OP $94", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AndA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x95), "OP $95", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.BitA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x96), "OP $96", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadA(FetchDirectAddress(), 4));
        RegisterOpcode(OpcodeKey.Base(0x97), "OP $97", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreA(FetchDirectAddress(), 4));
        RegisterOpcode(OpcodeKey.Base(0x98), "OP $98", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.EorA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x99), "OP $99", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AdcA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x9A), "OP $9A", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.OraA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x9B), "OP $9B", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AddA(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0x9C), "OP $9C", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpX(Ld16Direct()));
        RegisterOpcode(OpcodeKey.Base(0x9D), "OP $9D", 1, 0, M6800AddressingMode.Unknown.ToString(), JsrD);
        RegisterOpcode(OpcodeKey.Base(0x9E), "OP $9E", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadX(FetchDirectAddress(), 5));
        RegisterOpcode(OpcodeKey.Base(0x9F), "OP $9F", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreX(FetchDirectAddress(), 5));

        // A-column indexed (0xA0-0xAF)
        RegisterOpcode(OpcodeKey.Base(0xA0), "OP $A0", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SubA(LdIndexed(out int exA0)) + exA0);
        RegisterOpcode(OpcodeKey.Base(0xA1), "OP $A1", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpA(LdIndexed(out int exA1)) + exA1);
        RegisterOpcode(OpcodeKey.Base(0xA2), "OP $A2", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SbcA(LdIndexed(out int exA2)) + exA2);
        RegisterOpcode(OpcodeKey.Base(0xA3), "OP $A3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => SubD(Ld16Indexed(out int exA3)) + exA3);
        RegisterOpcode(OpcodeKey.Base(0xA4), "OP $A4", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AndA(LdIndexed(out int exA4)) + exA4);
        RegisterOpcode(OpcodeKey.Base(0xA5), "OP $A5", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.BitA(LdIndexed(out int exA5)) + exA5);
        RegisterOpcode(OpcodeKey.Base(0xA6), "OP $A6", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadA(FetchIndexed(out int exA6), 4 + exA6));
        RegisterOpcode(OpcodeKey.Base(0xA7), "OP $A7", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreA(FetchIndexed(out int exA7), 4 + exA7));
        RegisterOpcode(OpcodeKey.Base(0xA8), "OP $A8", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.EorA(LdIndexed(out int exA8)) + exA8);
        RegisterOpcode(OpcodeKey.Base(0xA9), "OP $A9", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AdcA(LdIndexed(out int exA9)) + exA9);
        RegisterOpcode(OpcodeKey.Base(0xAA), "OP $AA", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.OraA(LdIndexed(out int exAA)) + exAA);
        RegisterOpcode(OpcodeKey.Base(0xAB), "OP $AB", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AddA(LdIndexed(out int exAB)) + exAB);
        RegisterOpcode(OpcodeKey.Base(0xAC), "OP $AC", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpX(Ld16Indexed(out int exAC)) + exAC);
        RegisterOpcode(OpcodeKey.Base(0xAD), "OP $AD", 1, 0, M6800AddressingMode.Unknown.ToString(), JsrIdx);
        RegisterOpcode(OpcodeKey.Base(0xAE), "OP $AE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadX(FetchIndexed(out int exAE), 5 + exAE));
        RegisterOpcode(OpcodeKey.Base(0xAF), "OP $AF", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreX(FetchIndexed(out int exAF), 5 + exAF));

        // A-column extended (0xB0-0xBF)
        RegisterOpcode(OpcodeKey.Base(0xB0), "OP $B0", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SubA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xB1), "OP $B1", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xB2), "OP $B2", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SbcA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xB3), "OP $B3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => SubD(Ld16Extended()));
        RegisterOpcode(OpcodeKey.Base(0xB4), "OP $B4", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AndA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xB5), "OP $B5", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.BitA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xB6), "OP $B6", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadA(FetchExtended(), 5));
        RegisterOpcode(OpcodeKey.Base(0xB7), "OP $B7", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreA(FetchExtended(), 5));
        RegisterOpcode(OpcodeKey.Base(0xB8), "OP $B8", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.EorA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xB9), "OP $B9", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AdcA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xBA), "OP $BA", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.OraA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xBB), "OP $BB", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AddA(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xBC), "OP $BC", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpX(Ld16Extended()));
        RegisterOpcode(OpcodeKey.Base(0xBD), "OP $BD", 1, 0, M6800AddressingMode.Unknown.ToString(), JsrExt);
        RegisterOpcode(OpcodeKey.Base(0xBE), "OP $BE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadX(FetchExtended(), 6));
        RegisterOpcode(OpcodeKey.Base(0xBF), "OP $BF", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreX(FetchExtended(), 6));

        // B-column ALU (0xC0-0xCF)
        RegisterOpcode(OpcodeKey.Base(0xC0), "OP $C0", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SubB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xC1), "OP $C1", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xC2), "OP $C2", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SbcB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xC3), "OP $C3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => AddD(Fetch16()));
        RegisterOpcode(OpcodeKey.Base(0xC4), "OP $C4", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AndB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xC5), "OP $C5", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.BitB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xC6), "OP $C6", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LdbI(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xC7), "OP $C7", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0xC8), "OP $C8", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.EorB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xC9), "OP $C9", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AdcB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xCA), "OP $CA", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.OraB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xCB), "OP $CB", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AddB(Fetch()));
        RegisterOpcode(OpcodeKey.Base(0xCC), "OP $CC", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LddI(Fetch16()));
        RegisterOpcode(OpcodeKey.Base(0xCD), "OP $CD", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);
        RegisterOpcode(OpcodeKey.Base(0xCE), "OP $CE", 1, 0, M6800AddressingMode.Unknown.ToString(), () => LduI(Fetch16()));
        RegisterOpcode(OpcodeKey.Base(0xCF), "OP $CF", 1, 0, M6800AddressingMode.Unknown.ToString(), () => 2);

        // B-column dir (0xD0-0xDF)
        RegisterOpcode(OpcodeKey.Base(0xD0), "OP $D0", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SubB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xD1), "OP $D1", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xD2), "OP $D2", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SbcB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xD3), "OP $D3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => AddD(Ld16Direct()));
        RegisterOpcode(OpcodeKey.Base(0xD4), "OP $D4", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AndB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xD5), "OP $D5", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.BitB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xD6), "OP $D6", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadB(FetchDirectAddress(), 4));
        RegisterOpcode(OpcodeKey.Base(0xD7), "OP $D7", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreB(FetchDirectAddress(), 4));
        RegisterOpcode(OpcodeKey.Base(0xD8), "OP $D8", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.EorB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xD9), "OP $D9", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AdcB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xDA), "OP $DA", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.OraB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xDB), "OP $DB", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AddB(LdDirect()));
        RegisterOpcode(OpcodeKey.Base(0xDC), "OP $DC", 1, 0, M6800AddressingMode.Unknown.ToString(), LddD);
        RegisterOpcode(OpcodeKey.Base(0xDD), "OP $DD", 1, 0, M6800AddressingMode.Unknown.ToString(), StdD);
        RegisterOpcode(OpcodeKey.Base(0xDE), "OP $DE", 1, 0, M6800AddressingMode.Unknown.ToString(), LduD);
        RegisterOpcode(OpcodeKey.Base(0xDF), "OP $DF", 1, 0, M6800AddressingMode.Unknown.ToString(), StuD);

        // B-column indexed (0xE0-0xEF)
        RegisterOpcode(OpcodeKey.Base(0xE0), "OP $E0", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SubB(LdIndexed(out int exE0)) + exE0);
        RegisterOpcode(OpcodeKey.Base(0xE1), "OP $E1", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpB(LdIndexed(out int exE1)) + exE1);
        RegisterOpcode(OpcodeKey.Base(0xE2), "OP $E2", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SbcB(LdIndexed(out int exE2)) + exE2);
        RegisterOpcode(OpcodeKey.Base(0xE3), "OP $E3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => AddD(Ld16Indexed(out int exE3)) + exE3);
        RegisterOpcode(OpcodeKey.Base(0xE4), "OP $E4", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AndB(LdIndexed(out int exE4)) + exE4);
        RegisterOpcode(OpcodeKey.Base(0xE5), "OP $E5", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.BitB(LdIndexed(out int exE5)) + exE5);
        RegisterOpcode(OpcodeKey.Base(0xE6), "OP $E6", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadB(FetchIndexed(out int exE6), 4 + exE6));
        RegisterOpcode(OpcodeKey.Base(0xE7), "OP $E7", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreB(FetchIndexed(out int exE7), 4 + exE7));
        RegisterOpcode(OpcodeKey.Base(0xE8), "OP $E8", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.EorB(LdIndexed(out int exE8)) + exE8);
        RegisterOpcode(OpcodeKey.Base(0xE9), "OP $E9", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AdcB(LdIndexed(out int exE9)) + exE9);
        RegisterOpcode(OpcodeKey.Base(0xEA), "OP $EA", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.OraB(LdIndexed(out int exEA)) + exEA);
        RegisterOpcode(OpcodeKey.Base(0xEB), "OP $EB", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AddB(LdIndexed(out int exEB)) + exEB);
        RegisterOpcode(OpcodeKey.Base(0xEC), "OP $EC", 1, 0, M6800AddressingMode.Unknown.ToString(), LddIdx);
        RegisterOpcode(OpcodeKey.Base(0xED), "OP $ED", 1, 0, M6800AddressingMode.Unknown.ToString(), StdIdx);
        RegisterOpcode(OpcodeKey.Base(0xEE), "OP $EE", 1, 0, M6800AddressingMode.Unknown.ToString(), LduIdx);
        RegisterOpcode(OpcodeKey.Base(0xEF), "OP $EF", 1, 0, M6800AddressingMode.Unknown.ToString(), StuIdx);

        // B-column extended (0xF0-0xFF)
        RegisterOpcode(OpcodeKey.Base(0xF0), "OP $F0", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SubB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xF1), "OP $F1", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.CmpB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xF2), "OP $F2", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.SbcB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xF3), "OP $F3", 1, 0, M6800AddressingMode.Unknown.ToString(), () => AddD(Ld16Extended()));
        RegisterOpcode(OpcodeKey.Base(0xF4), "OP $F4", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AndB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xF5), "OP $F5", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.BitB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xF6), "OP $F6", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.LoadB(FetchExtended(), 5));
        RegisterOpcode(OpcodeKey.Base(0xF7), "OP $F7", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.StoreB(FetchExtended(), 5));
        RegisterOpcode(OpcodeKey.Base(0xF8), "OP $F8", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.EorB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xF9), "OP $F9", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AdcB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xFA), "OP $FA", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.OraB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xFB), "OP $FB", 1, 0, M6800AddressingMode.Unknown.ToString(), () => base.AddB(LdExtended()));
        RegisterOpcode(OpcodeKey.Base(0xFC), "OP $FC", 1, 0, M6800AddressingMode.Unknown.ToString(), LddExt);
        RegisterOpcode(OpcodeKey.Base(0xFD), "OP $FD", 1, 0, M6800AddressingMode.Unknown.ToString(), StdExt);
        RegisterOpcode(OpcodeKey.Base(0xFE), "OP $FE", 1, 0, M6800AddressingMode.Unknown.ToString(), LduExt);
        RegisterOpcode(OpcodeKey.Base(0xFF), "OP $FF", 1, 0, M6800AddressingMode.Unknown.ToString(), StuExt);
    }

    private int UnsupportedOpcode() => 2;

    private int ExecutePrefixedOpcode(byte page)
    {
        var definition = GetCommonOpcode(new OpcodeKey(page, Fetch()));
        return checked((int)definition.Execute(State, ExecutionContext).Cycles);
    }
}
