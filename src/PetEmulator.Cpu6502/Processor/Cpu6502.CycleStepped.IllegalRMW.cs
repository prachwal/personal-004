namespace Cpu6502;

/// <summary>
/// Nieudokumentowane opkody R-M-W (Read-Modify-Write) - DCP, ISC, RLA, RRA, SLO, SRE
/// </summary>
public partial class Cpu6502
{
    #region DCP (DCM) - DEC + CMP
    // Opcodes: $C7, $D7, $CF, $DF, $DB, $C3, $D3

    // DCP Zero Page - 5 cykli
    private void DcpZp_Cycle0()
    {
        _tempAddr = AddrZp();
        _tempValue = _memory.Read(_tempAddr);
    }

    private void DcpZp_Cycle1()
    {
        // R-M-W: dummy write (quirk)
        _memory.Write(_tempAddr, _tempValue);
    }

    private void DcpZp_Cycle2()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result; // Zapamiętaj wynik dla CMP
    }

    private void DcpZp_Cycle3()
    {
        ExecuteCmp(_a, _tempValue);
    }

    private void DcpZp_Cycle4()
    {
        _sync = true;
    }

    // DCP Zero Page,X - 6 cykli
    private void DcpZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    private void DcpZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void DcpZpX_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void DcpZpX_Cycle3()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void DcpZpX_Cycle4()
    {
        ExecuteCmp(_a, _tempValue);
    }

    private void DcpZpX_Cycle5()
    {
        _sync = true;
    }

    // DCP Absolute - 6 cykli
    private void DcpAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void DcpAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void DcpAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void DcpAbs_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void DcpAbs_Cycle4()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void DcpAbs_Cycle5()
    {
        ExecuteCmp(_a, _tempValue);
        _sync = true;
    }

    // DCP Absolute,X - 7 cykli
    private void DcpAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void DcpAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void DcpAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void DcpAbsX_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void DcpAbsX_Cycle4()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void DcpAbsX_Cycle5()
    {
        ExecuteCmp(_a, _tempValue);
    }

    private void DcpAbsX_Cycle6()
    {
        _sync = true;
    }

    // DCP Absolute,Y - 7 cykli
    private void DcpAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void DcpAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void DcpAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void DcpAbsY_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void DcpAbsY_Cycle4()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void DcpAbsY_Cycle5()
    {
        ExecuteCmp(_a, _tempValue);
    }

    private void DcpAbsY_Cycle6()
    {
        _sync = true;
    }

    // DCP (Indirect,X) - 8 cykli
    private void DcpIndX_Cycle0()
    {
        _tempZp = (byte)(_memory.Read(_pc++) + _x);
    }

    private void DcpIndX_Cycle1()
    {
        byte lo = _memory.Read(_tempZp);
        _tempAddr = lo;
    }

    private void DcpIndX_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempZp + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void DcpIndX_Cycle3()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void DcpIndX_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void DcpIndX_Cycle5()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void DcpIndX_Cycle6()
    {
        ExecuteCmp(_a, _tempValue);
    }

    private void DcpIndX_Cycle7()
    {
        _sync = true;
    }

    // DCP (Indirect),Y - 8 cykli
    private void DcpIndY_Cycle0()
    {
        _tempZp = _memory.Read(_pc++);
    }

    private void DcpIndY_Cycle1()
    {
        byte lo = _memory.Read(_tempZp);
        _tempAddr = lo;
    }

    private void DcpIndY_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempZp + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void DcpIndY_Cycle3()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void DcpIndY_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void DcpIndY_Cycle5()
    {
        byte result = (byte)(_tempValue - 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void DcpIndY_Cycle6()
    {
        ExecuteCmp(_a, _tempValue);
    }

    private void DcpIndY_Cycle7()
    {
        _sync = true;
    }

    #endregion

    #region ISC (ISB) - INC + SBC
    // Opcodes: $E7, $F7, $EF, $FF, $FB, $E3, $F3

    // ISC Zero Page - 5 cykli
    private void IscZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    private void IscZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void IscZp_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void IscZp_Cycle3()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void IscZp_Cycle4()
    {
        ExecuteSbc(_tempValue);
        _sync = true;
    }

    // ISC Zero Page,X - 6 cykli
    private void IscZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    private void IscZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void IscZpX_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void IscZpX_Cycle3()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void IscZpX_Cycle4()
    {
        ExecuteSbc(_tempValue);
    }

    private void IscZpX_Cycle5()
    {
        _sync = true;
    }

    // ISC Absolute - 6 cykli
    private void IscAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void IscAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void IscAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void IscAbs_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void IscAbs_Cycle4()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void IscAbs_Cycle5()
    {
        ExecuteSbc(_tempValue);
        _sync = true;
    }

    // ISC Absolute,X - 7 cykli
    private void IscAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void IscAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void IscAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void IscAbsX_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void IscAbsX_Cycle4()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void IscAbsX_Cycle5()
    {
        ExecuteSbc(_tempValue);
    }

    private void IscAbsX_Cycle6()
    {
        _sync = true;
    }

    // ISC Absolute,Y - 7 cykli
    private void IscAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void IscAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void IscAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void IscAbsY_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void IscAbsY_Cycle4()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void IscAbsY_Cycle5()
    {
        ExecuteSbc(_tempValue);
    }

    private void IscAbsY_Cycle6()
    {
        _sync = true;
    }

    // ISC (Indirect,X) - 8 cykli
    private void IscIndX_Cycle0()
    {
        _tempZp = (byte)(_memory.Read(_pc++) + _x);
    }

    private void IscIndX_Cycle1()
    {
        byte lo = _memory.Read(_tempZp);
        _tempAddr = lo;
    }

    private void IscIndX_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempZp + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void IscIndX_Cycle3()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void IscIndX_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void IscIndX_Cycle5()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void IscIndX_Cycle6()
    {
        ExecuteSbc(_tempValue);
    }

    private void IscIndX_Cycle7()
    {
        _sync = true;
    }

    // ISC (Indirect),Y - 8 cykli
    private void IscIndY_Cycle0()
    {
        _tempZp = _memory.Read(_pc++);
    }

    private void IscIndY_Cycle1()
    {
        byte lo = _memory.Read(_tempZp);
        _tempAddr = lo;
    }

    private void IscIndY_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempZp + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void IscIndY_Cycle3()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void IscIndY_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void IscIndY_Cycle5()
    {
        byte result = (byte)(_tempValue + 1);
        _memory.Write(_tempAddr, result);
        _tempValue = result;
    }

    private void IscIndY_Cycle6()
    {
        ExecuteSbc(_tempValue);
    }

    private void IscIndY_Cycle7()
    {
        _sync = true;
    }

    #endregion

    #region RLA - ROL + AND
    // Opcodes: $27, $37, $2F, $3F, $3B, $23, $33

    // RLA Zero Page - 5 cykli
    private void RlaZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    private void RlaZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RlaZp_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RlaZp_Cycle3()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
        _a &= result;
        SetNZ(_a);
    }

    private void RlaZp_Cycle4()
    {
        _sync = true;
    }

    // RLA Zero Page,X - 6 cykli
    private void RlaZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    private void RlaZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RlaZpX_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RlaZpX_Cycle3()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
        _a &= result;
        SetNZ(_a);
    }

    private void RlaZpX_Cycle4()
    {
    }

    private void RlaZpX_Cycle5()
    {
        _sync = true;
    }

    // RLA Absolute - 6 cykli
    private void RlaAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void RlaAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void RlaAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RlaAbs_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RlaAbs_Cycle4()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
        _a &= result;
        SetNZ(_a);
    }

    private void RlaAbs_Cycle5()
    {
        _sync = true;
    }

    // RLA Absolute,X - 7 cykli
    private void RlaAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void RlaAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void RlaAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RlaAbsX_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RlaAbsX_Cycle4()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
        _a &= result;
        SetNZ(_a);
    }

    private void RlaAbsX_Cycle5()
    {
    }

    private void RlaAbsX_Cycle6()
    {
        _sync = true;
    }

    // RLA Absolute,Y - 7 cykli
    private void RlaAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void RlaAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void RlaAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RlaAbsY_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RlaAbsY_Cycle4()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
        _a &= result;
        SetNZ(_a);
    }

    private void RlaAbsY_Cycle5()
    {
    }

    private void RlaAbsY_Cycle6()
    {
        _sync = true;
    }

    // RLA (Indirect,X) - 8 cykli
    private void RlaIndX_Cycle0()
    {
        _tempZp = (byte)(_memory.Read(_pc++) + _x);
    }

    private void RlaIndX_Cycle1()
    {
        byte lo = _memory.Read(_tempZp);
        _tempAddr = lo;
    }

    private void RlaIndX_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempZp + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void RlaIndX_Cycle3()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RlaIndX_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RlaIndX_Cycle5()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
        _a &= result;
        SetNZ(_a);
    }

    private void RlaIndX_Cycle6()
    {
    }

    private void RlaIndX_Cycle7()
    {
        _sync = true;
    }

    // RLA (Indirect),Y - 8 cykli
    private void RlaIndY_Cycle0()
    {
        _tempZp = _memory.Read(_pc++);
    }

    private void RlaIndY_Cycle1()
    {
        byte lo = _memory.Read(_tempZp);
        _tempAddr = lo;
    }

    private void RlaIndY_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempZp + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void RlaIndY_Cycle3()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RlaIndY_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RlaIndY_Cycle5()
    {
        byte result = ExecuteRol(_tempValue);
        _memory.Write(_tempAddr, result);
        _a &= result;
        SetNZ(_a);
    }

    private void RlaIndY_Cycle6()
    {
    }

    private void RlaIndY_Cycle7()
    {
        _sync = true;
    }

    #endregion

    #region RRA - ROR + ADC
    // Opcodes: $67, $77, $6F, $7F, $7B, $63, $73

    // RRA Zero Page - 5 cykli
    private void RraZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    private void RraZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RraZp_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RraZp_Cycle3()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
        ExecuteAdc(result);
        _sync = true;
    }

    // RRA Zero Page,X - 6 cykli
    private void RraZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)(zp + _x);
    }

    private void RraZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RraZpX_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RraZpX_Cycle3()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
        ExecuteAdc(result);
        _sync = true;
    }

    // RRA Absolute - 6 cykli
    private void RraAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void RraAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void RraAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RraAbs_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RraAbs_Cycle4()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
        ExecuteAdc(result);
        _sync = true;
    }

    // RRA Absolute,X - 7 cykli
    private void RraAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void RraAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void RraAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RraAbsX_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RraAbsX_Cycle4()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
        ExecuteAdc(result);
    }

    private void RraAbsX_Cycle5()
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

    private void RraAbsX_Cycle6()
    {
        _sync = true;
    }

    // RRA Absolute,Y - 7 cykli
    private void RraAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void RraAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void RraAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RraAbsY_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RraAbsY_Cycle4()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
        ExecuteAdc(result);
    }

    private void RraAbsY_Cycle5()
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

    private void RraAbsY_Cycle6()
    {
        _sync = true;
    }

    // RRA (Indirect,X) - 8 cykli
    private void RraIndX_Cycle0()
    {
        byte zp = (byte)(_memory.Read(_pc++) + _x);
        _tempAddr = zp;
    }

    private void RraIndX_Cycle1()
    {
        byte lo = _memory.Read(_tempAddr);
        _tempAddr = lo;
    }

    private void RraIndX_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempAddr + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void RraIndX_Cycle3()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RraIndX_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RraIndX_Cycle5()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
        ExecuteAdc(result);
        _sync = true;
    }

    // RRA (Indirect),Y - 8 cykli
    private void RraIndY_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = zp;
    }

    private void RraIndY_Cycle1()
    {
        byte lo = _memory.Read(_tempAddr);
        _tempAddr = lo;
    }

    private void RraIndY_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempAddr + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void RraIndY_Cycle3()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void RraIndY_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void RraIndY_Cycle5()
    {
        byte result = ExecuteRor(_tempValue);
        _memory.Write(_tempAddr, result);
        ExecuteAdc(result);
    }

    private void RraIndY_Cycle6()
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

    private void RraIndY_Cycle7()
    {
        _sync = true;
    }

    #endregion

    #region SLO (ASO) - ASL + ORA
    // Opcodes: $07, $17, $0F, $1F, $1B, $03, $13

    // SLO Zero Page - 5 cykli
    private void SloZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    private void SloZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SloZp_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SloZp_Cycle3()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
        _a |= result;
        SetNZ(_a);
    }

    private void SloZp_Cycle4()
    {
        _sync = true;
    }

    // SLO Zero Page,X - 6 cykli
    private void SloZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)((zp + _x) & 0xFF);
    }

    private void SloZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SloZpX_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SloZpX_Cycle3()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
        _a |= result;
        SetNZ(_a);
    }

    private void SloZpX_Cycle4()
    {
    }

    private void SloZpX_Cycle5()
    {
        _sync = true;
    }

    // SLO Absolute - 6 cykli
    private void SloAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void SloAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void SloAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SloAbs_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SloAbs_Cycle4()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
        _a |= result;
        SetNZ(_a);
    }

    private void SloAbs_Cycle5()
    {
        _sync = true;
    }

    // SLO Absolute,X - 7 cykli
    private void SloAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void SloAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void SloAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SloAbsX_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SloAbsX_Cycle4()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
        _a |= result;
        SetNZ(_a);
    }

    private void SloAbsX_Cycle5()
    {
    }

    private void SloAbsX_Cycle6()
    {
        _sync = true;
    }

    // SLO Absolute,Y - 7 cykli
    private void SloAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void SloAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void SloAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SloAbsY_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SloAbsY_Cycle4()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
        _a |= result;
        SetNZ(_a);
    }

    private void SloAbsY_Cycle5()
    {
    }

    private void SloAbsY_Cycle6()
    {
        _sync = true;
    }

    // SLO (Indirect,X) - 8 cykli
    private void SloIndX_Cycle0()
    {
        _tempZp = (byte)(_memory.Read(_pc++) + _x);
    }

    private void SloIndX_Cycle1()
    {
        byte lo = _memory.Read(_tempZp);
        _tempAddr = lo;
    }

    private void SloIndX_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempZp + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void SloIndX_Cycle3()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SloIndX_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SloIndX_Cycle5()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
        _a |= result;
        SetNZ(_a);
    }

    private void SloIndX_Cycle6()
    {
    }

    private void SloIndX_Cycle7()
    {
        _sync = true;
    }

    // SLO (Indirect),Y - 8 cykli
    private void SloIndY_Cycle0()
    {
        _tempZp = _memory.Read(_pc++);
    }

    private void SloIndY_Cycle1()
    {
        byte lo = _memory.Read(_tempZp);
        _tempAddr = lo;
    }

    private void SloIndY_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempZp + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void SloIndY_Cycle3()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SloIndY_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SloIndY_Cycle5()
    {
        byte result = ExecuteAsl(_tempValue);
        _memory.Write(_tempAddr, result);
        _a |= result;
        SetNZ(_a);
    }

    private void SloIndY_Cycle6()
    {
    }

    private void SloIndY_Cycle7()
    {
        _sync = true;
    }

    #endregion

    #region SRE (LSE) - LSR + EOR
    // Opcodes: $47, $57, $4F, $5F, $5B, $43, $53

    // SRE Zero Page - 5 cykli
    private void SreZp_Cycle0()
    {
        _tempAddr = AddrZp();
    }

    private void SreZp_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SreZp_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SreZp_Cycle3()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
        _a ^= result;
        SetNZ(_a);
        _sync = true;
    }

    // SRE Zero Page,X - 6 cykli
    private void SreZpX_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)(zp + _x);
    }

    private void SreZpX_Cycle1()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SreZpX_Cycle2()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SreZpX_Cycle3()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
        _a ^= result;
        SetNZ(_a);
        _sync = true;
    }

    // SRE Absolute - 6 cykli
    private void SreAbs_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void SreAbs_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void SreAbs_Cycle2()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SreAbs_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SreAbs_Cycle4()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
        _a ^= result;
        SetNZ(_a);
        _sync = true;
    }

    // SRE Absolute,X - 7 cykli
    private void SreAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void SreAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void SreAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SreAbsX_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SreAbsX_Cycle4()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
        _a ^= result;
        SetNZ(_a);
    }

    private void SreAbsX_Cycle5()
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

    private void SreAbsX_Cycle6()
    {
        _sync = true;
    }

    // SRE Absolute,Y - 7 cykli
    private void SreAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void SreAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void SreAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SreAbsY_Cycle3()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SreAbsY_Cycle4()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
        _a ^= result;
        SetNZ(_a);
    }

    private void SreAbsY_Cycle5()
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

    private void SreAbsY_Cycle6()
    {
        _sync = true;
    }

    // SRE (Indirect,X) - 8 cykli
    private void SreIndX_Cycle0()
    {
        byte zp = (byte)(_memory.Read(_pc++) + _x);
        _tempAddr = zp;
    }

    private void SreIndX_Cycle1()
    {
        byte lo = _memory.Read(_tempAddr);
        _tempAddr = lo;
    }

    private void SreIndX_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempAddr + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void SreIndX_Cycle3()
    {
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SreIndX_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SreIndX_Cycle5()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
        _a ^= result;
        SetNZ(_a);
        _sync = true;
    }

    // SRE (Indirect),Y - 8 cykli
    private void SreIndY_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = zp;
    }

    private void SreIndY_Cycle1()
    {
        byte lo = _memory.Read(_tempAddr);
        _tempAddr = lo;
    }

    private void SreIndY_Cycle2()
    {
        byte hi = _memory.Read((byte)(_tempAddr + 1));
        _tempAddr |= (ushort)(hi << 8);
    }

    private void SreIndY_Cycle3()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
        _tempValue = _memory.Read(_tempAddr);
    }

    private void SreIndY_Cycle4()
    {
        // R-M-W: dummy write
        _memory.Write(_tempAddr, _tempValue);
    }

    private void SreIndY_Cycle5()
    {
        byte result = ExecuteLsr(_tempValue);
        _memory.Write(_tempAddr, result);
        _a ^= result;
        SetNZ(_a);
    }

    private void SreIndY_Cycle6()
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

    private void SreIndY_Cycle7()
    {
        _sync = true;
    }

    #endregion
}
