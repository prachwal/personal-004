using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpc6128;

namespace PetEmulator.Cpc6128.Tests;

public sealed class Cpc6128MachineTests
{
    [Test]
    public void Reset_starts_with_64K_configuration_and_rom_overlays()
    {
        var machine = new Cpc6128Machine(new byte[Cpc6128MemoryBus.RomSize]);

        machine.GateArray.RamConfiguration.Should().Be(0);
        machine.Bus.CapturePhysicalRam().Should().HaveCount(Cpc6128MemoryBus.PhysicalRamSize);
        machine.Bus.ReadMemory(0x0000).Should().Be(0);
        machine.Bus.ReadMemory(0xC000).Should().Be(0);
    }

    [TestCase(0, 3)]
    [TestCase(1, 7)]
    [TestCase(2, 7)]
    [TestCase(3, 7)]
    [TestCase(4, 3)]
    [TestCase(5, 3)]
    [TestCase(6, 3)]
    [TestCase(7, 3)]
    public void Ram_configuration_maps_upper_window_to_expected_bank(byte configuration, int bank)
    {
        var machine = new Cpc6128Machine(new byte[Cpc6128MemoryBus.RomSize]);
        machine.Ports.Write(0x7F00, 0x8E); // mode 2, both ROM overlays disabled
        machine.Ports.Write(0x7F00, (byte)(0xC0 | configuration));
        machine.Bus.WriteMemory(0xC000, configuration);

        machine.Bus.ReadRam(0xC000).Should().Be(configuration);
        machine.Bus.CapturePhysicalRam()[bank * 0x4000].Should().Be(configuration);
    }

    [Test]
    public void Unmapped_ports_float_high()
    {
        var machine = new Cpc6128Machine(new byte[Cpc6128MemoryBus.RomSize]);
        machine.Ports.Read(0x1200).Should().Be(0xFF);
        machine.Ports.Read(0xFB7E).Should().Be(0xFF);
    }

    [Test]
    public void Cpu_bus_routes_ports_and_expansion_rom_selection()
    {
        var machine = new Cpc6128Machine(new byte[Cpc6128MemoryBus.RomSize]);
        machine.LoadExpansionRom(7, new byte[0x4000].Select(_ => (byte)0xA5).ToArray());
        machine.Ports.Write(0x7F00, 0x86); // lower ROM disabled, upper ROM enabled
        machine.Bus.WritePort(0xDF00, 7);

        machine.Bus.ReadPort(0xDF00).Should().Be(7);
        machine.Bus.ReadMemory(0xC000).Should().Be(0xA5);
    }
}
