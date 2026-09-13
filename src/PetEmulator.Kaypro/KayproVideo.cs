namespace PetEmulator.Kaypro;

/// <summary>Kaypro II text video memory: 80 columns by 24 rows.</summary>
public sealed class KayproVideo
{
    public const int Columns = 80;
    public const int Rows = 24;
    public const int MemorySize = 0x800;

    private readonly byte[] _memory = new byte[MemorySize];

    public byte Read(ushort offset) => offset < MemorySize ? _memory[offset] : (byte)0xFF;

    public void Write(ushort offset, byte value)
    {
        if (offset < MemorySize)
            _memory[offset] = value;
    }

    public void Reset() => Array.Fill(_memory, (byte)0x20);

    public string GetText()
    {
        var text = new char[Columns * Rows];
        for (var row = 0; row < Rows; row++)
        for (var column = 0; column < Columns; column++)
        {
            var value = _memory[row * Columns + column];
            text[row * Columns + column] = value is >= 0x20 and <= 0x7E ? (char)value : ' ';
        }

        return new string(text);
    }
}
