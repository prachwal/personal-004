using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class MOS2114Tests
{
    [Test]
    public void Constructor_ExposesNameLengthAndMappedBoundary()
    {
        var colorRam = new MOS2114("Color RAM", 0x9400, 4);

        colorRam.Name.Should().Be("Color RAM");
        colorRam.Length.Should().Be(4);
        colorRam.Write(0x9403, 0x0A);
        colorRam.Read(0x9403).Should().Be(0x0A);
    }

    [Test]
    public void Constructor_RejectsZeroSizeAndAddressSpaceOverflow()
    {
        FluentActions.Invoking(() => new MOS2114(size: 0))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new MOS2114(baseAddress: 0xFFFF, size: 2))
            .Should().Throw<ArgumentOutOfRangeException>();
    }
    [Test]
    public void ReadWrite_MasksToLowNibble()
    {
        var colorRam = new MOS2114(baseAddress: 0x9400);

        colorRam.Write(0x9400, 0xFF);

        colorRam.Read(0x9400).Should().Be(0x0F, "real hardware wires only 4 data-bus lines");
    }

    [Test]
    public void OffsetsAreRelativeToBaseAddress()
    {
        var colorRam = new MOS2114(baseAddress: 0x9400);

        colorRam.Write(0x9401, 0x05);

        colorRam.Read(0x9401).Should().Be(0x05);
        colorRam.Read(0x9400).Should().Be(0x00, "each offset is independent");
    }

    [Test]
    public void Reset_ClearsAllCells()
    {
        var colorRam = new MOS2114(baseAddress: 0x9400, size: 4);
        colorRam.Write(0x9400, 0x0F);

        colorRam.Reset();

        colorRam.Read(0x9400).Should().Be(0x00);
    }

    [Test]
    public void ReadAndWrite_RejectAddressesOutsideTheMappedRange()
    {
        var colorRam = new MOS2114(baseAddress: 0x9400, size: 4);

        FluentActions.Invoking(() => colorRam.Read(0x93FF))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => colorRam.Write(0x9404, 0x01))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Tick_DoesNotModifyStoredNibbles()
    {
        var colorRam = new MOS2114(baseAddress: 0x9400, size: 1);
        colorRam.Write(0x9400, 0x0B);

        colorRam.Tick(10_000);

        colorRam.Read(0x9400).Should().Be(0x0B);
    }
}
