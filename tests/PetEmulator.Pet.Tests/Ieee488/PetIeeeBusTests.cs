using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Ieee488;

namespace PetEmulator.Pet.Tests.Ieee488;

/// <summary>Pure PetIeeeBus/IIeeeDevice logic - no PetMachine/PIA coupling.</summary>
public sealed class PetIeeeBusTests
{
    [Test]
    public void Idle_InitialState()
    {
        var bus = new PetIeeeBus();
        bus.LastDio.Should().Be(0);
    }

    [Test]
    public void ListenAndDataOut_SendsBytesToDevice()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x45);
        bus.OnDioWrite(0x4C);

        dev.ReceivedBytes.Should().Equal(0x48, 0x45, 0x4C);
        dev.LastReadSec.Should().Be(0);
    }

    [Test]
    public void TalkAndDataIn_ReadsBytesFromDevice()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x01, 0x02, 0x03);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        // PetIeeeBus.SettleDelayCycles ticks (4,000) must fully elapse before the next byte is
        // pre-fetched - see that constant's doc comment for why it isn't 32 any more.
        const int settleDelayCycles = 4_000;

        bus.Tick();
        bus.OnDioRead().Should().Be(0x01);
        bus.SetNdacAccepted(true);
        for (int i = 0; i < settleDelayCycles + 1; i++) bus.Tick();
        bus.OnDioRead().Should().Be(0x02);
        bus.SetNdacAccepted(true);
        for (int i = 0; i < settleDelayCycles + 1; i++) bus.Tick();
        bus.OnDioRead().Should().Be(0x03);
        dev.LastWriteSec.Should().Be(0);
    }

    [Test]
    public void DataIn_NoMoreData_Returns0xFF()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.Tick();
        bus.OnDioRead().Should().Be(0xFF);
    }

    [Test]
    public void Unlisten_ClearsListenerAndCloses()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);
        bus.OnDioWrite(0x48);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x3F);
        bus.OnATNWrite(false);

        dev.IsOpen.Should().BeFalse();
        dev.ReceivedBytes.Should().Equal(0x48);
    }

    [Test]
    public void Untalk_ClearsTalkerAndCloses()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x99);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.Tick();
        bus.OnDioRead().Should().Be(0x99);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x5F);
        bus.OnATNWrite(false);

        dev.IsOpen.Should().BeFalse();
    }

    [Test]
    public void SecondDevice_AddressedCorrectly()
    {
        var bus = new PetIeeeBus();
        var dev8 = new MockDevice(8);
        var dev9 = new MockDevice(9);
        bus.AttachDevice(dev8);
        bus.AttachDevice(dev9);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x29);
        bus.OnDioWrite(0x61);
        bus.OnATNWrite(false);
        bus.OnDioWrite(0xFF);

        dev8.ReceivedBytes.Should().BeEmpty();
        dev9.ReceivedBytes.Should().Equal(0xFF);
        dev9.LastReadSec.Should().Be(1);
    }

    [Test]
    public void ViaPortBInput_AfterCommandHandshake()
    {
        var bus = new PetIeeeBus();
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);

        byte pb = bus.GetViaPortBInput();
        (pb & 0x01).Should().Be(1); // NDAC inactive until byte accepted
        (pb & 0x40).Should().Be(0); // NRFD=1 -> not ready
        (pb & 0x80).Should().Be(0); // DAV=1 -> data valid
    }

    [Test]
    public void ViaPortBInput_AfterReset()
    {
        var bus = new PetIeeeBus();
        bus.Reset();

        byte pb = bus.GetViaPortBInput();
        (pb & 0x01).Should().Be(0);
        (pb & 0x40).Should().Be(0);
        (pb & 0x80).Should().Be(0x80);
    }

    [Test]
    public void ListenThenTalk_SwitchesDirection()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x99);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);
        bus.OnDioWrite(0x48);
        bus.OnATNWrite(true);
        bus.OnDioWrite(0x3F);
        bus.OnATNWrite(false);

        dev.CloseCount.Should().Be(2);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        bus.Tick();
        bus.OnDioRead().Should().Be(0x99);
    }

    [Test]
    public void Reset_ClearsState()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnATNWrite(false);
        bus.OnDioWrite(0x41);

        bus.Reset();

        bus.LastDio.Should().Be(0);
        dev.ReceivedBytes.Should().Equal(0x41);
        dev.IsOpen.Should().BeFalse();
    }

    [Test]
    public void MultipleListen_UpdatesAddress()
    {
        var bus = new PetIeeeBus();
        var dev9 = new MockDevice(9);
        bus.AttachDevice(dev9);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x29);
        bus.OnDioWrite(0x62);
        bus.OnATNWrite(false);

        bus.OnDioWrite(0x99);

        dev9.ReceivedBytes.Should().Equal(0x99);
        dev9.LastReadSec.Should().Be(2);
    }

    [Test]
    public void PortBBinding_ReadPins_ReturnsFFWhenBusIdle()
    {
        var bus = new PetIeeeBus();
        var binding = new PetIeeePortBBinding(bus);

        byte pins = binding.ReadPins();
        pins.Should().Be(0x00, "idle bus -> $FF XOR'd with $FF = $00");
    }

    [Test]
    public void PortBBinding_ReadPins_ReturnsBusDataInDataInMode()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        dev.QueueBytes(0x55);
        bus.AttachDevice(dev);
        var binding = new PetIeeePortBBinding(bus);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);
        bus.Tick();

        byte pins = binding.ReadPins();
        pins.Should().Be((byte)(0x55 ^ 0xFF), "0x55 XOR'd with $FF");
    }

    [Test]
    public void PortBBinding_WritePins_ForwardsToBusWhenDdrOutput()
    {
        var bus = new PetIeeeBus();
        var dev = new MockDevice(8);
        bus.AttachDevice(dev);
        var binding = new PetIeeePortBBinding(bus);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x28);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);

        binding.WritePins(0x42, 0xFF);

        dev.ReceivedBytes.Should().Equal((byte)(0x42 ^ 0xFF));
    }

    [Test]
    public void AttachDevice_ReplacesAnExistingDeviceAtTheSamePrimaryAddress()
    {
        var bus = new PetIeeeBus();
        var original = new MockDevice(8);
        original.QueueBytes(0x11);
        bus.AttachDevice(original);

        var replacement = new MockDevice(8);
        replacement.QueueBytes(0x22);
        bus.AttachDevice(replacement);

        bus.OnATNWrite(true);
        bus.OnDioWrite(0x48);
        bus.OnDioWrite(0x60);
        bus.OnATNWrite(false);
        bus.Tick();

        bus.OnDioRead().Should().Be(0x22,
            "the second AttachDevice at address 8 should replace the first, not sit alongside it");
    }

    // The original project's own "WritePins_IgnoredWhenNotAllOutput" test is not ported: its
    // expectation (a no-op when ddrMask is 0) does not hold against PetIeeePortBBinding.WritePins
    // as ported verbatim from source, which always forwards regardless of ddrMask. See that
    // method's doc comment.

    private sealed class MockDevice(int primaryAddr) : IIeeeDevice
    {
        private readonly Queue<byte> _dataToSend = new();

        public int PrimaryAddress { get; } = primaryAddr;
        public List<byte> ReceivedBytes { get; } = [];
        public bool IsOpen { get; private set; }
        public int CloseCount { get; private set; }
        public byte LastReadSec { get; private set; }
        public byte LastWriteSec { get; private set; }
        public bool DataAvailable => _dataToSend.Count > 0;

        public void QueueBytes(params byte[] data)
        {
            foreach (byte b in data)
                _dataToSend.Enqueue(b);
        }

        public void OpenForRead(byte secondaryAddr)
        {
            IsOpen = true;
            LastReadSec = secondaryAddr;
        }

        public void OpenForWrite(byte secondaryAddr)
        {
            IsOpen = true;
            LastWriteSec = secondaryAddr;
        }

        public void Close()
        {
            IsOpen = false;
            CloseCount++;
        }

        public void Write(byte data) => ReceivedBytes.Add(data);

        public bool TryRead(out byte data)
        {
            if (_dataToSend.TryDequeue(out data))
                return true;
            data = 0;
            return false;
        }
    }
}
