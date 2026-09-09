namespace PetEmulator.Pet.Keyboard;

/// <summary>Ten-row, eight-column PET keyboard matrix with active-low columns.</summary>
public sealed class PetKeyboardMatrix
{
    public const int RowCount = 10;
    public const int ColumnCount = 8;

    private readonly byte[] _pressedColumns = new byte[RowCount];

    public void Press(int row, int column) => SetKey(row, column, true);

    public void Release(int row, int column) => SetKey(row, column, false);

    public byte ReadColumns(int row) => row is >= 0 and < RowCount
        ? (byte)~_pressedColumns[row]
        : (byte)0xFF;

    public void Reset() => Array.Clear(_pressedColumns);

    private void SetKey(int row, int column, bool pressed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, RowCount);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, ColumnCount);

        var mask = (byte)(1 << column);
        _pressedColumns[row] = pressed
            ? (byte)(_pressedColumns[row] | mask)
            : (byte)(_pressedColumns[row] & ~mask);
    }
}
