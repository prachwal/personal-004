namespace PetEmulator.Vic20.Keyboard;

/// <summary>The VIC-20's 8x8 keyboard matrix - row-selected through VIA1 port A (active-low: a 0
/// bit selects that row, mirroring real hardware's open-collector wiring), columns read back
/// through VIA1 port B. Ported from the same real hardware fact PetEmulator.Pet.Keyboard
/// .PetKeyboardMatrix models for PIA1, adapted for VIA-driven row select (real VIC-20 wiring -
/// see PetEmulator.Vic20.Vic20Machine's VIA1 PortAWritten/PortBInput binding, mirroring
/// PetMachine's PIA1 wiring) instead of a separate row-select write.</summary>
public sealed class Vic20KeyboardMatrix
{
    public const int RowCount = 8;
    public const int ColumnCount = 8;

    private readonly byte[] _pressedColumns = new byte[RowCount];
    private int _selectedRow = -1;

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
        _selectedRow = -1;
    }

    /// <summary>Selects the (single) row whose bit is 0 in <paramref name="rowMask"/> - VIA1
    /// port A's combined output (<c>ORA &amp; DDRA</c>), active-low per real hardware. No row
    /// selected (mask all 1s, or more than one bit low) reads back as no key pressed.</summary>
    public void SetRowSelect(byte rowMask)
    {
        _selectedRow = -1;
        for (var row = 0; row < RowCount; row++)
        {
            if ((rowMask & (1 << row)) == 0)
            {
                _selectedRow = row;
                return;
            }
        }
    }

    /// <summary>Active-low column mask for the currently selected row - a 0 bit means that
    /// column's key is pressed, matching VIA1 port B's real readback.</summary>
    public byte ReadColumns() => _selectedRow < 0 ? (byte)0xFF : (byte)~_pressedColumns[_selectedRow];
}
