using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class MC6850Tests
{
    private const ushort Base = 0xA000;

    [Test]
    public void Constructor_ExposesNameAndMappedLength()
    {
        var acia = new MC6850("Serial", Base);

        acia.Name.Should().Be("Serial");
        acia.Length.Should().Be(2);
        acia.Read(Base).Should().Be(MC6850.TransmitDataRegisterEmpty);
    }

    [Test]
    public void Constructor_RejectsAddressSpaceOverflow()
    {
        FluentActions.Invoking(() => new MC6850(baseAddress: 0xFFFF))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ReadWrite_RejectAddressesOutsideMappedRange()
    {
        var acia = new MC6850(baseAddress: Base);

        FluentActions.Invoking(() => acia.Read((ushort)(Base - 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => acia.Write((ushort)(Base + 2), 0))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ReadStatus_ReturnsStatus()
    {
        var acia = new MC6850(baseAddress: Base);

        acia.Read(Base).Should().Be(MC6850.TransmitDataRegisterEmpty);
    }

    [Test]
    public void WriteControl_MasterReset_ClearsStatus()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Receive(0x55);

        acia.Write(Base, 0x03);

        acia.Status.Should().Be(MC6850.TransmitDataRegisterEmpty);
    }

    [Test]
    public void Receive_SetsRdrfAndStoresData()
    {
        var acia = new MC6850(baseAddress: Base);

        acia.Receive(0x55);

        (acia.Status & (MC6850.ReceiveDataRegisterFull | MC6850.TransmitDataRegisterEmpty))
            .Should().Be(0x03);
        acia.Read((ushort)(Base + 1)).Should().Be(0x55);
    }

    [Test]
    public void Receive_WhenDataIsPending_SetsOverrun()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Receive(0x55);

        acia.Receive(0xAA);

        (acia.Status & MC6850.Overrun).Should().Be(MC6850.Overrun);
        acia.Read((ushort)(Base + 1)).Should().Be(0xAA);
    }

    [Test]
    public void ReadRxData_ClearsRdrf()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Receive(0xAA);

        acia.Read((ushort)(Base + 1));

        (acia.Status & MC6850.ReceiveDataRegisterFull).Should().Be(0);
    }

    [Test]
    public void WriteTxData_StoresValueAndClearsTdre()
    {
        var acia = new MC6850(baseAddress: Base);

        acia.Write((ushort)(Base + 1), 0x77);

        acia.TxData.Should().Be(0x77);
        (acia.Status & MC6850.TransmitDataRegisterEmpty).Should().Be(0);
    }

    [Test]
    public void TransmitComplete_SetsTdre()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Write((ushort)(Base + 1), 0x55);

        acia.TransmitComplete();

        (acia.Status & MC6850.TransmitDataRegisterEmpty).Should().Be(MC6850.TransmitDataRegisterEmpty);
    }

    [Test]
    public void WriteControl_StoresControl()
    {
        var acia = new MC6850(baseAddress: Base);

        acia.Write(Base, 0x55);

        acia.Control.Should().Be(0x55);
        acia.Status.Should().Be(MC6850.TransmitDataRegisterEmpty);
    }

    [Test]
    public void Irq_RiePlusRdrf_SetsIrqBit()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Write(Base, MC6850.ReceiveInterruptEnable);
        acia.Receive(0x55);

        acia.Irq.Should().BeTrue();
        (acia.Status & MC6850.InterruptRequest).Should().Be(MC6850.InterruptRequest);
    }

    [Test]
    public void Irq_RieWithoutRdrf_ClearsIrqBit()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Write(Base, MC6850.ReceiveInterruptEnable);

        acia.Irq.Should().BeFalse();
    }

    [Test]
    public void Irq_ReadStatus_DoesNotClearIrqBit()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Write(Base, MC6850.ReceiveInterruptEnable);
        acia.Receive(0x55);

        acia.Read(Base);

        acia.Irq.Should().BeTrue();
    }

    [Test]
    public void Irq_ReadRxData_ClearsIrqViaRdrfClear()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Write(Base, MC6850.ReceiveInterruptEnable);
        acia.Receive(0x55);

        acia.Read((ushort)(Base + 1));

        acia.Irq.Should().BeFalse();
    }

    [Test]
    public void Irq_TxIrqEnabledAndTdre_SetsIrq()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Write(Base, MC6850.TransmitInterruptEnable);

        acia.Irq.Should().BeTrue();
    }

    [Test]
    public void Reset_RestoresPowerOnState()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Write(Base, MC6850.ReceiveInterruptEnable);
        acia.Write((ushort)(Base + 1), 0x11);
        acia.Receive(0x99);

        acia.Reset();

        acia.Control.Should().Be(0);
        acia.Status.Should().Be(MC6850.TransmitDataRegisterEmpty);
        acia.TxData.Should().Be(0);
        acia.RxData.Should().Be(0);
        acia.Irq.Should().BeFalse();
    }

    [Test]
    public void Tick_DoesNotChangeRegisterState()
    {
        var acia = new MC6850(baseAddress: Base);
        acia.Write((ushort)(Base + 1), 0x42);

        acia.Tick(100_000);

        acia.TxData.Should().Be(0x42);
        (acia.Status & MC6850.TransmitDataRegisterEmpty).Should().Be(0);
    }
}
