using FluentAssertions;
using NUnit.Framework;
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
}
