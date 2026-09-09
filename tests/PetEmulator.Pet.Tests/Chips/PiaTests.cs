using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Chips;

namespace PetEmulator.Pet.Tests.Chips;

[TestFixture]
public sealed class PiaTests
{
    [Test]
    public void SelectsDirectionRegistersAndCombinesInputPinsWithOutputLatches()
    {
        var pia = new Pia { PortAInput = () => 0x3C, PortBInput = () => 0xC3 };

        pia.Write(0, 0xF0);
        pia.Write(2, 0x0F);
        pia.Write(1, 0x04);
        pia.Write(3, 0x04);
        pia.Write(0, 0xA5);
        pia.Write(2, 0x5A);

        pia.DDRA.Should().Be(0xF0);
        pia.DDRB.Should().Be(0x0F);
        pia.ORA.Should().Be(0xA5);
        pia.ORB.Should().Be(0x5A);
        pia.Read(0).Should().Be(0xAC);
        pia.Read(2).Should().Be(0xCA);
    }

    [Test]
    public void UsesPortBindingsForInputAndOutputLatchWrites()
    {
        var writes = new List<byte>();
        var pia = new Pia { PortAInput = () => 0x0F, PortAWritten = writes.Add };

        pia.Write(0, 0xF0);
        pia.Write(1, 0x04);
        pia.Write(0, 0xA5);

        writes.Should().Equal(0xA5);
        pia.ORA.Should().Be(0xA5);
        pia.Read(0).Should().Be(0xAF);
    }

    [Test]
    public void DetectsConfiguredEdgesOnAllControlInputs()
    {
        var pia = new Pia();

        pia.Write(1, 0x07); // CA1 rising, enabled, data selected.
        pia.CA1 = true;
        (pia.Read(1) & 0x80).Should().Be(0x80);

        pia.Reset();
        pia.Write(1, 0x05); // CA1 falling, enabled, data selected.
        pia.CA1 = true;
        pia.CA1 = false;
        (pia.Read(1) & 0x80).Should().Be(0x80);

        pia.Reset();
        pia.Write(1, 0x1C); // CA2 rising, enabled, data selected.
        pia.CA2 = true;
        (pia.Read(1) & 0x40).Should().Be(0x40);

        pia.Reset();
        pia.Write(1, 0x0C); // CA2 falling, enabled, data selected.
        pia.CA2 = true;
        pia.CA2 = false;
        (pia.Read(1) & 0x40).Should().Be(0x40);

        pia.Reset();
        pia.Write(3, 0x07); // CB1 rising, enabled, data selected.
        pia.CB1 = true;
        (pia.Read(3) & 0x80).Should().Be(0x80);

        pia.Reset();
        pia.Write(3, 0x05); // CB1 falling, enabled, data selected.
        pia.CB1 = true;
        pia.CB1 = false;
        (pia.Read(3) & 0x80).Should().Be(0x80);

        pia.Reset();
        pia.Write(3, 0x1C); // CB2 rising, enabled, data selected.
        pia.CB2 = true;
        (pia.Read(3) & 0x40).Should().Be(0x40);

        pia.Reset();
        pia.Write(3, 0x0C); // CB2 falling, enabled, data selected.
        pia.CB2 = true;
        pia.CB2 = false;
        (pia.Read(3) & 0x40).Should().Be(0x40);
    }

    [Test]
    public void UpdatesIrqOutputsAndClearsOnlyAfterDataRegisterReads()
    {
        var pia = new Pia();

        pia.Write(1, 0x06); // CA1 rising, interrupt masked.
        pia.CA1 = true;
        (pia.Read(1) & 0x80).Should().Be(0x80);
        pia.IRQA.Should().BeFalse();
        pia.IRQ.Should().BeFalse();

        pia.Write(1, 0x07);
        pia.IRQA.Should().BeTrue();
        pia.Write(1, 0x03); // Select DDRA: its read must not acknowledge IRQA.
        pia.Read(0);
        pia.IRQ.Should().BeTrue();

        pia.Write(1, 0x07);
        pia.Read(0);
        pia.IRQA.Should().BeFalse();
        pia.IRQ.Should().BeFalse();

        pia.Write(3, 0x1C); // CB2 rising, enabled.
        pia.CB2 = true;
        pia.IRQB.Should().BeTrue();
        pia.IRQ.Should().BeTrue();
        pia.Read(2);
        pia.IRQB.Should().BeFalse();
    }

