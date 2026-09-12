using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

[TestFixture]
public sealed class MT6520Tests
{
    [Test]
    public void SelectsDirectionRegistersAndCombinesInputPinsWithOutputLatches()
    {
        var pia = new MT6520 { PortAInput = () => 0x3C, PortBInput = () => 0xC3 };

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
        var reads = 0;
        var pia = new MT6520
        {
            PortAInput = () => 0x0F,
            PortAWritten = writes.Add,
            PortARead = () => reads++
        };

        pia.Write(0, 0xF0);
        pia.Write(1, 0x04);
        pia.Write(0, 0xA5);

        writes.Should().Equal(0xA5);
        pia.ORA.Should().Be(0xA5);
        pia.Read(0).Should().Be(0xAF);
        reads.Should().Be(1);
    }

    [Test]
    public void DetectsConfiguredEdgesOnAllControlInputs()
    {
        var pia = new MT6520();

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
        var pia = new MT6520();

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
        var pia = new MT6520();
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
        var first = new MT6520("PIA1", 0xE810) { PortAInput = () => 0x11 };
        var second = new MT6520("PIA2", 0xE820) { PortAInput = () => 0x22 };

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
        var pia = new MT6520();

        pia.Name.Should().Be("PIA");
        pia.Length.Should().Be(4);
        pia.IsCb2Output.Should().BeFalse();
        pia.HasInterrupt.Should().BeFalse();
    }

