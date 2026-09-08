namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje transferu między rejestrami

    /// <summary>
    /// TAX - Transfer Accumulator to X.
    /// Kopiuje wartość A do X i ustawia flagi N, Z na podstawie X.
    /// Opcode: 0xAA, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Tax()
    {
        _x = _a;
        SetNZ(_x);
    }

    /// <summary>
    /// TAY - Transfer Accumulator to Y.
    /// Kopiuje wartość A do Y i ustawia flagi N, Z na podstawie Y.
    /// Opcode: 0xA8, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Tay()
    {
        _y = _a;
        SetNZ(_y);
    }

    /// <summary>
    /// TSX - Transfer Stack Pointer to X.
    /// Kopiuje wartość SP do X i ustawia flagi N, Z na podstawie X.
    /// Opcode: 0xBA, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Tsx()
    {
        _x = _sp;
        SetNZ(_x);
    }

    /// <summary>
    /// TXA - Transfer X to Accumulator.
    /// Kopiuje wartość X do A i ustawia flagi N, Z na podstawie A.
    /// Opcode: 0x8A, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Txa()
    {
        _a = _x;
        SetNZ(_a);
    }

    /// <summary>
    /// TXS - Transfer X to Stack Pointer.
    /// Kopiuje wartość X do SP bez modyfikacji flag.
    /// Opcode: 0x9A, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Txs()
    {
        _sp = _x;
    }

    /// <summary>
    /// TYA - Transfer Y to Accumulator.
    /// Kopiuje wartość Y do A i ustawia flagi N, Z na podstawie A.
    /// Opcode: 0x98, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Tya()
    {
        _a = _y;
        SetNZ(_a);
    }

    #endregion
}
