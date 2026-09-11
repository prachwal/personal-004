namespace PetEmulator.Cpu6502;

public partial class Cpu6502
{
    internal bool ExecuteCycleBranches(ushort key)
    {
        switch (key)
        {
            // BCC - Branch if Carry Clear
            case 0x90 << 3 | 0: BccRel_Cycle0(); break;
            case 0x90 << 3 | 1: BccRel_Cycle1(); break;
            case 0x90 << 3 | 2: BccRel_Cycle2(); break;
            case 0x90 << 3 | 3: BccRel_Cycle3(); break;

            // BCS - Branch if Carry Set
            case 0xB0 << 3 | 0: BcsRel_Cycle0(); break;
            case 0xB0 << 3 | 1: BcsRel_Cycle1(); break;
            case 0xB0 << 3 | 2: BcsRel_Cycle2(); break;
            case 0xB0 << 3 | 3: BcsRel_Cycle3(); break;

            // BEQ - Branch if Equal
            case 0xF0 << 3 | 0: BeqRel_Cycle0(); break;
            case 0xF0 << 3 | 1: BeqRel_Cycle1(); break;
            case 0xF0 << 3 | 2: BeqRel_Cycle2(); break;
            case 0xF0 << 3 | 3: BeqRel_Cycle3(); break;

            // BMI - Branch if Minus
            case 0x30 << 3 | 0: BmiRel_Cycle0(); break;
            case 0x30 << 3 | 1: BmiRel_Cycle1(); break;
            case 0x30 << 3 | 2: BmiRel_Cycle2(); break;
            case 0x30 << 3 | 3: BmiRel_Cycle3(); break;

            // BNE - Branch if Not Equal
            case 0xD0 << 3 | 0: BneRel_Cycle0(); break;
            case 0xD0 << 3 | 1: BneRel_Cycle1(); break;
            case 0xD0 << 3 | 2: BneRel_Cycle2(); break;
            case 0xD0 << 3 | 3: BneRel_Cycle3(); break;

            // BPL - Branch if Plus
            case 0x10 << 3 | 0: BplRel_Cycle0(); break;
            case 0x10 << 3 | 1: BplRel_Cycle1(); break;
            case 0x10 << 3 | 2: BplRel_Cycle2(); break;
            case 0x10 << 3 | 3: BplRel_Cycle3(); break;

            // BVC - Branch if Overflow Clear
            case 0x50 << 3 | 0: BvcRel_Cycle0(); break;
            case 0x50 << 3 | 1: BvcRel_Cycle1(); break;
            case 0x50 << 3 | 2: BvcRel_Cycle2(); break;
            case 0x50 << 3 | 3: BvcRel_Cycle3(); break;

            // BVS - Branch if Overflow Set
            case 0x70 << 3 | 0: BvsRel_Cycle0(); break;
            case 0x70 << 3 | 1: BvsRel_Cycle1(); break;
            case 0x70 << 3 | 2: BvsRel_Cycle2(); break;
            case 0x70 << 3 | 3: BvsRel_Cycle3(); break;

            // BRA - Branch Always (65C02)
            case 0x80 << 3 | 0: BraRel_Cycle0(); break;
            case 0x80 << 3 | 1: BraRel_Cycle1(); break;
            case 0x80 << 3 | 2: BraRel_Cycle2(); break;
            case 0x80 << 3 | 3: BraRel_Cycle3(); break;

            default: return false;
        }

        return true;
    }
}