    [Test]
    public void RejectsAddressesOutsideTheFourRegisterWindow()
    {
        var pia = new MT6520("PIA", 0x1000);

        var read = () => pia.Read(0x0FFF);
        var write = () => pia.Write(0x1004, 0);

        read.Should().Throw<ArgumentOutOfRangeException>();
        write.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void UsesPortBDataCallbacksAndDirectionRegister()
    {
        var written = new List<byte>();
        var readCount = 0;
        var pia = new MT6520
        {
            PortBInput = () => 0x0F,
            PortBWritten = written.Add,
            PortBRead = () => readCount++
        };

        pia.Write(2, 0xF0);
        pia.Read(2).Should().Be(0xF0);
        pia.Write(3, 0x04);
        pia.Write(2, 0xA5);

        pia.Read(2).Should().Be(0xAF);
        written.Should().Equal(0xA5);
        readCount.Should().Be(1);
    }

    [Test]
    public void ReportsControlWritesAndMasksUnsupportedBits()
    {
        var controlA = new List<byte>();
        var controlB = new List<byte>();
        var pia = new MT6520
        {
            ControlAWritten = controlA.Add,
            ControlBWritten = controlB.Add
        };

        pia.Write(1, 0xFF);
        pia.Write(3, 0xFF);

        controlA.Should().Equal(0x3F);
        controlB.Should().Equal(0x3F);
        pia.Read(1).Should().Be(0x3F);
        pia.Read(3).Should().Be(0x3F);
        pia.IsCb2Output.Should().BeTrue();
    }

    [Test]
    public void PortBDataSelectionCanBeOverridden()
    {
        var pia = new MT6520
        {
            SelectPortBDataRegister = control => control == 0x30
        };

        pia.Write(3, 0x30);
        pia.Write(2, 0x5A);

        pia.DDRB.Should().Be(0);
        pia.ORB.Should().Be(0x5A);
        pia.Read(2).Should().Be(0);
    }

    [Test]
    public void InputControlLinesIgnoreWritesWhileConfiguredAsOutputs()
    {
        var pia = new MT6520();

        pia.Write(1, 0x20);
        pia.CA2 = true;
        pia.CA2.Should().BeTrue();

        pia.Write(3, 0x20);
        pia.CB2 = true;
        pia.CB2.Should().BeTrue();
        pia.IRQA.Should().BeFalse();
        pia.IRQB.Should().BeFalse();
    }

    [Test]
    public void ControlTwoInputFlagDoesNotRaiseIrqWhenItsMaskIsDisabled()
    {
        var pia = new MT6520();

        pia.Write(1, 0x14); // CA2 rising edge, input interrupt disabled.
        pia.CA2 = true;

        (pia.Read(1) & 0x40).Should().Be(0x40);
        pia.IRQA.Should().BeFalse();

        pia.Reset();
        pia.Write(1, 0x1C);
        pia.CA2 = true;
        pia.Write(1, 0x20); // Preserve the flag while changing CA2 to output mode.
        pia.IRQA.Should().BeFalse();
    }

    [Test]
    public void RepeatedControlLevelsDoNotCreateEdgesOrRestoreOutputs()
    {
        var ca2Changes = new List<bool>();
        var cb2Changes = new List<bool>();
        var pia = new MT6520
        {
            Ca2OutputChanged = ca2Changes.Add,
            Cb2OutputChanged = cb2Changes.Add
        };

        pia.Write(1, 0x27);
        pia.CA1 = false;
        pia.CA1 = true;
        pia.CA1 = true;
        pia.Write(3, 0x27);
        ca2Changes.Clear();
        cb2Changes.Clear();
        pia.CB1 = false;
        pia.CB1 = true;
        pia.CB1 = true;

        ca2Changes.Should().BeEmpty();
        cb2Changes.Should().BeEmpty();
        pia.IRQA.Should().BeTrue();
        pia.IRQB.Should().BeTrue();
    }

    [Test]
    public void NotifiesOutputTransitionsAndHandlesPartialPulseTicks()
    {
        var ca2Changes = new List<bool>();
        var cb2Changes = new List<bool>();
        var pia = new MT6520
        {
            PortAInput = () => 0,
            Ca2OutputChanged = ca2Changes.Add,
            Cb2OutputChanged = cb2Changes.Add
        };

        pia.Write(1, 0x2C);
        pia.Write(3, 0x2C);
        pia.Read(0);
        pia.Write(2, 0x55);
        pia.Tick(0);
        pia.CA2.Should().BeFalse();
        pia.CB2.Should().BeFalse();
        pia.Tick(1);

        ca2Changes.Should().Equal(true, false, true);
        cb2Changes.Should().Equal(true, false, true);
    }

    [Test]
    public void SelectPortADataRegisterOverridesTheStandardBit2Rule()
    {
        var pia = new MT6520
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
        var pia = new MT6520();

        pia.Write(1, 0x30); // bit 2 clear -> standard rule selects DDR
        pia.Write(0, 0xAA);

        pia.DDRA.Should().Be(0xAA);
        pia.ORA.Should().Be(0x00);
    }

    [Test]
    public void ManualOutputMode_SetsCa2AndCb2LevelDirectlyFromControlRegister()
    {
        var pia = new MT6520();

        pia.Write(1, 0x38); // CRA: output(bit5) + manual(bit4) + level=high(bit3)
        pia.CA2.Should().BeTrue();
        pia.Write(1, 0x30); // level=low
        pia.CA2.Should().BeFalse();

        pia.Write(3, 0x38);
        pia.CB2.Should().BeTrue();
        pia.Write(3, 0x30);
        pia.CB2.Should().BeFalse();
    }

    [Test]
    public void Ca2HandshakeMode_GoesLowOnPortARead_RestoresOnNextActiveCa1Edge()
    {
        var pia = new MT6520 { PortAInput = () => 0 };
        // CRA: data-select(bit2) + CA1 active=rising(bit1) + CA2 output(bit5) handshake(bit4=0,bit3=0)
        pia.Write(1, 0x26);

        pia.CA2.Should().BeTrue("handshake output idles high until a Port A read pulls it low");

        pia.Read(0);
        pia.CA2.Should().BeFalse("reading Port A's data register should drop CA2 in handshake mode");

        pia.Tick(1_000);
        pia.CA2.Should().BeFalse("handshake mode waits for a CA1 edge, not time, to restore");

        pia.CA1 = true; // configured active edge
        pia.CA2.Should().BeTrue("the next active CA1 edge should restore CA2 high");
    }

    [Test]
    public void Ca2PulseMode_AutoRestoresAfterOneCycle_WithoutNeedingACa1Edge()
    {
        var pia = new MT6520 { PortAInput = () => 0 };
        // CRA: data-select(bit2) + CA2 output(bit5) pulse(bit4=0,bit3=1)
        pia.Write(1, 0x2C);

        pia.Read(0);
        pia.CA2.Should().BeFalse();

        pia.Tick(1);

        pia.CA2.Should().BeTrue("pulse mode restores on its own after one cycle, with no CA1 edge");
    }

    [Test]
    public void Cb2HandshakeMode_GoesLowOnPortBWrite_RestoresOnNextActiveCb1Edge()
    {
        var pia = new MT6520();
        // CRB: data-select(bit2) + CB1 active=rising(bit1) + CB2 output(bit5) handshake(bit4=0,bit3=0)
        pia.Write(3, 0x26);

        pia.CB2.Should().BeTrue();

        pia.Write(2, 0x55); // Port B write is CB2's trigger (the write-side mirror of CA2's read-side trigger)
        pia.CB2.Should().BeFalse();

        pia.Tick(1_000);
        pia.CB2.Should().BeFalse("handshake mode waits for a CB1 edge, not time, to restore");

        pia.CB1 = true;
        pia.CB2.Should().BeTrue();
    }

    [Test]
    public void Cb2PulseMode_AutoRestoresAfterOneCycle_WithoutNeedingACb1Edge()
    {
        var pia = new MT6520();
        // CRB: data-select(bit2) + CB2 output(bit5) pulse(bit4=0,bit3=1)
        pia.Write(3, 0x2C);

        pia.Write(2, 0x55);
        pia.CB2.Should().BeFalse();

        pia.Tick(1);

        pia.CB2.Should().BeTrue();
    }

    [Test]
    public void TickIsANoOp()
    {
        var pia = new MT6520 { PortAInput = () => 0x3C };
        pia.Write(0, 0xF0);
        pia.Write(0, 0xA5);
        var before = pia.Read(0);

        pia.Tick(1_000_000);

        pia.Read(0).Should().Be(before);
    }
}
