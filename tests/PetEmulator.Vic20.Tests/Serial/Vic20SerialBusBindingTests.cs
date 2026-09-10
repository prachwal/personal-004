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
        via2.Write(MOS6522.PeripheralControl, 0xCC); // CA2/CB2 forced low
        binding.Tick();

        bus.ATN.Should().BeFalse();
        bus.CLK.Should().BeFalse();
        bus.DATA.Should().BeFalse();

        via1.Write(MOS6522.OraWithoutHandshake, 0x80);
        via2.Write(MOS6522.PeripheralControl, 0xEE); // CA2/CB2 forced high
        binding.Tick();

        bus.ATN.Should().BeTrue();
        bus.CLK.Should().BeTrue();
        bus.DATA.Should().BeTrue();
    }

    [Test]
    public void Bus_talker_levels_update_via1_clock_and_data_inputs()
    {
        var bus = new Vic20SerialBus();
        var device = new FakeDevice(8, 0x00);
        bus.AttachDevice(device);
        SelectTalker(bus);

        var via1 = new MOS6522();
        var via2 = new MOS6522();
        via2.Write(MOS6522.PeripheralControl, 0xEE); // release host CLK/DATA
        var binding = new Vic20SerialBusBinding(via1, via2, bus);

        binding.Tick(); // the talker asserts CLK and drives the zero data bit low

        (via1.PortAInput & 0x03).Should().Be(0x00);

        binding.Tick(); // the talker releases CLK; DATA remains the current bit

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
