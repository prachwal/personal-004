using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioTimingTests
{
    [Test]
    public void ZeroBaudRatePausesRxAndTxUntilBaudRateIsRestored()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 0 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        EnableTransmitter(sio, Z80Sio.ChannelA);
        var transmitted = new List<byte>();
        sio.Transmitted += (_, value) => transmitted.Add(value);

        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x41);
        sio.WritePort(0x00, 0x42);
        sio.Tick(100_000);

        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        transmitted.Should().BeEmpty();

        sio.BaudRate = 1_000;
        sio.Tick(10_000);
        sio.ReadPort(0x00).Should().Be(0x41);
        transmitted.Should().Equal(0x42);
    }

    [Test]
    public void StoppedExternalClocksPauseAndResumeBothDirections()
    {
        var sio = new Z80Sio { BaudRate = 1_000 };
        var channel = new ClockChannel(0, 0);
        sio.AttachChannel(Z80Sio.ChannelA, channel);
        EnableReceiver(sio, Z80Sio.ChannelA);
        EnableTransmitter(sio, Z80Sio.ChannelA);
        var transmitted = new List<byte>();
        sio.Transmitted += (_, value) => transmitted.Add(value);

        channel.FeedRx(0x51);
        sio.WritePort(0x00, 0x52);
        sio.Tick(100_000);
        transmitted.Should().BeEmpty();
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);

        channel.RxClockFrequencyHz = 1_000_000;
        channel.TxClockFrequencyHz = 1_000_000;
        sio.Tick(10_000);
        sio.ReadPort(0x00).Should().Be(0x51);
        transmitted.Should().Equal(0x52);
    }

    [Test]
    public void BaudRateChangeAppliesToNextFrameOnly()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableTransmitter(sio, Z80Sio.ChannelA);
        var transmitted = new List<byte>();
        sio.Transmitted += (_, value) => transmitted.Add(value);

        sio.WritePort(0x00, 0x61);
        sio.Tick(5_000);
        sio.BaudRate = 2_000;
        sio.Tick(4_999);
        transmitted.Should().BeEmpty();
        sio.Tick(1);
        transmitted.Should().Equal(0x61);

        sio.WritePort(0x00, 0x62);
        sio.Tick(4_999);
        transmitted.Should().Equal(0x61);
        sio.Tick(1);
        transmitted.Should().Equal(0x61, 0x62);
    }

    [Test]
    public void TransmitterQueuesSeveralBytesAndSnapshotPreservesTheQueue()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableTransmitter(sio, Z80Sio.ChannelA);
        var transmitted = new List<byte>();
        sio.Transmitted += (_, value) => transmitted.Add(value);

        foreach (var value in new byte[] { 1, 2, 3, 4, 5 })
            sio.WritePort(0x00, value);

        sio.CaptureState().Channels[Z80Sio.ChannelA].TransmitBuffer.Should().Equal(2, 3, 4, 5);
        sio.Tick(50_000);
        transmitted.Should().Equal(1, 2, 3, 4, 5);
    }

    private static void EnableReceiver(Z80Sio sio, int channel)
    {
        var port = (ushort)(channel + 2);
        sio.WritePort(port, 0x03);
        sio.WritePort(port, 0xC1);
    }

    private static void EnableTransmitter(Z80Sio sio, int channel)
    {
        var port = (ushort)(channel + 2);
        sio.WritePort(port, 0x05);
        sio.WritePort(port, 0x08);
    }

    private sealed class ClockChannel(int rxClockFrequencyHz, int txClockFrequencyHz) : IZ80SioChannel
    {
        public int RxClockFrequencyHz { get; set; } = rxClockFrequencyHz;
        public int TxClockFrequencyHz { get; set; } = txClockFrequencyHz;
        public bool CtsAsserted { get; } = true;
        public bool DcdAsserted { get; } = true;
        public event Action<byte, bool, bool>? RxDReceived;
        public event Action<bool>? CtsChanged;
        public event Action<bool>? DcdChanged;

        public void FeedRx(byte value) => RxDReceived?.Invoke(value, false, false);
        public void SetCts(bool asserted) => CtsChanged?.Invoke(asserted);
        public void SetDcd(bool asserted) => DcdChanged?.Invoke(asserted);
        public void WriteTxD(byte value) { }
        public void SetRts(bool asserted) { }
        public void SetDtr(bool asserted) { }
    }
}
