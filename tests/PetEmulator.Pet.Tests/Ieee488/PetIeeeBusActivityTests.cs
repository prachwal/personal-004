using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Ieee488;

namespace PetEmulator.Pet.Tests.Ieee488;

/// <summary>Proves PetIeeeBus.Activity fires the right Kind/Detail sequence for control-line
/// changes and byte transfers - not just that a memory location changed.</summary>
public sealed class PetIeeeBusActivityTests
{
    [Test]
    public void ListenAndDataOut_RaisesLineChangeAndByteActivity()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);
        var activity = new List<IeeeBusActivity>();
        bus.Activity += activity.Add;

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28); // LISTEN 8
        bus.OnDioWrite(0x60); // secondary address 0
        bus.OnATNWrite(false);
        bus.OnDioWrite(0x41); // data byte 'A'

        activity.Should().Equal(
            new IeeeBusActivity("line-change", "ATN=true"),
            new IeeeBusActivity("byte", "0x28 written"),
            new IeeeBusActivity("line-change", "DAV=true NRFD=true NDAC=true"), // LISTEN command handshake
            new IeeeBusActivity("line-change", "NRFD=true NDAC=false"), // AcceptHandshake, first command byte after ATN
            new IeeeBusActivity("byte", "0x60 written"),
            new IeeeBusActivity("line-change", "DAV=true NRFD=true NDAC=true"), // secondary address handshake
            new IeeeBusActivity("line-change", "ATN=false"),
            new IeeeBusActivity("byte", "0x41 written"),
            new IeeeBusActivity("line-change", "NRFD=true NDAC=false")); // AcceptHandshake after data byte
    }

    [Test]
    public void TalkAndDataIn_RaisesByteReadActivity()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x99);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48); // TALK 8
        bus.OnDioWrite(0x60); // secondary address 0
        bus.OnATNWrite(false);
        bus.Tick();

        var activity = new List<IeeeBusActivity>();
        bus.Activity += activity.Add;

        bus.OnDioRead().Should().Be(0x99);

        activity.Should().Equal(
            new IeeeBusActivity("line-change", "NRFD=false NDAC=true"), // ProvideHandshake
            new IeeeBusActivity("byte", "0x99 read"));
    }

    [Test]
    public void NoSubscriber_DoesNotThrow()
    {
        var bus = new PetIeeeBus();
        var act = () =>
        {
            bus.OnATNWrite(true);
            bus.OnDioWrite(0x28);
            bus.OnATNWrite(false);
        };
        act.Should().NotThrow();
    }

    private sealed class MockDevice(int primaryAddr) : IIeeeDevice
    {
        private readonly Queue<byte> _dataToSend = new();

        public int PrimaryAddress { get; } = primaryAddr;
        public bool DataAvailable => _dataToSend.Count > 0;

        public void QueueBytes(params byte[] data)
        {
            foreach (byte b in data)
                _dataToSend.Enqueue(b);
        }

        public void OpenForRead(byte secondaryAddr) { }
        public void OpenForWrite(byte secondaryAddr) { }
        public void Close() { }
        public void Write(byte data) { }

        public bool TryRead(out byte data)
        {
            if (_dataToSend.TryDequeue(out data))
                return true;
            data = 0;
            return false;
        }
    }
}
