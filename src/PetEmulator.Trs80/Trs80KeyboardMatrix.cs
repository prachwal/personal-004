using PetEmulator.Core;

namespace PetEmulator.Trs80;

public sealed class Trs80KeyboardMatrix : IMemoryBus
{
    private readonly byte[] _rows = new byte[8];

    public void SetKeyDown(Trs80Key key, bool down)
    {
        var (row, bit) = Position(key);
        if (down) _rows[row] |= bit;
        else _rows[row] &= (byte)~bit;
    }

    public byte Read(ushort address)
    {
        var selected = (byte)address;
        byte value = 0;
        for (var row = 0; row < _rows.Length; row++)
            if ((selected & (1 << row)) != 0) value |= _rows[row];
        return value;
    }

    public void Write(ushort address, byte value) { }

    private static (int Row, byte Bit) Position(Trs80Key key) => key switch
    {
        Trs80Key.At => (0, 1), Trs80Key.A => (0, 2), Trs80Key.B => (0, 4), Trs80Key.C => (0, 8),
        Trs80Key.D => (0, 0x10), Trs80Key.E => (0, 0x20), Trs80Key.F => (0, 0x40), Trs80Key.G => (0, 0x80),
        Trs80Key.H => (1, 1), Trs80Key.I => (1, 2), Trs80Key.J => (1, 4), Trs80Key.K => (1, 8),
        Trs80Key.L => (1, 0x10), Trs80Key.M => (1, 0x20), Trs80Key.N => (1, 0x40), Trs80Key.O => (1, 0x80),
        Trs80Key.P => (2, 1), Trs80Key.Q => (2, 2), Trs80Key.R => (2, 4), Trs80Key.S => (2, 8),
        Trs80Key.T => (2, 0x10), Trs80Key.U => (2, 0x20), Trs80Key.V => (2, 0x40), Trs80Key.W => (2, 0x80),
        Trs80Key.X => (3, 1), Trs80Key.Y => (3, 2), Trs80Key.Z => (3, 4), Trs80Key.LeftBracket => (3, 8),
        Trs80Key.Backslash => (3, 0x10), Trs80Key.RightBracket => (3, 0x20), Trs80Key.Caret => (3, 0x40), Trs80Key.Underscore => (3, 0x80),
        Trs80Key.D0 => (4, 1), Trs80Key.D1 => (4, 2), Trs80Key.D2 => (4, 4), Trs80Key.D3 => (4, 8),
        Trs80Key.D4 => (4, 0x10), Trs80Key.D5 => (4, 0x20), Trs80Key.D6 => (4, 0x40), Trs80Key.D7 => (4, 0x80),
        Trs80Key.D8 => (5, 1), Trs80Key.D9 => (5, 2), Trs80Key.Colon => (5, 4), Trs80Key.Semicolon => (5, 8),
        Trs80Key.Comma => (5, 0x10), Trs80Key.Minus => (5, 0x20), Trs80Key.Period => (5, 0x40), Trs80Key.Slash => (5, 0x80),
        Trs80Key.Enter => (6, 1), Trs80Key.Clear => (6, 2), Trs80Key.Break => (6, 4), Trs80Key.Up => (6, 8),
        Trs80Key.Down => (6, 0x10), Trs80Key.Left => (6, 0x20), Trs80Key.Right => (6, 0x40), Trs80Key.Space => (6, 0x80),
        Trs80Key.Shift => (7, 1), _ => throw new ArgumentOutOfRangeException(nameof(key))
    };
}
