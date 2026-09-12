using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Core;

namespace PetEmulator.Chips.Tests;

public sealed class MOS6702Tests
{
    private const ushort Base = 0xEFE0;

    [Test]
    public void Reset_ReturnsD6FromEveryAlias()
    {
        var dongle = new MOS6702(Base);

        for (var offset = 0; offset < 4; offset++)
            dongle.Read((ushort)(Base + offset)).Should().Be(0xD6);
    }

    [Test]
    public void Variant_UsesItsInitialValueAndShiftRegisterLengths()
    {
        var variant = new MOS6702Variant(0xA5, [1, 1, 1, 1, 1, 1, 1, 1]);
        var dongle = new MOS6702(Base, variant: variant);

        dongle.Variant.Should().BeSameAs(variant);
        dongle.Name.Should().Be("MOS6702");
        dongle.Output.Should().Be(0xA5);
        dongle.Tick(1);

        dongle.Write(Base, 0x00);
        dongle.Write(Base, 0x01);

        dongle.Read(Base).Should().Be(0x00);
    }

    [Test]
    public void Constructor_RejectsAnAddressOverflow()
    {
        FluentActions.Invoking(() => new MOS6702(0xFFFD))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Variant_RejectsMissingOrInvalidShiftLengths()
    {
        FluentActions.Invoking(() => new MOS6702Variant(0, null!))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new MOS6702Variant(0, [1, 2]))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new MOS6702Variant(0, [1, 1, 1, 1, 1, 1, 1, 9]))
            .Should().Throw<ArgumentException>();
    }

    [Test]
    public void Observer_ReceivesReadAndWriteAccesses()
    {
        var accesses = new List<BusAccess>();
        var dongle = new MOS6702(Base) { Observer = accesses.Add };

        dongle.Write(Base, 0x00);
        dongle.Read(Base);

        accesses.Should().HaveCount(2);
        accesses[0].IsWrite.Should().BeTrue();
        accesses[1].IsWrite.Should().BeFalse();
    }

    [Test]
    public void OddWriteWithoutEvenPrelude_IsIgnored()
    {
        var dongle = new MOS6702(Base);

        dongle.Write(Base, 0x35);

        dongle.Read(Base).Should().Be(0xD6);
    }

    [Test]
    public void EvenThenOdd_AdvancesTheCircularShiftRegisters()
    {
        var dongle = new MOS6702(Base);

        dongle.Write(Base, 0x12);
        dongle.Write((ushort)(Base + 3), 0x35);
        dongle.Write(Base, 0x42);
        dongle.Write((ushort)(Base + 3), 0x77);

        dongle.Read((ushort)(Base + 1)).Should().Be(0x56);
    }

    [Test]
    public void RepeatedEvenOrOddWrites_DoNotAdvanceTheStateTwice()
    {
        var dongle = new MOS6702(Base);

        dongle.Write(Base, 0x12);
        dongle.Write(Base, 0x14);
        dongle.Write(Base, 0x35);
        var afterFirstOdd = dongle.Read(Base);
        dongle.Write(Base, 0x37);

        dongle.Read(Base).Should().Be(afterFirstOdd);
    }

    [Test]
    public void Reset_RestoresTheInitialSequenceState()
    {
        var dongle = new MOS6702(Base);
        dongle.Write(Base, 0x12);
        dongle.Write(Base, 0x35);

        dongle.Reset();
        dongle.Write(Base, 0x12);
        dongle.Write(Base, 0x35);

        dongle.Read(Base).Should().Be(0xD6);
    }

    [Test]
    public void AccessOutsideFourAliases_IsRejected()
    {
        var dongle = new MOS6702(Base);

        FluentActions.Invoking(() => dongle.Read((ushort)(Base - 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => dongle.Write((ushort)(Base + 4), 0))
            .Should().Throw<ArgumentOutOfRangeException>();
    }
}
