namespace PetEmulator.Cpc464;

public sealed class Cpc464Keyboard
{
    private readonly bool[,] _keys = new bool[10, 8];

    public void Reset() => Array.Clear(_keys);

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
