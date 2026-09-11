namespace PetEmulator.Cpu6502;

public partial class Cpu6502
{
    internal bool ExecuteCycleMathsStackBranches(ushort key)
    {
        switch (key)
        {
            // INC - Increment Memory (R-M-W)
            case 0xE6 << 3 | 0: IncZp_Cycle0(); break;
            case 0xE6 << 3 | 1: IncZp_Cycle1(); break;
            case 0xE6 << 3 | 2: IncZp_Cycle2(); break;
            case 0xE6 << 3 | 3: IncZp_Cycle3(); break;
            case 0xE6 << 3 | 4: IncZp_Cycle4(); break;

            case 0xF6 << 3 | 0: IncZpX_Cycle0(); break;
            case 0xF6 << 3 | 1: IncZpX_Cycle1(); break;
            case 0xF6 << 3 | 2: IncZpX_Cycle2(); break;
            case 0xF6 << 3 | 3: IncZpX_Cycle3(); break;
            case 0xF6 << 3 | 4: IncZpX_Cycle4(); break;
            case 0xF6 << 3 | 5: IncZpX_Cycle5(); break;

            case 0xEE << 3 | 0: IncAbs_Cycle0(); break;
            case 0xEE << 3 | 1: IncAbs_Cycle1(); break;
            case 0xEE << 3 | 2: IncAbs_Cycle2(); break;
            case 0xEE << 3 | 3: IncAbs_Cycle3(); break;
            case 0xEE << 3 | 4: IncAbs_Cycle4(); break;
            case 0xEE << 3 | 5: IncAbs_Cycle5(); break;

            case 0xFE << 3 | 0: IncAbsX_Cycle0(); break;
            case 0xFE << 3 | 1: IncAbsX_Cycle1(); break;
            case 0xFE << 3 | 2: IncAbsX_Cycle2(); break;
            case 0xFE << 3 | 3: IncAbsX_Cycle3(); break;
            case 0xFE << 3 | 4: IncAbsX_Cycle4(); break;
            case 0xFE << 3 | 5: IncAbsX_Cycle5(); break;
            case 0xFE << 3 | 6: IncAbsX_Cycle6(); break;

            // DEC - Decrement Memory (R-M-W)
            case 0xC6 << 3 | 0: DecZp_Cycle0(); break;
            case 0xC6 << 3 | 1: DecZp_Cycle1(); break;
            case 0xC6 << 3 | 2: DecZp_Cycle2(); break;
            case 0xC6 << 3 | 3: DecZp_Cycle3(); break;
            case 0xC6 << 3 | 4: DecZp_Cycle4(); break;

            case 0xD6 << 3 | 0: DecZpX_Cycle0(); break;
            case 0xD6 << 3 | 1: DecZpX_Cycle1(); break;
            case 0xD6 << 3 | 2: DecZpX_Cycle2(); break;
            case 0xD6 << 3 | 3: DecZpX_Cycle3(); break;
            case 0xD6 << 3 | 4: DecZpX_Cycle4(); break;
            case 0xD6 << 3 | 5: DecZpX_Cycle5(); break;

            case 0xCE << 3 | 0: DecAbs_Cycle0(); break;
            case 0xCE << 3 | 1: DecAbs_Cycle1(); break;
            case 0xCE << 3 | 2: DecAbs_Cycle2(); break;
            case 0xCE << 3 | 3: DecAbs_Cycle3(); break;
            case 0xCE << 3 | 4: DecAbs_Cycle4(); break;
            case 0xCE << 3 | 5: DecAbs_Cycle5(); break;

            case 0xDE << 3 | 0: DecAbsX_Cycle0(); break;
            case 0xDE << 3 | 1: DecAbsX_Cycle1(); break;
            case 0xDE << 3 | 2: DecAbsX_Cycle2(); break;
            case 0xDE << 3 | 3: DecAbsX_Cycle3(); break;
            case 0xDE << 3 | 4: DecAbsX_Cycle4(); break;
            case 0xDE << 3 | 5: DecAbsX_Cycle5(); break;
            case 0xDE << 3 | 6: DecAbsX_Cycle6(); break;

            // ASL - Arithmetic Shift Left (R-M-W)
            case 0x0A << 3 | 0: AslAcc(); break;
            case 0x0A << 3 | 1: _sync = true; break;
            case 0x06 << 3 | 0: AslZp_Cycle0(); break;
            case 0x06 << 3 | 1: AslZp_Cycle1(); break;
            case 0x06 << 3 | 2: AslZp_Cycle2(); break;
            case 0x06 << 3 | 3: AslZp_Cycle3(); break;
            case 0x06 << 3 | 4: AslZp_Cycle4(); break;

            case 0x16 << 3 | 0: AslZpX_Cycle0(); break;
            case 0x16 << 3 | 1: AslZpX_Cycle1(); break;
            case 0x16 << 3 | 2: AslZpX_Cycle2(); break;
            case 0x16 << 3 | 3: AslZpX_Cycle3(); break;
            case 0x16 << 3 | 4: AslZpX_Cycle4(); break;
            case 0x16 << 3 | 5: AslZpX_Cycle5(); break;

            case 0x0E << 3 | 0: AslAbs_Cycle0(); break;
            case 0x0E << 3 | 1: AslAbs_Cycle1(); break;
            case 0x0E << 3 | 2: AslAbs_Cycle2(); break;
            case 0x0E << 3 | 3: AslAbs_Cycle3(); break;
            case 0x0E << 3 | 4: AslAbs_Cycle4(); break;
            case 0x0E << 3 | 5: AslAbs_Cycle5(); break;

            case 0x1E << 3 | 0: AslAbsX_Cycle0(); break;
            case 0x1E << 3 | 1: AslAbsX_Cycle1(); break;
            case 0x1E << 3 | 2: AslAbsX_Cycle2(); break;
            case 0x1E << 3 | 3: AslAbsX_Cycle3(); break;
            case 0x1E << 3 | 4: AslAbsX_Cycle4(); break;
            case 0x1E << 3 | 5: AslAbsX_Cycle5(); break;
            case 0x1E << 3 | 6: AslAbsX_Cycle6(); break;

            // LSR - Logical Shift Right (R-M-W)
            case 0x4A << 3 | 0: LsrAcc(); break;
            case 0x4A << 3 | 1: _sync = true; break;
            case 0x46 << 3 | 0: LsrZp_Cycle0(); break;
            case 0x46 << 3 | 1: LsrZp_Cycle1(); break;
            case 0x46 << 3 | 2: LsrZp_Cycle2(); break;
            case 0x46 << 3 | 3: LsrZp_Cycle3(); break;
            case 0x46 << 3 | 4: LsrZp_Cycle4(); break;

            case 0x56 << 3 | 0: LsrZpX_Cycle0(); break;
            case 0x56 << 3 | 1: LsrZpX_Cycle1(); break;
            case 0x56 << 3 | 2: LsrZpX_Cycle2(); break;
            case 0x56 << 3 | 3: LsrZpX_Cycle3(); break;
            case 0x56 << 3 | 4: LsrZpX_Cycle4(); break;
            case 0x56 << 3 | 5: LsrZpX_Cycle5(); break;

            case 0x4E << 3 | 0: LsrAbs_Cycle0(); break;
            case 0x4E << 3 | 1: LsrAbs_Cycle1(); break;
            case 0x4E << 3 | 2: LsrAbs_Cycle2(); break;
            case 0x4E << 3 | 3: LsrAbs_Cycle3(); break;
            case 0x4E << 3 | 4: LsrAbs_Cycle4(); break;
            case 0x4E << 3 | 5: LsrAbs_Cycle5(); break;

            case 0x5E << 3 | 0: LsrAbsX_Cycle0(); break;
            case 0x5E << 3 | 1: LsrAbsX_Cycle1(); break;
            case 0x5E << 3 | 2: LsrAbsX_Cycle2(); break;
            case 0x5E << 3 | 3: LsrAbsX_Cycle3(); break;
            case 0x5E << 3 | 4: LsrAbsX_Cycle4(); break;
            case 0x5E << 3 | 5: LsrAbsX_Cycle5(); break;
            case 0x5E << 3 | 6: LsrAbsX_Cycle6(); break;

            // ROL - Rotate Left (R-M-W)
            case 0x2A << 3 | 0: RolAcc(); break;
            case 0x2A << 3 | 1: _sync = true; break;
            case 0x26 << 3 | 0: RolZp_Cycle0(); break;
            case 0x26 << 3 | 1: RolZp_Cycle1(); break;
            case 0x26 << 3 | 2: RolZp_Cycle2(); break;
            case 0x26 << 3 | 3: RolZp_Cycle3(); break;
            case 0x26 << 3 | 4: RolZp_Cycle4(); break;

            case 0x36 << 3 | 0: RolZpX_Cycle0(); break;
            case 0x36 << 3 | 1: RolZpX_Cycle1(); break;
            case 0x36 << 3 | 2: RolZpX_Cycle2(); break;
            case 0x36 << 3 | 3: RolZpX_Cycle3(); break;
            case 0x36 << 3 | 4: RolZpX_Cycle4(); break;
            case 0x36 << 3 | 5: RolZpX_Cycle5(); break;

            case 0x2E << 3 | 0: RolAbs_Cycle0(); break;
            case 0x2E << 3 | 1: RolAbs_Cycle1(); break;
            case 0x2E << 3 | 2: RolAbs_Cycle2(); break;
            case 0x2E << 3 | 3: RolAbs_Cycle3(); break;
            case 0x2E << 3 | 4: RolAbs_Cycle4(); break;
            case 0x2E << 3 | 5: RolAbs_Cycle5(); break;

            case 0x3E << 3 | 0: RolAbsX_Cycle0(); break;
            case 0x3E << 3 | 1: RolAbsX_Cycle1(); break;
            case 0x3E << 3 | 2: RolAbsX_Cycle2(); break;
            case 0x3E << 3 | 3: RolAbsX_Cycle3(); break;
            case 0x3E << 3 | 4: RolAbsX_Cycle4(); break;
            case 0x3E << 3 | 5: RolAbsX_Cycle5(); break;
            case 0x3E << 3 | 6: RolAbsX_Cycle6(); break;

            // ROR - Rotate Right (R-M-W)
            case 0x6A << 3 | 0: RorAcc(); break;
            case 0x6A << 3 | 1: _sync = true; break;
            case 0x66 << 3 | 0: RorZp_Cycle0(); break;
            case 0x66 << 3 | 1: RorZp_Cycle1(); break;
            case 0x66 << 3 | 2: RorZp_Cycle2(); break;
            case 0x66 << 3 | 3: RorZp_Cycle3(); break;
            case 0x66 << 3 | 4: RorZp_Cycle4(); break;

            case 0x76 << 3 | 0: RorZpX_Cycle0(); break;
            case 0x76 << 3 | 1: RorZpX_Cycle1(); break;
            case 0x76 << 3 | 2: RorZpX_Cycle2(); break;
            case 0x76 << 3 | 3: RorZpX_Cycle3(); break;
            case 0x76 << 3 | 4: RorZpX_Cycle4(); break;
            case 0x76 << 3 | 5: RorZpX_Cycle5(); break;

            case 0x6E << 3 | 0: RorAbs_Cycle0(); break;
            case 0x6E << 3 | 1: RorAbs_Cycle1(); break;
            case 0x6E << 3 | 2: RorAbs_Cycle2(); break;
            case 0x6E << 3 | 3: RorAbs_Cycle3(); break;
            case 0x6E << 3 | 4: RorAbs_Cycle4(); break;
            case 0x6E << 3 | 5: RorAbs_Cycle5(); break;

            case 0x7E << 3 | 0: RorAbsX_Cycle0(); break;
            case 0x7E << 3 | 1: RorAbsX_Cycle1(); break;
            case 0x7E << 3 | 2: RorAbsX_Cycle2(); break;
            case 0x7E << 3 | 3: RorAbsX_Cycle3(); break;
            case 0x7E << 3 | 4: RorAbsX_Cycle4(); break;
            case 0x7E << 3 | 5: RorAbsX_Cycle5(); break;
            case 0x7E << 3 | 6: RorAbsX_Cycle6(); break;

            // Stack operations with functional execution on cycle 0 and timing padding.
            case 0x48 << 3 | 0: Pha(); break;
            case 0x48 << 3 | 1: break;
            case 0x48 << 3 | 2: _sync = true; break;
            case 0x08 << 3 | 0: Php(); break;
            case 0x08 << 3 | 1: break;
            case 0x08 << 3 | 2: _sync = true; break;
            case 0x68 << 3 | 0: Pla(); break;
            case 0x68 << 3 | 1: break;
            case 0x68 << 3 | 2: break;
            case 0x68 << 3 | 3: _sync = true; break;
            case 0x28 << 3 | 0: Plp(); break;
            case 0x28 << 3 | 1: break;
            case 0x28 << 3 | 2: break;
            case 0x28 << 3 | 3: _sync = true; break;

            // Register increment/decrement (single cycle)
            case 0xE8 << 3 | 0: Inx(); break;
            case 0xE8 << 3 | 1: _sync = true; break;
            case 0xC8 << 3 | 0: Iny(); break;
            case 0xC8 << 3 | 1: _sync = true; break;
            case 0xCA << 3 | 0: Dex(); break;
            case 0xCA << 3 | 1: _sync = true; break;
            case 0x88 << 3 | 0: Dey(); break;
            case 0x88 << 3 | 1: _sync = true; break;

            default: return false;
        }

        return true;
    }
}
