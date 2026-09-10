namespace PetEmulator.Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Instrukcje JMP (Jump)

    /// <summary>
    /// JmpAbs - Jump Absolute.
    /// Opcode: 0x4C, Tryb: Absolute, Cykle: 3
    /// </summary>
    private void JmpAbs()
    {
        byte lo = _memory.Read(_pc++);
        byte hi = _memory.Read(_pc);
        _pc = (ushort)((hi << 8) | lo);
    }

    /// <summary>
    /// JmpInd - Jump Indirect.
    /// Opcode: 0x6C, Tryb: Indirect, Cykle: 5
    /// Uwaga: Implementacja NMOS bug - gdy adres ma $xxFF, high byte czytany z $xx00 zamiast $(xx+1)00.
    /// </summary>
    private void JmpInd()
    {
        _pc = AddrIndirectAbs();
    }

    #endregion

    #region Instrukcje JSR/RTS (Subroutine Call/Return)

    /// <summary>
    /// JsrAbs - Jump to Subroutine, Absolute.
    /// Opcode: 0x20, Tryb: Absolute, Cykle: 6
    /// </summary>
    private void JsrAbs()
    {
        // Push return address (PC+2)
        ushort returnAddr = (ushort)(_pc + 2);
        Push((byte)(returnAddr >> 8));
        Push((byte)(returnAddr & 0xFF));

        // Jump to subroutine
        byte lo = _memory.Read(_pc++);
        byte hi = _memory.Read(_pc);
        _pc = (ushort)((hi << 8) | lo);
    }

    /// <summary>
    /// Rts - Return from Subroutine.
    /// Opcode: 0x60, Tryb: Implied, Cykle: 6
    /// </summary>
    private void Rts()
    {
        byte lo = Pop();
        byte hi = Pop();
        _pc = (ushort)((hi << 8) | lo);
        _pc++; // RTS dodaje 1 do adresu powrotu
    }

    #endregion

    #region Instrukcje BCC/BCS/BEQ/BMI/BNE/BPL/BVC/BVS (Branch)

    /// <summary>
    /// BccRel - Branch if Carry Clear.
    /// Opcode: 0x90, Tryb: Relative, Cykle: 2 (not taken), 3 (taken same page), 4 (taken diff page)
    /// </summary>
    private void BccRel()
    {
        ExecuteBranch(!GetFlag(FlagC));
    }

    /// <summary>
    /// BcsRel - Branch if Carry Set.
    /// Opcode: 0xB0, Tryb: Relative, Cykle: 2 (not taken), 3 (taken same page), 4 (taken diff page)
    /// </summary>
    private void BcsRel()
    {
        ExecuteBranch(GetFlag(FlagC));
    }

    /// <summary>
    /// BeqRel - Branch if Equal (Z=1).
    /// Opcode: 0xF0, Tryb: Relative, Cykle: 2 (not taken), 3 (taken same page), 4 (taken diff page)
    /// </summary>
    private void BeqRel()
    {
        ExecuteBranch(GetFlag(FlagZ));
    }

    /// <summary>
    /// BmiRel - Branch if Minus (N=1).
    /// Opcode: 0x30, Tryb: Relative, Cykle: 2 (not taken), 3 (taken same page), 4 (taken diff page)
    /// </summary>
    private void BmiRel()
    {
        ExecuteBranch(GetFlag(FlagN));
    }

    /// <summary>
    /// BneRel - Branch if Not Equal (Z=0).
    /// Opcode: 0xD0, Tryb: Relative, Cykle: 2 (not taken), 3 (taken same page), 4 (taken diff page)
    /// </summary>
    private void BneRel()
    {
        ExecuteBranch(!GetFlag(FlagZ));
    }

    /// <summary>
    /// BplRel - Branch if Plus (N=0).
    /// Opcode: 0x10, Tryb: Relative, Cykle: 2 (not taken), 3 (taken same page), 4 (taken diff page)
    /// </summary>
    private void BplRel()
    {
        ExecuteBranch(!GetFlag(FlagN));
    }

    /// <summary>
    /// BvcRel - Branch if Overflow Clear (V=0).
    /// Opcode: 0x50, Tryb: Relative, Cykle: 2 (not taken), 3 (taken same page), 4 (taken diff page)
    /// </summary>
    private void BvcRel()
    {
        ExecuteBranch(!GetFlag(FlagV));
    }

    /// <summary>
    /// BvsRel - Branch if Overflow Set (V=1).
    /// Opcode: 0x70, Tryb: Relative, Cykle: 2 (not taken), 3 (taken same page), 4 (taken diff page)
    /// </summary>
    private void BvsRel()
    {
        ExecuteBranch(GetFlag(FlagV));
    }

    #endregion

    #region Wspólne metody dla branch

    /// <summary>
    /// ExecuteBranch - Wykonuje rozgałęzienie względne.
    /// </summary>
    /// <param name="condition">Warunek rozgałęzienia.</param>
    private void ExecuteBranch(bool condition)
    {
        sbyte offset = (sbyte)_memory.Read(_pc);
        _pc++;

        if (condition)
        {
            ushort oldPc = _pc;
            _pc = (ushort)(oldPc + offset);

            // Sprawdź przekroczenie strony
            bool pageCrossed = (oldPc & 0xFF00) != (_pc & 0xFF00);
            if (pageCrossed)
            {
                // Dodatkowy cykl za przekroczenie strony
            }
        }
    }

    #endregion
}
