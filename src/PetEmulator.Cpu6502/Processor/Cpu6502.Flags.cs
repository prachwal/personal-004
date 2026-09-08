namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Metody pomocnicze dla flag

    /// <summary>
    /// Sprawdza czy podana flaga jest ustawiona w rejestrze statusu.
    /// </summary>
    /// <param name="flag">Bit flagi do sprawdzenia.</param>
    /// <returns>True jeśli flaga jest ustawiona.</returns>
    public bool GetFlag(byte flag) => (_p & flag) != 0;

    /// <summary>
    /// Ustawia lub kasuje podaną flagę w rejestrze statusu.
    /// </summary>
    /// <param name="flag">Bit flagi do modyfikacji.</param>
    /// <param name="value">Wartość do ustawienia (true = ustaw, false = kasuj).</param>
    public void SetFlag(byte flag, bool value)
    {
        if (value)
            _p |= flag;
        else
            _p &= (byte)~flag;
    }

    /// <summary>
    /// Ustawia flagi Negative i Zero na podstawie wartości.
    /// Wykorzystywane przez instrukcje load i store.
    /// </summary>
    /// <param name="value">Wartość do sprawdzenia.</param>
    private void SetNZ(byte value)
    {
        _p = (byte)((_p & ~(FlagN | FlagZ)) | (value & FlagN) | (value == 0 ? FlagZ : 0));
    }

    #endregion
}
