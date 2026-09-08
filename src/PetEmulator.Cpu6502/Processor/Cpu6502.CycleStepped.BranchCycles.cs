namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region BCC - Branch if Carry Clear

    /// <summary>
    /// BCC - Cykl 0: Fetch offset
    /// </summary>
    private void BccRel_Cycle0()
    {
        _tempValue = _memory.Read(_pc++);
        _tempAddr = (ushort)(_pc + (sbyte)_tempValue);
        _branchTaken = !GetFlag(FlagC);
    }

    /// <summary>
    /// BCC - Cykl 1: Sprawdź warunek i ewentualnie skocz (same page)
    /// Przerwania sprawdzane tutaj (przedostatni cykl dla not taken i same page)
    /// </summary>
    private void BccRel_Cycle1()
    {
        if (!_branchTaken)
        {
            // Not taken - 2 cykle, sprawdź przerwania na przedostatnim cyklu
            _suppressPostInstructionIrq = true;
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
        else
        {
            // Taken - sprawdź page crossing
            bool pageCrossed = ((_pc >> 8) != (_tempAddr >> 8));
            if (!pageCrossed)
            {
                // Same page - 3 cykle, sprawdź przerwania na przedostatnim cyklu
                _suppressPostInstructionIrq = true;
                if (_irqPending && !GetFlag(FlagI))
                {
                    _irqReadyAtBoundary = true;
                }
                _pc = _tempAddr;
            }
            else
            {
                // Different page - 4 cykle, potrzebny dodatkowy cykl
                _pageCrossed = true;
                _pc = _tempAddr;
            }
        }
    }

    /// <summary>
    /// BCC - Cykl 2: Sync dla same page
    /// </summary>
    private void BccRel_Cycle2()
    {
        if (_branchTaken && !_pageCrossed)
        {
            // Same page - sprawdź przerwania na przedostatnim cyklu (cykl 2 dla 3-cyklowych)
            _suppressPostInstructionIrq = true;
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    /// <summary>
    /// BCC - Cykl 3: Sync dla different page
    /// </summary>
    private void BccRel_Cycle3()
    {
        if (_branchTaken && _pageCrossed)
        {
            // Different page - sprawdź przerwania na przedostatnim cyklu (cykl 3 dla 4-cyklowych)
            _suppressPostInstructionIrq = true;
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    #endregion

    #region BCS - Branch if Carry Set

    /// <summary>
    /// BCS - Cykl 0: Fetch offset
    /// </summary>
    private void BcsRel_Cycle0()
    {
        _tempValue = _memory.Read(_pc++);
        _tempAddr = (ushort)(_pc + (sbyte)_tempValue);
        _branchTaken = GetFlag(FlagC);
    }

    /// <summary>
    /// BCS - Cykl 1: Sprawdź warunek i ewentualnie skocz (same page)
    /// </summary>
    private void BcsRel_Cycle1()
    {
        if (!_branchTaken)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
        else
        {
            bool pageCrossed = ((_pc >> 8) != (_tempAddr >> 8));
            if (!pageCrossed)
            {
                if (_irqPending && !GetFlag(FlagI))
                {
                    _irqReadyAtBoundary = true;
                }
                _pc = _tempAddr;
            }
            else
            {
                _pageCrossed = true;
                _pc = _tempAddr;
            }
        }
    }

    /// <summary>
    /// BCS - Cykl 2: Sync dla same page
    /// </summary>
    private void BcsRel_Cycle2()
    {
        if (_branchTaken && !_pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    /// <summary>
    /// BCS - Cykl 3: Sync dla different page
    /// </summary>
    private void BcsRel_Cycle3()
    {
        if (_branchTaken && _pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    #endregion

    #region BEQ - Branch if Equal

    /// <summary>
    /// BEQ - Cykl 0: Fetch offset
    /// </summary>
    private void BeqRel_Cycle0()
    {
        _tempValue = _memory.Read(_pc++);
        _tempAddr = (ushort)(_pc + (sbyte)_tempValue);
        _branchTaken = GetFlag(FlagZ);
    }

    /// <summary>
    /// BEQ - Cykl 1: Sprawdź warunek i ewentualnie skocz (same page)
    /// </summary>
    private void BeqRel_Cycle1()
    {
        if (!_branchTaken)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
        else
        {
            bool pageCrossed = ((_pc >> 8) != (_tempAddr >> 8));
            if (!pageCrossed)
            {
                if (_irqPending && !GetFlag(FlagI))
                {
                    _irqReadyAtBoundary = true;
                }
                _pc = _tempAddr;
            }
            else
            {
                _pageCrossed = true;
                _pc = _tempAddr;
            }
        }
    }

    /// <summary>
    /// BEQ - Cykl 2: Sync dla same page
    /// </summary>
    private void BeqRel_Cycle2()
    {
        if (_branchTaken && !_pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    /// <summary>
    /// BEQ - Cykl 3: Sync dla different page
    /// </summary>
    private void BeqRel_Cycle3()
    {
        if (_branchTaken && _pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    #endregion

    #region BMI - Branch if Minus

    /// <summary>
    /// BMI - Cykl 0: Fetch offset
    /// </summary>
    private void BmiRel_Cycle0()
    {
        _tempValue = _memory.Read(_pc++);
        _tempAddr = (ushort)(_pc + (sbyte)_tempValue);
        _branchTaken = GetFlag(FlagN);
    }

    /// <summary>
    /// BMI - Cykl 1: Sprawdź warunek i ewentualnie skocz (same page)
    /// </summary>
    private void BmiRel_Cycle1()
    {
        if (!_branchTaken)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
        else
        {
            bool pageCrossed = ((_pc >> 8) != (_tempAddr >> 8));
            if (!pageCrossed)
            {
                if (_irqPending && !GetFlag(FlagI))
                {
                    _irqReadyAtBoundary = true;
                }
                _pc = _tempAddr;
            }
            else
            {
                _pageCrossed = true;
                _pc = _tempAddr;
            }
        }
    }

    /// <summary>
    /// BMI - Cykl 2: Sync dla same page
    /// </summary>
    private void BmiRel_Cycle2()
    {
        if (_branchTaken && !_pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    /// <summary>
    /// BMI - Cykl 3: Sync dla different page
    /// </summary>
    private void BmiRel_Cycle3()
    {
        if (_branchTaken && _pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    #endregion

    #region BNE - Branch if Not Equal

    /// <summary>
    /// BNE - Cykl 0: Fetch offset
    /// </summary>
    private void BneRel_Cycle0()
    {
        _tempValue = _memory.Read(_pc++);
        _tempAddr = (ushort)(_pc + (sbyte)_tempValue);
        _branchTaken = !GetFlag(FlagZ);
    }

    /// <summary>
    /// BNE - Cykl 1: Sprawdź warunek i ewentualnie skocz (same page)
    /// </summary>
    private void BneRel_Cycle1()
    {
        if (!_branchTaken)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
        else
        {
            bool pageCrossed = ((_pc >> 8) != (_tempAddr >> 8));
            if (!pageCrossed)
            {
                if (_irqPending && !GetFlag(FlagI))
                {
                    _irqReadyAtBoundary = true;
                }
                _pc = _tempAddr;
            }
            else
            {
                _pageCrossed = true;
                _pc = _tempAddr;
            }
        }
    }

    /// <summary>
    /// BNE - Cykl 2: Sync dla same page
    /// </summary>
    private void BneRel_Cycle2()
    {
        if (_branchTaken && !_pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    /// <summary>
    /// BNE - Cykl 3: Sync dla different page
    /// </summary>
    private void BneRel_Cycle3()
    {
        if (_branchTaken && _pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    #endregion

    #region BPL - Branch if Plus

    /// <summary>
    /// BPL - Cykl 0: Fetch offset
    /// </summary>
    private void BplRel_Cycle0()
    {
        _tempValue = _memory.Read(_pc++);
        _tempAddr = (ushort)(_pc + (sbyte)_tempValue);
        _branchTaken = !GetFlag(FlagN);
    }

    /// <summary>
    /// BPL - Cykl 1: Sprawdź warunek i ewentualnie skocz (same page)
    /// </summary>
    private void BplRel_Cycle1()
    {
        if (!_branchTaken)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
        else
        {
            bool pageCrossed = ((_pc >> 8) != (_tempAddr >> 8));
            if (!pageCrossed)
            {
                if (_irqPending && !GetFlag(FlagI))
                {
                    _irqReadyAtBoundary = true;
                }
                _pc = _tempAddr;
            }
            else
            {
                _pageCrossed = true;
                _pc = _tempAddr;
            }
        }
    }

    /// <summary>
    /// BPL - Cykl 2: Sync dla same page
    /// </summary>
    private void BplRel_Cycle2()
    {
        if (_branchTaken && !_pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    /// <summary>
    /// BPL - Cykl 3: Sync dla different page
    /// </summary>
    private void BplRel_Cycle3()
    {
        if (_branchTaken && _pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    #endregion

    #region BVC - Branch if Overflow Clear

    /// <summary>
    /// BVC - Cykl 0: Fetch offset
    /// </summary>
    private void BvcRel_Cycle0()
    {
        _tempValue = _memory.Read(_pc++);
        _tempAddr = (ushort)(_pc + (sbyte)_tempValue);
        _branchTaken = !GetFlag(FlagV);
    }

    /// <summary>
    /// BVC - Cykl 1: Sprawdź warunek i ewentualnie skocz (same page)
    /// </summary>
    private void BvcRel_Cycle1()
    {
        if (!_branchTaken)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
        else
        {
            bool pageCrossed = ((_pc >> 8) != (_tempAddr >> 8));
            if (!pageCrossed)
            {
                if (_irqPending && !GetFlag(FlagI))
                {
                    _irqReadyAtBoundary = true;
                }
                _pc = _tempAddr;
            }
            else
            {
                _pageCrossed = true;
                _pc = _tempAddr;
            }
        }
    }

    /// <summary>
    /// BVC - Cykl 2: Sync dla same page
    /// </summary>
    private void BvcRel_Cycle2()
    {
        if (_branchTaken && !_pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    /// <summary>
    /// BVC - Cykl 3: Sync dla different page
    /// </summary>
    private void BvcRel_Cycle3()
    {
        if (_branchTaken && _pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    #endregion

    #region BVS - Branch if Overflow Set

    /// <summary>
    /// BVS - Cykl 0: Fetch offset
    /// </summary>
    private void BvsRel_Cycle0()
    {
        _tempValue = _memory.Read(_pc++);
        _tempAddr = (ushort)(_pc + (sbyte)_tempValue);
        _branchTaken = GetFlag(FlagV);
    }

    /// <summary>
    /// BVS - Cykl 1: Sprawdź warunek i ewentualnie skocz (same page)
    /// </summary>
    private void BvsRel_Cycle1()
    {
        if (!_branchTaken)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
        else
        {
            bool pageCrossed = ((_pc >> 8) != (_tempAddr >> 8));
            if (!pageCrossed)
            {
                if (_irqPending && !GetFlag(FlagI))
                {
                    _irqReadyAtBoundary = true;
                }
                _pc = _tempAddr;
            }
            else
            {
                _pageCrossed = true;
                _pc = _tempAddr;
            }
        }
    }

    /// <summary>
    /// BVS - Cykl 2: Sync dla same page
    /// </summary>
    private void BvsRel_Cycle2()
    {
        if (_branchTaken && !_pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    /// <summary>
    /// BVS - Cykl 3: Sync dla different page
    /// </summary>
    private void BvsRel_Cycle3()
    {
        if (_branchTaken && _pageCrossed)
        {
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            _sync = true;
        }
    }

    #endregion
}
