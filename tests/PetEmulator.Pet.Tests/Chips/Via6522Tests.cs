using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Chips;

namespace PetEmulator.Pet.Tests.Chips;

[TestFixture]
public sealed class Via6522Tests
{
    [Test]
    public void Reset_clears_registers_and_exposes_sixteen_local_addresses()
    {
        var via = new Via6522();
        via.Write(Via6522.Ddra, 0xFF);
        via.Write(Via6522.Ora, 0xA5);

        via.Reset();

        via.Length.Should().Be(16);
        via.ORA.Should().Be(0);
        via.ORB.Should().Be(0);
        via.DDRA.Should().Be(0);
        via.DDRB.Should().Be(0);
        via.Read(Via6522.AuxiliaryControl).Should().Be(0);
        via.Read(16).Should().Be(0xFF);
        var act = () => via.Write(16, 0xFF);
        act.Should().NotThrow();
    }

    [Test]
    public void All_sixteen_register_offsets_are_mapped()
    {
        var via = new Via6522();

        for (ushort offset = 0; offset < via.Length; offset++)
        {
            var localOffset = offset;
            var writeAct = () => via.Write(localOffset, 0);
            var readAct = () => via.Read(localOffset);
            writeAct.Should().NotThrow();
            readAct.Should().NotThrow();
        }
    }

    [Test]
    public void Port_reads_combine_output_latches_directions_and_external_inputs()
    {
        var via = new Via6522 { PortAInput = 0x3C, PortBInput = 0xC3 };
        via.Write(Via6522.Ddra, 0xF0);
        via.Write(Via6522.Ddrb, 0x0F);
        via.Write(Via6522.Ora, 0xA5);
        via.Write(Via6522.Orb, 0x5A);

        via.Read(Via6522.Ora).Should().Be(0xAC);
        via.Read(Via6522.Orb).Should().Be(0xCA);
        via.PortAOutput.Should().Be(0xA0);
        via.PortBOutput.Should().Be(0x0A);
        via.Read(Via6522.OraWithoutHandshake).Should().Be(0xAC);
    }

    [Test]
    public void Interrupt_flags_enable_register_and_irq_are_aggregated()
    {
        var via = new Via6522();
        via.Write(Via6522.InterruptEnable, (byte)(Via6522.AnyInterrupt | Via6522.Ca1Interrupt));
        via.Write(Via6522.PeripheralControl, 0x01);
        via.CA1 = true;
        via.Update();

        via.Read(Via6522.InterruptEnable).Should().Be(0x82);
        via.Read(Via6522.InterruptFlag).Should().Be(0x82);
        via.IRQ.Should().BeTrue();

        via.Write(Via6522.InterruptFlag, Via6522.Ca1Interrupt);
        via.IRQ.Should().BeFalse();
    }

    [Test]
    public void Timer1_supports_one_shot_and_free_run()
    {
        var via = new Via6522();
        via.Write(Via6522.T1CounterLow, 0x01);
        via.Write(Via6522.T1CounterHigh, 0x00);
        via.Update();
        via.Update();
        (via.Read(Via6522.InterruptFlag) & Via6522.Timer1Interrupt).Should().NotBe(0);

        via.Write(Via6522.InterruptFlag, Via6522.Timer1Interrupt);
        via.Update();
        (via.Read(Via6522.InterruptFlag) & Via6522.Timer1Interrupt).Should().Be(0);

        via.Write(Via6522.AuxiliaryControl, 0x40);
        via.Write(Via6522.T1CounterLow, 0x00);
        via.Write(Via6522.T1CounterHigh, 0x00);
        via.Update();
        (via.Read(Via6522.InterruptFlag) & Via6522.Timer1Interrupt).Should().NotBe(0);
        via.Timer1Counter.Should().Be(0);
    }

    [Test]
    public void Timer2_is_a_one_shot_phi2_timer()
    {
        var via = new Via6522();
        via.Write(Via6522.T2CounterLow, 0x01);
        via.Write(Via6522.T2CounterHigh, 0x00);
        via.Update();
        via.Update();

        (via.Read(Via6522.InterruptFlag) & Via6522.Timer2Interrupt).Should().NotBe(0);
        via.Read(Via6522.T2CounterLow);
        via.Update();
        (via.Read(Via6522.InterruptFlag) & Via6522.Timer2Interrupt).Should().Be(0);
    }

    [Test]
    public void Pcr_configures_ca1_and_cb1_edges()
    {
        var via = new Via6522();
        via.Write(Via6522.PeripheralControl, 0x11);
        via.CA1 = true;
        via.CB1 = true;
        via.Update();

        (via.Read(Via6522.InterruptFlag) & (Via6522.Ca1Interrupt | Via6522.Cb1Interrupt))
            .Should().Be(Via6522.Ca1Interrupt | Via6522.Cb1Interrupt);
    }

    [Test]
    public void Pcr_configures_ca2_and_cb2_input_edges()
    {
        var via = new Via6522();
        via.Write(Via6522.PeripheralControl, 0x44);
        via.CA2 = true;
        via.CB2 = true;
        via.Update();

        (via.Read(Via6522.InterruptFlag) & (Via6522.Ca2Interrupt | Via6522.Cb2Interrupt))
            .Should().Be(Via6522.Ca2Interrupt | Via6522.Cb2Interrupt);
    }

    [Test]
    public void Shift_register_sets_its_interrupt_after_eight_phi2_clocks()
    {
        var via = new Via6522();
        via.Write(Via6522.AuxiliaryControl, 0x18);
        via.Write(Via6522.ShiftRegister, 0x80);
        for (var i = 0; i < 8; i++)
            via.Update();

        via.SR.Should().Be(0);
        (via.Read(Via6522.InterruptFlag) & Via6522.ShiftRegisterInterrupt).Should().NotBe(0);
    }

    [Test]
    public void Tick_advances_by_the_given_number_of_phi2_cycles()
    {
        var via = new Via6522();
        via.Write(Via6522.T1CounterLow, 0x02);
        via.Write(Via6522.T1CounterHigh, 0x00);

        via.Tick(3);

        (via.Read(Via6522.InterruptFlag) & Via6522.Timer1Interrupt).Should().NotBe(0);
    }

    [Test]
    public void KeepsTwoInstancesIndependentWhenMappedAtDifferentBaseAddresses()
    {
        var first = new Via6522("VIA1", 0xE840) { PortAInput = 0x11 };
        var second = new Via6522("VIA2", 0xE850) { PortAInput = 0x22 };

        first.Write((ushort)(0xE840 + Via6522.Ddra), 0xF0);
        first.Write((ushort)(0xE840 + Via6522.Ora), 0xA0);
        second.Write((ushort)(0xE850 + Via6522.Ddra), 0x0F);
        second.Write((ushort)(0xE850 + Via6522.Ora), 0x05);

        first.ORA.Should().Be(0xA0);
        second.ORA.Should().Be(0x05);
        first.Name.Should().Be("VIA1");
        second.Name.Should().Be("VIA2");
    }
}
