namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje ASL (Arithmetic Shift Left)

    /// <summary>
    /// AslAcc - Arithmetic Shift Left, Accumulator.
    /// Opcode: 0x0A, Tryb: Accumulator, Cykle: 2
    /// </summary>
    private void AslAcc()
    {
        _a = ExecuteAsl(_a);
    }

    /// <summary>
    /// AslZp - Arithmetic Shift Left, Zero Page.
    /// Opcode: 0x06, Tryb: Zero Page, Cykle: 5
    /// </summary>
    private void AslZp()
    {
        var (addr, _, _) = Zp();
        var value = _memory.Read(addr);
        var result = ExecuteAsl(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// AslZpX - Arithmetic Shift Left, Zero Page, X.
    /// Opcode: 0x16, Tryb: Zero Page,X, Cykle: 6
    /// </summary>
    private void AslZpX()
    {
        var (addr, _, _) = ZpX();
        var value = _memory.Read(addr);
        var result = ExecuteAsl(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// AslAbs - Arithmetic Shift Left, Absolute.
    /// Opcode: 0x0E, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void AslAbs()
    {
        var (addr, _, _) = Abs();
        var value = _memory.Read(addr);
        var result = ExecuteAsl(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// AslAbsX - Arithmetic Shift Left, Absolute, X.
    /// Opcode: 0x1E, Tryb: Absolute,X, Cykle: 7
    /// </summary>
    private void AslAbsX()
    {
        var (addr, _, _) = AbsX();
        var value = _memory.Read(addr);
        var result = ExecuteAsl(value);
        _memory.Write(addr, result);
    }

    #endregion

    #region Instrukcje LSR (Logical Shift Right)

    /// <summary>
    /// LsrAcc - Logical Shift Right, Accumulator.
    /// Opcode: 0x4A, Tryb: Accumulator, Cykle: 2
    /// </summary>
    private void LsrAcc()
    {
        _a = ExecuteLsr(_a);
    }

    /// <summary>
    /// LsrZp - Logical Shift Right, Zero Page.
    /// Opcode: 0x46, Tryb: Zero Page, Cykle: 5
    /// </summary>
    private void LsrZp()
    {
        var (addr, _, _) = Zp();
        var value = _memory.Read(addr);
        var result = ExecuteLsr(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// LsrZpX - Logical Shift Right, Zero Page, X.
    /// Opcode: 0x56, Tryb: Zero Page,X, Cykle: 6
    /// </summary>
    private void LsrZpX()
    {
        var (addr, _, _) = ZpX();
        var value = _memory.Read(addr);
        var result = ExecuteLsr(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// LsrAbs - Logical Shift Right, Absolute.
    /// Opcode: 0x4E, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void LsrAbs()
    {
        var (addr, _, _) = Abs();
        var value = _memory.Read(addr);
        var result = ExecuteLsr(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// LsrAbsX - Logical Shift Right, Absolute, X.
    /// Opcode: 0x5E, Tryb: Absolute,X, Cykle: 7
    /// </summary>
    private void LsrAbsX()
    {
        var (addr, _, _) = AbsX();
        var value = _memory.Read(addr);
        var result = ExecuteLsr(value);
        _memory.Write(addr, result);
    }

    #endregion

    #region Instrukcje ROL (Rotate Left)

    /// <summary>
    /// RolAcc - Rotate Left, Accumulator.
    /// Opcode: 0x2A, Tryb: Accumulator, Cykle: 2
    /// </summary>
    private void RolAcc()
    {
        _a = ExecuteRol(_a);
    }

    /// <summary>
    /// RolZp - Rotate Left, Zero Page.
    /// Opcode: 0x26, Tryb: Zero Page, Cykle: 5
    /// </summary>
    private void RolZp()
    {
        var (addr, _, _) = Zp();
        var value = _memory.Read(addr);
        var result = ExecuteRol(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// RolZpX - Rotate Left, Zero Page, X.
    /// Opcode: 0x36, Tryb: Zero Page,X, Cykle: 6
    /// </summary>
    private void RolZpX()
    {
        var (addr, _, _) = ZpX();
        var value = _memory.Read(addr);
        var result = ExecuteRol(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// RolAbs - Rotate Left, Absolute.
    /// Opcode: 0x2E, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void RolAbs()
    {
        var (addr, _, _) = Abs();
        var value = _memory.Read(addr);
        var result = ExecuteRol(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// RolAbsX - Rotate Left, Absolute, X.
    /// Opcode: 0x3E, Tryb: Absolute,X, Cykle: 7
    /// </summary>
    private void RolAbsX()
    {
        var (addr, _, _) = AbsX();
        var value = _memory.Read(addr);
        var result = ExecuteRol(value);
        _memory.Write(addr, result);
    }

    #endregion

    #region Instrukcje ROR (Rotate Right)

    /// <summary>
    /// RorAcc - Rotate Right, Accumulator.
    /// Opcode: 0x6A, Tryb: Accumulator, Cykle: 2
    /// </summary>
    private void RorAcc()
    {
        _a = ExecuteRor(_a);
    }

    /// <summary>
    /// RorZp - Rotate Right, Zero Page.
    /// Opcode: 0x66, Tryb: Zero Page, Cykle: 5
    /// </summary>
    private void RorZp()
    {
        var (addr, _, _) = Zp();
        var value = _memory.Read(addr);
        var result = ExecuteRor(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// RorZpX - Rotate Right, Zero Page, X.
    /// Opcode: 0x76, Tryb: Zero Page,X, Cykle: 6
    /// </summary>
    private void RorZpX()
    {
        var (addr, _, _) = ZpX();
        var value = _memory.Read(addr);
        var result = ExecuteRor(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// RorAbs - Rotate Right, Absolute.
    /// Opcode: 0x6E, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void RorAbs()
    {
        var (addr, _, _) = Abs();
        var value = _memory.Read(addr);
        var result = ExecuteRor(value);
        _memory.Write(addr, result);
    }

    /// <summary>
    /// RorAbsX - Rotate Right, Absolute, X.
    /// Opcode: 0x7E, Tryb: Absolute,X, Cykle: 7
    /// </summary>
    private void RorAbsX()
    {
        var (addr, _, _) = AbsX();
        var value = _memory.Read(addr);
        var result = ExecuteRor(value);
        _memory.Write(addr, result);
    }

    #endregion

    #region Wspólne metody ASL/LSR/ROL/ROR

    /// <summary>
    /// ExecuteAsl - Wykonuje ASL (Arithmetic Shift Left).
    /// C ← [7 6 5 4 3 2 1 0] ← 0
    /// </summary>
    /// <param name="value">Wartość do przesunięcia.</param>
    /// <returns>Wynik przesunięcia.</returns>
    private byte ExecuteAsl(byte value)
    {
        bool carry = (value & 0x80) != 0;
        byte result = (byte)(value << 1);
        SetFlag(FlagC, carry);
        SetNZ(result);
        return result;
    }

    /// <summary>
    /// ExecuteLsr - Wykonuje LSR (Logical Shift Right).
    /// 0 → [7 6 5 4 3 2 1 0] → C
    /// </summary>
    /// <param name="value">Wartość do przesunięcia.</param>
    /// <returns>Wynik przesunięcia.</returns>
    private byte ExecuteLsr(byte value)
    {
        bool carry = (value & 0x01) != 0;
        byte result = (byte)(value >> 1);
        SetFlag(FlagC, carry);
        SetNZ(result);
        return result;
    }

    /// <summary>
    /// ExecuteRol - Wykonuje ROL (Rotate Left).
    /// C ← [7 6 5 4 3 2 1 0] ← C
    /// </summary>
    /// <param name="value">Wartość do rotacji.</param>
    /// <returns>Wynik rotacji.</returns>
    private byte ExecuteRol(byte value)
    {
        bool newBit0 = (_p & FlagC) != 0;  // old carry → bit 0
        bool carry = (value & 0x80) != 0;   // bit 7 → new carry
        byte result = (byte)((uint)(value << 1) | (newBit0 ? 1u : 0u));
        SetFlag(FlagC, carry);
        SetNZ(result);
        return result;
    }

    /// <summary>
    /// ExecuteRor - Wykonuje ROR (Rotate Right).
    /// C → [7 6 5 4 3 2 1 0] → C
    /// </summary>
    /// <param name="value">Wartość do rotacji.</param>
    /// <returns>Wynik rotacji.</returns>
    private byte ExecuteRor(byte value)
    {
        bool newBit7 = (_p & FlagC) != 0;  // old carry → bit 7
        bool carry = (value & 0x01) != 0;   // bit 0 → new carry
        byte result = (byte)((uint)(value >> 1) | (newBit7 ? 0x80u : 0u));
        SetFlag(FlagC, carry);
        SetNZ(result);
        return result;
    }

    #endregion
}
