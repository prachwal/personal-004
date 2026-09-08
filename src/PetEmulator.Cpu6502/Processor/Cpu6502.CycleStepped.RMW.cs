namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region INC - Increment Memory (R-M-W)

    /// <summary>
    /// INC Zero Page - Cykl 0: Fetch adresu
    /// </summary>
    private void IncZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    /// <summary>
    /// INC Zero Page - Cykl 1: Odczyt wartości
    /// </summary>
    private void IncZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// INC Zero Page - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void IncZp_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);  // Zapis oryginalnej wartości
    }

    /// <summary>
    /// INC Zero Page - Cykl 3: Inkrementacja i zapis
    /// </summary>
    private void IncZp_Cycle3()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        SetNZ(result);
    }

    /// <summary>
    /// INC Zero Page - Cykl 4: Sync
    /// </summary>
    private void IncZp_Cycle4()
    {
        _sync = true;
    }

    /// <summary>
    /// INC Zero Page,X - Cykl 0: Fetch adresu bazowego
    /// </summary>
    private void IncZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    /// <summary>
    /// INC Zero Page,X - Cykl 1: Odczyt wartości
    /// </summary>
    private void IncZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// INC Zero Page,X - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void IncZpX_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// INC Zero Page,X - Cykl 3: Inkrementacja i zapis
    /// </summary>
    private void IncZpX_Cycle3()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        SetNZ(result);
    }

    /// <summary>
    /// INC Zero Page,X - Cykl 4: Dummy indexing cycle
    /// </summary>
    private void IncZpX_Cycle4()
    {
    }

    /// <summary>
    /// INC Zero Page,X - Cykl 5: Sync
    /// </summary>
    private void IncZpX_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// INC Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void IncAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// INC Absolute - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void IncAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// INC Absolute - Cykl 2: Odczyt wartości
    /// </summary>
    private void IncAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// INC Absolute - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void IncAbs_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// INC Absolute - Cykl 4: Inkrementacja i zapis
    /// </summary>
    private void IncAbs_Cycle4()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        SetNZ(result);
    }

    /// <summary>
    /// INC Absolute - Cykl 5: Sync
    /// </summary>
    private void IncAbs_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// INC Absolute,X - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void IncAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// INC Absolute,X - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void IncAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// INC Absolute,X - Cykl 2: Odczyt wartości (z page crossing)
    /// </summary>
    private void IncAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// INC Absolute,X - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void IncAbsX_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// INC Absolute,X - Cykl 4: Inkrementacja i zapis
    /// </summary>
    private void IncAbsX_Cycle4()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        SetNZ(result);
    }

    /// <summary>
    /// INC Absolute,X - Cykl 5: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void IncAbsX_Cycle5()
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

    /// <summary>
    /// INC Absolute,X - Cykl 6: Sync
    /// </summary>
    private void IncAbsX_Cycle6()
    {
        _sync = true;
    }

    #endregion

    #region DEC - Decrement Memory (R-M-W)

    /// <summary>
    /// DEC Zero Page - Cykl 0: Fetch adresu
    /// </summary>
    private void DecZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    /// <summary>
    /// DEC Zero Page - Cykl 1: Odczyt wartości
    /// </summary>
    private void DecZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// DEC Zero Page - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void DecZp_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// DEC Zero Page - Cykl 3: Dekrementacja i zapis
    /// </summary>
    private void DecZp_Cycle3()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        SetNZ(result);
    }

    /// <summary>
    /// DEC Zero Page - Cykl 4: Sync
    /// </summary>
    private void DecZp_Cycle4()
    {
        _sync = true;
    }

    /// <summary>
    /// DEC Zero Page,X - Cykl 0: Fetch adresu bazowego
    /// </summary>
    private void DecZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    /// <summary>
    /// DEC Zero Page,X - Cykl 1: Odczyt wartości
    /// </summary>
    private void DecZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// DEC Zero Page,X - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void DecZpX_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// DEC Zero Page,X - Cykl 3: Dekrementacja i zapis
    /// </summary>
    private void DecZpX_Cycle3()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        SetNZ(result);
    }

    /// <summary>
    /// DEC Zero Page,X - Cykl 4: Dummy indexing cycle
    /// </summary>
    private void DecZpX_Cycle4()
    {
    }

    /// <summary>
    /// DEC Zero Page,X - Cykl 5: Sync
    /// </summary>
    private void DecZpX_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// DEC Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void DecAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// DEC Absolute - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void DecAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// DEC Absolute - Cykl 2: Odczyt wartości
    /// </summary>
    private void DecAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// DEC Absolute - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void DecAbs_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// DEC Absolute - Cykl 4: Dekrementacja i zapis
    /// </summary>
    private void DecAbs_Cycle4()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        SetNZ(result);
    }

    /// <summary>
    /// DEC Absolute - Cykl 5: Sync
    /// </summary>
    private void DecAbs_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// DEC Absolute,X - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void DecAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// DEC Absolute,X - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void DecAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// DEC Absolute,X - Cykl 2: Odczyt wartości (z page crossing)
    /// </summary>
    private void DecAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// DEC Absolute,X - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void DecAbsX_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// DEC Absolute,X - Cykl 4: Dekrementacja i zapis
    /// </summary>
    private void DecAbsX_Cycle4()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        SetNZ(result);
    }

    /// <summary>
    /// DEC Absolute,X - Cykl 5: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void DecAbsX_Cycle5()
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

    /// <summary>
    /// DEC Absolute,X - Cykl 6: Sync
    /// </summary>
    private void DecAbsX_Cycle6()
    {
        _sync = true;
    }

    #endregion

    #region ASL - Arithmetic Shift Left (R-M-W)

    /// <summary>
    /// ASL Zero Page - Cykl 0: Fetch adresu
    /// </summary>
    private void AslZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    /// <summary>
    /// ASL Zero Page - Cykl 1: Odczyt wartości
    /// </summary>
    private void AslZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ASL Zero Page - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void AslZp_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ASL Zero Page - Cykl 3: ASL i zapis
    /// </summary>
    private void AslZp_Cycle3()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ASL Zero Page - Cykl 4: Sync
    /// </summary>
    private void AslZp_Cycle4()
    {
        _sync = true;
    }

    /// <summary>
    /// ASL Zero Page,X - Cykl 0: Fetch adresu bazowego
    /// </summary>
    private void AslZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    /// <summary>
    /// ASL Zero Page,X - Cykl 1: Odczyt wartości
    /// </summary>
    private void AslZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ASL Zero Page,X - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void AslZpX_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ASL Zero Page,X - Cykl 3: ASL i zapis
    /// </summary>
    private void AslZpX_Cycle3()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ASL Zero Page,X - Cykl 4: Dummy indexing cycle
    /// </summary>
    private void AslZpX_Cycle4()
    {
    }

    /// <summary>
    /// ASL Zero Page,X - Cykl 5: Sync
    /// </summary>
    private void AslZpX_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// ASL Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void AslAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// ASL Absolute - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void AslAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// ASL Absolute - Cykl 2: Odczyt wartości
    /// </summary>
    private void AslAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ASL Absolute - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void AslAbs_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ASL Absolute - Cykl 4: ASL i zapis
    /// </summary>
    private void AslAbs_Cycle4()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ASL Absolute - Cykl 5: Sync
    /// </summary>
    private void AslAbs_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// ASL Absolute,X - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void AslAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// ASL Absolute,X - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void AslAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// ASL Absolute,X - Cykl 2: Odczyt wartości (z page crossing)
    /// </summary>
    private void AslAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ASL Absolute,X - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void AslAbsX_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ASL Absolute,X - Cykl 4: ASL i zapis
    /// </summary>
    private void AslAbsX_Cycle4()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ASL Absolute,X - Cykl 5: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void AslAbsX_Cycle5()
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

    /// <summary>
    /// ASL Absolute,X - Cykl 6: Sync
    /// </summary>
    private void AslAbsX_Cycle6()
    {
        _sync = true;
    }

    #endregion

    #region LSR - Logical Shift Right (R-M-W)

    /// <summary>
    /// LSR Zero Page - Cykl 0: Fetch adresu
    /// </summary>
    private void LsrZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    /// <summary>
    /// LSR Zero Page - Cykl 1: Odczyt wartości
    /// </summary>
    private void LsrZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// LSR Zero Page - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void LsrZp_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// LSR Zero Page - Cykl 3: LSR i zapis
    /// </summary>
    private void LsrZp_Cycle3()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// LSR Zero Page - Cykl 4: Sync
    /// </summary>
    private void LsrZp_Cycle4()
    {
        _sync = true;
    }

    /// <summary>
    /// LSR Zero Page,X - Cykl 0: Fetch adresu bazowego
    /// </summary>
    private void LsrZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    /// <summary>
    /// LSR Zero Page,X - Cykl 1: Odczyt wartości
    /// </summary>
    private void LsrZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// LSR Zero Page,X - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void LsrZpX_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// LSR Zero Page,X - Cykl 3: LSR i zapis
    /// </summary>
    private void LsrZpX_Cycle3()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// LSR Zero Page,X - Cykl 4: Dummy indexing cycle
    /// </summary>
    private void LsrZpX_Cycle4()
    {
    }

    /// <summary>
    /// LSR Zero Page,X - Cykl 5: Sync
    /// </summary>
    private void LsrZpX_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// LSR Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void LsrAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// LSR Absolute - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void LsrAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// LSR Absolute - Cykl 2: Odczyt wartości
    /// </summary>
    private void LsrAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// LSR Absolute - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void LsrAbs_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// LSR Absolute - Cykl 4: LSR i zapis
    /// </summary>
    private void LsrAbs_Cycle4()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// LSR Absolute - Cykl 5: Sync
    /// </summary>
    private void LsrAbs_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// LSR Absolute,X - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void LsrAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// LSR Absolute,X - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void LsrAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// LSR Absolute,X - Cykl 2: Odczyt wartości (z page crossing)
    /// </summary>
    private void LsrAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// LSR Absolute,X - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void LsrAbsX_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// LSR Absolute,X - Cykl 4: LSR i zapis
    /// </summary>
    private void LsrAbsX_Cycle4()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// LSR Absolute,X - Cykl 5: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void LsrAbsX_Cycle5()
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

    /// <summary>
    /// LSR Absolute,X - Cykl 6: Sync
    /// </summary>
    private void LsrAbsX_Cycle6()
    {
        _sync = true;
    }

    #endregion

    #region ROL - Rotate Left (R-M-W)

    /// <summary>
    /// ROL Zero Page - Cykl 0: Fetch adresu
    /// </summary>
    private void RolZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    /// <summary>
    /// ROL Zero Page - Cykl 1: Odczyt wartości
    /// </summary>
    private void RolZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ROL Zero Page - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void RolZp_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ROL Zero Page - Cykl 3: ROL i zapis
    /// </summary>
    private void RolZp_Cycle3()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ROL Zero Page - Cykl 4: Sync
    /// </summary>
    private void RolZp_Cycle4()
    {
        _sync = true;
    }

    /// <summary>
    /// ROL Zero Page,X - Cykl 0: Fetch adresu bazowego
    /// </summary>
    private void RolZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    /// <summary>
    /// ROL Zero Page,X - Cykl 1: Odczyt wartości
    /// </summary>
    private void RolZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ROL Zero Page,X - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void RolZpX_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ROL Zero Page,X - Cykl 3: ROL i zapis
    /// </summary>
    private void RolZpX_Cycle3()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ROL Zero Page,X - Cykl 4: Dummy indexing cycle
    /// </summary>
    private void RolZpX_Cycle4()
    {
    }

    /// <summary>
    /// ROL Zero Page,X - Cykl 5: Sync
    /// </summary>
    private void RolZpX_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// ROL Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void RolAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// ROL Absolute - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void RolAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// ROL Absolute - Cykl 2: Odczyt wartości
    /// </summary>
    private void RolAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ROL Absolute - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void RolAbs_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ROL Absolute - Cykl 4: ROL i zapis
    /// </summary>
    private void RolAbs_Cycle4()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ROL Absolute - Cykl 5: Sync
    /// </summary>
    private void RolAbs_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// ROL Absolute,X - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void RolAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// ROL Absolute,X - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void RolAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// ROL Absolute,X - Cykl 2: Odczyt wartości (z page crossing)
    /// </summary>
    private void RolAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ROL Absolute,X - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void RolAbsX_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ROL Absolute,X - Cykl 4: ROL i zapis
    /// </summary>
    private void RolAbsX_Cycle4()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ROL Absolute,X - Cykl 5: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void RolAbsX_Cycle5()
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

    /// <summary>
    /// ROL Absolute,X - Cykl 6: Sync
    /// </summary>
    private void RolAbsX_Cycle6()
    {
        _sync = true;
    }

    #endregion

    #region ROR - Rotate Right (R-M-W)

    /// <summary>
    /// ROR Zero Page - Cykl 0: Fetch adresu
    /// </summary>
    private void RorZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    /// <summary>
    /// ROR Zero Page - Cykl 1: Odczyt wartości
    /// </summary>
    private void RorZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ROR Zero Page - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void RorZp_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ROR Zero Page - Cykl 3: ROR i zapis
    /// </summary>
    private void RorZp_Cycle3()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ROR Zero Page - Cykl 4: Sync
    /// </summary>
    private void RorZp_Cycle4()
    {
        _sync = true;
    }

    /// <summary>
    /// ROR Zero Page,X - Cykl 0: Fetch adresu bazowego
    /// </summary>
    private void RorZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    /// <summary>
    /// ROR Zero Page,X - Cykl 1: Odczyt wartości
    /// </summary>
    private void RorZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ROR Zero Page,X - Cykl 2: Dummy write (R-M-W quirk)
    /// </summary>
    private void RorZpX_Cycle2()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ROR Zero Page,X - Cykl 3: ROR i zapis
    /// </summary>
    private void RorZpX_Cycle3()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ROR Zero Page,X - Cykl 4: Dummy indexing cycle
    /// </summary>
    private void RorZpX_Cycle4()
    {
    }

    /// <summary>
    /// ROR Zero Page,X - Cykl 5: Sync
    /// </summary>
    private void RorZpX_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// ROR Absolute - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void RorAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// ROR Absolute - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void RorAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// ROR Absolute - Cykl 2: Odczyt wartości
    /// </summary>
    private void RorAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ROR Absolute - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void RorAbs_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ROR Absolute - Cykl 4: ROR i zapis
    /// </summary>
    private void RorAbs_Cycle4()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ROR Absolute - Cykl 5: Sync
    /// </summary>
    private void RorAbs_Cycle5()
    {
        _sync = true;
    }

    /// <summary>
    /// ROR Absolute,X - Cykl 0: Fetch low byte adresu
    /// </summary>
    private void RorAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    /// <summary>
    /// ROR Absolute,X - Cykl 1: Fetch high byte adresu
    /// </summary>
    private void RorAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    /// <summary>
    /// ROR Absolute,X - Cykl 2: Odczyt wartości (z page crossing)
    /// </summary>
    private void RorAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    /// <summary>
    /// ROR Absolute,X - Cykl 3: Dummy write (R-M-W quirk)
    /// </summary>
    private void RorAbsX_Cycle3()
    {
        _memory.Write(_tempAddr, _tempValue);
    }

    /// <summary>
    /// ROR Absolute,X - Cykl 4: ROR i zapis
    /// </summary>
    private void RorAbsX_Cycle4()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
    }

    /// <summary>
    /// ROR Absolute,X - Cykl 5: Sync (dodatkowy cykl przy page crossing)
    /// </summary>
    private void RorAbsX_Cycle5()
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

    /// <summary>
    /// ROR Absolute,X - Cykl 6: Sync
    /// </summary>
    private void RorAbsX_Cycle6()
    {
        _sync = true;
    }

    #endregion
}
