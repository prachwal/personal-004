namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje AND (Logical AND)

    /// <summary>
    /// AndImm - Logical AND, Immediate.
    /// Opcode: 0x29, Tryb: Immediate, Cykle: 2
    /// </summary>
    private void AndImm()
    {
        var (_, val, _) = Imm();
        ExecuteAnd(val);
    }

    /// <summary>
    /// AndZp - Logical AND, Zero Page.
    /// Opcode: 0x25, Tryb: Zero Page, Cykle: 3
    /// </summary>
    private void AndZp()
    {
        var (_, val, _) = Zp();
        ExecuteAnd(val);
    }

    /// <summary>
    /// AndZpX - Logical AND, Zero Page, X.
    /// Opcode: 0x35, Tryb: Zero Page,X, Cykle: 4
    /// </summary>
    private void AndZpX()
    {
        var (_, val, _) = ZpX();
        ExecuteAnd(val);
    }

    /// <summary>
    /// AndAbs - Logical AND, Absolute.
    /// Opcode: 0x2D, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void AndAbs()
    {
        var (_, val, _) = Abs();
        ExecuteAnd(val);
    }

    /// <summary>
    /// AndAbsX - Logical AND, Absolute, X.
    /// Opcode: 0x3D, Tryb: Absolute,X, Cykle: 4 + page crossing
    /// </summary>
    private void AndAbsX()
    {
        var (_, val, _) = AbsX();
        ExecuteAnd(val);
    }

    /// <summary>
    /// AndAbsY - Logical AND, Absolute, Y.
    /// Opcode: 0x39, Tryb: Absolute,Y, Cykle: 4 + page crossing
    /// </summary>
    private void AndAbsY()
    {
        var (_, val, _) = AbsY();
        ExecuteAnd(val);
    }

    /// <summary>
    /// AndIndX - Logical AND, Indirect X.
    /// Opcode: 0x21, Tryb: (Indirect,X), Cykle: 6
    /// </summary>
    private void AndIndX()
    {
        var (_, val, _) = IndX();
        ExecuteAnd(val);
    }

    /// <summary>
    /// AndIndY - Logical AND, Indirect Y.
    /// Opcode: 0x31, Tryb: (Indirect),Y, Cykle: 5 + page crossing
    /// </summary>
    private void AndIndY()
    {
        var (_, val, _) = IndY();
        ExecuteAnd(val);
    }

    #endregion

    #region Instrukcje ORA (Logical OR)

    /// <summary>
    /// OraImm - Logical OR, Immediate.
    /// Opcode: 0x09, Tryb: Immediate, Cykle: 2
    /// </summary>
    private void OraImm()
    {
        var (_, val, _) = Imm();
        ExecuteOra(val);
    }

    /// <summary>
    /// OraZp - Logical OR, Zero Page.
    /// Opcode: 0x05, Tryb: Zero Page, Cykle: 3
    /// </summary>
    private void OraZp()
    {
        var (_, val, _) = Zp();
        ExecuteOra(val);
    }

    /// <summary>
    /// OraZpX - Logical OR, Zero Page, X.
    /// Opcode: 0x15, Tryb: Zero Page,X, Cykle: 4
    /// </summary>
    private void OraZpX()
    {
        var (_, val, _) = ZpX();
        ExecuteOra(val);
    }

    /// <summary>
    /// OraAbs - Logical OR, Absolute.
    /// Opcode: 0x0D, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void OraAbs()
    {
        var (_, val, _) = Abs();
        ExecuteOra(val);
    }

    /// <summary>
    /// OraAbsX - Logical OR, Absolute, X.
    /// Opcode: 0x1D, Tryb: Absolute,X, Cykle: 4 + page crossing
    /// </summary>
    private void OraAbsX()
    {
        var (_, val, _) = AbsX();
        ExecuteOra(val);
    }

    /// <summary>
    /// OraAbsY - Logical OR, Absolute, Y.
    /// Opcode: 0x19, Tryb: Absolute,Y, Cykle: 4 + page crossing
    /// </summary>
    private void OraAbsY()
    {
        var (_, val, _) = AbsY();
        ExecuteOra(val);
    }

    /// <summary>
    /// OraIndX - Logical OR, Indirect X.
    /// Opcode: 0x01, Tryb: (Indirect,X), Cykle: 6
    /// </summary>
    private void OraIndX()
    {
        var (_, val, _) = IndX();
        ExecuteOra(val);
    }

    /// <summary>
    /// OraIndY - Logical OR, Indirect Y.
    /// Opcode: 0x11, Tryb: (Indirect),Y, Cykle: 5 + page crossing
    /// </summary>
    private void OraIndY()
    {
        var (_, val, _) = IndY();
        ExecuteOra(val);
    }

    #endregion

    #region Instrukcje EOR (Exclusive OR)

    /// <summary>
    /// EorImm - Exclusive OR, Immediate.
    /// Opcode: 0x49, Tryb: Immediate, Cykle: 2
    /// </summary>
    private void EorImm()
    {
        var (_, val, _) = Imm();
        ExecuteEor(val);
    }

    /// <summary>
    /// EorZp - Exclusive OR, Zero Page.
    /// Opcode: 0x45, Tryb: Zero Page, Cykle: 3
    /// </summary>
    private void EorZp()
    {
        var (_, val, _) = Zp();
        ExecuteEor(val);
    }

    /// <summary>
    /// EorZpX - Exclusive OR, Zero Page, X.
    /// Opcode: 0x55, Tryb: Zero Page,X, Cykle: 4
    /// </summary>
    private void EorZpX()
    {
        var (_, val, _) = ZpX();
        ExecuteEor(val);
    }

    /// <summary>
    /// EorAbs - Exclusive OR, Absolute.
    /// Opcode: 0x4D, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void EorAbs()
    {
        var (_, val, _) = Abs();
        ExecuteEor(val);
    }

    /// <summary>
    /// EorAbsX - Exclusive OR, Absolute, X.
    /// Opcode: 0x5D, Tryb: Absolute,X, Cykle: 4 + page crossing
    /// </summary>
    private void EorAbsX()
    {
        var (_, val, _) = AbsX();
        ExecuteEor(val);
    }

    /// <summary>
    /// EorAbsY - Exclusive OR, Absolute, Y.
    /// Opcode: 0x59, Tryb: Absolute,Y, Cykle: 4 + page crossing
    /// </summary>
    private void EorAbsY()
    {
        var (_, val, _) = AbsY();
        ExecuteEor(val);
    }

    /// <summary>
    /// EorIndX - Exclusive OR, Indirect X.
    /// Opcode: 0x41, Tryb: (Indirect,X), Cykle: 6
    /// </summary>
    private void EorIndX()
    {
        var (_, val, _) = IndX();
        ExecuteEor(val);
    }

    /// <summary>
    /// EorIndY - Exclusive OR, Indirect Y.
    /// Opcode: 0x51, Tryb: (Indirect),Y, Cykle: 5 + page crossing
    /// </summary>
    private void EorIndY()
    {
        var (_, val, _) = IndY();
        ExecuteEor(val);
    }

    #endregion

    #region Wspólne metody AND/OR/EOR

    /// <summary>
    /// ExecuteAnd - Wykonuje operację AND z operandem.
    /// </summary>
    /// <param name="operand">Operand z pamięci.</param>
    private void ExecuteAnd(byte operand)
    {
        _a &= operand;
        SetNZ(_a);
    }

    /// <summary>
    /// ExecuteOra - Wykonuje operację ORA z operandem.
    /// </summary>
    /// <param name="operand">Operand z pamięci.</param>
    private void ExecuteOra(byte operand)
    {
        _a |= operand;
        SetNZ(_a);
    }

    /// <summary>
    /// ExecuteEor - Wykonuje operację EOR z operandem.
    /// </summary>
    /// <param name="operand">Operand z pamięci.</param>
    private void ExecuteEor(byte operand)
    {
        _a ^= operand;
        SetNZ(_a);
    }

    #endregion
}
