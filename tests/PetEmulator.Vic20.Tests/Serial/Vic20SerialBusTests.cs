using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Ieee488;
using PetEmulator.Vic20.Serial;

namespace PetEmulator.Vic20.Tests.Serial;

public sealed class Vic20SerialBusTests
{
    [Test]
    public void ListenOpenAndDataOut_ShiftsLsbFirstBytesToDevice()
    {
        var bus = new Vic20SerialBus();
        var device = new FakeDevice(8);
        bus.AttachDevice(device);

        bus.SetHostAtn(false);
        SendByte(bus, 0x28);
        SendByte(bus, 0xF3);
        bus.SetHostAtn(true);
        SendByte(bus, 0x48);
        SendByte(bus, 0x45);

        device.ReceivedBytes.Should().Equal(0x48, 0x45);
        device.LastReadSecondary.Should().Be(3);
    }

    [Test]
    public void Talk_ShiftsLsbFirstBytesAndAssertsEoiForTheLastByte()
    {
        var bus = new Vic20SerialBus();
        var device = new FakeDevice(8);
        device.QueueBytes(0x81, 0x42);
        bus.AttachDevice(device);

        bus.SetHostAtn(false);
        SendByte(bus, 0x48);
        SendByte(bus, 0x62);
        bus.SetHostAtn(true);
        bus.SetHostData(true);
        Tick(bus, TalkerAcknowledgeTicks); // the newly-addressed talker acknowledges before any byte

        ReadByte(bus, out bool firstEoi).Should().Be(0x81);
        Tick(bus, 100); // the talker's extra final CLK-low pulse after the 8th bit (real KERNAL
                         // FACPTR's LAB_EF66 waits for it before requesting the next byte)
        ReadByte(bus, out bool lastEoi).Should().Be(0x42);

        firstEoi.Should().BeFalse();
        lastEoi.Should().BeTrue();
        device.LastWriteSecondary.Should().Be(2);
    }

    [Test]
    public void Listener_AcknowledgesAttentionAndCompletedBytesOnData()
    {
        var bus = new Vic20SerialBus();
        bus.AttachDevice(new FakeDevice(8));

        bus.SetHostAtn(false);
        bus.DATA.Should().BeFalse("a device acknowledges ATN before the first command bit");

        bus.SetHostClock(false);
        bus.SetHostData(true);
        bus.SetHostClock(true);
        bus.DATA.Should().BeTrue("raising CLK releases the command acknowledge");

        for (var bit = 0; bit < 8; bit++)
        {
            bus.SetHostData((0x28 & (1 << bit)) != 0);
            bus.SetHostClock(false);
            bus.SetHostClock(true);
        }

        bus.DATA.Should().BeFalse("the listener acknowledges the completed command byte");
    }

    [Test]
    public void Talker_WaitsForEoiAcknowledgeAndKeepsEachBitStable()
    {
        var bus = new Vic20SerialBus();
        var device = new FakeDevice(8);
        device.QueueBytes(0x81);
        bus.AttachDevice(device);
        SelectTalker(bus);
        Tick(bus, TalkerAcknowledgeTicks); // the newly-addressed talker acknowledges before any byte

        bus.SetHostData(false);
        bus.SetHostClock(true);
        bus.SetHostData(true);
        bus.Tick();
        bus.EOI.Should().BeTrue();
        bus.CLK.Should().BeTrue("EOI is a clock-high gap, not a side-band flag");

        bus.SetHostData(false);
        bus.Tick();
        bus.SetHostData(true);
        bus.Tick();
        bus.CLK.Should().BeFalse("the talker starts only after the listener acknowledges EOI");

        Tick(bus, BitPhaseTicks);
        bus.DATA.Should().BeTrue("the first bit remains available for the KERNAL polling loop");
    }

