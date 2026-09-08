namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region JMP Absolute (3 cykle)

    /// <summary>
    /// JMP Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void JmpAbs_Cycle0()
    {
        _tempAddr = _memory.Read(_pc++);
    }

    /// <summary>
    /// JMP Absolute - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void JmpAbs_Cycle1()
    {
        _tempAddr |= (ushort)(_memory.Read(_pc++) << 8);
    }

    /// <summary>
    /// JMP Absolute - Cykl 2: Skok
    /// </summary>
    private void JmpAbs_Cycle2()
    {
        _pc = _tempAddr;
        _sync = true;
    }

    #endregion

    #region JMP Indirect (5 cykli z bugiem)

    /// <summary>
    /// JMP Indirect - Cykl 0: Fetch low byte adresu pośredniego
    /// </summary>
    private void JmpInd_Cycle0()
    {
        _tempAddr = _memory.Read(_pc++);
    }

    /// <summary>
    /// JMP Indirect - Cykl 1: Fetch high byte adresu pośredniego
    /// </summary>
    private void JmpInd_Cycle1()
    {
        _tempAddr |= (ushort)(_memory.Read(_pc++) << 8);
    }

    /// <summary>
    /// JMP Indirect - Cykl 2: Odczyt low byte adresu docelowego
    /// </summary>
    private void JmpInd_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// JMP Indirect - Cykl 3: Odczyt high byte adresu docelowego (z bugiem)
    /// </summary>
    private void JmpInd_Cycle3()
    {
        // NMOS 6502 bug: jeśli adres pośredni kończy się na $xxFF,
        // high byte czytany z $xx00 zamiast $(xx+1)00
        ushort hiAddr;
        if ((_tempAddr & 0xFF) == 0xFF)
        {
            hiAddr = (ushort)(_tempAddr & 0xFF00);  // ta sama strona
        }
        else
        {
            hiAddr = (ushort)(_tempAddr + 1);
        }
        byte hi = _memory.Read(hiAddr);
        _tempAddr = (ushort)((hi << 8) | _tempValue);  // Zapamiętaj nowy PC w _tempAddr
    }

    /// <summary>
    /// JMP Indirect - Cykl 4: Ustaw PC i sync
    /// </summary>
    private void JmpInd_Cycle4()
    {
        _pc = _tempAddr;
        _sync = true;
    }

    #endregion

    #region JSR Absolute (6 cykli)

    /// <summary>
    /// JSR Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void JsrAbs_Cycle0()
    {
        _tempAddr = _memory.Read(_pc++);
    }

    /// <summary>
    /// JSR Absolute - Cykl 1: Push PCH
    /// </summary>
    private void JsrAbs_Cycle1()
    {
        // Internal cycle. The return address is not complete until the high operand byte is fetched.
    }

    /// <summary>
    /// JSR Absolute - Cykl 2: Fetch high byte adresu
    /// </summary>
    private void JsrAbs_Cycle2()
    {
        _tempAddr |= (ushort)(_memory.Read(_pc++) << 8);
    }

    /// <summary>
    /// JSR Absolute - Cykl 3: Push PCL
    /// </summary>
    private void JsrAbs_Cycle3()
    {
        ushort returnAddress = (ushort)(_pc - 1);
        Push((byte)(returnAddress >> 8));
    }

    /// <summary>
    /// JSR Absolute - Cykl 4: Push PCL i przygotuj PC
    /// </summary>
    private void JsrAbs_Cycle4()
    {
        ushort returnAddress = (ushort)(_pc - 1);
        Push((byte)(returnAddress & 0xFF));
        _pc = _tempAddr;
    }

    /// <summary>
    /// JSR Absolute - Cykl 5: Sync
    /// </summary>
    private void JsrAbs_Cycle5()
    {
        _sync = true;
    }

    #endregion

    #region RTS (6 cykli)

    /// <summary>
    /// RTS - Cykl 0: Pull PCL
    /// </summary>
    private void Rts_Cycle0()
    {
        _tempAddr = Pop();
    }

    /// <summary>
    /// RTS - Cykl 1: Pull PCH
    /// </summary>
    private void Rts_Cycle1()
    {
        _tempAddr |= (ushort)(Pop() << 8);
    }

    /// <summary>
    /// RTS - Cykl 2: Przygotuj PC
    /// </summary>
    private void Rts_Cycle2()
    {
        _pc = _tempAddr;
    }

    /// <summary>
    /// RTS - Cykl 3: Inkrementuj PC
    /// </summary>
    private void Rts_Cycle3()
    {
        _pc++;
    }

    /// <summary>
    /// RTS - Cykl 4: ostatni wewnętrzny cykl przed synchronizacją
    /// </summary>
    private void Rts_Cycle4()
    {
    }

    /// <summary>
    /// RTS - Cykl 5: Sync
    /// </summary>
    private void Rts_Cycle5()
    {
        _sync = true;
    }

    #endregion

    #region BIT Zero Page (3 cykle)

    /// <summary>
    /// BIT Zero Page - Cykl 0: Fetch adresu
    /// </summary>
    private void BitZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    /// <summary>
    /// BIT Zero Page - Cykl 1: Odczyt wartości
    /// </summary>
    private void BitZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// BIT Zero Page - Cykl 2: Wykonaj BIT i sync
    /// </summary>
    private void BitZp_Cycle2()
    {
        byte result = (byte)(_a & _tempValue);
        SetFlag(FlagZ, result == 0);
        SetFlag(FlagN, (_tempValue & 0x80) != 0);
        SetFlag(FlagV, (_tempValue & 0x40) != 0);
        _sync = true;
    }

    #endregion

    #region BIT Absolute (4 cykle)

    /// <summary>
    /// BIT Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void BitAbs_Cycle0()
    {
        _tempAddr = _memory.Read(_pc++);
    }

    /// <summary>
    /// BIT Absolute - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void BitAbs_Cycle1()
    {
        _tempAddr |= (ushort)(_memory.Read(_pc++) << 8);
    }

    /// <summary>
    /// BIT Absolute - Cykl 2: Odczyt wartości
    /// </summary>
    private void BitAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// BIT Absolute - Cykl 3: Wykonaj BIT i sync
    /// </summary>
    private void BitAbs_Cycle3()
    {
        byte result = (byte)(_a & _tempValue);
        SetFlag(FlagZ, result == 0);
        SetFlag(FlagN, (_tempValue & 0x80) != 0);
        SetFlag(FlagV, (_tempValue & 0x40) != 0);
        _sync = true;
    }

    #endregion
}
