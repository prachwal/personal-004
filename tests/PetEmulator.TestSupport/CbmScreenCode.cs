namespace PetEmulator.TestSupport;

/// <summary>Decodes Commodore "screen code" bytes (what PET and VIC-20 video RAM actually stores -
/// not PETSCII, not ASCII) back to characters. Verified against this repo's own PET boot tests
/// (READY.'s bytes decode as R=0x12 E=0x05 A=0x01 D=0x04 Y=0x19 .=0x2E) and VIC-20 boot tests
/// (letters are screen codes 1-26; space/digits/most punctuation mirror ASCII 0x20-0x3F directly) -
/// both machines share this table because both use the same character-ROM design lineage. Neither
/// machine needs pixel-glyph OCR (see ScreenTextOcr, built for CPC464 which has no character-code
/// video RAM at all): their video RAM already holds these codes as plain, exactly-addressable
/// bytes - this is a lookup table, not image recognition.</summary>
public static class CbmScreenCode
{
    /// <summary>Screen code to character, or '?' for a code this table doesn't cover (control
    /// codes, graphics characters, reversed-video range 0x80+) - deliberately not silently wrong.</summary>
    public static char ToChar(byte screenCode) => screenCode switch
    {
        0 => '@',
        >= 1 and <= 26 => (char)('A' + screenCode - 1),
        >= 0x20 and <= 0x3F => (char)screenCode, // space, digits, most punctuation mirror ASCII
        _ => '?',
    };

    /// <summary>Decodes a rectangular screen-RAM region (row-major, <paramref name="columns"/> wide)
    /// into text, one line per row, trailing spaces trimmed.</summary>
    public static string ToText(IReadOnlyList<byte> screenRam, int columns, int rows)
    {
        var sb = new System.Text.StringBuilder();
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < columns; col++)
                sb.Append(ToChar(screenRam[row * columns + col]));
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
