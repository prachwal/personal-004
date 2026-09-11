using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

[TestFixture]
public sealed class MOS6522Tests
{
    [Test]
    public void Reset_clears_registers_and_exposes_sixteen_local_addresses()
    {
        var via = new MOS6522();
        via.Write(MOS6522.Ddra, 0xFF);
        via.Write(MOS6522.Ora, 0xA5);

        via.Reset();

        via.Length.Should().Be(16);
        via.ORA.Should().Be(0);
        via.ORB.Should().Be(0);
        via.DDRA.Should().Be(0);
        via.DDRB.Should().Be(0);
        via.Read(MOS6522.AuxiliaryControl).Should().Be(0);
        via.Read(16).Should().Be(0xFF);
        var act = () => via.Write(16, 0xFF);
        act.Should().NotThrow();
    }

    [Test]
    public void All_sixteen_register_offsets_are_mapped()
    {
        var via = new MOS6522();

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
        var via = new MOS6522 { PortAInput = 0x3C, PortBInput = 0xC3 };
        via.Write(MOS6522.Ddra, 0xF0);
        via.Write(MOS6522.Ddrb, 0x0F);
        via.Write(MOS6522.Ora, 0xA5);
        via.Write(MOS6522.Orb, 0x5A);

        via.Read(MOS6522.Ora).Should().Be(0xAC);
        via.Read(MOS6522.Orb).Should().Be(0xCA);
        via.PortAOutput.Should().Be(0xA0);
        via.PortBOutput.Should().Be(0x0A);
        via.Read(MOS6522.OraWithoutHandshake).Should().Be(0xAC);
    }

    [Test]
    public void Port_write_bindings_receive_only_the_output_bits()
    {
        var portAWrites = new List<byte>();
        var portBWrites = new List<byte>();
        var via = new MOS6522
        {
            PortAWritten = portAWrites.Add,
            PortBWritten = portBWrites.Add
        };

        via.Write(MOS6522.Ddra, 0xF0);
        via.Write(MOS6522.Ddrb, 0x0F);
        via.Write(MOS6522.OraWithoutHandshake, 0xA5);
        via.Write(MOS6522.Orb, 0x5A);

        portAWrites.Should().Equal(0x00, 0xA0);
        portBWrites.Should().Equal(0x00, 0x0A);
    }

    [Test]
    public void PortAHandshakeWriteInvokesItsBinding()
    {
        var writes = new List<byte>();
        var via = new MOS6522 { PortAWritten = writes.Add };

        via.Write(MOS6522.Ora, 0xA5);

        writes.Should().Equal(0);
    }

    [Test]
    public void Reset_releases_external_port_inputs_and_control_lines()
    {
        var via = new MOS6522
        {
            PortAInput = 0xFF,
            PortBInput = 0xFF,
            CA1 = true,
            CA2 = true,
            CB1 = true,
            CB2 = true
        };

        via.Reset();

        via.PortAInput.Should().Be(0);
        via.PortBInput.Should().Be(0);
        via.CA1.Should().BeFalse();
        via.CA2.Should().BeFalse();
        via.CB1.Should().BeFalse();
        via.CB2.Should().BeFalse();
    }

    [Test]
    public void Interrupt_flags_enable_register_and_irq_are_aggregated()
    {
        var via = new MOS6522();
        via.Write(MOS6522.InterruptEnable, (byte)(MOS6522.AnyInterrupt | MOS6522.Ca1Interrupt));
        via.Write(MOS6522.PeripheralControl, 0x01);
        via.CA1 = true;
        via.Update();

        via.Read(MOS6522.InterruptEnable).Should().Be(0x82);
        via.Read(MOS6522.InterruptFlag).Should().Be(0x82);
        via.IRQ.Should().BeTrue();

        via.Write(MOS6522.InterruptFlag, MOS6522.Ca1Interrupt);
        via.IRQ.Should().BeFalse();
    }

    [Test]
    public void Timer1_supports_one_shot_and_free_run()
    {
        var via = new MOS6522();
        via.Write(MOS6522.T1CounterLow, 0x01);
        via.Write(MOS6522.T1CounterHigh, 0x00);
        via.Update();
        via.Update();
        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer1Interrupt).Should().NotBe(0);