    [Test]
    public void ResetClearsRegistersControlInputsFlagsAndIrq()
    {
        var pia = new Pia();
        pia.Write(0, 0xFF);
        pia.Write(1, 0x07);
        pia.Write(0, 0xA5);
        pia.CA1 = true;

        pia.Reset();

        pia.DDRA.Should().Be(0);
        pia.DDRB.Should().Be(0);
        pia.ORA.Should().Be(0);
        pia.ORB.Should().Be(0);
        pia.CA1.Should().BeFalse();
        pia.CA2.Should().BeFalse();
        pia.CB1.Should().BeFalse();
        pia.CB2.Should().BeFalse();
        pia.Read(1).Should().Be(0);
        pia.Read(3).Should().Be(0);
        pia.IRQ.Should().BeFalse();
    }

    [Test]
    public void KeepsTwoInstancesIndependentWhenMappedAtDifferentBaseAddresses()
    {
        var first = new Pia("PIA1", 0xE810) { PortAInput = () => 0x11 };
        var second = new Pia("PIA2", 0xE820) { PortAInput = () => 0x22 };

        first.Write(0xE810, 0xF0);
        first.Write(0xE811, 0x04);
        first.Write(0xE810, 0xA0);
        second.Write(0xE820, 0x0F);
        second.Write(0xE821, 0x04);
        second.Write(0xE820, 0x05);

        first.Read(0xE810).Should().Be(0xA1);
        second.Read(0xE820).Should().Be(0x25);
        first.ORA.Should().Be(0xA0);
        second.ORA.Should().Be(0x05);
        first.DDRA.Should().Be(0xF0);
        second.DDRA.Should().Be(0x0F);
        first.Name.Should().Be("PIA1");
        second.Name.Should().Be("PIA2");
    }

    [Test]
    public void DefaultConstructorUsesPiaNameAndZeroBaseAddress()
    {
        var pia = new Pia();

        pia.Name.Should().Be("PIA");
    }

    [Test]
    public void SelectPortADataRegisterOverridesTheStandardBit2Rule()
    {
        var pia = new Pia
        {
            SelectPortADataRegister = control => control == 0x30 || (control & 0x04) != 0
        };

        pia.Write(1, 0x38); // bit 2 clear, not $30 -> DDR still selected via the fallback
        pia.Write(0, 0xFF);
        pia.Write(1, 0x30); // bit 2 clear, but $30 -> data selected per the override
        pia.Write(0, 0xAA);

        pia.DDRA.Should().Be(0xFF);
        pia.ORA.Should().Be(0xAA);
        pia.Read(0).Should().Be(0xAA); // all-output DDRA: input pin ignored
    }

    [Test]
    public void SelectPortADataRegisterDefaultsToStandardBit2RuleWhenNotSet()
    {
        var pia = new Pia();

        pia.Write(1, 0x30); // bit 2 clear -> standard rule selects DDR
        pia.Write(0, 0xAA);

        pia.DDRA.Should().Be(0xAA);
        pia.ORA.Should().Be(0x00);
    }

    [Test]
    public void TickIsANoOp()
    {
        var pia = new Pia { PortAInput = () => 0x3C };
        pia.Write(0, 0xF0);
        pia.Write(0, 0xA5);
        var before = pia.Read(0);

        pia.Tick(1_000_000);

        pia.Read(0).Should().Be(before);
    }
}
