using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioTests
{
    [Test]
    public void ChannelsUseIndependentDataAndControlPorts()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        EnableReceiver(sio, Z80Sio.ChannelB);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x41);
        sio.EnqueueRxByte(Z80Sio.ChannelB, 0x42);

        sio.Tick(400_000);

        sio.ReadPort(0x00).Should().Be(0x41);
        sio.ReadPort(0x01).Should().Be(0x42);
        (sio.ReadPort(0x02) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        (sio.ReadPort(0x03) & Z80Sio.Rr0RxAvailable).Should().Be(0);
    }

    [Test]
    public void RegisterPointerProgramsAndReadsBackWr1()
    {
        var sio = new Z80Sio();

        sio.WritePort(0x02, 0x09); // select WR1 on channel A
        sio.WritePort(0x02, 0x1A);
        sio.WritePort(0x02, 0x01); // select RR1

        sio.ReadPort(0x02).Should().Be(Z80Sio.Rr1AllSent);
    }

    [Test]
    public void ReceiverWaitsForCharacterTimeAndReportsErrors()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiver(sio, Z80Sio.ChannelB);
        sio.EnqueueRxByte(Z80Sio.ChannelB, 0x55, parityError: true, framingError: true);

        (sio.ReadPort(0x03) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        sio.Tick(10_000);
        (sio.ReadPort(0x03) & Z80Sio.Rr0RxAvailable).Should().NotBe(0);

        sio.WritePort(0x03, 0x01); // select RR1
        (sio.ReadPort(0x03) & (Z80Sio.Rr1ParityError | Z80Sio.Rr1FramingError))
            .Should().Be(Z80Sio.Rr1ParityError | Z80Sio.Rr1FramingError);
        sio.ReadPort(0x01).Should().Be(0x55);
    }

    [Test]
    public void FullReceiverSetsOverrunAndErrorResetClearsIt()
    {
        var sio = new Z80Sio { BaudRate = 1_000_000 };
        EnableReceiver(sio, Z80Sio.ChannelB);
        sio.EnqueueRxByte(Z80Sio.ChannelB, 1);
        sio.EnqueueRxByte(Z80Sio.ChannelB, 2);
        sio.EnqueueRxByte(Z80Sio.ChannelB, 3);

        sio.WritePort(0x03, 0x09); // select WR1
        sio.WritePort(0x03, 0x00); // leave RX interrupts disabled
        sio.WritePort(0x03, 0x31); // WR0 command 110: error reset, pointer 1
        sio.WritePort(0x03, 0x00);
        sio.WritePort(0x03, 0x01); // select RR1

        (sio.ReadPort(0x03) & Z80Sio.Rr1OverrunError).Should().Be(0);
    }

    [Test]
    public void TxWriteRaisesEventAndOptionalInterrupt()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        var transmitted = new List<(int Channel, byte Value)>();
        sio.Transmitted += (channel, value) => transmitted.Add((channel, value));

        sio.WritePort(0x03, 0x09); // WR1 B
        sio.WritePort(0x03, 0x02); // Tx interrupt enable
        SelectRegister(sio, 0x03, 5);
        sio.WritePort(0x03, 0x08); // Tx enable
        sio.WritePort(0x03, 0x02); // WR2
        sio.WritePort(0x03, 0x80);
        sio.WritePort(0x01, 0xA5);

        transmitted.Should().BeEmpty();
        sio.Tick(9_999);
        transmitted.Should().BeEmpty();
        sio.Tick(1);
        transmitted.Should().ContainSingle().Which.Should().Be((Z80Sio.ChannelB, (byte)0xA5));
        sio.TryConsumePendingInterrupt(out var vector).Should().BeTrue();
        vector.Should().Be(0x80);
    }

    [Test]
    public void ChannelResetClearsRegistersAndBufferedData()
    {
        var sio = new Z80Sio { BaudRate = 1_000_000 };
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0xA5);
        sio.WritePort(0x02, 0x1B); // WR3 pointer + channel reset command
        sio.WritePort(0x02, 0x00); // select RR0

        (sio.ReadPort(0x02) & Z80Sio.Rr0TxEmpty).Should().Be(Z80Sio.Rr0TxEmpty);
        sio.ReadPort(0x00).Should().Be(0);
    }

    [Test]
    public void Wr3GatesReceiverAndWr4DecodesAsyncFormat()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0xA5);
        sio.Tick(100_000);

        (sio.ReadPort(0x02) & Z80Sio.Rr0RxAvailable).Should().Be(0);

        SelectRegister(sio, 0x02, 3);
        sio.WritePort(0x02, 0xC1); // Rx enable, 8 data bits
        SelectRegister(sio, 0x02, 4);
        sio.WritePort(0x02, 0x47); // async, x16, parity enabled/even, one stop bit

        var state = sio.GetChannelState(Z80Sio.ChannelA);
        state.RxEnabled.Should().BeTrue();
        state.ReceiveDataBits.Should().Be(8);
        state.ClockMultiplier.Should().Be(16);
        state.AsyncMode.Should().BeTrue();
        state.ParityEnabled.Should().BeTrue();
        state.EvenParity.Should().BeTrue();
        (sio.ReadPort(0x02) & Z80Sio.Rr0RxAvailable).Should().NotBe(0);
    }

    [Test]
    public void CtsBlocksTxAndDcdCtsAreReportedInRr0()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        var transmitted = new List<byte>();
        sio.Transmitted += (_, value) => transmitted.Add(value);
        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x8A); // Tx enable, RTS and DTR
        sio.SetCts(Z80Sio.ChannelA, false);
        sio.SetDcd(Z80Sio.ChannelA, false);

        sio.WritePort(0x00, 0x10);
        transmitted.Should().BeEmpty();
        (sio.ReadPort(0x02) & (Z80Sio.Rr0Dcd | Z80Sio.Rr0Cts)).Should().Be(0);

        sio.SetCts(Z80Sio.ChannelA, true);
        sio.SetDcd(Z80Sio.ChannelA, true);
        sio.WritePort(0x00, 0x11);
        sio.Tick(10_000);
        transmitted.Should().Equal(0x11);
        var state = sio.GetChannelState(Z80Sio.ChannelA);
        state.RtsAsserted.Should().BeTrue();
        state.DtrAsserted.Should().BeTrue();
    }

    [Test]
    public void TxBuffersOneByteAndSnapshotRestoresThePendingFrame()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        var transmitted = new List<byte>();
        sio.Transmitted += (_, value) => transmitted.Add(value);
        EnableTransmitter(sio, Z80Sio.ChannelA);

        sio.WritePort(0x00, 0x21);
        sio.WritePort(0x00, 0x22);
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0TxEmpty).Should().Be(0);

        var snapshot = sio.CaptureState();
        sio.Reset();
        sio.RestoreState(snapshot);
        sio.Tick(10_000);
        transmitted.Should().Equal(0x21);
        sio.Tick(9_999);
        transmitted.Should().Equal(0x21);
        sio.Tick(1);
        transmitted.Should().Equal(0x21, 0x22);

        sio.WritePort(0x02, 0x01); // select RR1
        (sio.ReadPort(0x02) & Z80Sio.Rr1AllSent).Should().Be(Z80Sio.Rr1AllSent);
    }

    [Test]
    public void SnapshotRestoresConfigurationBuffersTimingAndModemLines()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        SelectRegister(sio, 0x02, 4);
        sio.WritePort(0x02, 0x47);
        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x8A);
        sio.SetCts(Z80Sio.ChannelA, false);
        sio.SetDcd(Z80Sio.ChannelA, false);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x31, parityError: true, framingError: false);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x32, parityError: false, framingError: true);
        var snapshot = sio.CaptureState();
        sio.Reset();
        sio.RestoreState(snapshot);

        var restored = sio.GetChannelState(Z80Sio.ChannelA);
        restored.RxEnabled.Should().BeTrue();
        restored.TxEnabled.Should().BeTrue();
        restored.CtsAsserted.Should().BeFalse();
        restored.DcdAsserted.Should().BeFalse();
        restored.RtsAsserted.Should().BeTrue();
        restored.DtrAsserted.Should().BeTrue();
        restored.ClockMultiplier.Should().Be(16);
        restored.ParityEnabled.Should().BeTrue();

        sio.Tick(687);
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        sio.Tick(1);
        sio.WritePort(0x02, 0x01); // select RR1
        (sio.ReadPort(0x02) & Z80Sio.Rr1ParityError).Should().NotBe(0);
        sio.ReadPort(0x00).Should().Be(0x31);
        sio.Tick(688);
        sio.ReadPort(0x00).Should().Be(0x32);
    }

    [Test]
    public void SnapshotDoesNotAliasLiveRegistersOrReceiveBuffer()
    {
        var sio = new Z80Sio { BaudRate = 1_000_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0xA5);

        var snapshot = sio.CaptureState();
        sio.Reset();

        snapshot.Channels[0].WriteRegisters[3].Should().Be(0xC1);
        snapshot.Channels[0].ReceiveBuffer.Should().ContainSingle()
            .Which.Value.Should().Be(0xA5);
        sio.GetChannelState(Z80Sio.ChannelA).RxEnabled.Should().BeFalse();
    }

    [Test]
    public void AsyncClockMultiplierAndFrameFormatChangeReceiverTiming()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        SelectRegister(sio, 0x02, 4);
        sio.WritePort(0x02, 0x44); // async, x16, 8N1 => 10 bits / 16
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x11);

        sio.Tick(624);
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        sio.Tick(1);
        sio.ReadPort(0x00).Should().Be(0x11);

        SelectRegister(sio, 0x02, 4);
        sio.WritePort(0x02, 0x4F); // async, x16, 8E2 => 12 bits / 16
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x22);
        sio.Tick(749);
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        sio.Tick(1);
        sio.ReadPort(0x00).Should().Be(0x22);
    }

    [Test]
    public void RestoreRejectsStateWithInvalidChannelShape()
    {
        var sio = new Z80Sio();
        var invalid = new Z80SioState(0, 300, 0, false, true, []);

        var action = () => sio.RestoreState(invalid);

        action.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Rr3ReportsAndConsumesExternalStatusInterrupt()
    {
        var sio = new Z80Sio();
        SelectRegister(sio, 0x02, 1);
        sio.WritePort(0x02, 0x05); // Ext/Status interrupt + status affects vector
        SelectRegister(sio, 0x03, 1);
        sio.WritePort(0x03, 0x04); // status affects vector is shared from channel B
        sio.SetCts(Z80Sio.ChannelA, false);

        SelectRegister(sio, 0x02, 3);
        (sio.ReadPort(0x02) & Z80Sio.Rr3AExtStatusInterrupt)
            .Should().Be(Z80Sio.Rr3AExtStatusInterrupt);
        sio.TryConsumePendingInterrupt(out var vector).Should().BeTrue();
        vector.Should().Be(0x08);

        SelectRegister(sio, 0x02, 3);
        (sio.ReadPort(0x02) & Z80Sio.Rr3AExtStatusInterrupt).Should().Be(0);
        sio.TryConsumePendingInterrupt(out _).Should().BeFalse();
    }

    [Test]
    public void Rr3UsesIndependentBitsForBothChannelsAndPendingSources()
    {
        var sio = new Z80Sio { BaudRate = 1_000_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        EnableReceiver(sio, Z80Sio.ChannelB);
        EnableTransmitter(sio, Z80Sio.ChannelA);
        EnableTransmitter(sio, Z80Sio.ChannelB);
        SelectRegister(sio, 0x02, 1);
        sio.WritePort(0x02, 0x07); // A external + Tx interrupt
        SelectRegister(sio, 0x03, 1);
        sio.WritePort(0x03, 0x07); // B external + Tx interrupt
        sio.SetDcd(Z80Sio.ChannelA, false);
        sio.SetDcd(Z80Sio.ChannelB, false);
        sio.WritePort(0x00, 0x10);
        sio.WritePort(0x01, 0x20);
        sio.Tick(25); // 10-bit frame at 1 MHz / 1 MHz

        SelectRegister(sio, 0x02, 3);
        var pending = sio.ReadPort(0x02);
        pending.Should().Be(
            Z80Sio.Rr3BTxInterrupt | Z80Sio.Rr3BExtStatusInterrupt |
            Z80Sio.Rr3ATxInterrupt | Z80Sio.Rr3AExtStatusInterrupt);
    }

    [Test]
    public void DisabledExternalStatusInterruptDoesNotLatchLineTransition()
    {
        var sio = new Z80Sio();
        sio.SetCts(Z80Sio.ChannelA, false);
        SelectRegister(sio, 0x02, 3);

        (sio.ReadPort(0x02) & Z80Sio.Rr3AExtStatusInterrupt).Should().Be(0);
        sio.TryConsumePendingInterrupt(out _).Should().BeFalse();
    }

    [Test]
    public void Wr5SendBreakRaisesLineEventAndExternalStatusInterrupt()
    {
        var sio = new Z80Sio();
        var transitions = new List<bool>();
        sio.BreakChanged += (_, asserted) => transitions.Add(asserted);
        SelectRegister(sio, 0x02, 1);
        sio.WritePort(0x02, 0x01); // enable Ext/Status interrupt
        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x18); // Tx enable + Send Break

        sio.GetChannelState(Z80Sio.ChannelA).BreakAsserted.Should().BeTrue();
        transitions.Should().Equal(true);
        SelectRegister(sio, 0x02, 3);
        (sio.ReadPort(0x02) & Z80Sio.Rr3AExtStatusInterrupt)
            .Should().Be(Z80Sio.Rr3AExtStatusInterrupt);

        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x08); // clear Send Break
        sio.GetChannelState(Z80Sio.ChannelA).BreakAsserted.Should().BeFalse();
        transitions.Should().Equal(true, false);
    }

    [Test]
    public void ReceivedBreakIsReportedInRr1AndSurvivesSnapshotRestore()
    {
        var sio = new Z80Sio();
        sio.SetRxBreak(Z80Sio.ChannelB, true);
        SelectRegister(sio, 0x03, 1);
        (sio.ReadPort(0x03) & Z80Sio.Rr1BreakDetected)
            .Should().Be(Z80Sio.Rr1BreakDetected);

        var snapshot = sio.CaptureState();
        sio.Reset();
        sio.RestoreState(snapshot);
        sio.GetChannelState(Z80Sio.ChannelB).BreakDetected.Should().BeTrue();
        SelectRegister(sio, 0x03, 1);
        (sio.ReadPort(0x03) & Z80Sio.Rr1BreakDetected)
            .Should().Be(Z80Sio.Rr1BreakDetected);

        sio.SetRxBreak(Z80Sio.ChannelB, false);
        SelectRegister(sio, 0x03, 1);
        (sio.ReadPort(0x03) & Z80Sio.Rr1BreakDetected).Should().Be(0);
    }

    [Test]
    public void FlushReceiveBufferClearsQueuedDataOverrunAndTiming()
    {
        var sio = new Z80Sio { BaudRate = 1_000_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x10, parityError: true, framingError: false);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x11, parityError: false, framingError: true);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x12);

        sio.FlushReceiveBuffer(Z80Sio.ChannelA).Should().Be(2);
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        SelectRegister(sio, 0x02, 1);
        (sio.ReadPort(0x02) & Z80Sio.Rr1OverrunError).Should().Be(0);
        sio.ReadPort(0x00).Should().Be(0);
    }

    [Test]
    public void AutoEchoReturnsReceivedByteThroughTimedTransmitter()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        EnableTransmitter(sio, Z80Sio.ChannelA);
        sio.SetAutoEcho(Z80Sio.ChannelA, true);
        var transmitted = new List<byte>();
        sio.Transmitted += (_, value) => transmitted.Add(value);

        sio.EnqueueRxByte(Z80Sio.ChannelA, 0xA6);
        sio.Tick(9_999);
        transmitted.Should().BeEmpty();
        sio.Tick(1);
        transmitted.Should().Equal(0xA6);
    }

    [Test]
    public void LocalLoopbackRoutesCompletedTxBackToReceiver()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        EnableTransmitter(sio, Z80Sio.ChannelA);
        sio.SetLocalLoopback(Z80Sio.ChannelA, true);

        sio.WritePort(0x00, 0x5A);
        sio.Tick(10_000);
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        sio.Tick(10_000);
        sio.ReadPort(0x00).Should().Be(0x5A);
    }

    [Test]
    public void Sync8AndExternalSyncHoldInputUntilSynchronization()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        sio.ConfigureMode(Z80Sio.ChannelA, Z80SioFrameMode.Sync8, 0x16);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x11);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x16); // sync character is consumed
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x22);
        sio.Tick(10_000);
        sio.ReadPort(0x00).Should().Be(0x22);

        sio.ConfigureMode(Z80Sio.ChannelA, Z80SioFrameMode.ExternalSync);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x33);
        sio.Tick(10_000);
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        sio.SignalExternalSync(Z80Sio.ChannelA);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x44);
        sio.Tick(10_000);
        sio.ReadPort(0x00).Should().Be(0x44);
    }

    private static void EnableReceiver(Z80Sio sio, int channel)
    {
        var controlPort = (ushort)(channel + 2);
        SelectRegister(sio, controlPort, 3);
        sio.WritePort(controlPort, 0xC1);
    }

    private static void EnableTransmitter(Z80Sio sio, int channel)
    {
        var controlPort = (ushort)(channel + 2);
        SelectRegister(sio, controlPort, 5);
        sio.WritePort(controlPort, 0x08);
    }

    private static void SelectRegister(Z80Sio sio, ushort controlPort, int register)
    {
        sio.WritePort(controlPort, (byte)register);
    }
}
