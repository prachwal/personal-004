namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje stosowe (PHA, PHP, PLA, PLP)

    /// <summary>
    /// Pha - Push Accumulator.
    /// Opcode: 0x48, Tryb: Implied, Cykle: 3
    /// </summary>
    private void Pha()
    {
        Push(_a);
    }

    /// <summary>
    /// Php - Push Processor Status.
    /// Opcode: 0x08, Tryb: Implied, Cykle: 3
    /// Pushes P with B=1 and bit5=1 (NMOS behavior).
    /// </summary>
    private void Php()
    {
        // PHP pushes P with B=1 and bit5=1
        byte pWithFlags = (byte)(_p | 0x30); // B=1 (bit4), bit5=1
        Push(pWithFlags);
    }

    /// <summary>
    /// Pla - Pull Accumulator.
    /// Opcode: 0x68, Tryb: Implied, Cykle: 4
    /// </summary>
    private void Pla()
    {
        _a = Pop();
        SetNZ(_a);
    }

    /// <summary>
    /// Plp - Pull Processor Status.
    /// Opcode: 0x28, Tryb: Implied, Cykle: 4
    /// </summary>
    private void Plp()
    {
        _p = (byte)((Pop() & ~FlagB) | FlagU);
    }

    /// <summary>
    /// Phx - Push X Register (65C02).
    /// Opcode: 0xDA, Tryb: Implied, Cykle: 3
    /// </summary>
    private void Phx()
    {
        Push(_x);
    }

    /// <summary>
    /// Phy - Push Y Register (65C02).
    /// Opcode: 0x5A, Tryb: Implied, Cykle: 3
    /// </summary>
    private void Phy()
    {
        Push(_y);
    }

    /// <summary>
    /// Plx - Pull X Register (65C02).
    /// Opcode: 0xFA, Tryb: Implied, Cykle: 4
    /// </summary>
    private void Plx()
    {
        _x = Pop();
        SetNZ(_x);
    }

    /// <summary>
    /// Ply - Pull Y Register (65C02).
    /// Opcode: 0x7A, Tryb: Implied, Cykle: 4
    /// </summary>
    private void Ply()
    {
        _y = Pop();
        SetNZ(_y);
    }

    #endregion

    #region Instrukcja NOP

    /// <summary>
    /// Nop - No Operation.
    /// Opcode: 0xEA, Tryb: Implied, Cykle: 2
    /// </summary>
    private void Nop()
    {
        // No operation - just waste cycles
    }

    #endregion
}
