namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Stałe flag

    /// <summary>
    /// Carry flag - flaga przeniesienia.
    /// </summary>
    public const byte FlagC = 0x01;

    /// <summary>
    /// Zero flag - flaga wyniku zerowego.
    /// </summary>
    public const byte FlagZ = 0x02;

    /// <summary>
    /// Interrupt disable flag - blokada przerwań.
    /// </summary>
    public const byte FlagI = 0x04;

    /// <summary>
    /// Decimal mode flag - tryb BCD.
    /// </summary>
    public const byte FlagD = 0x08;

    /// <summary>
    /// Break command flag - instrukcja BRK.
    /// </summary>
    public const byte FlagB = 0x10;

    /// <summary>
    /// Unused flag - zawsze ustawiona.
    /// </summary>
    public const byte FlagU = 0x20;

    /// <summary>
    /// Overflow flag - flaga przepełnienia.
    /// </summary>
    public const byte FlagV = 0x40;

    /// <summary>
    /// Negative flag - flaga wyniku ujemnego.
    /// </summary>
    public const byte FlagN = 0x80;

    #endregion

    #region Typy przerwań

    /// <summary>
    /// Typ przerwania.
    /// </summary>
    private enum InterruptType
    {
        IRQ,
        NMI,
        BRK
    }

    #endregion
}