    [Test]
    public void AtNAssertedMidTransfer_AbortsTalkAndResumesCommandMode()
    {
        var bus = new Vic20SerialBus();
        var device = new FakeDevice(8);
        device.QueueBytes(0x55);
        bus.AttachDevice(device);

        bus.SetHostAtn(false);
        SendByte(bus, 0x48);
        bus.SetHostAtn(true);
        bus.Tick();

        bus.SetHostAtn(false);
        SendByte(bus, 0x28);
        SendByte(bus, 0xF1);
        bus.SetHostAtn(true);
        SendByte(bus, 0xAA);

        device.CloseCount.Should().Be(1);
        device.ReceivedBytes.Should().Equal(0xAA);
        device.LastReadSecondary.Should().Be(1);
        bus.EOI.Should().BeFalse();
    }

    [Test]
    public void CloseSecondary_ClosesTheListenerBeforeDataPhase()
    {
        var bus = new Vic20SerialBus();
        var device = new FakeDevice(8);
        bus.AttachDevice(device);

        bus.SetHostAtn(false);
        SendByte(bus, 0x28);
        SendByte(bus, 0xF4);
        SendByte(bus, 0xE4);
        bus.SetHostAtn(true);
        SendByte(bus, 0x12);

        device.CloseCount.Should().Be(1);
        device.ReceivedBytes.Should().BeEmpty();
    }

    private static void SendByte(Vic20SerialBus bus, byte data)
    {
        bus.SetHostClock(false);
        bus.SetHostData(true);
        bus.SetHostClock(true);

        for (int bit = 0; bit < 8; bit++)
        {
            bus.SetHostData((data & (1 << bit)) != 0);
            bus.SetHostClock(false);
            bus.SetHostClock(true);
        }
    }

    private static byte ReadByte(Vic20SerialBus bus, out bool eoi)
    {
        byte data = 0;
        bus.SetHostData(false);
        bus.SetHostClock(true);
        bus.SetHostData(true);
        bus.Tick();
        eoi = bus.EOI;
        if (eoi)
        {
            bus.SetHostData(false);
            bus.Tick();
            bus.SetHostData(true);
            bus.Tick();
        }

        for (int bit = 0; bit < 8; bit++)
        {
            Tick(bus, BitPhaseTicks);
            if (bus.DATA)
                data |= (byte)(1 << bit);
            Tick(bus, BitPhaseTicks);
        }

        return data;
    }

    private static void SelectTalker(Vic20SerialBus bus)
    {
        bus.SetHostAtn(false);
        SendByte(bus, 0x48);
        bus.SetHostAtn(true);
    }

    // Matches Vic20SerialBus's own TalkerAcknowledgePulseCycles: the newly-addressed talker pulses
    // CLK low, unprompted, before the per-byte ready handshake - see that constant's doc comment.
    private const int TalkerAcknowledgeTicks = 100;

    // Matches Vic20SerialBus's own BitPhaseCycles - see that constant's doc comment.
    private const int BitPhaseTicks = 100;

    private static void Tick(Vic20SerialBus bus, int cycles)
    {
        for (var i = 0; i < cycles; i++)
            bus.Tick();
    }

    private sealed class FakeDevice(int primaryAddress) : IIeeeDevice
    {
        private readonly Queue<byte> _toSend = new();

        public int PrimaryAddress { get; } = primaryAddress;
        public List<byte> ReceivedBytes { get; } = [];
        public int CloseCount { get; private set; }
        public byte LastReadSecondary { get; private set; }
        public byte LastWriteSecondary { get; private set; }
        public bool DataAvailable => _toSend.Count > 0;

        public void QueueBytes(params byte[] data)
        {
            foreach (byte value in data)
                _toSend.Enqueue(value);
        }

        public void OpenForRead(byte secondaryAddr) => LastReadSecondary = secondaryAddr;
        public void OpenForWrite(byte secondaryAddr) => LastWriteSecondary = secondaryAddr;
        public void Close() => CloseCount++;
        public void Write(byte data) => ReceivedBytes.Add(data);

        public bool TryRead(out byte data) => _toSend.TryDequeue(out data);
    }
}
