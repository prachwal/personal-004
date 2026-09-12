namespace PetEmulator.Vic20.Keyboard;

/// <summary>The VIC-20's 8x8 keyboard matrix - row-selected through VIA2 port B (active-low: a 0
/// bit selects that row, mirroring real hardware's open-collector wiring), columns read back
/// through VIA2 port A. Confirmed against the real KERNAL disassembly (see
/// PetEmulator.Vic20.Vic20Machine's VIA2 PortBWritten/PortAInput binding for the exact wiring and
/// citation) - VIA2, not VIA1, and port B for output/port A for input, not the reverse.</summary>
public sealed class Vic20KeyboardMatrix
{
    public const int RowCount = 8;
    public const int ColumnCount = 8;

    private readonly byte[] _pressedColumns = new byte[RowCount];
    private byte _rowMask = 0xFF;

    public void Press(int row, int column)
    {
        if ((uint)row < RowCount && (uint)column < ColumnCount)
            _pressedColumns[row] |= (byte)(1 << column);
    }

    public void Release(int row, int column)
    {
        if ((uint)row < RowCount && (uint)column < ColumnCount)
            _pressedColumns[row] &= (byte)~(1 << column);
    }

    public void Reset()
    {
        Array.Clear(_pressedColumns);
        _rowMask = 0xFF;
    }

    /// <summary>Latches VIA2 port B's written output (<c>ORB &amp; DDRB</c>), active-low per real
    /// hardware. Every bit that's 0 asserts that row - real open-collector wiring lets a caller
    /// (and the real KERNAL genuinely does this - see <see cref="ReadColumns"/>'s doc comment)
    /// assert MULTIPLE rows at once, not just one.</summary>
    public void SetRowSelect(byte rowMask) => _rowMask = rowMask;

    /// <summary>Active-low column mask, OR-ed (open-collector wire-OR - a column reads pressed if
    /// ANY currently-asserted row has that column pressed) across every row <see cref="_rowMask"/>
    /// currently asserts. With exactly one row asserted (the normal case during the KERNAL's
    /// per-row matrix scan) this is just that row's own columns, same as a single-row model would
    /// give - the real bug this generalizes past is the KERNAL's own "is anything pressed at all"
    /// fast-path check (confirmed in docs/vic20/disassembly/kernal.asm and
    /// docs/vic20/rendering-fixes.md's keyboard investigation): it asserts ALL EIGHT rows at once
    /// ($9120=$00) and reads $9121 exactly once before ever running the real per-row scan that
    /// would populate the keyboard buffer - a single-row model always read $FF (nothing pressed)
    /// for that combined check regardless of what was actually held, silently dropping every
    /// keypress before the real scan ever got a chance to see it.</summary>
    public byte ReadColumns()
    {
        byte result = 0xFF;
        for (var row = 0; row < RowCount; row++)
            if ((_rowMask & (1 << row)) == 0)
                result &= (byte)~_pressedColumns[row];
        return result;
    }
}
