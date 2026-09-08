namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region BRK (7 cykli)

    /// <summary>
    /// BRK - Cykl 0: Inkrementuj PC (pomiń signature byte)
    /// </summary>
    private void Brk_Cycle0()
    {
        _pc++;
    }

    /// <summary>
    /// BRK - Cykl 1: Push PCH
    /// </summary>
    private void Brk_Cycle1()
    {
        Push((byte)(_pc >> 8));
    }

    /// <summary>
    /// BRK - Cykl 2: Push PCL
    /// </summary>
    private void Brk_Cycle2()
    {
        Push((byte)(_pc & 0xFF));
    }

    /// <summary>
    /// BRK - Cykl 3: Push P z B=1
    /// </summary>
    private void Brk_Cycle3()
    {
        byte pushedP = _p;
        pushedP |= FlagB;  // B=1 dla BRK
        pushedP |= FlagU;  // bit5=1
        Push(pushedP);
    }

    /// <summary>
    /// BRK - Cykl 4: Ustaw I=1
    /// </summary>
    private void Brk_Cycle4()
    {
        SetFlag(FlagI, true);
    }

    /// <summary>
    /// BRK - Cykl 5: Fetch low byte wektora IRQ
    /// </summary>
    private void Brk_Cycle5()
    {
        _tempAddr = _memory.Read(0xFFFE);
    }

    /// <summary>
    /// BRK - Cykl 6: Fetch high byte wektora i skocz
    /// </summary>
    private void Brk_Cycle6()
    {
        _tempAddr |= (ushort)(_memory.Read(0xFFFF) << 8);
        _pc = _tempAddr;
        _sync = true;
    }

    #endregion

    #region RTI (6 cykli)

    /// <summary>
    /// RTI - Cykl 0: Pull P
    /// </summary>
    private void Rti_Cycle0()
    {
        _p = (byte)((Pop() & ~FlagB) | FlagU);
    }

    /// <summary>
    /// RTI - Cykl 1: Pull PCL
    /// </summary>
    private void Rti_Cycle1()
    {
        _tempAddr = Pop();
    }

    /// <summary>
    /// RTI - Cykl 2: Pull PCH
    /// </summary>
    private void Rti_Cycle2()
    {
        _tempAddr |= (ushort)(Pop() << 8);
    }

    /// <summary>
    /// RTI - Cykl 3: Ustaw PC
    /// </summary>
    private void Rti_Cycle3()
    {
        _pc = _tempAddr;
    }

    /// <summary>
    /// RTI - Cykl 4: Opóźnienie sprawdzania przerwań
    /// </summary>
    private void Rti_Cycle4()
    {
        _interruptDelay = true;
    }

    /// <summary>
    /// RTI - Cykl 5: Sync
    /// </summary>
    private void Rti_Cycle5()
    {
        _sync = true;
    }

    #endregion
}
