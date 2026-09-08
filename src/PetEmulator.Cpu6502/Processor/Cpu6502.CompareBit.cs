namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje CMP (Compare Accumulator)

    /// <summary>
    /// CmpImm - Compare Accumulator, Immediate.
    /// Opcode: 0xC9, Tryb: Immediate, Cykle: 2
    /// Porównuje A z operandem: ustawia N, Z, C (A >= operand → C=1).
    /// </summary>
    private void CmpImm()
    {
        var (_, val, _) = Imm();
        ExecuteCmp(_a, val);
    }

    /// <summary>
    /// CmpZp - Compare Accumulator, Zero Page.
    /// Opcode: 0xC5, Tryb: Zero Page, Cykle: 3
    /// </summary>
    private void CmpZp()
    {
        var (_, val, _) = Zp();
        ExecuteCmp(_a, val);
    }

    /// <summary>
    /// CmpZpX - Compare Accumulator, Zero Page, X.
    /// Opcode: 0xD5, Tryb: Zero Page,X, Cykle: 4
    /// </summary>
    private void CmpZpX()
    {
        var (_, val, _) = ZpX();
        ExecuteCmp(_a, val);
    }

    /// <summary>
    /// CmpAbs - Compare Accumulator, Absolute.
    /// Opcode: 0xCD, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void CmpAbs()
    {
        var (_, val, _) = Abs();
        ExecuteCmp(_a, val);
    }

    /// <summary>
    /// CmpAbsX - Compare Accumulator, Absolute, X.
    /// Opcode: 0xDD, Tryb: Absolute,X, Cykle: 4 + page crossing
    /// </summary>
    private void CmpAbsX()
    {
        var (_, val, _) = AbsX();
        ExecuteCmp(_a, val);
    }

    /// <summary>
    /// CmpAbsY - Compare Accumulator, Absolute, Y.
    /// Opcode: 0xD9, Tryb: Absolute,Y, Cykle: 4 + page crossing
    /// </summary>
    private void CmpAbsY()
    {
        var (_, val, _) = AbsY();
        ExecuteCmp(_a, val);
    }

    /// <summary>
    /// CmpIndX - Compare Accumulator, Indirect X.
    /// Opcode: 0xC1, Tryb: (Indirect,X), Cykle: 6
    /// </summary>
    private void CmpIndX()
    {
        var (_, val, _) = IndX();
        ExecuteCmp(_a, val);
    }

    /// <summary>
    /// CmpIndY - Compare Accumulator, Indirect Y.
    /// Opcode: 0xD1, Tryb: (Indirect),Y, Cykle: 5 + page crossing
    /// </summary>
    private void CmpIndY()
    {
        var (_, val, _) = IndY();
        ExecuteCmp(_a, val);
    }

    #endregion

    #region Instrukcje CPX (Compare X Register)

    /// <summary>
    /// CpxImm - Compare X Register, Immediate.
    /// Opcode: 0xE0, Tryb: Immediate, Cykle: 2
    /// </summary>
    private void CpxImm()
    {
        var (_, val, _) = Imm();
        ExecuteCmp(_x, val);
    }

    /// <summary>
    /// CpxZp - Compare X Register, Zero Page.
    /// Opcode: 0xE4, Tryb: Zero Page, Cykle: 3
    /// </summary>
    private void CpxZp()
    {
        var (_, val, _) = Zp();
        ExecuteCmp(_x, val);
    }

    /// <summary>
    /// CpxAbs - Compare X Register, Absolute.
    /// Opcode: 0xEC, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void CpxAbs()
    {
        var (_, val, _) = Abs();
        ExecuteCmp(_x, val);
    }

    #endregion

    #region Instrukcje CPY (Compare Y Register)

    /// <summary>
    /// CpyImm - Compare Y Register, Immediate.
    /// Opcode: 0xC0, Tryb: Immediate, Cykle: 2
    /// </summary>
    private void CpyImm()
    {
        var (_, val, _) = Imm();
        ExecuteCmp(_y, val);
    }

    /// <summary>
    /// CpyZp - Compare Y Register, Zero Page.
    /// Opcode: 0xC4, Tryb: Zero Page, Cykle: 3
    /// </summary>
    private void CpyZp()
    {
        var (_, val, _) = Zp();
        ExecuteCmp(_y, val);
    }

    /// <summary>
    /// CpyAbs - Compare Y Register, Absolute.
    /// Opcode: 0xCC, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void CpyAbs()
    {
        var (_, val, _) = Abs();
        ExecuteCmp(_y, val);
    }

    #endregion

    #region Instrukcje BIT (Bit Test)

    /// <summary>
    /// BitZp - Bit Test, Zero Page.
    /// Opcode: 0x24, Tryb: Zero Page, Cykle: 3
    /// Testuje A & M, ustawia N (bit 7 operandu), V (bit 6 operandu), Z (result).
    /// </summary>
    private void BitZp()
    {
        var (_, val, _) = Zp();
        ExecuteBit(val);
    }

    /// <summary>
    /// BitAbs - Bit Test, Absolute.
    /// Opcode: 0x2C, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void BitAbs()
    {
        var (_, val, _) = Abs();
        ExecuteBit(val);
    }

    /// <summary>
    /// BitImm - Bit Test, Immediate (65C02).
    /// Opcode: 0x89, Tryb: Immediate, Cykle: 2
    /// Sets only Z flag (unlike zp/abs BIT which also set N and V).
    /// </summary>
    private void BitImm()
    {
        byte val = _memory.Read(_pc++);
        ExecuteBitImmediate(val);
    }

    /// <summary>
    /// BitZpX - Bit Test, Zero Page, X (65C02).
    /// Opcode: 0x34, Tryb: Zero Page,X, Cykle: 4
    /// </summary>
    private void BitZpX()
    {
        var (_, val, _) = ZpX();
        ExecuteBit(val);
    }

    /// <summary>
    /// BitAbsX - Bit Test, Absolute, X (65C02).
    /// Opcode: 0x3C, Tryb: Absolute,X, Cykle: 4 + page crossing
    /// </summary>
    private void BitAbsX()
    {
        var (_, val, _) = AbsX();
        ExecuteBit(val);
    }

    #endregion

    #region Wspólne metody CMP/CPX/CPY

    /// <summary>
    /// ExecuteCmp - Wykonuje porównanie przez odejmowanie bez zapisu wyniku.
    /// C = 1 gdy reg >= operand (brak pożyczenia).
    /// </summary>
    /// <param name="reg">Wartość z rejestru (A, X lub Y).</param>
    /// <param name="operand">Operand z pamięci.</param>
    private void ExecuteCmp(byte reg, byte operand)
    {
        // CMP: reg - operand (użycie ushort do wykrycia pożyczenia)
        ushort diff = (ushort)(reg - operand);

        // C = 1 gdy reg >= operand (brak pożyczenia)
        SetFlag(FlagC, reg >= operand);

        // Z = 1 gdy różnica = 0
        SetFlag(FlagZ, (diff & 0xFF) == 0);

        // N = bit 7 różnicy
        SetFlag(FlagN, (diff & 0x80) != 0);
    }

    /// <summary>
    /// ExecuteBit - Wykonuje test bitów (A & M) bez zmiany A.
    /// Ustawia: Z (result), N (bit 7 operandu), V (bit 6 operandu).
    /// </summary>
    /// <param name="operand">Operand z pamięci.</param>
    private void ExecuteBit(byte operand)
    {
        byte result = (byte)(_a & operand);

        // Z = 1 gdy A & M = 0
        SetFlag(FlagZ, result == 0);

        // N = bit 7 operandu (nie wyniku!)
        SetFlag(FlagN, (operand & 0x80) != 0);

        // V = bit 6 operandu (nie wyniku!)
        SetFlag(FlagV, (operand & 0x40) != 0);
    }

    /// <summary>
    /// ExecuteBitImmediate - Wykonuje test bitów dla trybu immediate (65C02).
    /// Sets only Z flag (unlike zp/abs BIT which also set N and V).
    /// </summary>
    /// <param name="operand">Immediate operand.</param>
    private void ExecuteBitImmediate(byte operand)
    {
        byte result = (byte)(_a & operand);
        // Only set Z flag, don't touch N or V
        SetFlag(FlagZ, result == 0);
    }

    #endregion
}
