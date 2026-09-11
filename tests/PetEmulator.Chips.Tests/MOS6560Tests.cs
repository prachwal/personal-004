using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Audio;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

/// <summary>Layer 0 (see docs/vic20-migration-plan.md step 12): pure chip logic, no CPU/bus.</summary>
public sealed class MOS6560Tests
{
    [Test]
    public void ColumnsAndRows_DecodeFromRegister2And3()
    {
        var vic = new MOS6560(baseAddress: 0x9000);

        vic.Write(0x9002, 0x16); // columns = 0x16 & 0x7F = 22
        vic.Write(0x9003, 0x2F); // rows = (0x2F >> 1) & 0x3F = 23

        vic.Columns.Should().Be(22);
        vic.Rows.Should().Be(23);
    }

    [Test]
    public void ScreenAddr_TranslatesScreenMatrixBaseThroughA13Inversion()
    {
        var vic = new MOS6560(baseAddress: 0x9000);

        // Real default unexpanded VIC-20 screen at $1E00: ScreenMatrixBase must decode to it.
        vic.Write(0x9002, 0x16); // bit7 contributes to ScreenMatrixBase high bit
        vic.Write(0x9005, 0xF0); // high nibble -> ScreenMatrixBase bits 9:6

        MOS6560.ToCpuAddress(vic.ScreenMatrixBase).Should().Be((ushort)vic.ScreenAddr);
    }

    [Test]
    public void Reset_ClearsAllRegistersAndRaster()
    {
        var vic = new MOS6560(baseAddress: 0x9000);
        vic.Write(0x9002, 0xFF);
        vic.Tick(1000);

        vic.Reset();

        vic.Columns.Should().Be(0);
        vic.Raster.Should().Be(0);
    }

    [Test]
    public void Tick_AdvancesRasterOncePerCyclesPerLine_AndWrapsAtTotalScanlines()
    {
        var vic = new MOS6560(baseAddress: 0x9000);

        vic.Tick((ulong)MOS6560.CyclesPerLine);

        vic.Raster.Should().Be(1);
    }

    [Test]
    public void ReadWrite_RoundTripsThroughBaseAddressOffset()
    {
        var vic = new MOS6560(baseAddress: 0x9000);

        vic.Write(0x900F, 0x3C);

        vic.Read(0x900F).Should().Be(0x3C);
        vic.ScreenColor.Should().Be(0x03);
        vic.ReverseMode.Should().BeFalse(); // bit 3 set = normal (not reversed) - see ReverseMode's doc comment
    }

    [Test]
    public void ReverseMode_BitClear_IsReversed()
    {
        // Bit 3's real polarity is inverted from what its name suggests: 1 = normal, 0 = reversed
        // (confirmed via the real KERNAL boot value $1B - see MOS6560.ReverseMode's doc comment
        // and docs/vic20-rendering-fixes.md). This is the case the round-trip test above didn't
        // cover, letting the polarity bug through unnoticed.
        var vic = new MOS6560(baseAddress: 0x9000);

        vic.Write(0x900F, 0x34); // same as 0x3C but bit 3 cleared

        vic.ReverseMode.Should().BeTrue();
    }

    [Test]
    public void Length_IsSixteenRegisters()
    {
        var vic = new MOS6560();

        vic.Length.Should().Be(16);
    }

    [Test]
    public void Render_UsesConfiguredOscillatorFrequency()
    {
        var vic = new MOS6560(baseAddress: 0x9000);
        vic.Write(0x900A, 0xFF); // 127 raw, enabled
        vic.Write(0x900E, 0x0F);
        var frames = new AudioFrame[44_100];

        vic.Render(frames);

        var risingEdges = frames.Zip(frames.Skip(1))
            .Count(pair => pair.First.Left <= 0 && pair.Second.Left > 0);
        risingEdges.Should().BeInRange(29, 33);
    }

    [Test]
    public void Render_IsSilentWhenAllGeneratorsAreDisabled()
    {
        var vic = new MOS6560(baseAddress: 0x9000);
        var frames = new AudioFrame[128];

        vic.Render(frames);

        frames.Should().OnlyContain(frame => frame == new AudioFrame(0, 0));
    }

    [Test]
    public void Render_ScalesVolumeToSilence()
    {
        var vic = new MOS6560(baseAddress: 0x9000);
        vic.Write(0x900A, 0xFF);
        var silent = new AudioFrame[128];
        var loud = new AudioFrame[128];

        vic.Write(0x900E, 0x00);
        vic.Render(silent);
        vic.Write(0x900E, 0x0F);
        vic.Render(loud);

        silent.Max(frame => Math.Abs(frame.Left)).Should().Be(0);
        loud.Max(frame => Math.Abs(frame.Left)).Should().BeGreaterThan(0);
    }

    [Test]
    public void Render_NoiseIsNonSilentAndNotConstant()
    {
        var vic = new MOS6560(baseAddress: 0x9000);
        vic.Write(0x900D, 0xFF);
        vic.Write(0x900E, 0x0F);
        var frames = new AudioFrame[4_410];

        vic.Render(frames);

        frames.Select(frame => frame.Left).Distinct().Should().HaveCount(2);
        frames.Should().Contain(frame => frame.Left != 0);
    }
}
