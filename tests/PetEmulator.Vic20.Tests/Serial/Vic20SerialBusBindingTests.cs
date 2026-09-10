using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Pet.Ieee488;
using PetEmulator.Vic20.Serial;

namespace PetEmulator.Vic20.Tests.Serial;

public sealed class Vic20SerialBusBindingTests
{
    [Test]
    public void Via_outputs_drive_active_low_iec_lines()
    {
        var via1 = new MOS6522();
        var via2 = new MOS6522();
        var bus = new Vic20SerialBus();
        var binding = new Vic20SerialBusBinding(via1, via2, bus);

        via1.Write(MOS6522.Ddra, 0x80);
        via1.Write(MOS6522.OraWithoutHandshake, 0x00);
        via2.Write(MOS6522.PeripheralControl, 0xCC); // VIA outputs low: IEC transceivers release lines
        binding.Tick();

        bus.ATN.Should().BeTrue();
        bus.CLK.Should().BeTrue();
        bus.DATA.Should().BeTrue();

        via1.Write(MOS6522.OraWithoutHandshake, 0x80);
        via2.Write(MOS6522.PeripheralControl, 0xEE); // VIA outputs high: IEC transceivers assert lines
        binding.Tick();

        bus.ATN.Should().BeFalse();
        bus.CLK.Should().BeFalse();
        bus.DATA.Should().BeFalse();
    }

    [Test]
    public void Bus_talker_levels_update_via1_clock_and_data_inputs()
    {
        var bus = new Vic20SerialBus();
        var device = new FakeDevice(8, 0x00, 0xFF);
        bus.AttachDevice(device);
        SelectTalker(bus);

        var via1 = new MOS6522();
        var via2 = new MOS6522();
        via2.Write(MOS6522.PeripheralControl, 0xCC); // low VIA outputs release host CLK/DATA
        var binding = new Vic20SerialBusBinding(via1, via2, bus);

        // The newly-addressed talker acknowledges (pulses CLK low, unprompted) before it starts
        // the per-byte ready handshake - see Vic20SerialBus.TalkerAcknowledgePulseCycles.
        for (var i = 0; i < 100; i++)
            binding.Tick();

        binding.Tick(); // the talker asserts CLK and drives the zero data bit low

        (via1.PortAInput & 0x03).Should().Be(0x00);

        for (var i = 0; i < 100; i++)
            binding.Tick(); // the talker holds each bit phase long enough for the KERNAL poll loop -
                             // see Vic20SerialBus.BitPhaseCycles

        (via1.PortAInput & 0x01).Should().Be(0x01);
        (via1.PortAInput & 0x02).Should().Be(0x00);
    }

    private static void SelectTalker(Vic20SerialBus bus)
    {
        bus.SetHostAtn(false);
        SendByte(bus, 0x48); // TALK 8
        bus.SetHostAtn(true);
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

    private sealed class FakeDevice(int primaryAddress, params byte[] data) : IIeeeDevice
    {
        private readonly Queue<byte> _data = new(data);

        public int PrimaryAddress { get; } = primaryAddress;
        public bool DataAvailable => _data.Count > 0;
        public void OpenForRead(byte secondaryAddr) { }
        public void OpenForWrite(byte secondaryAddr) { }
        public void Close() { }
        public void Write(byte data) { }
        public bool TryRead(out byte data) => _data.TryDequeue(out data);
    }
}
