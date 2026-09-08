namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje ADC (Add with Carry) - binarny tryb

    /// <summary>
    /// AdcImm - Add with Carry, Immediate.
    /// Opcode: 0x69, Tryb: Immediate, Cykle: 2
    /// </summary>
    private void AdcImm()
    {
        var (_, val, _) = Imm();
        ExecuteAdc(val);
    }

    /// <summary>
    /// AdcZp - Add with Carry, Zero Page.
    /// Opcode: 0x65, Tryb: Zero Page, Cykle: 3
    /// </summary>
    private void AdcZp()
    {
        var (_, val, _) = Zp();
        ExecuteAdc(val);
    }

    /// <summary>
    /// AdcZpX - Add with Carry, Zero Page, X.
    /// Opcode: 0x75, Tryb: Zero Page,X, Cykle: 4
    /// </summary>
    private void AdcZpX()
    {
        var (_, val, _) = ZpX();
        ExecuteAdc(val);
    }

    /// <summary>
    /// AdcAbs - Add with Carry, Absolute.
    /// Opcode: 0x6D, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void AdcAbs()
    {
        var (_, val, _) = Abs();
        ExecuteAdc(val);
    }

    /// <summary>
    /// AdcAbsX - Add with Carry, Absolute, X.
    /// Opcode: 0x7D, Tryb: Absolute,X, Cykle: 4 + page crossing
    /// </summary>
    private void AdcAbsX()
    {
        var (_, val, _) = AbsX();
        ExecuteAdc(val);
    }

    /// <summary>
    /// AdcAbsY - Add with Carry, Absolute, Y.
    /// Opcode: 0x79, Tryb: Absolute,Y, Cykle: 4 + page crossing
    /// </summary>
    private void AdcAbsY()
    {
        var (_, val, _) = AbsY();
        ExecuteAdc(val);
    }

    /// <summary>
    /// AdcIndX - Add with Carry, Indirect X.
    /// Opcode: 0x61, Tryb: (Indirect,X), Cykle: 6
    /// </summary>
    private void AdcIndX()
    {
        var (_, val, _) = IndX();
        ExecuteAdc(val);
    }

    /// <summary>
    /// AdcIndY - Add with Carry, Indirect Y.
    /// Opcode: 0x71, Tryb: (Indirect),Y, Cykle: 5 + page crossing
    /// </summary>
    private void AdcIndY()
    {
        var (_, val, _) = IndY();
        ExecuteAdc(val);
    }

    #endregion

    #region Instrukcje SBC (Subtract with Carry) - binarny tryb

    /// <summary>
    /// SbcImm - Subtract with Carry, Immediate.
    /// Opcode: 0xE9, Tryb: Immediate, Cykle: 2
    /// </summary>
    private void SbcImm()
    {
        var (_, val, _) = Imm();
        ExecuteSbc(val);
    }

    /// <summary>
    /// SbcZp - Subtract with Carry, Zero Page.
    /// Opcode: 0xE5, Tryb: Zero Page, Cykle: 3
    /// </summary>
    private void SbcZp()
    {
        var (_, val, _) = Zp();
        ExecuteSbc(val);
    }

    /// <summary>
    /// SbcZpX - Subtract with Carry, Zero Page, X.
    /// Opcode: 0xF5, Tryb: Zero Page,X, Cykle: 4
    /// </summary>
    private void SbcZpX()
    {
        var (_, val, _) = ZpX();
        ExecuteSbc(val);
    }

    /// <summary>
    /// SbcAbs - Subtract with Carry, Absolute.
    /// Opcode: 0xED, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void SbcAbs()
    {
        var (_, val, _) = Abs();
        ExecuteSbc(val);
    }

    /// <summary>
    /// SbcAbsX - Subtract with Carry, Absolute, X.
    /// Opcode: 0xFD, Tryb: Absolute,X, Cykle: 4 + page crossing
    /// </summary>
    private void SbcAbsX()
    {
        var (_, val, _) = AbsX();
        ExecuteSbc(val);
    }

    /// <summary>
    /// SbcAbsY - Subtract with Carry, Absolute, Y.
    /// Opcode: 0xF9, Tryb: Absolute,Y, Cykle: 4 + page crossing
    /// </summary>
    private void SbcAbsY()
    {
        var (_, val, _) = AbsY();
        ExecuteSbc(val);
    }

    /// <summary>
    /// SbcIndX - Subtract with Carry, Indirect X.
    /// Opcode: 0xE1, Tryb: (Indirect,X), Cykle: 6
    /// </summary>
    private void SbcIndX()
    {
        var (_, val, _) = IndX();
        ExecuteSbc(val);
    }

    /// <summary>
    /// SbcIndY - Subtract with Carry, Indirect Y.
    /// Opcode: 0xF1, Tryb: (Indirect),Y, Cykle: 5 + page crossing
    /// </summary>
    private void SbcIndY()
    {
        var (_, val, _) = IndY();
        ExecuteSbc(val);
    }

    #endregion

    #region Wspólne metody ADC/SBC

    /// <summary>
    /// ExecuteAdc - Wykonuje ADC z użyciem podanego operandu.
    /// ADC = A + M + C
    /// Ustawia flagi: N, Z, C, V.
    /// </summary>
    /// <param name="operand">Operand z pamięci.</param>
    private void ExecuteAdc(byte operand)
    {
        // Check BCD mode
        if (DecimalModeEnabled && (_p & FlagD) != 0)
        {
            ExecuteAdcBcd(operand);
            return;
        }

        // Binary mode ADC = A + M + C
        byte carry = (byte)((_p & FlagC) != 0 ? 1 : 0);
        ushort sum = (ushort)(_a + operand + carry);
        bool carryFlag = sum > 0xFF;
        byte result = (byte)(sum & 0xFF);

        // Overflow: (A^result) & (M^result) & 0x80
        bool overflow = ((A ^ result) & (operand ^ result) & 0x80) != 0;

        _a = result;
        SetFlag(FlagC, carryFlag);
        SetFlag(FlagZ, result == 0);
        SetFlag(FlagN, (result & 0x80) != 0);
        SetFlag(FlagV, overflow);
    }

    /// <summary>
    /// ExecuteSbc - Wykonuje SBC z użyciem podanego operandu.
    /// SBC = A - M - ~C = A + ~M + C
    /// Ustawia flagi: N, Z, C, V.
    /// </summary>
    /// <param name="operand">Operand z pamięci.</param>
    private void ExecuteSbc(byte operand)
    {
        // Check BCD mode
        if (DecimalModeEnabled && (_p & FlagD) != 0)
        {
            ExecuteSbcBcd(operand);
            return;
        }

        // Binary mode SBC = A + ~M + C
        byte carry = (byte)((_p & FlagC) != 0 ? 1 : 0);
        byte notOperand = (byte)~operand;
        ushort sum = (ushort)(_a + notOperand + carry);
        bool carryFlag = sum > 0xFF;
        byte result = (byte)(sum & 0xFF);

        // Overflow dla SBC: (A^result) & (~M^result) & 0x80
        bool overflow = ((A ^ result) & (notOperand ^ result) & 0x80) != 0;

        _a = result;
        SetFlag(FlagC, carryFlag);
        SetFlag(FlagZ, result == 0);
        SetFlag(FlagN, (result & 0x80) != 0);
        SetFlag(FlagV, overflow);
    }

    #endregion

    #region Illegal Opcodes - Arithmetic

    /// <summary>
    /// AncImm - AND + C ← bit7
    /// Opcode: 0x0B, Tryb: Immediate, Cykle: 2
    /// Flagi: N, Z, C
    /// </summary>
    private void AncImm()
    {
        byte val = _memory.Read(_pc++);
        _a &= val;
        SetNZ(_a);
        SetFlag(FlagC, (_a & 0x80) != 0);
    }

    /// <summary>
    /// AncImm2 - AND + C ← bit7 (alternate opcode)
    /// Opcode: 0x2B, Tryb: Immediate, Cykle: 2
    /// Flagi: N, Z, C
    /// </summary>
    private void AncImm2()
    {
        byte val = _memory.Read(_pc++);
        _a &= val;
        SetNZ(_a);
        SetFlag(FlagC, (_a & 0x80) != 0);
    }

    /// <summary>
    /// AlrImm - AND + LSR
    /// Opcode: 0x4B, Tryb: Immediate, Cykle: 2
    /// Flagi: C, N, Z
    /// </summary>
    private void AlrImm()
    {
        byte val = _memory.Read(_pc++);
        _a &= val;
        SetFlag(FlagC, (_a & 0x01) != 0);
        _a >>= 1;
        SetNZ(_a);
    }

    /// <summary>
    /// ArrImm - AND + ROR
    /// Opcode: 0x6B, Tryb: Immediate, Cykle: 2
    /// Flagi: C, V, N, Z
    /// </summary>
    private void ArrImm()
    {
        byte val = _memory.Read(_pc++);
        _a &= val;

        bool carryIn = (_p & FlagC) != 0;
        bool carryOut = (_a & 0x01) != 0;
        _a = (byte)((_a >> 1) | (carryIn ? 0x80 : 0x00));
        SetFlag(FlagC, carryOut);
        SetNZ(_a);

        // V = (A ^ (A >> 1)) & 0x40
        SetFlag(FlagV, ((_a ^ (_a >> 1)) & 0x40) != 0);
    }

    /// <summary>
    /// SbxImm - (A & X) - operand → X
    /// Opcode: 0xCB, Tryb: Immediate, Cykle: 2
    /// Flagi: N, Z, C
    /// </summary>
    private void SbxImm()
    {
        byte val = _memory.Read(_pc++);
        byte andVal = (byte)(_a & _x);
        int result = andVal - val;

        SetFlag(FlagC, result >= 0);
        _x = (byte)result;
        SetNZ(_x);
    }

    #endregion
}
