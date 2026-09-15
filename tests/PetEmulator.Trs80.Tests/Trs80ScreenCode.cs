namespace PetEmulator.Trs80.Tests;

/// <summary>Decodes TRS-80 Model I video RAM bytes to characters. Unlike PET/VIC-20 (see
/// PetEmulator.TestSupport.CbmScreenCode), this is a near-identity mapping: the MCM6670/6673
/// character ROM's low bank indexes glyphs directly by ASCII code for the printable range (see
/// Trs80CharacterFont.GetGlyphRow's own `characterCode &amp; 0x7F` indexing) - video RAM byte 0x41
/// really is 'A', not a remapped screen code the way CBM machines use. High-bit-set bytes select
/// the semigraphics bank and aren't text.</summary>
public static class Trs80ScreenCode
{
    public static char ToChar(byte code) => code is >= 0x20 and <= 0x5F ? (char)code : '?';

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
