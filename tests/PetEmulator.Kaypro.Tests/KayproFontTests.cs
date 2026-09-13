using NUnit.Framework;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproFontTests
{
    [Test]
    public void ScreenCodesIgnoreBitSeven()
    {
        var font = KayproFont.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "roms", "kaypro", "kaypro-81-146.bin"));
        for (var code = 0; code < 128; code++)
            for (var row = 0; row < KayproFont.GlyphHeight; row++)
                Assert.That(font.GetRow((byte)code, row), Is.EqualTo(font.GetRow((byte)(code | 0x80), row)));
    }

    [Test]
    public void PrintableGlyphsAreLoadedAndSpaceIsBlank()
    {
        var font = KayproFont.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "roms", "kaypro", "kaypro-81-146.bin"));
        Assert.That(Enumerable.Range(0, 8).Select(row => font.GetRow((byte)' ', row)).Sum(row => row), Is.EqualTo(0));
        Assert.That(Enumerable.Range(0, 8).Select(row => font.GetRow((byte)'A', row)).Sum(row => row), Is.GreaterThan(0));
    }

    [Test]
    public void VideoRasterMapsAsciiAndHighBitScreenCodesToTheSameGlyph()
    {
        var font = KayproFont.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "roms", "kaypro", "kaypro-81-146.bin"));
        var plain = new KayproVideo();
        var highBit = new KayproVideo();
        plain.Write(0, (byte)'A');
        highBit.Write(0, (byte)('A' | 0x80));
        var first = new uint[KayproVideo.PixelWidth * KayproVideo.PixelHeight];
        var second = new uint[first.Length];

        plain.Render(first, font);
        highBit.Render(second, font);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first.Any(pixel => pixel == 0xFF33FF66), Is.True);
    }
}
