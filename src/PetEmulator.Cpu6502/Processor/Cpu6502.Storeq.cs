namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje STZ (Store Zero - 65C02)

    /// <summary>
    /// StzZp - Store Zero, Zero Page.
    /// Opcode: 0x64, Tryb: Zero Page, Cykle: 3
    /// Stores 0 into the addressed location. No flags affected.
    /// </summary>
    private void StzZp()
    {
        ushort addr = AddrZp();
        _memory.Write(addr, 0);
    }

    /// <summary>
    /// StzZpX - Store Zero, Zero Page, X.
    /// Opcode: 0x74, Tryb: Zero Page,X, Cykle: 4
    /// </summary>
    private void StzZpX()
    {
        ushort addr = AddrZpX();
        _memory.Write(addr, 0);
    }

    /// <summary>
    /// StzAbs - Store Zero, Absolute.
    /// Opcode: 0x9C, Tryb: Absolute, Cykle: 4
    /// </summary>
    private void StzAbs()
    {
        ushort addr = AddrAbs();
        _memory.Write(addr, 0);
    }

    /// <summary>
    /// StzAbsX - Store Zero, Absolute, X.
    /// Opcode: 0x9E, Tryb: Absolute,X, Cykle: 5
    /// </summary>
    private void StzAbsX()
    {
        ushort addr = AddrAbsX(out _);
        _memory.Write(addr, 0);
    }

    #endregion
}
