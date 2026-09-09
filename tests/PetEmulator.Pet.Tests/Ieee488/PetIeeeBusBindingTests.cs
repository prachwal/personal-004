using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.CbmDos;
using PetEmulator.Pet.Chips;
using PetEmulator.Pet.Ieee488;

namespace PetEmulator.Pet.Tests.Ieee488;

/// <summary>Focused tests for PetIeeeBusBinding's own wiring (ATN via VIA Port B, DIO via PIA2
/// Port A/B) on top of a bare Pia+Via6522+PetIeeeBus - not through a full PetMachine/ROM.</summary>
public sealed class PetIeeeBusBindingTests
{
    private const byte AtnOutViaDdrb = 0x04; // VIA DDRB bit 2 = ATN output enabled

    [Test]
    public void Via_port_b_write_drives_atn_into_the_bus()
    {
        var pia2 = new Pia();
        var via = new Via6522();
        var bus = new PetIeeeBus();
        _ = new PetIeeeBusBinding(pia2, via, bus);

        via.Write(Via6522.Ddrb, AtnOutViaDdrb);
        via.Write(Via6522.Orb, 0x00); // bit2=0 -> ATN asserted (active low)

        // OnATNWrite(true) sets exactly this handshake state - see PetIeeeBus.
        bus.DAV.Should().BeFalse();
        bus.NRFD.Should().BeFalse();
        bus.NDAC.Should().BeTrue();
    }

    [Test]
    public void Pia2_port_b_write_delivers_an_inverted_command_byte_to_the_bus()
    {
        var pia2 = new Pia();
        var via = new Via6522();
        var bus = new PetIeeeBus();
        _ = new PetIeeeBusBinding(pia2, via, bus);

        pia2.Write(3, 0x04); // select PIA2 CRB's data register
        via.Write(Via6522.Ddrb, AtnOutViaDdrb);
        via.Write(Via6522.Orb, 0x00); // ATN asserted -> command phase
        pia2.Write(2, (byte)(0x28 ^ 0xFF)); // LISTEN 8, inverted per the real hardware transceiver

        bus.LastDio.Should().Be(0x28);
    }

    [Test]
    public void Pia2_port_a_read_delivers_an_inverted_talker_byte()
    {
        var pia2 = new Pia();
        var via = new Via6522();
        var bus = new PetIeeeBus();
        _ = new PetIeeeBusBinding(pia2, via, bus);
        var drive = new PetIeeeDiskDrive(8);
        bus.AttachDevice(drive);
        // AttachImage sets the error channel's status message, always available without needing
        // an actual .d64 file - a convenient real byte source for this test.
        drive.Engine.AttachImage(D64Image.Load(D64Image.CreateEmpty()));

        pia2.Write(3, 0x04); // select PIA2 CRB's data register
        via.Write(Via6522.Ddrb, AtnOutViaDdrb);
        via.Write(Via6522.Orb, 0x00); // ATN asserted
        pia2.Write(2, (byte)(0x48 ^ 0xFF)); // TALK device 8
        pia2.Write(2, (byte)(0x6F ^ 0xFF)); // SECONDARY 15 (error channel)
        via.Write(Via6522.Orb, 0x04); // ATN released -> DataIn, device becomes talker

        for (var i = 0; i < 8; i++)
            bus.Tick(); // let the talker-side prefetch delay elapse

        pia2.Write(1, 0x04); // select PIA2 CRA's data register (bit 2 set = standard data-select)
        var read = pia2.Read(0);
        ((byte)(read ^ 0xFF)).Should().Be((byte)'7'); // error text starts "73,CBM DOS..."
    }
}
