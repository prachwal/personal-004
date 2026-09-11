namespace PetEmulator.Cpu6502;

public partial class Cpu6502
{
    internal bool ExecuteCycleControlFlow(ushort key)
    {
        switch (key)
        {
            // BRK (7 cykli) - zwraca true tylko w ostatnim cyklu
            case 0x00 << 3 | 0: Brk_Cycle0(); return true;
            case 0x00 << 3 | 1: Brk_Cycle1(); return true;
            case 0x00 << 3 | 2: Brk_Cycle2(); return true;
            case 0x00 << 3 | 3: Brk_Cycle3(); return true;
            case 0x00 << 3 | 4: Brk_Cycle4(); return true;
            case 0x00 << 3 | 5: Brk_Cycle5(); return true;
            case 0x00 << 3 | 6: Brk_Cycle6(); return true;

            // RTI (6 cykli)
            case 0x40 << 3 | 0: Rti_Cycle0(); return true;
            case 0x40 << 3 | 1: Rti_Cycle1(); return true;
            case 0x40 << 3 | 2: Rti_Cycle2(); return true;
            case 0x40 << 3 | 3: Rti_Cycle3(); return true;
            case 0x40 << 3 | 4: Rti_Cycle4(); return true;
            case 0x40 << 3 | 5: Rti_Cycle5(); return true;

            // JMP Absolute (3 cykle)
            case 0x4C << 3 | 0: JmpAbs_Cycle0(); return true;
            case 0x4C << 3 | 1: JmpAbs_Cycle1(); return true;
            case 0x4C << 3 | 2: JmpAbs_Cycle2(); return true;

            // JMP Indirect (5 cykli)
            case 0x6C << 3 | 0: JmpInd_Cycle0(); return true;
            case 0x6C << 3 | 1: JmpInd_Cycle1(); return true;
            case 0x6C << 3 | 2: JmpInd_Cycle2(); return true;
            case 0x6C << 3 | 3: JmpInd_Cycle3(); return true;
            case 0x6C << 3 | 4: JmpInd_Cycle4(); return true;

            // JSR Absolute (6 cykli)
            case 0x20 << 3 | 0: JsrAbs_Cycle0(); return true;
            case 0x20 << 3 | 1: JsrAbs_Cycle1(); return true;
            case 0x20 << 3 | 2: JsrAbs_Cycle2(); return true;
            case 0x20 << 3 | 3: JsrAbs_Cycle3(); return true;
            case 0x20 << 3 | 4: JsrAbs_Cycle4(); return true;
            case 0x20 << 3 | 5: JsrAbs_Cycle5(); return true;

            // RTS (6 cykli)
            case 0x60 << 3 | 0: Rts_Cycle0(); return true;
            case 0x60 << 3 | 1: Rts_Cycle1(); return true;
            case 0x60 << 3 | 2: Rts_Cycle2(); return true;
            case 0x60 << 3 | 3: Rts_Cycle3(); return true;
            case 0x60 << 3 | 4: Rts_Cycle4(); return true;
            case 0x60 << 3 | 5: Rts_Cycle5(); return true;

            // BIT Zero Page (3 cykle)
            case 0x24 << 3 | 0: BitZp_Cycle0(); return true;
            case 0x24 << 3 | 1: BitZp_Cycle1(); return true;
            case 0x24 << 3 | 2: BitZp_Cycle2(); return true;

            // BIT Absolute (4 cykle)
            case 0x2C << 3 | 0: BitAbs_Cycle0(); return true;
            case 0x2C << 3 | 1: BitAbs_Cycle1(); return true;
            case 0x2C << 3 | 2: BitAbs_Cycle2(); return true;
            case 0x2C << 3 | 3: BitAbs_Cycle3(); return true;

            // JMP Indirect,X (6 cykle - 65C02)
            case 0x7C << 3 | 0: JmpIndX_Cycle0(); return true;
            case 0x7C << 3 | 1: JmpIndX_Cycle1(); return true;
            case 0x7C << 3 | 2: JmpIndX_Cycle2(); return true;
            case 0x7C << 3 | 3: JmpIndX_Cycle3(); return true;
            case 0x7C << 3 | 4: JmpIndX_Cycle4(); return true;
            case 0x7C << 3 | 5: JmpIndX_Cycle5(); return true;

            default: return false;
        }
    }
}
