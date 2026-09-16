namespace PetEmulator.Cpc;

public class CpcKeyboard
{
    private readonly bool[,] _keys = new bool[10, 8];

    public void Reset() => Array.Clear(_keys);

    public CpcKeyboardSnapshot CaptureState()
        => new() { Keys = Enumerable.Range(0, 10).SelectMany(row => Enumerable.Range(0, 8).Select(column => _keys[row, column])).ToArray() };

    public void RestoreState(CpcKeyboardSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Keys.Length != 80) throw new ArgumentException("Keyboard snapshot must contain 80 keys.", nameof(state));
        for (var row = 0; row < 10; row++) for (var column = 0; column < 8; column++) _keys[row, column] = state.Keys[row * 8 + column];
    }

    public void SetKey(byte row, byte column, bool pressed)
    {
        if (row < 10 && column < 8)
            _keys[row, column] = pressed;
    }

    public byte ReadRow(byte row)
    {
        if (row >= 10) return 0xFF;
        byte value = 0xFF;
        for (byte column = 0; column < 8; column++)
            if (_keys[row, column]) value &= (byte)~(1 << column);
        return value;
    }
}
