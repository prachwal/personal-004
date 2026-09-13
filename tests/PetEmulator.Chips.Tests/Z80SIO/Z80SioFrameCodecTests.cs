using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioFrameCodecTests
{
    [Test]
    public void AsyncCodecRoundTripsFormatsAndReportsParityAndFramingErrors()
    {
        var format = new Z80SioAsyncFormat(7, true, true, 2);
        var bits = Z80SioBitCodec.EncodeAsync(0x55, format);

        var decoded = Z80SioBitCodec.DecodeAsync(bits, format);

        decoded.Success.Should().BeTrue();
        decoded.Value.Should().Be(0x55);
        decoded.ConsumedBits.Should().Be(bits.Count);

        var parityCorrupt = bits.ToArray();
        parityCorrupt[8] = !parityCorrupt[8];
        Z80SioBitCodec.DecodeAsync(parityCorrupt, format).ParityError.Should().BeTrue();
        var framingCorrupt = bits.ToArray();
        framingCorrupt[^1] = false;
        Z80SioBitCodec.DecodeAsync(framingCorrupt, format).FramingError.Should().BeTrue();
    }

    [Test]
    public void SyncCodecRecognizesEightAndSixteenBitCharacters()
    {
        Z80SioSyncCodec.IsSynchronized(new byte[] { 0x11, 0x16, 0x22 }, Z80SioFrameMode.Sync8, 0x16)
            .Should().BeTrue();
        Z80SioSyncCodec.IsSynchronized(new byte[] { 0x12, 0x34, 0x56 }, Z80SioFrameMode.Sync16, 0x3456)
            .Should().BeTrue();
        Z80SioSyncCodec.IsSynchronized(new byte[] { 0x12, 0x35 }, Z80SioFrameMode.Sync16, 0x3456)
            .Should().BeFalse();
    }

    [Test]
    public void SdlcRoundTripUsesFlagsAndZeroStuffing()
    {
        var payload = new byte[] { 0x00, 0xF8, 0x1F, 0x7E };
        var bits = Z80SioSdlcCodec.EncodeFrame(payload);

        bits.Count.Should().BeGreaterThan(8 * (payload.Length + 4));
        Z80SioSdlcCodec.DecodeFrame(bits).Should().BeEquivalentTo(
            new Z80SioSdlcDecodeResult(true, payload, true, false, true));
    }

    [Test]
    public void SdlcRejectsBadCrcAndDetectsAbort()
    {
        var bits = Z80SioSdlcCodec.EncodeFrame(new byte[] { 0x01, 0x02 });
        var corrupted = bits.ToArray();
        corrupted[8] = !corrupted[8];
        var result = Z80SioSdlcCodec.DecodeFrame(corrupted);
        result.Success.Should().BeFalse();
        result.CrcValid.Should().BeFalse();
        result.EndOfFrame.Should().BeTrue();

        var withAbort = Z80SioSdlcCodec.EncodeFrame(new byte[] { 0x44 }).Take(8).Concat(Z80SioSdlcCodec.EncodeAbort()).ToArray();
        Z80SioSdlcCodec.DecodeFrame(withAbort).Aborted.Should().BeTrue();
    }

    [Test]
    public void SdlcCrcMatchesX25ReferenceVector()
    {
        Z80SioCrc.ComputeSdlc("123456789"u8.ToArray()).Should().Be(0x906E);
    }
}
