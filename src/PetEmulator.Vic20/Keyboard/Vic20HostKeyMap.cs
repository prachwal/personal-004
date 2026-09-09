namespace PetEmulator.Vic20.Keyboard;

/// <summary>Row/column for one VIC-20 keyboard key.
///
/// Empirically discovered against the real emulated hardware, not ported from a reference
/// implementation - see docs/vic20-rendering-fixes.md's keyboard investigation. An earlier
/// version of this table (copied from cpu-vibe-001's VicHostKeyMap.LetterMap) was simply wrong:
/// besides the real bugs it took to even get a key press to register at all (VIA1 vs VIA2,
/// single-row vs OR-ed multi-row select - see Vic20Machine/Vic20KeyboardMatrix), the (row, col)
/// assignments themselves didn't match this real KERNAL's actual scan order. Verified by booting
/// to real BASIC, pressing each of the 64 matrix cells alone, and reading back the real PETSCII
/// screen code the KERNAL echoed for it - not guessed, not copied.</summary>
public static class Vic20HostKeyMap
{
    private static readonly (int Row, int Col, char Char)[] LetterMap =
    [
        (0, 0, '1'), (0, 1, '3'), (0, 2, '5'), (0, 3, '7'), (0, 4, '9'), (0, 5, '+'),
        (1, 1, 'W'), (1, 2, 'R'), (1, 3, 'Y'), (1, 4, 'I'), (1, 5, 'P'), (1, 6, '*'),
        (2, 1, 'A'), (2, 2, 'D'), (2, 3, 'G'), (2, 4, 'J'), (2, 5, 'L'), (2, 6, ';'),
        (3, 2, 'X'), (3, 3, 'V'), (3, 4, 'N'), (3, 5, ','), (3, 6, '/'),
        (4, 1, 'Z'), (4, 2, 'C'), (4, 3, 'B'), (4, 4, 'M'), (4, 5, '.'),
        (5, 1, 'S'), (5, 2, 'F'), (5, 3, 'H'), (5, 4, 'K'), (5, 5, ':'), (5, 6, '='),
        (6, 0, 'Q'), (6, 1, 'E'), (6, 2, 'T'), (6, 3, 'U'), (6, 4, 'O'),
        (7, 0, '2'), (7, 1, '4'), (7, 2, '6'), (7, 3, '8'), (7, 4, '0'), (7, 5, '-'),
    ];

    // Confirmed separately from LetterMap (see the investigation doc): Enter verified by the
    // KERNAL's screen line-pointer ($D1/$D2) advancing by exactly one row (22 = the profile's
    // column count) after this cell alone; Space by "A" + this cell + "B" echoing as "A B" with a
    // real blank cell between them, not "AB".
    private const int ReturnRow = 3, ReturnCol = 7;
    private const int SpaceRow = 4, SpaceCol = 0;

    /// <summary>Maps a character to its (row, column); <c>null</c> if this keyboard can't type
    /// it (no lowercase, matches real VIC-20 keyboard - same convention as PET's
    /// PetEmulator.Pet.Keyboard.TextTyper.ToHostKey). '@' and some punctuation are real VIC-20
    /// keys this table doesn't cover yet - a documented gap (see the 64-cell empirical scan in
    /// the investigation doc), not a guess.</summary>
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
