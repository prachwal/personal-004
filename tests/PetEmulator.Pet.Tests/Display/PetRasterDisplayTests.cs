using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.Display;
using PetEmulator.Pet.Fonts;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.Display;

public class PetRasterDisplayTests
{
    private const byte SpaceScreenCode = 0x20;
    private const byte LetterAScreenCode = 0x01; // PET screen codes, not ASCII — see PetCharacterRomLoaderTests.

    private static string Pet2001Rom8 => Path.Combine(
        RomLocator.Directory("pet-2001-8", "characters-1.901447-08.bin"), "characters-1.901447-08.bin");

    private static IGlyphFont LoadRealFont() => new PetCharacterRomLoader().Load(Pet2001Rom8);

    [Test]
    public void PixelDimensions_Pet2001_8_MatchColumnsRowsTimesGlyphSize()
    {
        var font = LoadRealFont();
        var profile = PetProfileCatalog.Pet2001_8;
        var display = new PetRasterDisplay(profile, new FakeMemoryBus(), font);

        display.PixelWidth.Should().Be(profile.Columns * font.GlyphWidth);
        display.PixelHeight.Should().Be(profile.Rows * font.GlyphHeight);
    }

    [Test]
    public void PixelDimensions_Cbm8032_MatchColumnsRowsTimesGlyphSize()
    {
        var font = LoadRealFont();
        var profile = PetProfileCatalog.Cbm8032;
        var display = new PetRasterDisplay(profile, new FakeMemoryBus(), font);

        display.PixelWidth.Should().Be(profile.Columns * font.GlyphWidth);
        display.PixelHeight.Should().Be(profile.Rows * font.GlyphHeight);
    }

    [Test]
    public void Render_SpaceCell_IsAllBackground()
    {
        var font = LoadRealFont();
        var profile = PetProfileCatalog.Pet2001_8;
        var memory = new FakeMemoryBus();
        const int cellCol = 3;
        const int cellRow = 2;
        memory.Write((ushort)(profile.VideoRamStart + cellRow * profile.Columns + cellCol), SpaceScreenCode);
        var display = new PetRasterDisplay(profile, memory, font);
        var frame = new uint[display.PixelWidth * display.PixelHeight];

        display.Render(frame);

        foreach (var pixel in CellPixels(frame, display.PixelWidth, font, cellCol, cellRow))
            pixel.Should().Be(0xFF102810);
    }

    [Test]
    public void Render_LetterACell_HasAtLeastOneForegroundPixel()
    {
        var font = LoadRealFont();
        var profile = PetProfileCatalog.Pet2001_8;
        var memory = new FakeMemoryBus();
        const int cellCol = 5;
        const int cellRow = 1;
        memory.Write((ushort)(profile.VideoRamStart + cellRow * profile.Columns + cellCol), LetterAScreenCode);
        var display = new PetRasterDisplay(profile, memory, font);
        var frame = new uint[display.PixelWidth * display.PixelHeight];

        display.Render(frame);

        CellPixels(frame, display.PixelWidth, font, cellCol, cellRow).Should().Contain(0xFF8DFF72u);
    }

    [Test]
    public void Render_WrongSizedBuffer_Throws()
    {
        var font = LoadRealFont();
        var display = new PetRasterDisplay(PetProfileCatalog.Pet2001_8, new FakeMemoryBus(), font);

        var act = () => display.Render(new uint[1]);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_GeometryExceedsVideoRamLength_Throws()
    {
        var font = LoadRealFont();
        var badProfile = PetProfileCatalog.Pet2001_8 with { VideoRamLength = 10 };

        var act = () => new PetRasterDisplay(badProfile, new FakeMemoryBus(), font);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Cursor_NoCrtc_BlinksOnAtStart_ThenOffAfterHalfPeriod()
    {
        var font = LoadRealFont();
        var profile = PetProfileCatalog.Pet2001_8;
        var memory = new FakeMemoryBus();
        const int cursorCol = 4;
        const int cursorRow = 2;
        var cursorAddress = (ushort)(profile.VideoRamStart + cursorRow * profile.Columns + cursorCol);
        memory.Write((ushort)(cursorAddress), SpaceScreenCode);
        memory.Write(0x00C4, (byte)(cursorAddress & 0xFF));
        memory.Write(0x00C5, (byte)(cursorAddress >> 8));
        memory.Write(0x00C6, cursorCol);
        var display = new PetRasterDisplay(profile, memory, font);
        var frame = new uint[display.PixelWidth * display.PixelHeight];

        // Cursor starts visible: a blank cell under the cursor renders fully inverted (all foreground).
        display.Render(frame);
        CellPixels(frame, display.PixelWidth, font, cursorCol, cursorRow).Should().OnlyContain(p => p == 0xFF8DFF72u);

        for (var i = 0; i < 30; i++)
            display.Tick();

        display.Render(frame);
        CellPixels(frame, display.PixelWidth, font, cursorCol, cursorRow).Should().OnlyContain(p => p == 0xFF102810u);
    }

    [Test]
    public void Cursor_OnACrtcEquippedProfile_StillLocatedByTheZeroPagePointer_NotACrtcRegister()
    {
        // Was: a CRTC-equipped profile (BASIC 4, CBM 4032/8032) located the cursor via the CRTC's
        // own hardware cursor register (R14/R15) instead of the zero-page pointer. Wrong - traced
        // against a real boot (see PetRasterDisplay.GetCursorPosition's doc comment): the real
        // BASIC 4 KERNAL writes R14/R15 exactly once during CRTC init (to $0000 - immediately
        // invalid) and never touches them again; $C4/$C5/$C6 track a genuinely live, valid
        // position throughout. This test used to write directly to a standalone Crtc6545's
        // register (mimicking what the OLD, wrong code path read) - now it writes the zero-page
        // pointer instead, the same way the no-CRTC test above does, proving a CRTC-equipped
        // profile's cursor works identically.
        var font = LoadRealFont();
        var profile = PetProfileCatalog.Cbm8032;
        var memory = new FakeMemoryBus();
        const int cursorCol = 10;
        const int cursorRow = 3;
        var cursorAddress = (ushort)(profile.VideoRamStart + cursorRow * profile.Columns + cursorCol);
        memory.Write(cursorAddress, SpaceScreenCode);
        memory.Write(0x00C4, (byte)(cursorAddress & 0xFF));
        memory.Write(0x00C5, (byte)(cursorAddress >> 8));
        memory.Write(0x00C6, cursorCol);

        var display = new PetRasterDisplay(profile, memory, font);
        var frame = new uint[display.PixelWidth * display.PixelHeight];

        display.Render(frame);

        CellPixels(frame, display.PixelWidth, font, cursorCol, cursorRow).Should().OnlyContain(p => p == 0xFF8DFF72u);
    }

    private static IEnumerable<uint> CellPixels(uint[] frame, int pixelWidth, IGlyphFont font, int col, int row)
    {
        for (var glyphRow = 0; glyphRow < font.GlyphHeight; glyphRow++)
            for (var bit = 0; bit < font.GlyphWidth; bit++)
                yield return frame[(row * font.GlyphHeight + glyphRow) * pixelWidth + col * font.GlyphWidth + bit];
    }

    private sealed class FakeMemoryBus : IMemoryBus
    {
        private readonly byte[] _ram = new byte[0x10000];

        public byte Read(ushort address) => _ram[address];

        public void Write(ushort address, byte value) => _ram[address] = value;
    }
}
