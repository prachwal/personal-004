using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Trs80;

public sealed class Trs80CharacterRomProvider : ICharacterRomProvider
{
    private readonly ILogger _log;

    public Trs80CharacterRomProvider(ILogger? logger = null) => _log = logger ?? NullLogger.Instance;

    public IEnumerable<FontSource> DiscoverFonts(string romsRoot)
    {
        var path = Path.Combine(romsRoot, "trs80", "character_set_8s.bin");
        if (File.Exists(path))
        {
            _log.LogInformation("Font discovered: '{Path}'.", path);
            yield return new FontSource("TRS-80 Model I MCM6670/6673", new Trs80CharacterFont(File.ReadAllBytes(path)));
        }
        else
        {
            _log.LogInformation("Font missing: '{Path}'.", path);
        }
    }
}

public sealed class Trs80CharacterFont : IGlyphFont
{
    private readonly byte[] _rom;
    public Trs80CharacterFont(byte[] rom) { if (rom.Length < 2048) throw new InvalidDataException("TRS-80 character ROM must contain at least 2048 bytes."); _rom = rom; }
    public int GlyphWidth => 6;
    public int GlyphHeight => 12;
    public byte GetGlyphRow(int characterCode, int row)
    {
        if ((uint)characterCode >= 256 || (uint)row >= 12) return 0;
        var bank = characterCode >= 128 ? 1024 : 0;
        var sourceRow = row * 8 / 12;
        return (byte)((_rom[bank + (characterCode & 0x7F) * 8 + sourceRow] & 0x1F) << 3);
    }
}
