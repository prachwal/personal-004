using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioWiringTests
{
    [Test]
    public void AttachedChannelCarriesRxTxClocksAndModemLines()
    {
        var sio = new Z80Sio { BaudRate = 1_000 };
        var channel = new TestChannel(1_000_000, 2_000_000);
        sio.AttachChannel(Z80Sio.ChannelA, channel);
        EnableReceiver(sio, Z80Sio.ChannelA);
        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x8A); // Tx enable, RTS and DTR

        channel.RtsAsserted.Should().BeTrue();
        channel.DtrAsserted.Should().BeTrue();
        channel.FeedRx(0x41);
        sio.Tick(9_999);
        (sio.GetStatus(Z80Sio.ChannelA) & Z80Sio.Rr0RxAvailable).Should().Be(0);
        sio.Tick(1);
        sio.ReadPort(0x00).Should().Be(0x41);

        sio.WritePort(0x00, 0x55);
        sio.Tick(19_999);
        channel.Transmitted.Should().BeEmpty();
        sio.Tick(1);
        channel.Transmitted.Should().Equal(0x55);

        channel.SetCts(false);
        channel.SetDcd(false);
        (sio.GetStatus(Z80Sio.ChannelA) & (Z80Sio.Rr0Cts | Z80Sio.Rr0Dcd)).Should().Be(0);
        sio.WritePort(0x00, 0x56);
        sio.Tick(20_000);
        channel.Transmitted.Should().Equal(0x55);
        channel.SetCts(true);
        channel.SetDcd(true);
        sio.WritePort(0x00, 0x57);
        sio.Tick(20_000);
        channel.Transmitted.Should().Equal(0x55, 0x57);
    }

    [Test]
    public void DetachedChannelStopsForwardingRx()
    {
        var sio = new Z80Sio { BaudRate = 1_000 };
        var channel = new TestChannel(1_000_000, 1_000_000);
        sio.AttachChannel(Z80Sio.ChannelB, channel);
        EnableReceiver(sio, Z80Sio.ChannelB);
        sio.DetachChannel(Z80Sio.ChannelB);

        channel.FeedRx(0x61);
        sio.Tick(20_000);
        (sio.GetStatus(Z80Sio.ChannelB) & Z80Sio.Rr0RxAvailable).Should().Be(0);
    }

    [Test]
    public void InterruptSourceImplementsIeiIeoAcknowledgeAndRetiChain()
    {
        var first = new Z80Sio();
        var second = new Z80Sio();
        EnableExternalInterrupt(first, Z80Sio.ChannelA);
        EnableExternalInterrupt(second, Z80Sio.ChannelA);
        first.SetCts(Z80Sio.ChannelA, false);
        second.SetCts(Z80Sio.ChannelA, false);

        first.InterruptRequested.Should().BeTrue();
        first.InterruptOutputEnabled.Should().BeFalse();
        second.InterruptInputEnabled = first.InterruptOutputEnabled;
        second.TryAcknowledgeInterrupt(out _).Should().BeFalse();

        first.TryAcknowledgeInterrupt(out _).Should().BeTrue();
        first.InterruptInService.Should().BeTrue();
        first.CompleteInterrupt();
        first.InterruptOutputEnabled.Should().BeTrue(); // acknowledge consumed the source

        first.SetCts(Z80Sio.ChannelA, true);
        first.TryConsumePendingInterrupt(out _).Should().BeTrue();
        second.InterruptInputEnabled = first.InterruptOutputEnabled;
        second.TryAcknowledgeInterrupt(out _).Should().BeTrue();
        second.CompleteInterrupt();
    }

    [Test]
    public void BitChannelTransmitsAndReceivesAsyncFrame()
    {
        var sio = new Z80Sio { BaudRate = 1_000 };
        var channel = new TestBitChannel(1_000_000, 1_000_000);
        sio.AttachChannel(Z80Sio.ChannelA, channel);
        EnableReceiver(sio, Z80Sio.ChannelA);
        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x68); // Tx enable, 8 data bits

        channel.FeedRxBits(Z80SioBitCodec.EncodeAsync(0xA5, Z80SioAsyncFormat.EightNOne));
        sio.Tick(10_000);
        sio.ReadPort(0x00).Should().Be(0xA5);

        sio.WritePort(0x00, 0x5A);
        sio.Tick(999);
        channel.TransmittedBits.Should().BeEmpty();
        sio.Tick(1);
        channel.TransmittedBits.Should().ContainSingle().Which.Should().BeFalse();
        sio.Tick(9_000);
        channel.TransmittedBits.Should().Equal(Z80SioBitCodec.EncodeAsync(0x5A, Z80SioAsyncFormat.EightNOne));
    }

    [Test]
    public void BitChannelTransmitsAndReceivesSdlcFrame()
    {
        var sio = new Z80Sio { BaudRate = 1_000 };
        var channel = new TestBitChannel(1_000_000, 1_000_000);
        sio.AttachChannel(Z80Sio.ChannelA, channel);
        EnableReceiver(sio, Z80Sio.ChannelA);
        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x68);
        sio.ConfigureMode(Z80Sio.ChannelA, Z80SioFrameMode.Sdlc);

        var payload = new byte[] { 0x00, 0xF8, 0x7E };
        var expectedBits = Z80SioSdlcCodec.EncodeFrame(payload);
        sio.TransmitSdlcFrame(Z80Sio.ChannelA, payload);
        sio.Tick(expectedBits.Count * 1_000);
        channel.TransmittedBits.Should().Equal(expectedBits);

        channel.FeedRxBits(expectedBits);
        sio.Tick(10_000);
        sio.ReadPort(0x00).Should().Be(payload[0]);
    }

    [Test]
    public void SnapshotRestoresPartialAsyncBitTransmission()
    {
        var sio = new Z80Sio { BaudRate = 1_000 };
        var channel = new TestBitChannel(1_000_000, 1_000_000);
        sio.AttachChannel(Z80Sio.ChannelA, channel);
        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x68);
        var expected = Z80SioBitCodec.EncodeAsync(0x3C, Z80SioAsyncFormat.EightNOne);

        sio.WritePort(0x00, 0x3C);
        sio.Tick(1_000);
        var snapshot = sio.CaptureState();
        channel.TransmittedBits.Clear();

        var restored = new Z80Sio { BaudRate = 1_000 };
        restored.AttachChannel(Z80Sio.ChannelA, channel);
        restored.RestoreState(snapshot);
        restored.Tick(9_000);

        channel.TransmittedBits.Should().Equal(expected.Skip(1));
    }

    private static void EnableReceiver(Z80Sio sio, int channel)
    {
        var controlPort = (ushort)(channel + 2);
        SelectRegister(sio, controlPort, 3);
        sio.WritePort(controlPort, 0xC1);
    }

    private static void EnableExternalInterrupt(Z80Sio sio, int channel)
    {
        var controlPort = (ushort)(channel + 2);
        SelectRegister(sio, controlPort, 1);
        sio.WritePort(controlPort, 0x01);
    }

    private static void SelectRegister(Z80Sio sio, ushort controlPort, int register) =>
        sio.WritePort(controlPort, (byte)register);

    private sealed class TestChannel(int rxClockFrequencyHz, int txClockFrequencyHz) : IZ80SioChannel
    {
        public int RxClockFrequencyHz { get; } = rxClockFrequencyHz;
        public int TxClockFrequencyHz { get; } = txClockFrequencyHz;
        public bool CtsAsserted { get; private set; } = true;
        public bool DcdAsserted { get; private set; } = true;
        public bool RtsAsserted { get; private set; }
        public bool DtrAsserted { get; private set; }
        public List<byte> Transmitted { get; } = [];
        public event Action<byte, bool, bool>? RxDReceived;
        public event Action<bool>? CtsChanged;
        public event Action<bool>? DcdChanged;

        public void FeedRx(byte value, bool parityError = false, bool framingError = false) =>
            RxDReceived?.Invoke(value, parityError, framingError);

        public void SetCts(bool asserted)
        {
            CtsAsserted = asserted;
            CtsChanged?.Invoke(asserted);
        }

        public void SetDcd(bool asserted)
        {
            DcdAsserted = asserted;
            DcdChanged?.Invoke(asserted);
        }

        public void WriteTxD(byte value) => Transmitted.Add(value);
        public void SetRts(bool asserted) => RtsAsserted = asserted;
        public void SetDtr(bool asserted) => DtrAsserted = asserted;
    }

    private sealed class TestBitChannel(int rxClockFrequencyHz, int txClockFrequencyHz) : IZ80SioBitChannel
    {
        public int RxClockFrequencyHz { get; } = rxClockFrequencyHz;
        public int TxClockFrequencyHz { get; } = txClockFrequencyHz;
        public bool CtsAsserted { get; private set; } = true;
        public bool DcdAsserted { get; private set; } = true;
        public List<bool> TransmittedBits { get; } = [];
        public event Action<byte, bool, bool>? RxDReceived;
        public event Action<bool>? RxDBitReceived;
        public event Action<bool>? CtsChanged;
        public event Action<bool>? DcdChanged;

        public void FeedRxBits(IEnumerable<bool> bits)
        {
            foreach (var bit in bits)
                RxDBitReceived?.Invoke(bit);
        }

        public void FeedRx(byte value) => RxDReceived?.Invoke(value, false, false);
        public void SetCts(bool asserted)
        {
            CtsAsserted = asserted;
            CtsChanged?.Invoke(asserted);
        }

        public void SetDcd(bool asserted)
        {
            DcdAsserted = asserted;
            DcdChanged?.Invoke(asserted);
        }

        public void WriteTxDBit(bool value) => TransmittedBits.Add(value);
        public void WriteTxD(byte value) => throw new AssertionException("Bit channel must transmit through TxD bits.");
        public void SetRts(bool asserted) { }
        public void SetDtr(bool asserted) { }
    }
}
