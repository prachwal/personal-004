using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Chips;

namespace PetEmulator.Vic20.Tests.Chips;

/// <summary>Layer 0 (see docs/vic20-migration-plan.md step 12): pure chip logic, no CPU/bus.</summary>
public sealed class Vic6560Tests
{
    [Test]
    public void ColumnsAndRows_DecodeFromRegister2And3()
    {
        var vic = new Vic6560(baseAddress: 0x9000);

        vic.Write(0x9002, 0x16); // columns = 0x16 & 0x7F = 22
        vic.Write(0x9003, 0x2F); // rows = (0x2F >> 1) & 0x3F = 23

        vic.Columns.Should().Be(22);
        vic.Rows.Should().Be(23);
    }

    [Test]
    public void ScreenAddr_TranslatesScreenMatrixBaseThroughA13Inversion()
    {
        var vic = new Vic6560(baseAddress: 0x9000);

        // Real default unexpanded VIC-20 screen at $1E00: ScreenMatrixBase must decode to it.
        vic.Write(0x9002, 0x16); // bit7 contributes to ScreenMatrixBase high bit
        vic.Write(0x9005, 0xF0); // high nibble -> ScreenMatrixBase bits 9:6

        Vic6560.ToCpuAddress(vic.ScreenMatrixBase).Should().Be((ushort)vic.ScreenAddr);
    }

    [Test]
    public void Reset_ClearsAllRegistersAndRaster()
    {
        var vic = new Vic6560(baseAddress: 0x9000);
        vic.Write(0x9002, 0xFF);
        vic.Tick(1000);

        vic.Reset();

        vic.Columns.Should().Be(0);
        vic.Raster.Should().Be(0);
    }

    [Test]
    public void Tick_AdvancesRasterOncePerCyclesPerLine_AndWrapsAtTotalScanlines()
    {
        var vic = new Vic6560(baseAddress: 0x9000);

        vic.Tick((ulong)Vic6560.CyclesPerLine);

        vic.Raster.Should().Be(1);
    }

    [Test]
    public void ReadWrite_RoundTripsThroughBaseAddressOffset()
    {
        var vic = new Vic6560(baseAddress: 0x9000);

        vic.Write(0x900F, 0x3C);

        vic.Read(0x900F).Should().Be(0x3C);
        vic.ScreenColor.Should().Be(0x03);
        vic.ReverseMode.Should().BeTrue();
    }

    [Test]
    public void Length_IsSixteenRegisters()
    {
        var vic = new Vic6560();

        vic.Length.Should().Be(16);
    }
}
