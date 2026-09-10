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

        ReadByte(bus, out bool firstEoi).Should().Be(0x81);
        ReadByte(bus, out bool lastEoi).Should().Be(0x42);

        firstEoi.Should().BeFalse();
        lastEoi.Should().BeTrue();
        device.LastWriteSecondary.Should().Be(2);
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
        eoi = false;
        for (int bit = 0; bit < 8; bit++)
        {
            bus.Tick();
            bus.Tick();
            if (bus.DATA)
                data |= (byte)(1 << bit);
            eoi |= bus.EOI;
        }

        return data;
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
