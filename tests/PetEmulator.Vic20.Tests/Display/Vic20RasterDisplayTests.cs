using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Vic20.Chips;
using PetEmulator.Vic20.Display;

namespace PetEmulator.Vic20.Tests.Display;

public sealed class Vic20RasterDisplayTests
{
    [Test]
    public void PixelWidthAndHeight_AreFixedRegardlessOfVicConfiguration()
    {
        var (display, _, _) = CreateDisplay();

        display.PixelWidth.Should().Be(Vic20RasterDisplay.MaxWidth);
        display.PixelHeight.Should().Be(Vic20RasterDisplay.MaxHeight);
    }

    [Test]
    public void Render_BeforeVicIsConfigured_FillsBorderOnly()
    {
        var (display, vic, _) = CreateDisplay();
        vic.Write(0x900F, 0x02); // border color = 2 (Vic6560.BorderColor = reg & 0x07)
        var buffer = new uint[display.PixelWidth * display.PixelHeight];

        display.Render(buffer);

        buffer.Should().OnlyContain(px => px == Vic20Palette.ToArgb(2));
    }

    [Test]
    public void Render_WithNonMulticolorGlyph_PaintsInkForSetBitsAndPaperForClearBits()
    {
        var (display, vic, memory) = CreateDisplay();
        // 1 column, 1 row, screen at $1E00, char matrix at $8000 (real char ROM base, but this
        // test uses a synthetic memory bus with plain RAM everywhere - see CreateDisplay).
        vic.Write(0x9002, 0x01); // columns = 1
        vic.Write(0x9003, 0x02); // rows = (0x02>>1)&0x3F = 1
        // ScreenMatrixBase from reg5's high nibble (0) -> screenAddr $8000; CharMatrixBase from
        // reg5's low nibble (1) -> charAddr $8400 - distinct regions in this synthetic 64K RAM.
        vic.Write(0x9005, 0x01);
        vic.Write(0x900F, 0x19); // screenColor=1, borderColor=1, bit3 set = normal/not reversed (see Vic6560.ReverseMode)

        var screenAddr = (ushort)vic.ScreenAddr;
        var charAddr = (ushort)vic.CharAddr;
        memory.Write(screenAddr, 0x00); // char code 0, not reversed
        memory.Write((ushort)(charAddr + 0), 0b1000_0001); // top row of the glyph: leftmost + rightmost pixel set
        memory.Write(Vic20MemoryMap.ColorRamStart, 0x02); // ink color 2

        var buffer = new uint[display.PixelWidth * display.PixelHeight];
        display.Render(buffer);

        var inkArgb = Vic20Palette.ToArgb(2);
        var paperArgb = Vic20Palette.ToArgb(1); // screenColor
        buffer[0].Should().Be(inkArgb, "leftmost pixel of the glyph's top row is set");
        buffer[1].Should().Be(paperArgb, "second pixel is clear");
        buffer[7].Should().Be(inkArgb, "rightmost pixel is set");
    }

    private static (Vic20RasterDisplay Display, Vic6560 Vic, FakeMemoryBus Memory) CreateDisplay()
    {
        var vic = new Vic6560("VIC", 0x9000);
        var memory = new FakeMemoryBus();
        var display = new Vic20RasterDisplay(memory, vic);
        return (display, vic, memory);
    }

    /// <summary>Plain 64K RAM, no chip decode - lets a test write screen/char/color-RAM bytes at
    /// whatever CPU addresses Vic6560's own registers resolve to, without needing a full
    /// Vic20MemoryBus + real ROM images just to test the rasterizer in isolation.</summary>
    private sealed class FakeMemoryBus : IMemoryBus
    {
        private readonly byte[] _data = new byte[65536];
        public byte Read(ushort address) => _data[address];
        public void Write(ushort address, byte value) => _data[address] = value;
    }
}
