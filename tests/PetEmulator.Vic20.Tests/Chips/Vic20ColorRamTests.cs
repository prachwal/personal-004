using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Chips;

namespace PetEmulator.Vic20.Tests.Chips;

public sealed class Vic20ColorRamTests
{
    [Test]
    public void ReadWrite_MasksToLowNibble()
    {
        var colorRam = new Vic20ColorRam(baseAddress: 0x9400);

        colorRam.Write(0x9400, 0xFF);

        colorRam.Read(0x9400).Should().Be(0x0F, "real hardware wires only 4 data-bus lines");
    }

    [Test]
    public void OffsetsAreRelativeToBaseAddress()
    {
        var colorRam = new Vic20ColorRam(baseAddress: 0x9400);

        colorRam.Write(0x9401, 0x05);

        colorRam.Read(0x9401).Should().Be(0x05);
        colorRam.Read(0x9400).Should().Be(0x00, "each offset is independent");
    }

    [Test]
    public void Reset_ClearsAllCells()
    {
        var colorRam = new Vic20ColorRam(baseAddress: 0x9400, size: 4);
        colorRam.Write(0x9400, 0x0F);

        colorRam.Reset();

        colorRam.Read(0x9400).Should().Be(0x00);
    }
}
