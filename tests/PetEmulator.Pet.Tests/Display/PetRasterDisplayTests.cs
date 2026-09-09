using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.Chips;
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
    public void Cursor_WithCrtc_LocatedByCrtcCursorRegister()
    {
        var font = LoadRealFont();
        var profile = PetProfileCatalog.Cbm8032;
        var memory = new FakeMemoryBus();
        const int cursorCol = 10;
        const int cursorRow = 3;
        var cursorOffset = (ushort)(cursorRow * profile.Columns + cursorCol);
        memory.Write((ushort)(profile.VideoRamStart + cursorOffset), SpaceScreenCode);

        // The CRTC is a standalone chip here, not routed through `memory` - PetRasterDisplay reads
        // its cursor register directly off the chip instance, the same way PetMachine's bus wiring
        // would, so register writes go straight to `crtc`, not through the fake screen-RAM bus.
        var crtc = new Crtc6545();
        WriteCrtcRegister(crtc, 14, (byte)(cursorOffset >> 8));
        WriteCrtcRegister(crtc, 15, (byte)(cursorOffset & 0xFF));

        var display = new PetRasterDisplay(profile, memory, font, crtc);
        var frame = new uint[display.PixelWidth * display.PixelHeight];

        display.Render(frame);

        CellPixels(frame, display.PixelWidth, font, cursorCol, cursorRow).Should().OnlyContain(p => p == 0xFF8DFF72u);
    }

    private static void WriteCrtcRegister(Crtc6545 crtc, byte register, byte value)
    {
        crtc.Write(0, register);
        crtc.Write(1, value);
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
