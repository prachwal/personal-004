using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Audio;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

/// <summary>Layer 0 (see docs/vic20/migration-plan.md step 12): pure chip logic, no CPU/bus.</summary>
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
    public void ControlPortInputs_AreReadThroughLightPenAndPaddleRegisters()
    {
        var vic = new MOS6560(baseAddress: 0x9000);

        vic.StrobeLightPen(0x12, 0x34);
        vic.SetPaddlePosition(0x56, 0x78);

        vic.Read(0x9006).Should().Be(0x12);
        vic.Read(0x9007).Should().Be(0x34);
        vic.Read(0x9008).Should().Be(0x56);
        vic.Read(0x9009).Should().Be(0x78);
    }

    [Test]
    public void ControlPortInputRegisters_IgnoreCpuWrites_AndResetToZero()
    {
        var vic = new MOS6560(baseAddress: 0x9000);
        vic.StrobeLightPen(0x12, 0x34);
        vic.SetPaddlePosition(0x56, 0x78);

        vic.Write(0x9006, 0xFF);
        vic.Write(0x9007, 0xFF);
        vic.Write(0x9008, 0xFF);
        vic.Write(0x9009, 0xFF);

        vic.Read(0x9006).Should().Be(0x12);
        vic.Read(0x9007).Should().Be(0x34);
        vic.Read(0x9008).Should().Be(0x56);
        vic.Read(0x9009).Should().Be(0x78);

        vic.Reset();

        vic.Read(0x9006).Should().Be(0);
        vic.Read(0x9007).Should().Be(0);
        vic.Read(0x9008).Should().Be(0);
        vic.Read(0x9009).Should().Be(0);
    }

    [Test]
    public void ReverseMode_BitClear_IsReversed()
    {
        // Bit 3's real polarity is inverted from what its name suggests: 1 = normal, 0 = reversed
        // (confirmed via the real KERNAL boot value $1B - see MOS6560.ReverseMode's doc comment
        // and docs/vic20/rendering-fixes.md). This is the case the round-trip test above didn't
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
    public void DefaultStandardUsesNtscTiming()
    {
        var vic = new MOS6560();

        vic.Standard.Should().Be(MOS6560Standard.Ntsc);
        vic.Phi2.Should().Be(MOS6560.Phi2Ntsc);
        vic.TimingTotalScanlines.Should().Be(MOS6560.TotalScanlines);
        vic.TimingCyclesPerLine.Should().Be(MOS6560.CyclesPerLine);
    }

    [Test]
    public void PalStandardUses6561TimingAndWrapsAfter312Lines()
    {
        var vic = new MOS6560(standard: MOS6560Standard.Pal);

        vic.Standard.Should().Be(MOS6560Standard.Pal);
        vic.Phi2.Should().Be(MOS6560.Phi2Pal);
        vic.TimingTotalScanlines.Should().Be(312);
        vic.TimingCyclesPerLine.Should().Be(71);

        vic.Tick(70);
        vic.Raster.Should().Be(0);
        vic.Tick(1);
        vic.Raster.Should().Be(1);
        vic.Tick((ulong)(71 * 311));
        vic.Raster.Should().Be(0);
    }

    [Test]
    public void PalAudioUsesPalPhi2ForFrequencyRegisters()
    {
        var vic = new MOS6560(standard: MOS6560Standard.Pal);
        vic.Write(0x000A, 0xF0);

        vic.Oscillator1Frequency.Should().BeApproximately(MOS6560.Phi2Pal / 256 / 16, 0.01);
    }

    [Test]
    public void ToCpuAddressMapsAllA13SelectionsAndMasksTo14Bits()
    {
        MOS6560.ToCpuAddress(0x0000).Should().Be(0x8000);
        MOS6560.ToCpuAddress(0x1FFF).Should().Be(0x9FFF);
        MOS6560.ToCpuAddress(0x2000).Should().Be(0);
        MOS6560.ToCpuAddress(0x3FFF).Should().Be(0x1FFF);
        MOS6560.ToCpuAddress(0x7FFF).Should().Be(0x1FFF);
    }

    [Test]
    public void AudioMixesAllToneGeneratorsAndResetsNoiseDeterministically()
    {
        var vic = new MOS6560();
        vic.Write(0x000A, 0xFF);
        vic.Write(0x000B, 0xFF);
        vic.Write(0x000C, 0xFF);
        vic.Write(0x000D, 0xFF);
        vic.Write(0x000E, 0x0F);
        var first = new AudioFrame[256];
        vic.Render(first);

        vic.Reset();
        vic.Write(0x000D, 0xFF);
        vic.Write(0x000E, 0x0F);
        var noiseBeforeReset = new AudioFrame[256];
        vic.Render(noiseBeforeReset);
        vic.Reset();
        vic.Write(0x000D, 0xFF);
        vic.Write(0x000E, 0x0F);
        var noiseAfterReset = new AudioFrame[256];
        vic.Render(noiseAfterReset);

        first.Should().Contain(frame => frame.Left != 0);
        noiseAfterReset.Should().Equal(noiseBeforeReset);
    }

    [Test]
    public void ExposesAllDecodedVideoAndAudioProperties()
    {
        var vic = new MOS6560("TEST", 0x9000);
        vic.Write(0x9002, 0x96);
        vic.Write(0x9003, 0x2F);
        vic.Write(0x9005, 0xAF);
        vic.Write(0x900A, 0x80);
        vic.Write(0x900B, 0x80);
        vic.Write(0x900C, 0x80);
        vic.Write(0x900D, 0x80);
        vic.Write(0x900E, 0xBF);
        vic.Write(0x900F, 0x3F);
        vic.SampleRate = 48_000;

        vic.Name.Should().Be("TEST");
        vic.Format.Should().Be(new AudioFormat(48_000, 2));
        vic.DoubleHeightChars.Should().BeTrue();
        vic.CharMatrixBase.Should().Be(0x3C00);
        vic.CharAddr.Should().Be(MOS6560.ToCpuAddress(vic.CharMatrixBase));
        vic.ColorMatrixOffset.Should().Be(vic.ScreenMatrixBase & 0x3FF);
        vic.AuxColor.Should().Be(0x0B);
        vic.BorderColor.Should().Be(7);
        vic.Oscillator1Enabled.Should().BeTrue();
        vic.Oscillator2Enabled.Should().BeTrue();
        vic.Oscillator3Enabled.Should().BeTrue();
        vic.NoiseEnabled.Should().BeTrue();
        vic.Volume.Should().Be(0x0F);
    }

    [Test]
    public void ReadsEveryRegisterOffsetAndUsesReadOnlyExternalValues()
    {
        var vic = new MOS6560(baseAddress: 0x9000);
        vic.StrobeLightPen(1, 2);
        vic.SetPaddlePosition(3, 4);

        for (ushort offset = 0; offset < MOS6560.RegisterCount; offset++)
            _ = vic.Read((ushort)(0x9000 + offset));

        vic.Read(0x9006).Should().Be(1);
        vic.Read(0x9007).Should().Be(2);
        vic.Read(0x9008).Should().Be(3);
        vic.Read(0x9009).Should().Be(4);
    }

    [Test]
    public void NoiseRestoresTheNonZeroLfsrIfAZeroStateIsObserved()
    {
        var vic = new MOS6560 { SampleRate = 44_100 };
        vic.Write(0x000D, 0xFF);
        vic.Write(0x000E, 0x0F);
        var lfsr = typeof(MOS6560).GetField("_noiseLfsr", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        lfsr.SetValue(vic, (ushort)0);

        var frames = new AudioFrame[3];
        vic.Render(frames);

        frames.Should().Contain(frame => frame.Left != 0);
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
        risingEdges.Should().BeInRange(3_950, 4_050);
    }

    [Test]
    public void EnabledFrequencyUsesTheEnableBitAsPartOfTheVicDivider()
    {
        var vic = new MOS6560(baseAddress: 0x9000);

        vic.Write(0x900A, 0xF0);

        vic.Oscillator1Frequency.Should().BeApproximately(MOS6560.Phi2Ntsc / 256 / 16, 0.01);
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
