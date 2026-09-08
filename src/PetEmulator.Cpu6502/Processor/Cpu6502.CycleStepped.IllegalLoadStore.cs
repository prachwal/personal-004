namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region LAX - LDA + LDX (Illegal Opcode)

    /// <summary>
    /// LAX Zero Page - Cykl 0: Fetch adresu i wartości
    /// </summary>
    private void LaxZp_Cycle0()
    {
        _tempAddr = AddrZp();
        _tempValue = _memory.Read(_tempAddr);
        LaxFinal();
        _sync = true;
    }

    /// <summary>
    /// LAX Zero Page,Y - Cykl 0: Fetch adresu i wartości
    /// </summary>
    private void LaxZpY_Cycle0()
    {
        _tempAddr = AddrZpY();
        _tempValue = _memory.Read(_tempAddr);
        LaxFinal();
        _sync = true;
    }

    /// <summary>
    /// LAX Absolute - Cykl 0: Fetch adresu i odczyt wartości
    /// </summary>
    private void LaxAbs_Cycle0()
    {
        _tempAddr = AddrAbs();
        _tempValue = _memory.Read(_tempAddr);
        LaxFinal();
        _sync = true;
    }

    /// <summary>
    /// LAX Absolute - Cykl 1: (nieużywany, ale zarezerwowany)
    /// </summary>
    private void LaxAbs_Cycle1()
    {
        _sync = true;
    }

    /// <summary>
    /// LAX Absolute,Y - Cykl 0: Fetch adresu i odczyt wartości (z page crossing)
    /// </summary>
    private void LaxAbsY_Cycle0()
    {
        _tempAddr = AddrAbsY(out _pageCrossed);
        _tempValue = _memory.Read(_tempAddr);
        LaxFinal();
        if (!_pageCrossed)
        {
            _sync = true;
        }
    }

    /// <summary>
    /// LAX Absolute,Y - Cykl 1: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void LaxAbsY_Cycle1()
    {
        _sync = true;
    }

    /// <summary>
    /// LAX (Indirect,X) - Cykl 0: Fetch adresu i odczyt wartości
    /// </summary>
    private void LaxIndX_Cycle0()
    {
        _tempAddr = AddrIndX();
        _tempValue = _memory.Read(_tempAddr);
        LaxFinal();
        _sync = true;
    }

    /// <summary>
    /// LAX (Indirect,X) - Cykl 1: (nieużywany)
    /// </summary>
    private void LaxIndX_Cycle1()
    {
        _sync = true;
    }

    /// <summary>
    /// LAX (Indirect,X) - Cykl 2: (nieużywany)
    /// </summary>
    private void LaxIndX_Cycle2()
    {
        _sync = true;
    }

    /// <summary>
    /// LAX (Indirect),Y - Cykl 0: Fetch adresu i odczyt wartości (z page crossing)
    /// </summary>
    private void LaxIndY_Cycle0()
    {
        _tempAddr = AddrIndY(out _pageCrossed);
        _tempValue = _memory.Read(_tempAddr);
        LaxFinal();
        if (!_pageCrossed)
        {
            _sync = true;
        }
    }

    /// <summary>
    /// LAX (Indirect),Y - Cykl 1: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void LaxIndY_Cycle1()
    {
        _sync = true;
    }

    /// <summary>
    /// LAX (Indirect),Y - Cykl 2: (nieużywany)
    /// </summary>
    private void LaxIndY_Cycle2()
    {
        _sync = true;
    }

    /// <summary>
    /// LAX (Indirect),Y) - Cykl 3: (nieużywany)
    /// </summary>
    private void LaxIndY_Cycle3()
    {
        _sync = true;
    }

    /// <summary>
    /// LAX (Indirect),Y - Cykl 4: (nieużywany)
    /// </summary>
    private void LaxIndY_Cycle4()
    {
        _sync = true;
    }

    /// <summary>
    /// Wspólna metoda końcowa dla LAX - ustawia A, X i flagi N, Z
    /// </summary>
    private void LaxFinal()
    {
        _a = _tempValue;
        _x = _tempValue;
        SetNZ(_tempValue);
    }

    #endregion

    #region SAX - Store A & X (Illegal Opcode)

    /// <summary>
    /// SAX Zero Page - Cykl 0: Fetch adresu i zapis
    /// </summary>
    private void SaxZp_Cycle0()
    {
        _tempAddr = AddrZp();
        _memory.Write(_tempAddr, (byte)(_a & _x));
        _sync = true;
    }

    /// <summary>
    /// SAX Zero Page,Y - Cykl 0: Fetch adresu i zapis
    /// </summary>
    private void SaxZpY_Cycle0()
    {
        _tempAddr = AddrZpY();
        _memory.Write(_tempAddr, (byte)(_a & _x));
        _sync = true;
    }

    /// <summary>
    /// SAX Absolute - Cykl 0: Fetch adresu i zapis
    /// </summary>
    private void SaxAbs_Cycle0()
    {
        _tempAddr = AddrAbs();
        _memory.Write(_tempAddr, (byte)(_a & _x));
        _sync = true;
    }

    /// <summary>
    /// SAX Absolute - Cykl 1: (nieużywany)
    /// </summary>
    private void SaxAbs_Cycle1()
    {
        _sync = true;
    }

    /// <summary>
    /// SAX (Indirect,X) - Cykl 0: wylicz adres i zapisz A & X
    /// </summary>
    private void SaxIndX_Cycle0()
    {
        _tempAddr = AddrIndX();
        _memory.Write(_tempAddr, (byte)(_a & _x));
    }

    #endregion

    #region LAS (LAR) - Load A, X, SP from memory AND SP

    /// <summary>
    /// LAS Absolute,Y - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void LasAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// LAS Absolute,Y - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void LasAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// LAS Absolute,Y - Cykl 2: Odczyt wartości z pamięci
    /// </summary>
    private void LasAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        byte val = _memory.Read(_tempAddr);
        byte result = (byte)(val & _sp);
        _a = result;
        _x = result;
        _sp = result;
        SetNZ(result);
        _sync = true;
    }

    /// <summary>
    /// LAS Absolute,Y - Cykl 3: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void LasAbsY_Cycle3()
    {
        if (_pageCrossed)
        {
            // Dodatkowy cykl za page crossing
        }
        else
        {
            _sync = true;
        }
    }

    #endregion
}
