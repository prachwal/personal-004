using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Core.Serial;

namespace PetEmulator.Chips.Tests;

public sealed class MOS6551Tests
{
    private const ushort Base = 0xEFF0;

    [Test]
    public void Constructor_ExposesFourMappedRegisters()
    {
        using var transport = new BufferedSerialTransport();
        var acia = new MOS6551(transport, baseAddress: Base);

        acia.Length.Should().Be(4);
        acia.Read((ushort)(Base + 1)).Should().Be(MOS6551.TransmitDataRegisterEmpty);
    }

    [Test]
    public void Constructor_RejectsAddressSpaceOverflow()
    {
        using var transport = new BufferedSerialTransport();

        FluentActions.Invoking(() => new MOS6551(transport, baseAddress: 0xFFFD))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ReadWrite_RejectAddressesOutsideMappedRange()
    {
        using var transport = new BufferedSerialTransport();
        var acia = new MOS6551(transport, baseAddress: Base);

        FluentActions.Invoking(() => acia.Read((ushort)(Base - 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => acia.Write((ushort)(Base + 4), 0))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void DataRegister_RoundTripsThroughTransport()
    {
        using var transport = new BufferedSerialTransport();
        var acia = new MOS6551(transport, baseAddress: Base);

        acia.Write(Base, 0xAB);
        transport.TryReadTransmitted(out var transmitted).Should().BeTrue();
        transmitted.Should().Be(0xAB);

        transport.ReceiveFromHost(0x42);
        acia.Read(Base).Should().Be(0x42);
    }

    [Test]
    public void Status_ReportsReceiveAndTransmitReady()
    {
        using var transport = new BufferedSerialTransport();
        var acia = new MOS6551(transport, baseAddress: Base);
        transport.ReceiveFromHost(0x11);

        var status = acia.Read((ushort)(Base + 1));

        (status & MOS6551.ReceiveDataRegisterFull).Should().Be(MOS6551.ReceiveDataRegisterFull);
        (status & MOS6551.TransmitDataRegisterEmpty).Should().Be(MOS6551.TransmitDataRegisterEmpty);
    }

    [Test]
    public void Receive_RaisesIrqWhenCommandEnablesIt()
    {
        using var transport = new BufferedSerialTransport();
        var acia = new MOS6551(transport, baseAddress: Base);
        acia.Write((ushort)(Base + 2), 0x00);

        transport.ReceiveFromHost(0x55);

        acia.Irq.Should().BeTrue();
        (acia.Read((ushort)(Base + 1)) & MOS6551.InterruptRequest).Should().Be(MOS6551.InterruptRequest);
        acia.Irq.Should().BeFalse();
    }

    [Test]
    public void Receive_DoesNotRaiseIrqWhenCommandDisablesIt()
    {
        using var transport = new BufferedSerialTransport();
        var acia = new MOS6551(transport, baseAddress: Base);
        acia.Write((ushort)(Base + 2), 0x02);

        transport.ReceiveFromHost(0x55);

        acia.Irq.Should().BeFalse();
    }

    [Test]
    public void CommandAndControlRegisters_RoundTrip()
    {
        using var transport = new BufferedSerialTransport();
        var acia = new MOS6551(transport, baseAddress: Base);

        acia.Write((ushort)(Base + 2), 0x3C);
        acia.Write((ushort)(Base + 3), 0x1F);

        acia.Read((ushort)(Base + 2)).Should().Be(0x3C);
        acia.Read((ushort)(Base + 3)).Should().Be(0x1F);
    }

    [Test]
    public void Reset_ClearsCommandControlAndIrq()
    {
        using var transport = new BufferedSerialTransport();
        var acia = new MOS6551(transport, baseAddress: Base);
        acia.Write((ushort)(Base + 2), 0x00);
        acia.Write((ushort)(Base + 3), 0x1F);
        transport.ReceiveFromHost(0x99);

        acia.Reset();

        acia.Command.Should().Be(0);
        acia.Control.Should().Be(0);
        acia.Irq.Should().BeFalse();
    }
}
