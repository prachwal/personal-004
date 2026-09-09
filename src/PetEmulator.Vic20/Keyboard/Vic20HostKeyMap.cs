namespace PetEmulator.Vic20.Keyboard;

/// <summary>Row/column for one VIC-20 keyboard key. Ported from cpu-vibe-001's
/// VicHostKeyMap.LetterMap - real matrix positions, not implementation-specific.</summary>
public static class Vic20HostKeyMap
{
    private static readonly (int Row, int Col, char Char)[] LetterMap =
    [
        (0, 0, '@'), (0, 1, 'A'), (0, 2, 'B'), (0, 3, 'C'), (0, 4, 'D'), (0, 5, 'E'), (0, 6, 'F'), (0, 7, 'G'),
        (1, 0, 'H'), (1, 1, 'I'), (1, 2, 'J'), (1, 3, 'K'), (1, 4, 'L'), (1, 5, 'M'), (1, 6, 'N'), (1, 7, 'O'),
        (2, 0, 'P'), (2, 1, 'Q'), (2, 2, 'R'), (2, 3, 'S'), (2, 4, 'T'), (2, 5, 'U'), (2, 6, 'V'), (2, 7, 'W'),
        (5, 0, 'X'), (5, 1, 'Y'), (5, 2, 'Z'), (5, 3, '['), (5, 4, '\\'), (5, 5, ']'), (5, 6, '^'), (5, 7, '_'),
        (6, 0, '0'), (6, 1, '1'), (6, 2, '2'), (6, 3, '3'), (6, 4, '4'), (6, 5, '5'), (6, 6, '6'), (6, 7, '7'),
        (7, 0, '8'), (7, 1, '9'), (7, 2, ':'), (7, 3, ';'), (7, 4, ','), (7, 5, '-'), (7, 6, '.'), (7, 7, '/'),
    ];

    private const int ReturnRow = 3, ReturnCol = 0;
    private const int SpaceRow = 4, SpaceCol = 0;

    /// <summary>Maps a character to its (row, column); <c>null</c> if this keyboard can't type
    /// it (no lowercase, matches real VIC-20 keyboard - same convention as PET's
    /// PetEmulator.Pet.Keyboard.TextTyper.ToHostKey).</summary>
    public static (int Row, int Col)? Find(char ch)
    {
        if (ch is '\n' or '\r')
            return (ReturnRow, ReturnCol);
        if (ch == ' ')
            return (SpaceRow, SpaceCol);

        var upper = char.ToUpperInvariant(ch);
        foreach (var (row, col, mapped) in LetterMap)
            if (mapped == upper)
                return (row, col);

        return null;
    }
}
