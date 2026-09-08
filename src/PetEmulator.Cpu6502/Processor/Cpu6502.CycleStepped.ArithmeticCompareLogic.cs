namespace Cpu6502;

public partial class Cpu6502
{
    internal bool ExecuteCycleArithmeticCompareLogic(ushort key)
    {
        byte opcode = (byte)(key >> 3);
        byte cycle = (byte)(key & 0x07);

        if (cycle > 0 && IsFunctionalArithmeticCompareLogicOpcode(opcode))
        {
            _sync = cycle >= GetEffectiveInstructionCycles(opcode) - 1;
            return true;
        }

        switch (key)
        {
            case 0x69 << 3 | 0: AdcImm(); break;
            case 0x65 << 3 | 0: AdcZp(); break;
            case 0x75 << 3 | 0: AdcZpX(); break;
            case 0x6D << 3 | 0: AdcAbs(); break;
            case 0x7D << 3 | 0: AdcAbsX(); break;
            case 0x79 << 3 | 0: AdcAbsY(); break;
            case 0x61 << 3 | 0: AdcIndX(); break;
            case 0x71 << 3 | 0: AdcIndY(); break;
            case 0xE9 << 3 | 0: SbcImm(); break;
            case 0xE5 << 3 | 0: SbcZp(); break;
            case 0xF5 << 3 | 0: SbcZpX(); break;
            case 0xED << 3 | 0: SbcAbs(); break;
            case 0xFD << 3 | 0: SbcAbsX(); break;
            case 0xF9 << 3 | 0: SbcAbsY(); break;
            case 0xE1 << 3 | 0: SbcIndX(); break;
            case 0xF1 << 3 | 0: SbcIndY(); break;
            case 0xC9 << 3 | 0: CmpImm(); break;
            case 0xC5 << 3 | 0: CmpZp(); break;
            case 0xD5 << 3 | 0: CmpZpX(); break;
            case 0xCD << 3 | 0: CmpAbs(); break;
            case 0xDD << 3 | 0: CmpAbsX(); break;
            case 0xD9 << 3 | 0: CmpAbsY(); break;
            case 0xC1 << 3 | 0: CmpIndX(); break;
            case 0xD1 << 3 | 0: CmpIndY(); break;
            case 0xE0 << 3 | 0: CpxImm(); break;
            case 0xE4 << 3 | 0: CpxZp(); break;
            case 0xEC << 3 | 0: CpxAbs(); break;
            case 0xC0 << 3 | 0: CpyImm(); break;
            case 0xC4 << 3 | 0: CpyZp(); break;
            case 0xCC << 3 | 0: CpyAbs(); break;
            case 0x29 << 3 | 0: AndImm(); break;
            case 0x25 << 3 | 0: AndZp(); break;
            case 0x35 << 3 | 0: AndZpX(); break;
            case 0x2D << 3 | 0: AndAbs(); break;
            case 0x3D << 3 | 0: AndAbsX(); break;
            case 0x39 << 3 | 0: AndAbsY(); break;
            case 0x21 << 3 | 0: AndIndX(); break;
            case 0x31 << 3 | 0: AndIndY(); break;
            case 0x09 << 3 | 0: OraImm(); break;
            case 0x05 << 3 | 0: OraZp(); break;
            case 0x15 << 3 | 0: OraZpX(); break;
            case 0x0D << 3 | 0: OraAbs(); break;
            case 0x1D << 3 | 0: OraAbsX(); break;
            case 0x19 << 3 | 0: OraAbsY(); break;
            case 0x01 << 3 | 0: OraIndX(); break;
            case 0x11 << 3 | 0: OraIndY(); break;
            case 0x49 << 3 | 0: EorImm(); break;
            case 0x45 << 3 | 0: EorZp(); break;
            case 0x55 << 3 | 0: EorZpX(); break;
            case 0x4D << 3 | 0: EorAbs(); break;
            case 0x5D << 3 | 0: EorAbsX(); break;
            case 0x59 << 3 | 0: EorAbsY(); break;
            case 0x41 << 3 | 0: EorIndX(); break;
            case 0x51 << 3 | 0: EorIndY(); break;
            // ANC
            case 0x0B << 3 | 0: AncImm(); _sync = true; break;
            case 0x2B << 3 | 0: AncImm2(); _sync = true; break;
            // ALR
            case 0x4B << 3 | 0: AlrImm(); _sync = true; break;
            // ARR
            case 0x6B << 3 | 0: ArrImm(); _sync = true; break;
            // SBX
            case 0xCB << 3 | 0: SbxImm(); _sync = true; break;
            default: return false;
        }

        return true;
    }

    private static bool IsFunctionalArithmeticCompareLogicOpcode(byte opcode)
    {
        return opcode is
            0x69 or 0x65 or 0x75 or 0x6D or 0x7D or 0x79 or 0x61 or 0x71 or
            0xE9 or 0xE5 or 0xF5 or 0xED or 0xFD or 0xF9 or 0xE1 or 0xF1 or
            0xC9 or 0xC5 or 0xD5 or 0xCD or 0xDD or 0xD9 or 0xC1 or 0xD1 or
            0xE0 or 0xE4 or 0xEC or
            0xC0 or 0xC4 or 0xCC or
            0x29 or 0x25 or 0x35 or 0x2D or 0x3D or 0x39 or 0x21 or 0x31 or
            0x09 or 0x05 or 0x15 or 0x0D or 0x1D or 0x19 or 0x01 or 0x11 or
            0x49 or 0x45 or 0x55 or 0x4D or 0x5D or 0x59 or 0x41 or 0x51;
    }
}
