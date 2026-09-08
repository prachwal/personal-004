namespace Cpu6502;

public partial class Cpu6502
{
    internal bool ExecuteCycleLoadStoreTransferFlags(byte opcode, byte cycle, ushort key)
    {
        if (cycle > 0 && IsFunctionalLoadStoreTransferFlagOpcode(opcode))
        {
            _sync = cycle >= GetEffectiveInstructionCycles(opcode) - 1;
            return true;
        }

        switch (key)
        {
            case 0xA9 << 3 | 0: LdaImm(); break;
            case 0xA5 << 3 | 0: LdaZp(); break;
            case 0xB5 << 3 | 0: LdaZpX(); break;
            case 0xAD << 3 | 0: LdaAbs(); break;
            case 0xBD << 3 | 0: LdaAbsX(); break;
            case 0xB9 << 3 | 0: LdaAbsY(); break;
            case 0xA1 << 3 | 0: LdaIndX(); break;
            case 0xB1 << 3 | 0: LdaIndY(); break;
            case 0x85 << 3 | 0: StaZp(); break;
            case 0x95 << 3 | 0: StaZpX(); break;
            case 0x8D << 3 | 0: StaAbs(); break;
            case 0x9D << 3 | 0: StaAbsX(); break;
            case 0x99 << 3 | 0: StaAbsY(); break;
            case 0x81 << 3 | 0: StaIndX(); break;
            case 0x91 << 3 | 0: StaIndY(); break;
            case 0xA2 << 3 | 0: LdxImm(); break;
            case 0xA6 << 3 | 0: LdxZp(); break;
            case 0xB6 << 3 | 0: LdxZpY(); break;
            case 0xAE << 3 | 0: LdxAbs(); break;
            case 0xBE << 3 | 0: LdxAbsY(); break;
            case 0x86 << 3 | 0: StxZp(); break;
            case 0x96 << 3 | 0: StxZpY(); break;
            case 0x8E << 3 | 0: StxAbs(); break;
            case 0xA0 << 3 | 0: LdyImm(); break;
            case 0xA4 << 3 | 0: LdyZp(); break;
            case 0xB4 << 3 | 0: LdyZpX(); break;
            case 0xAC << 3 | 0: LdyAbs(); break;
            case 0xBC << 3 | 0: LdyAbsX(); break;
            case 0x84 << 3 | 0: StyZp(); break;
            case 0x94 << 3 | 0: StyZpX(); break;
            case 0x8C << 3 | 0: StyAbs(); break;
            case 0xEA << 3 | 0: Nop(); break;
            case 0xAA << 3 | 0: Tax(); break;
            case 0xA8 << 3 | 0: Tay(); break;
            case 0xBA << 3 | 0: Tsx(); break;
            case 0x8A << 3 | 0: Txa(); break;
            case 0x9A << 3 | 0: Txs(); break;
            case 0x98 << 3 | 0: Tya(); break;
            case 0x18 << 3 | 0: Clc(); break;
            case 0x38 << 3 | 0: Sec(); break;
            case 0xD8 << 3 | 0: Cld(); break;
            case 0xF8 << 3 | 0: Sed(); break;
            case 0x58 << 3 | 0: Cli(); break;
            case 0x78 << 3 | 0: Sei(); break;
            case 0xB8 << 3 | 0: Clv(); break;

            // LAS - Load A, X, SP from memory AND SP
            case 0xBB << 3 | 0: LasAbsY_Cycle0(); break;
            case 0xBB << 3 | 1: LasAbsY_Cycle1(); break;
            case 0xBB << 3 | 2: LasAbsY_Cycle2(); break;
            case 0xBB << 3 | 3: LasAbsY_Cycle3(); break;

            // LAX - LDA + LDX (Illegal Opcode)
            case 0xA7 << 3 | 0: LaxZp_Cycle0(); break;
            case 0xB7 << 3 | 0: LaxZpY_Cycle0(); break;
            case 0xAF << 3 | 0: LaxAbs_Cycle0(); break;
            case 0xBF << 3 | 0: LaxAbsY_Cycle0(); break;
            case 0xBF << 3 | 1: LaxAbsY_Cycle1(); break;
            case 0xA3 << 3 | 0: LaxIndX_Cycle0(); break;
            case 0xB3 << 3 | 0: LaxIndY_Cycle0(); break;
            case 0xB3 << 3 | 1: LaxIndY_Cycle1(); break;

            // SAX - Store A & X (Illegal Opcode)
            case 0x87 << 3 | 0: SaxZp_Cycle0(); break;
            case 0x97 << 3 | 0: SaxZpY_Cycle0(); break;
            case 0x8F << 3 | 0: SaxAbs_Cycle0(); break;
            case 0x83 << 3 | 0: SaxIndX_Cycle0(); break;
            case 0x83 << 3 | 1: break;
            case 0x83 << 3 | 2: break;
            case 0x83 << 3 | 3: break;
            case 0x83 << 3 | 4: break;
            case 0x83 << 3 | 5: _sync = true; break;

            default: return false;
        }

        return true;
    }

    private static bool IsFunctionalLoadStoreTransferFlagOpcode(byte opcode)
    {
        return opcode is
            0xA9 or 0xA5 or 0xB5 or 0xAD or 0xBD or 0xB9 or 0xA1 or 0xB1 or
            0x85 or 0x95 or 0x8D or 0x9D or 0x99 or 0x81 or 0x91 or
            0xA2 or 0xA6 or 0xB6 or 0xAE or 0xBE or
            0x86 or 0x96 or 0x8E or
            0xA0 or 0xA4 or 0xB4 or 0xAC or 0xBC or
            0x84 or 0x94 or 0x8C or
            0xEA or
            0xAA or 0xA8 or 0xBA or 0x8A or 0x9A or 0x98 or
            0x18 or 0x38 or 0xD8 or 0xF8 or 0x58 or 0x78 or 0xB8;
    }
}