        via.Write(MOS6522.InterruptFlag, MOS6522.Timer1Interrupt);
        via.Update();
        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer1Interrupt).Should().Be(0);

        via.Write(MOS6522.AuxiliaryControl, 0x40);
        via.Write(MOS6522.T1CounterLow, 0x00);
        via.Write(MOS6522.T1CounterHigh, 0x00);
        via.Update();
        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer1Interrupt).Should().NotBe(0);
        via.Timer1Counter.Should().Be(0);
    }

    [Test]
    public void Timer2_is_a_one_shot_phi2_timer()
    {
        var via = new MOS6522();
        via.Write(MOS6522.T2CounterLow, 0x01);
        via.Write(MOS6522.T2CounterHigh, 0x00);
        via.Update();
        via.Update();

        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer2Interrupt).Should().NotBe(0);
        via.Read(MOS6522.T2CounterLow);
        via.Update();
        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer2Interrupt).Should().Be(0);
    }

    [Test]
    public void Timer1_oneShot_keeps_counting_and_wrapping_after_it_fires()
    {
        var via = new MOS6522();
        via.Write(MOS6522.T1CounterLow, 0x02);
        via.Write(MOS6522.T1CounterHigh, 0x00); // ACR bit6=0: one-shot
        via.Update(); // 0x0002 -> 0x0001
        via.Update(); // 0x0001 -> 0x0000
        via.Update(); // 0x0000 -> 0xFFFF: fires once

        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer1Interrupt).Should().NotBe(0);
        via.Write(MOS6522.InterruptFlag, MOS6522.Timer1Interrupt);

        via.Update(); // 0xFFFF -> 0xFFFE: real hardware keeps decrementing, doesn't freeze
        via.Timer1Counter.Should().Be(0xFFFE);
        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer1Interrupt).Should().Be(0,
            "one-shot mode fires exactly once per T1CH write, not on every subsequent wrap");
    }

    [Test]
    public void Timer2_oneShot_keeps_counting_and_wrapping_after_it_fires()
    {
        var via = new MOS6522();
        via.Write(MOS6522.T2CounterLow, 0x01);
        via.Write(MOS6522.T2CounterHigh, 0x00);
        via.Update();
        via.Update(); // underflows, fires once

        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer2Interrupt).Should().NotBe(0);
        via.Read(MOS6522.T2CounterLow); // acknowledges the interrupt

        via.Update();
        var counterAfterOneMoreTick = via.Read(MOS6522.T2CounterLow);
        counterAfterOneMoreTick.Should().Be(0xFE, "T2 keeps decrementing/wrapping after a one-shot fire");
        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer2Interrupt).Should().Be(0);
    }

    [Test]
    public void Ca2_independent_input_mode_interrupt_survives_a_port_a_read()
    {
        var via = new MOS6522();
        via.Write(MOS6522.PeripheralControl, 0x02); // CA2 negative-edge, INDEPENDENT (bit1 set)
        via.CA2 = true; // idle high first so the next transition is a real negative edge
        via.Update(); // captures the high level into _previousCa2 before the actual transition
        via.CA2 = false;
        via.Update();

        (via.Read(MOS6522.InterruptFlag) & MOS6522.Ca2Interrupt).Should().NotBe(0);
        via.Read(MOS6522.Ora); // a normal (non-independent) CA2 mode would clear the flag here

        (via.Read(MOS6522.InterruptFlag) & MOS6522.Ca2Interrupt).Should().NotBe(0,
            "independent mode's CA2 flag is only cleared by an explicit IFR write, not a Port A read");

        via.Write(MOS6522.InterruptFlag, MOS6522.Ca2Interrupt);
        (via.Read(MOS6522.InterruptFlag) & MOS6522.Ca2Interrupt).Should().Be(0);
    }

    [Test]
    public void Cb2_independent_input_mode_interrupt_survives_a_port_b_read()
    {
        var via = new MOS6522();
        via.Write(MOS6522.PeripheralControl, 0x20); // CB2 negative-edge, INDEPENDENT
        via.CB2 = true;
        via.Update();
        via.CB2 = false;
        via.Update();

        (via.Read(MOS6522.InterruptFlag) & MOS6522.Cb2Interrupt).Should().NotBe(0);
        via.Read(MOS6522.Orb);

        (via.Read(MOS6522.InterruptFlag) & MOS6522.Cb2Interrupt).Should().NotBe(0);
    }

    [Test]
    public void ShiftRegister_inT2Mode_clocksOnceEveryTimer2Underflow()
    {
        var via = new MOS6522();
        via.Write(MOS6522.AuxiliaryControl, 0x04); // SR mode 100=0x04: shift IN under T2 control
        via.CB2 = true; // the bit that gets shifted in on each T2 underflow

        via.Write(MOS6522.T2CounterLow, 0x01);
        via.Write(MOS6522.T2CounterHigh, 0x00);
        for (var i = 0; i < 8; i++)
        {
            via.Update(); // -> 0
            via.Update(); // -> underflow, shifts one bit, re-arm not needed for SR clocking itself
            via.Write(MOS6522.T2CounterLow, 0x01);
            via.Write(MOS6522.T2CounterHigh, 0x00);
        }

        (via.Read(MOS6522.InterruptFlag) & MOS6522.ShiftRegisterInterrupt).Should().NotBe(0,
            "eight T2-driven shifts should complete the byte and set the SR interrupt");
        via.SR.Should().Be(0xFF, "CB2 was held high for all eight shifts-in");
    }

    [Test]
    public void ShiftRegister_T2FreeRunMode_ReloadsTimerAndContinuesClocking()
    {
        var via = new MOS6522();
        via.Write(MOS6522.AuxiliaryControl, 0x10); // SR output, T2 free-run
        via.Write(MOS6522.ShiftRegister, 0x80);
        via.Write(MOS6522.T2CounterLow, 0x01);
        via.Write(MOS6522.T2CounterHigh, 0x00);

        for (var i = 0; i < 8; i++)
        {
            via.Update();
            via.Update();
        }

        (via.Read(MOS6522.InterruptFlag) & MOS6522.ShiftRegisterInterrupt).Should().NotBe(0,
            "free-run T2 should provide all eight shift clocks");
        via.Timer2Counter.Should().Be(0x0001, "T2 should reload from its latch after each underflow");
    }

    [Test]
    public void Pcr_configures_ca1_and_cb1_edges()
    {
        var via = new MOS6522();
        via.Write(MOS6522.PeripheralControl, 0x11);
        via.CA1 = true;
        via.CB1 = true;
        via.Update();

        (via.Read(MOS6522.InterruptFlag) & (MOS6522.Ca1Interrupt | MOS6522.Cb1Interrupt))
            .Should().Be(MOS6522.Ca1Interrupt | MOS6522.Cb1Interrupt);
    }

    [Test]
    public void Pcr_configures_ca2_and_cb2_input_edges()
    {
        var via = new MOS6522();
        via.Write(MOS6522.PeripheralControl, 0x55);
        via.CA2 = true;
        via.CB2 = true;
        via.Update();

        (via.Read(MOS6522.InterruptFlag) & (MOS6522.Ca2Interrupt | MOS6522.Cb2Interrupt))
            .Should().Be(MOS6522.Ca2Interrupt | MOS6522.Cb2Interrupt);
    }

    [Test]
    public void Shift_register_sets_its_interrupt_after_eight_phi2_clocks()
    {
        var via = new MOS6522();
        via.Write(MOS6522.AuxiliaryControl, 0x18);
        via.Write(MOS6522.ShiftRegister, 0x80);
        for (var i = 0; i < 8; i++)
            via.Update();

        via.SR.Should().Be(0);
        (via.Read(MOS6522.InterruptFlag) & MOS6522.ShiftRegisterInterrupt).Should().NotBe(0);
    }

    [Test]
    public void Tick_advances_by_the_given_number_of_phi2_cycles()
    {
        var via = new MOS6522();
        via.Write(MOS6522.T1CounterLow, 0x02);
        via.Write(MOS6522.T1CounterHigh, 0x00);

        via.Tick(3);

        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer1Interrupt).Should().NotBe(0);
    }

    [Test]
    public void KeepsTwoInstancesIndependentWhenMappedAtDifferentBaseAddresses()
    {
        var first = new MOS6522("VIA1", 0xE840) { PortAInput = 0x11 };
        var second = new MOS6522("VIA2", 0xE850) { PortAInput = 0x22 };

        first.Write((ushort)(0xE840 + MOS6522.Ddra), 0xF0);
        first.Write((ushort)(0xE840 + MOS6522.Ora), 0xA0);
        second.Write((ushort)(0xE850 + MOS6522.Ddra), 0x0F);
        second.Write((ushort)(0xE850 + MOS6522.Ora), 0x05);

        first.ORA.Should().Be(0xA0);
        second.ORA.Should().Be(0x05);
        first.Name.Should().Be("VIA1");
        second.Name.Should().Be("VIA2");
    }

    [Test]
    public void ExposesTimerControlAndInterruptProperties()
    {
        var via = new MOS6522("TEST");

        via.Name.Should().Be("TEST");
        via.Timer1Latch.Should().Be(0);
        via.Timer2Counter.Should().Be(0);
        via.SR.Should().Be(0);
        via.ACR.Should().Be(0);
        via.PCR.Should().Be(0);
        via.IFR.Should().Be(0);
        via.IER.Should().Be(0);
        via.HasInterrupt.Should().BeFalse();
    }

    [Test]
    public void PortBOutputUsesTimerOnePb7WhenSelected()
    {
        var via = new MOS6522();
        via.Write(MOS6522.Ddrb, 0xFF);
        via.Write(MOS6522.Orb, 0x00);
        via.Write(MOS6522.AuxiliaryControl, 0x80);
        via.Write(MOS6522.T1CounterLow, 0x00);
        via.Write(MOS6522.T1CounterHigh, 0x00);

        via.PortBOutput.Should().Be(0);
        via.Update();
        via.PortBOutput.Should().Be(0x80);
    }

    [Test]
    public void Ca1EdgeLatchesPortAAndReleasesHandshake()
    {
        var via = new MOS6522 { PortAInput = 0xA5 };
        via.Write(MOS6522.AuxiliaryControl, 0x01);
        via.Write(MOS6522.PeripheralControl, 0x09); // CA1 rising, CA2 handshake output.
        via.CA2Output.Should().BeTrue();
        via.Read(MOS6522.Ora);
        via.CA2Output.Should().BeFalse();

        via.CA1 = true;

        via.CA1.Should().BeTrue();
        via.CA2Output.Should().BeTrue();
        via.Read(MOS6522.Ora).Should().Be(0xA5);
        via.CA2Output.Should().BeFalse();
    }

    [Test]
    public void ClockTimer2CountsPb6PulsesOnlyInConfiguredMode()
    {
        var via = new MOS6522();
        via.ClockTimer2();
        via.Write(MOS6522.AuxiliaryControl, 0x20);
        via.Write(MOS6522.T2CounterLow, 0x01);
        via.Write(MOS6522.T2CounterHigh, 0x00);

        via.ClockTimer2();
        via.Timer2Counter.Should().Be(0);
        via.ClockTimer2();
        (via.Read(MOS6522.InterruptFlag) & MOS6522.Timer2Interrupt).Should().NotBe(0);

        via.Reset();
        via.Write(MOS6522.T2CounterLow, 0x01);
        via.Write(MOS6522.T2CounterHigh, 0x00);
        via.ClockTimer2();
        via.Timer2Counter.Should().Be(1);

        via.Reset();
        via.Write(MOS6522.T2CounterLow, 0x01);
        via.Write(MOS6522.T2CounterHigh, 0x00);
        via.ClockTimer2();
        via.Timer2Counter.Should().Be(1);
    }

    [Test]
    public void PortReadsSupportLatchedInputsAndClearNormalControlFlags()
    {
        var via = new MOS6522 { PortAInput = 0x12, PortBInput = 0x34 };
        via.Write(MOS6522.AuxiliaryControl, 0x03);
        via.Write(MOS6522.PeripheralControl, 0x55);
        via.CA1 = true;
        via.CB1 = true;
        via.Update();

        via.Read(MOS6522.Ora).Should().Be(0x12);
        via.Read(MOS6522.Orb).Should().Be(0x34);
        (via.IFR & (MOS6522.Ca1Interrupt | MOS6522.Cb1Interrupt)).Should().Be(0);
    }

    [Test]
    public void PortBReadUsesTimerOutputBitAndLatchedInput()
    {
        var via = new MOS6522 { PortBInput = 0x55 };
        via.Write(MOS6522.Ddrb, 0x00);
        via.Write(MOS6522.AuxiliaryControl, 0x82);
        via.Write(MOS6522.PeripheralControl, 0x55);
        via.CB1 = true;
        via.Update();
        via.Read(MOS6522.Orb).Should().Be(0x55);

        via.Write(MOS6522.Ddrb, 0xFF);
        via.Write(MOS6522.AuxiliaryControl, 0x80);
        via.Write(MOS6522.T1CounterLow, 0x00);
        via.Write(MOS6522.T1CounterHigh, 0x00);
        via.Update();
        via.Read(MOS6522.Orb).Should().Be(0x80);
    }

    [Test]
    public void UpdateControlOutputsSupportsCa2AndCb2Modes()
    {
        var via = new MOS6522();

        foreach (var pcr in new byte[] { 0x00, 0x08, 0x0A, 0x0C, 0x0E, 0x20, 0x80, 0xA0, 0xC0, 0xE0 })
        {
            via.Write(MOS6522.PeripheralControl, pcr);
            via.CA2Output.Should().Be((pcr & 0x0E) switch
            {
                0x0C => false,
                0x0E => true,
                >= 0x08 => true,
                _ => false
            });
            via.CB2Output.Should().Be((pcr & 0xE0) switch
            {
                0xC0 => false,
                0xE0 => true,
                >= 0x80 => true,
                _ => false
            });
        }
    }

    [Test]
    public void ShiftRegisterSupportsCb1ClockedInputAndOutputModes()
    {
        var input = new MOS6522 { CB2 = true };
        input.Write(MOS6522.AuxiliaryControl, 0x0C);
        input.Write(MOS6522.ShiftRegister, 0);
        input.Write(MOS6522.PeripheralControl, 0x10); // CB1 rising edge.
        input.CB1 = true;
        input.Update();
        input.SR.Should().Be(0x80);

        var output = new MOS6522();
        output.Write(MOS6522.AuxiliaryControl, 0x10);
        output.Write(MOS6522.ShiftRegister, 0x80);
        output.Write(MOS6522.T2CounterLow, 0);
        output.Write(MOS6522.T2CounterHigh, 0);
        output.Update();
        output.Update();
        output.SR.Should().Be(0);

        var inputLow = new MOS6522();
        inputLow.Write(MOS6522.AuxiliaryControl, 0x0C);
        inputLow.Write(MOS6522.ShiftRegister, 0xFF);
        inputLow.Write(MOS6522.PeripheralControl, 0x10);
        inputLow.CB1 = true;
        inputLow.Update();
        inputLow.SR.Should().Be(0x7F);
    }

    [Test]
    public void UpdateSamplesCa1EdgeAndLatchesItsInput()
    {
        var via = new MOS6522 { PortAInput = 0xA5 };
        via.Write(MOS6522.AuxiliaryControl, 0x01);
        via.Write(MOS6522.PeripheralControl, 0x09);
        via.CA1 = true;

        // The public setter handles the real-time edge and synchronizes the sample state.
        // Re-arm only the sampler to cover the same edge path used by a clocked update.
        var previous = typeof(MOS6522).GetField("_previousCa1", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        previous.SetValue(via, false);
        via.Update();

        via.CA2Output.Should().BeTrue();
        via.Read(MOS6522.Ora).Should().Be(0xA5);
    }

    [Test]
    public void HandshakeIsNotChangedForNonHandshakePcrModes()
    {
        var via = new MOS6522();
        via.Write(MOS6522.PeripheralControl, 0x00);
        via.Write(MOS6522.Ora, 0xFF);
        via.CA1 = true;
        via.Read(MOS6522.Ora).Should().Be(0);
        via.CA2Output.Should().BeFalse();
    }

    [Test]
    public void Cb2OutputModeDoesNotSampleAnInputEdge()
    {
        var via = new MOS6522();
        via.Write(MOS6522.PeripheralControl, 0x80);
        via.CB2 = true;
        via.Update();
        (via.IFR & MOS6522.Cb2Interrupt).Should().Be(0);
    }
}
