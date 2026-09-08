namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje TRB i TSB (Test Bits, Reset/Set - 65C02)

    /// <summary>
    /// TrbZp - Test Bits and Reset, Zero Page (65C02).
    /// Opcode: 0x14, Tryb: Zero Page, Cykle: 5
    /// Z = (A & M) == 0; M &= ~A; (RMW operation)
    /// </summary>
    private void TrbZp()
    {
        var (addr, _, _) = Zp();
        var value = _memory.Read(addr);
        byte result = (byte)(value & _a);
        SetFlag(FlagZ, result == 0);
        byte newValue = (byte)(value & ~_a);
        _memory.Write(addr, newValue);  // Dummy write then real write
        _memory.Write(addr, newValue);
    }

    /// <summary>
    /// TrbAbs - Test Bits and Reset, Absolute (65C02).
    /// Opcode: 0x1C, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void TrbAbs()
    {
        var (addr, _, _) = Abs();
        var value = _memory.Read(addr);
        byte result = (byte)(value & _a);
        SetFlag(FlagZ, result == 0);
        byte newValue = (byte)(value & ~_a);
        _memory.Write(addr, newValue);  // Dummy write then real write
        _memory.Write(addr, newValue);
    }

    /// <summary>
    /// TsbZp - Test Bits and Set, Zero Page (65C02).
    /// Opcode: 0x04, Tryb: Zero Page, Cykle: 5
    /// Z = (A & M) == 0; M |= A; (RMW operation)
    /// </summary>
    private void TsbZp()
    {
        var (addr, _, _) = Zp();
        var value = _memory.Read(addr);
        byte result = (byte)(value & _a);
        SetFlag(FlagZ, result == 0);
        byte newValue = (byte)(value | _a);
        _memory.Write(addr, newValue);  // Dummy write then real write
        _memory.Write(addr, newValue);
    }

    /// <summary>
    /// TsbAbs - Test Bits and Set, Absolute (65C02).
    /// Opcode: 0x0C, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void TsbAbs()
    {
        var (addr, _, _) = Abs();
        var value = _memory.Read(addr);
        byte result = (byte)(value & _a);
        SetFlag(FlagZ, result == 0);
        byte newValue = (byte)(value | _a);
        _memory.Write(addr, newValue);  // Dummy write then real write
        _memory.Write(addr, newValue);
    }

    #endregion
}
