namespace PetEmulator.Cpu6502;

/// <summary>
/// Dispatch cykli dla nieudokumentowanych opkodów R-M-W
/// </summary>
public partial class Cpu6502
{
    /// <summary>
    /// Obsługuje nieudokumentowane opkody R-M-W (DCP, ISC, RLA, RRA, SLO, SRE)
    /// </summary>
    internal bool ExecuteCycleIllegalRMW(ushort key)
    {
        switch (key)
        {
            // ==================== DCP (DEC + CMP) ====================
            // DCP Zero Page ($C7)
            case 0xC7 << 3 | 0: DcpZp_Cycle0(); break;
            case 0xC7 << 3 | 1: DcpZp_Cycle1(); break;
            case 0xC7 << 3 | 2: DcpZp_Cycle2(); break;
            case 0xC7 << 3 | 3: DcpZp_Cycle3(); break;
            case 0xC7 << 3 | 4: DcpZp_Cycle4(); break;

            // DCP Zero Page,X ($D7)
            case 0xD7 << 3 | 0: DcpZpX_Cycle0(); break;
            case 0xD7 << 3 | 1: DcpZpX_Cycle1(); break;
            case 0xD7 << 3 | 2: DcpZpX_Cycle2(); break;
            case 0xD7 << 3 | 3: DcpZpX_Cycle3(); break;
            case 0xD7 << 3 | 4: DcpZpX_Cycle4(); break;
            case 0xD7 << 3 | 5: DcpZpX_Cycle5(); break;

            // DCP Absolute ($CF)
            case 0xCF << 3 | 0: DcpAbs_Cycle0(); break;
            case 0xCF << 3 | 1: DcpAbs_Cycle1(); break;
            case 0xCF << 3 | 2: DcpAbs_Cycle2(); break;
            case 0xCF << 3 | 3: DcpAbs_Cycle3(); break;
            case 0xCF << 3 | 4: DcpAbs_Cycle4(); break;
            case 0xCF << 3 | 5: DcpAbs_Cycle5(); break;

            // DCP Absolute,X ($DF)
            case 0xDF << 3 | 0: DcpAbsX_Cycle0(); break;
            case 0xDF << 3 | 1: DcpAbsX_Cycle1(); break;
            case 0xDF << 3 | 2: DcpAbsX_Cycle2(); break;
            case 0xDF << 3 | 3: DcpAbsX_Cycle3(); break;
            case 0xDF << 3 | 4: DcpAbsX_Cycle4(); break;
            case 0xDF << 3 | 5: DcpAbsX_Cycle5(); break;
            case 0xDF << 3 | 6: DcpAbsX_Cycle6(); break;

            // DCP Absolute,Y ($DB)
            case 0xDB << 3 | 0: DcpAbsY_Cycle0(); break;
            case 0xDB << 3 | 1: DcpAbsY_Cycle1(); break;
            case 0xDB << 3 | 2: DcpAbsY_Cycle2(); break;
            case 0xDB << 3 | 3: DcpAbsY_Cycle3(); break;
            case 0xDB << 3 | 4: DcpAbsY_Cycle4(); break;
            case 0xDB << 3 | 5: DcpAbsY_Cycle5(); break;
            case 0xDB << 3 | 6: DcpAbsY_Cycle6(); break;

            // DCP (Indirect,X) ($C3)
            case 0xC3 << 3 | 0: DcpIndX_Cycle0(); break;
            case 0xC3 << 3 | 1: DcpIndX_Cycle1(); break;
            case 0xC3 << 3 | 2: DcpIndX_Cycle2(); break;
            case 0xC3 << 3 | 3: DcpIndX_Cycle3(); break;
            case 0xC3 << 3 | 4: DcpIndX_Cycle4(); break;
            case 0xC3 << 3 | 5: DcpIndX_Cycle5(); break;
            case 0xC3 << 3 | 6: DcpIndX_Cycle6(); break;
            case 0xC3 << 3 | 7: DcpIndX_Cycle7(); break;

            // DCP (Indirect),Y ($D3)
            case 0xD3 << 3 | 0: DcpIndY_Cycle0(); break;
            case 0xD3 << 3 | 1: DcpIndY_Cycle1(); break;
            case 0xD3 << 3 | 2: DcpIndY_Cycle2(); break;
            case 0xD3 << 3 | 3: DcpIndY_Cycle3(); break;
            case 0xD3 << 3 | 4: DcpIndY_Cycle4(); break;
            case 0xD3 << 3 | 5: DcpIndY_Cycle5(); break;
            case 0xD3 << 3 | 6: DcpIndY_Cycle6(); break;
            case 0xD3 << 3 | 7: DcpIndY_Cycle7(); break;

            // ==================== ISC (INC + SBC) ====================
            // ISC Zero Page ($E7)
            case 0xE7 << 3 | 0: IscZp_Cycle0(); break;
            case 0xE7 << 3 | 1: IscZp_Cycle1(); break;
            case 0xE7 << 3 | 2: IscZp_Cycle2(); break;
            case 0xE7 << 3 | 3: IscZp_Cycle3(); break;
            case 0xE7 << 3 | 4: IscZp_Cycle4(); break;

            // ISC Zero Page,X ($F7)
            case 0xF7 << 3 | 0: IscZpX_Cycle0(); break;
            case 0xF7 << 3 | 1: IscZpX_Cycle1(); break;
            case 0xF7 << 3 | 2: IscZpX_Cycle2(); break;
            case 0xF7 << 3 | 3: IscZpX_Cycle3(); break;
            case 0xF7 << 3 | 4: IscZpX_Cycle4(); break;
            case 0xF7 << 3 | 5: IscZpX_Cycle5(); break;

            // ISC Absolute ($EF)
            case 0xEF << 3 | 0: IscAbs_Cycle0(); break;
            case 0xEF << 3 | 1: IscAbs_Cycle1(); break;
            case 0xEF << 3 | 2: IscAbs_Cycle2(); break;
            case 0xEF << 3 | 3: IscAbs_Cycle3(); break;
            case 0xEF << 3 | 4: IscAbs_Cycle4(); break;
            case 0xEF << 3 | 5: IscAbs_Cycle5(); break;

            // ISC Absolute,X ($FF)
            case 0xFF << 3 | 0: IscAbsX_Cycle0(); break;
            case 0xFF << 3 | 1: IscAbsX_Cycle1(); break;
            case 0xFF << 3 | 2: IscAbsX_Cycle2(); break;
            case 0xFF << 3 | 3: IscAbsX_Cycle3(); break;
            case 0xFF << 3 | 4: IscAbsX_Cycle4(); break;
            case 0xFF << 3 | 5: IscAbsX_Cycle5(); break;
            case 0xFF << 3 | 6: IscAbsX_Cycle6(); break;

            // ISC Absolute,Y ($FB)
            case 0xFB << 3 | 0: IscAbsY_Cycle0(); break;
            case 0xFB << 3 | 1: IscAbsY_Cycle1(); break;
            case 0xFB << 3 | 2: IscAbsY_Cycle2(); break;
            case 0xFB << 3 | 3: IscAbsY_Cycle3(); break;
            case 0xFB << 3 | 4: IscAbsY_Cycle4(); break;
            case 0xFB << 3 | 5: IscAbsY_Cycle5(); break;
            case 0xFB << 3 | 6: IscAbsY_Cycle6(); break;

            // ISC (Indirect,X) ($E3)
            case 0xE3 << 3 | 0: IscIndX_Cycle0(); break;
            case 0xE3 << 3 | 1: IscIndX_Cycle1(); break;
            case 0xE3 << 3 | 2: IscIndX_Cycle2(); break;
            case 0xE3 << 3 | 3: IscIndX_Cycle3(); break;
            case 0xE3 << 3 | 4: IscIndX_Cycle4(); break;
            case 0xE3 << 3 | 5: IscIndX_Cycle5(); break;
            case 0xE3 << 3 | 6: IscIndX_Cycle6(); break;
            case 0xE3 << 3 | 7: IscIndX_Cycle7(); break;

            // ISC (Indirect),Y ($F3)
            case 0xF3 << 3 | 0: IscIndY_Cycle0(); break;
            case 0xF3 << 3 | 1: IscIndY_Cycle1(); break;
            case 0xF3 << 3 | 2: IscIndY_Cycle2(); break;
            case 0xF3 << 3 | 3: IscIndY_Cycle3(); break;
            case 0xF3 << 3 | 4: IscIndY_Cycle4(); break;
            case 0xF3 << 3 | 5: IscIndY_Cycle5(); break;
            case 0xF3 << 3 | 6: IscIndY_Cycle6(); break;
            case 0xF3 << 3 | 7: IscIndY_Cycle7(); break;

            // ==================== RLA (ROL + AND) ====================
            // RLA Zero Page ($27)
            case 0x27 << 3 | 0: RlaZp_Cycle0(); break;
            case 0x27 << 3 | 1: RlaZp_Cycle1(); break;
            case 0x27 << 3 | 2: RlaZp_Cycle2(); break;
            case 0x27 << 3 | 3: RlaZp_Cycle3(); break;
            case 0x27 << 3 | 4: RlaZp_Cycle4(); break;

            // RLA Zero Page,X ($37)
            case 0x37 << 3 | 0: RlaZpX_Cycle0(); break;
            case 0x37 << 3 | 1: RlaZpX_Cycle1(); break;
            case 0x37 << 3 | 2: RlaZpX_Cycle2(); break;
            case 0x37 << 3 | 3: RlaZpX_Cycle3(); break;
            case 0x37 << 3 | 4: RlaZpX_Cycle4(); break;
            case 0x37 << 3 | 5: RlaZpX_Cycle5(); break;

            // RLA Absolute ($2F)
            case 0x2F << 3 | 0: RlaAbs_Cycle0(); break;
            case 0x2F << 3 | 1: RlaAbs_Cycle1(); break;
            case 0x2F << 3 | 2: RlaAbs_Cycle2(); break;
            case 0x2F << 3 | 3: RlaAbs_Cycle3(); break;
            case 0x2F << 3 | 4: RlaAbs_Cycle4(); break;
            case 0x2F << 3 | 5: RlaAbs_Cycle5(); break;

            // RLA Absolute,X ($3F)
            case 0x3F << 3 | 0: RlaAbsX_Cycle0(); break;
            case 0x3F << 3 | 1: RlaAbsX_Cycle1(); break;
            case 0x3F << 3 | 2: RlaAbsX_Cycle2(); break;
            case 0x3F << 3 | 3: RlaAbsX_Cycle3(); break;
            case 0x3F << 3 | 4: RlaAbsX_Cycle4(); break;
            case 0x3F << 3 | 5: RlaAbsX_Cycle5(); break;
            case 0x3F << 3 | 6: RlaAbsX_Cycle6(); break;

            // RLA Absolute,Y ($3B)
            case 0x3B << 3 | 0: RlaAbsY_Cycle0(); break;
            case 0x3B << 3 | 1: RlaAbsY_Cycle1(); break;
            case 0x3B << 3 | 2: RlaAbsY_Cycle2(); break;
            case 0x3B << 3 | 3: RlaAbsY_Cycle3(); break;
            case 0x3B << 3 | 4: RlaAbsY_Cycle4(); break;
            case 0x3B << 3 | 5: RlaAbsY_Cycle5(); break;
            case 0x3B << 3 | 6: RlaAbsY_Cycle6(); break;

            // RLA (Indirect,X) ($23)
            case 0x23 << 3 | 0: RlaIndX_Cycle0(); break;
            case 0x23 << 3 | 1: RlaIndX_Cycle1(); break;
            case 0x23 << 3 | 2: RlaIndX_Cycle2(); break;
            case 0x23 << 3 | 3: RlaIndX_Cycle3(); break;
            case 0x23 << 3 | 4: RlaIndX_Cycle4(); break;
            case 0x23 << 3 | 5: RlaIndX_Cycle5(); break;
            case 0x23 << 3 | 6: RlaIndX_Cycle6(); break;
            case 0x23 << 3 | 7: RlaIndX_Cycle7(); break;

            // RLA (Indirect),Y ($33)
            case 0x33 << 3 | 0: RlaIndY_Cycle0(); break;
            case 0x33 << 3 | 1: RlaIndY_Cycle1(); break;
            case 0x33 << 3 | 2: RlaIndY_Cycle2(); break;
            case 0x33 << 3 | 3: RlaIndY_Cycle3(); break;
            case 0x33 << 3 | 4: RlaIndY_Cycle4(); break;
            case 0x33 << 3 | 5: RlaIndY_Cycle5(); break;
            case 0x33 << 3 | 6: RlaIndY_Cycle6(); break;
            case 0x33 << 3 | 7: RlaIndY_Cycle7(); break;

            // ==================== RRA (ROR + ADC) ====================
            // RRA Zero Page ($67)
            case 0x67 << 3 | 0: RraZp_Cycle0(); break;
            case 0x67 << 3 | 1: RraZp_Cycle1(); break;
            case 0x67 << 3 | 2: RraZp_Cycle2(); break;
            case 0x67 << 3 | 3: RraZp_Cycle3(); break;

            // RRA Zero Page,X ($77)
            case 0x77 << 3 | 0: RraZpX_Cycle0(); break;
            case 0x77 << 3 | 1: RraZpX_Cycle1(); break;
            case 0x77 << 3 | 2: RraZpX_Cycle2(); break;
            case 0x77 << 3 | 3: RraZpX_Cycle3(); break;

            // RRA Absolute ($6F)
            case 0x6F << 3 | 0: RraAbs_Cycle0(); break;
            case 0x6F << 3 | 1: RraAbs_Cycle1(); break;
            case 0x6F << 3 | 2: RraAbs_Cycle2(); break;
            case 0x6F << 3 | 3: RraAbs_Cycle3(); break;
            case 0x6F << 3 | 4: RraAbs_Cycle4(); break;

            // RRA Absolute,X ($7F)
            case 0x7F << 3 | 0: RraAbsX_Cycle0(); break;
            case 0x7F << 3 | 1: RraAbsX_Cycle1(); break;
            case 0x7F << 3 | 2: RraAbsX_Cycle2(); break;
            case 0x7F << 3 | 3: RraAbsX_Cycle3(); break;
            case 0x7F << 3 | 4: RraAbsX_Cycle4(); break;
            case 0x7F << 3 | 5: RraAbsX_Cycle5(); break;
            case 0x7F << 3 | 6: RraAbsX_Cycle6(); break;

            // RRA Absolute,Y ($7B)
            case 0x7B << 3 | 0: RraAbsY_Cycle0(); break;
            case 0x7B << 3 | 1: RraAbsY_Cycle1(); break;
            case 0x7B << 3 | 2: RraAbsY_Cycle2(); break;
            case 0x7B << 3 | 3: RraAbsY_Cycle3(); break;
            case 0x7B << 3 | 4: RraAbsY_Cycle4(); break;
            case 0x7B << 3 | 5: RraAbsY_Cycle5(); break;
            case 0x7B << 3 | 6: RraAbsY_Cycle6(); break;

            // RRA (Indirect,X) ($63)
            case 0x63 << 3 | 0: RraIndX_Cycle0(); break;
            case 0x63 << 3 | 1: RraIndX_Cycle1(); break;
            case 0x63 << 3 | 2: RraIndX_Cycle2(); break;
            case 0x63 << 3 | 3: RraIndX_Cycle3(); break;
            case 0x63 << 3 | 4: RraIndX_Cycle4(); break;
            case 0x63 << 3 | 5: RraIndX_Cycle5(); break;

            // RRA (Indirect),Y ($73)
            case 0x73 << 3 | 0: RraIndY_Cycle0(); break;
            case 0x73 << 3 | 1: RraIndY_Cycle1(); break;
            case 0x73 << 3 | 2: RraIndY_Cycle2(); break;
            case 0x73 << 3 | 3: RraIndY_Cycle3(); break;
            case 0x73 << 3 | 4: RraIndY_Cycle4(); break;
            case 0x73 << 3 | 5: RraIndY_Cycle5(); break;
            case 0x73 << 3 | 6: RraIndY_Cycle6(); break;
            case 0x73 << 3 | 7: RraIndY_Cycle7(); break;

            // ==================== SLO (ASL + ORA) ====================
            // SLO Zero Page ($07)
            case 0x07 << 3 | 0: SloZp_Cycle0(); break;
            case 0x07 << 3 | 1: SloZp_Cycle1(); break;
            case 0x07 << 3 | 2: SloZp_Cycle2(); break;
            case 0x07 << 3 | 3: SloZp_Cycle3(); break;
            case 0x07 << 3 | 4: SloZp_Cycle4(); break;

            // SLO Zero Page,X ($17)
            case 0x17 << 3 | 0: SloZpX_Cycle0(); break;
            case 0x17 << 3 | 1: SloZpX_Cycle1(); break;
            case 0x17 << 3 | 2: SloZpX_Cycle2(); break;
            case 0x17 << 3 | 3: SloZpX_Cycle3(); break;
            case 0x17 << 3 | 4: SloZpX_Cycle4(); break;
            case 0x17 << 3 | 5: SloZpX_Cycle5(); break;

            // SLO Absolute ($0F)
            case 0x0F << 3 | 0: SloAbs_Cycle0(); break;
            case 0x0F << 3 | 1: SloAbs_Cycle1(); break;
            case 0x0F << 3 | 2: SloAbs_Cycle2(); break;
            case 0x0F << 3 | 3: SloAbs_Cycle3(); break;
            case 0x0F << 3 | 4: SloAbs_Cycle4(); break;
            case 0x0F << 3 | 5: SloAbs_Cycle5(); break;

            // SLO Absolute,X ($1F)
            case 0x1F << 3 | 0: SloAbsX_Cycle0(); break;
            case 0x1F << 3 | 1: SloAbsX_Cycle1(); break;
            case 0x1F << 3 | 2: SloAbsX_Cycle2(); break;
            case 0x1F << 3 | 3: SloAbsX_Cycle3(); break;
            case 0x1F << 3 | 4: SloAbsX_Cycle4(); break;
            case 0x1F << 3 | 5: SloAbsX_Cycle5(); break;
            case 0x1F << 3 | 6: SloAbsX_Cycle6(); break;

            // SLO Absolute,Y ($1B)
            case 0x1B << 3 | 0: SloAbsY_Cycle0(); break;
            case 0x1B << 3 | 1: SloAbsY_Cycle1(); break;
            case 0x1B << 3 | 2: SloAbsY_Cycle2(); break;
            case 0x1B << 3 | 3: SloAbsY_Cycle3(); break;
            case 0x1B << 3 | 4: SloAbsY_Cycle4(); break;
            case 0x1B << 3 | 5: SloAbsY_Cycle5(); break;
            case 0x1B << 3 | 6: SloAbsY_Cycle6(); break;

            // SLO (Indirect,X) ($03)
            case 0x03 << 3 | 0: SloIndX_Cycle0(); break;
            case 0x03 << 3 | 1: SloIndX_Cycle1(); break;
            case 0x03 << 3 | 2: SloIndX_Cycle2(); break;
            case 0x03 << 3 | 3: SloIndX_Cycle3(); break;
            case 0x03 << 3 | 4: SloIndX_Cycle4(); break;
            case 0x03 << 3 | 5: SloIndX_Cycle5(); break;
            case 0x03 << 3 | 6: SloIndX_Cycle6(); break;
            case 0x03 << 3 | 7: SloIndX_Cycle7(); break;

            // SLO (Indirect),Y ($13)
            case 0x13 << 3 | 0: SloIndY_Cycle0(); break;
            case 0x13 << 3 | 1: SloIndY_Cycle1(); break;
            case 0x13 << 3 | 2: SloIndY_Cycle2(); break;
            case 0x13 << 3 | 3: SloIndY_Cycle3(); break;
            case 0x13 << 3 | 4: SloIndY_Cycle4(); break;
            case 0x13 << 3 | 5: SloIndY_Cycle5(); break;
            case 0x13 << 3 | 6: SloIndY_Cycle6(); break;
            case 0x13 << 3 | 7: SloIndY_Cycle7(); break;

            // ==================== SRE (LSR + EOR) ====================
            // SRE Zero Page ($47)
            case 0x47 << 3 | 0: SreZp_Cycle0(); break;
            case 0x47 << 3 | 1: SreZp_Cycle1(); break;
            case 0x47 << 3 | 2: SreZp_Cycle2(); break;
            case 0x47 << 3 | 3: SreZp_Cycle3(); break;

            // SRE Zero Page,X ($57)
            case 0x57 << 3 | 0: SreZpX_Cycle0(); break;
            case 0x57 << 3 | 1: SreZpX_Cycle1(); break;
            case 0x57 << 3 | 2: SreZpX_Cycle2(); break;
            case 0x57 << 3 | 3: SreZpX_Cycle3(); break;

            // SRE Absolute ($4F)
            case 0x4F << 3 | 0: SreAbs_Cycle0(); break;
            case 0x4F << 3 | 1: SreAbs_Cycle1(); break;
            case 0x4F << 3 | 2: SreAbs_Cycle2(); break;
            case 0x4F << 3 | 3: SreAbs_Cycle3(); break;
            case 0x4F << 3 | 4: SreAbs_Cycle4(); break;

            // SRE Absolute,X ($5F)
            case 0x5F << 3 | 0: SreAbsX_Cycle0(); break;
            case 0x5F << 3 | 1: SreAbsX_Cycle1(); break;
            case 0x5F << 3 | 2: SreAbsX_Cycle2(); break;
            case 0x5F << 3 | 3: SreAbsX_Cycle3(); break;
            case 0x5F << 3 | 4: SreAbsX_Cycle4(); break;
            case 0x5F << 3 | 5: SreAbsX_Cycle5(); break;
            case 0x5F << 3 | 6: SreAbsX_Cycle6(); break;

            // SRE Absolute,Y ($5B)
            case 0x5B << 3 | 0: SreAbsY_Cycle0(); break;
            case 0x5B << 3 | 1: SreAbsY_Cycle1(); break;
            case 0x5B << 3 | 2: SreAbsY_Cycle2(); break;
            case 0x5B << 3 | 3: SreAbsY_Cycle3(); break;
            case 0x5B << 3 | 4: SreAbsY_Cycle4(); break;
            case 0x5B << 3 | 5: SreAbsY_Cycle5(); break;
            case 0x5B << 3 | 6: SreAbsY_Cycle6(); break;

            // SRE (Indirect,X) ($43)
            case 0x43 << 3 | 0: SreIndX_Cycle0(); break;
            case 0x43 << 3 | 1: SreIndX_Cycle1(); break;
            case 0x43 << 3 | 2: SreIndX_Cycle2(); break;
            case 0x43 << 3 | 3: SreIndX_Cycle3(); break;
            case 0x43 << 3 | 4: SreIndX_Cycle4(); break;
            case 0x43 << 3 | 5: SreIndX_Cycle5(); break;

            // SRE (Indirect),Y ($53)
            case 0x53 << 3 | 0: SreIndY_Cycle0(); break;
            case 0x53 << 3 | 1: SreIndY_Cycle1(); break;
            case 0x53 << 3 | 2: SreIndY_Cycle2(); break;
            case 0x53 << 3 | 3: SreIndY_Cycle3(); break;
            case 0x53 << 3 | 4: SreIndY_Cycle4(); break;
            case 0x53 << 3 | 5: SreIndY_Cycle5(); break;
            case 0x53 << 3 | 6: SreIndY_Cycle6(); break;
            case 0x53 << 3 | 7: SreIndY_Cycle7(); break;

            default:
                return false;
        }

        return true;
    }
}
