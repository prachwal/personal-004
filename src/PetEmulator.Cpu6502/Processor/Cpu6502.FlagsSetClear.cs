namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje ustawiania i czyszczenia flag

    /// <summary>
    /// CLC - Clear Carry.
    /// Czyści flagę Carry (C=0). Inne flagi pozostają bez zmian.
    /// Opcode: 0x18, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Clc()
    {
        SetFlag(FlagC, false);
    }

    /// <summary>
    /// SEC - Set Carry.
    /// Ustawia flagę Carry (C=1). Inne flagi pozostają bez zmian.
    /// Opcode: 0x38, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Sec()
    {
        SetFlag(FlagC, true);
    }

    /// <summary>
    /// CLD - Clear Decimal.
    /// Czyści flagę Decimal (D=0). Inne flagi pozostają bez zmian.
    /// Opcode: 0xD8, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Cld()
    {
        SetFlag(FlagD, false);
    }

    /// <summary>
    /// SED - Set Decimal.
    /// Ustawia flagę Decimal (D=1). Inne flagi pozostają bez zmian.
    /// Opcode: 0xF8, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Sed()
    {
        SetFlag(FlagD, true);
    }

    /// <summary>
    /// CLI - Clear Interrupt Disable.
    /// Czyści flagę Interrupt Disable (I=0). Inne flagi pozostają bez zmian.
    /// Opcode: 0x58, Tryb: Implied, Cykle: 2
    /// Uwaga: IRQ jest opóźnione o 1 instrukcję po CLI.
    /// </summary>
    private void Cli()
    {
        SetFlag(FlagI, false);
        _interruptDelay = true;
    }

    /// <summary>
    /// SEI - Set Interrupt Disable.
    /// Ustawia flagę Interrupt Disable (I=1). Inne flagi pozostają bez zmian.
    /// Opcode: 0x78, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Sei()
    {
        SetFlag(FlagI, true);
    }

    /// <summary>
    /// CLV - Clear Overflow.
    /// Czyści flagę Overflow (V=0). Inne flagi pozostają bez zmian.
    /// Opcode: 0xB8, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Clv()
    {
        SetFlag(FlagV, false);
    }

    #endregion
}
