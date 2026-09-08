namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje INC (Increment Memory)

    /// <summary>
    /// IncZp - Increment Memory, Zero Page.
    /// Opcode: 0xE6, Tryb: Zero Page, Cykle: 5
    /// </summary>
    private void IncZp()
    {
        var (addr, _, _) = Zp();
        var value = _memory.Read(addr);
        var result = (byte)(value + 1);
        _memory.Write(addr, result);
        SetNZ(result);
    }

    /// <summary>
    /// IncZpX - Increment Memory, Zero Page, X.
    /// Opcode: 0xF6, Tryb: Zero Page,X, Cykle: 6
    /// </summary>
    private void IncZpX()
    {
        var (addr, _, _) = ZpX();
        var value = _memory.Read(addr);
        var result = (byte)(value + 1);
        _memory.Write(addr, result);
        SetNZ(result);
    }

    /// <summary>
    /// IncAbs - Increment Memory, Absolute.
    /// Opcode: 0xEE, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void IncAbs()
    {
        var (addr, _, _) = Abs();
        var value = _memory.Read(addr);
        var result = (byte)(value + 1);
        _memory.Write(addr, result);
        SetNZ(result);
    }

    /// <summary>
    /// IncAbsX - Increment Memory, Absolute, X.
    /// Opcode: 0xFE, Tryb: Absolute,X, Cykle: 7
    /// </summary>
    private void IncAbsX()
    {
        var (addr, _, _) = AbsX();
        var value = _memory.Read(addr);
        var result = (byte)(value + 1);
        _memory.Write(addr, result);
        SetNZ(result);
    }

    #endregion

    #region Instrukcje DEC (Decrement Memory)

    /// <summary>
    /// DecZp - Decrement Memory, Zero Page.
    /// Opcode: 0xC6, Tryb: Zero Page, Cykle: 5
    /// </summary>
    private void DecZp()
    {
        var (addr, _, _) = Zp();
        var value = _memory.Read(addr);
        var result = (byte)(value - 1);
        _memory.Write(addr, result);
        SetNZ(result);
    }

    /// <summary>
    /// DecZpX - Decrement Memory, Zero Page, X.
    /// Opcode: 0xD6, Tryb: Zero Page,X, Cykle: 6
    /// </summary>
    private void DecZpX()
    {
        var (addr, _, _) = ZpX();
        var value = _memory.Read(addr);
        var result = (byte)(value - 1);
        _memory.Write(addr, result);
        SetNZ(result);
    }

    /// <summary>
    /// DecAbs - Decrement Memory, Absolute.
    /// Opcode: 0xCE, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void DecAbs()
    {
        var (addr, _, _) = Abs();
        var value = _memory.Read(addr);
        var result = (byte)(value - 1);
        _memory.Write(addr, result);
        SetNZ(result);
    }

    /// <summary>
    /// DecAbsX - Decrement Memory, Absolute, X.
    /// Opcode: 0xDE, Tryb: Absolute,X, Cykle: 7
    /// </summary>
    private void DecAbsX()
    {
        var (addr, _, _) = AbsX();
        var value = _memory.Read(addr);
        var result = (byte)(value - 1);
        _memory.Write(addr, result);
        SetNZ(result);
    }

    #endregion

    #region Instrukcje INX, INY, DEX, DEY (Register Increment/Decrement)

    /// <summary>
    /// Inx - Increment X Register.
    /// Opcode: 0xE8, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Inx()
    {
        _x = (byte)(_x + 1);
        SetNZ(_x);
    }

    /// <summary>
    /// Iny - Increment Y Register.
    /// Opcode: 0xC8, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Iny()
    {
        _y = (byte)(_y + 1);
        SetNZ(_y);
    }

    /// <summary>
    /// Dex - Decrement X Register.
    /// Opcode: 0xCA, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Dex()
    {
        _x = (byte)(_x - 1);
        SetNZ(_x);
    }

    /// <summary>
    /// Dey - Decrement Y Register.
    /// Opcode: 0x88, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Dey()
    {
        _y = (byte)(_y - 1);
        SetNZ(_y);
    }

    #endregion
}
