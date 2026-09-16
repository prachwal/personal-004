using System.Text;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpc6128;
using PetEmulator.CpcFdc;

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
        machine.Ports.Read(0xFA7F).Should().Be(0xFF);
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

    [Test]
    public void Standard_dsk_is_loaded_and_exposed_through_8272_ports()
    {
        var machine = new Cpc6128Machine(new byte[Cpc6128MemoryBus.RomSize]);
        var image = DskDiskImage.Load(CreateDsk(0x5A));

        machine.LoadDisk(0, image);

        machine.Fdc.Drive0.Should().BeOfType<DskFloppyDrive>();
        machine.Ports.Read(0xFB7E).Should().Be(0x80);
        machine.Ports.Write(0xFB7F, 0x46);
        foreach (var value in new byte[] { 0, 0, 0, 1, 2, 1, 0x1B, 0xFF }) machine.Ports.Write(0xFB7F, value);
        var data = new byte[512];
        for (var index = 0; index < data.Length; index++) data[index] = machine.Ports.Read(0xFB7F);
        data.Should().OnlyContain(value => value == 0x5A);
        machine.Ports.Read(0xFB7F).Should().Be(0);
    }

    [Test]
    public void Snapshot_RestoresCpuRamPortsMediaAndVideoState()
    {
        var machine = new Cpc6128Machine(new byte[Cpc6128MemoryBus.RomSize]);
        machine.Bus.WriteMemory(0x4000, 0xA5);
        machine.Ports.Write(0x7F00, 0x86);
        machine.Ports.Write(0xDF00, 7);
        machine.Ports.Write(0xF700, 0x80);
        machine.Keyboard.SetKey(3, 4, true);
        machine.Cassette.LoadTape([1, 2, 3]);
        machine.StepInstruction();
        var snapshot = machine.CaptureState();

        machine.Bus.WriteMemory(0x4000, 0x11);
        machine.Ports.Write(0xDF00, 2);
        machine.Keyboard.SetKey(3, 4, false);
        machine.StepInstruction();
        machine.RestoreState(snapshot);

        machine.Bus.ReadRam(0x4000).Should().Be(0xA5);
        machine.Bus.UpperRomNumber.Should().Be(7);
        machine.Keyboard.ReadRow(3).Should().Be((byte)0xEF);
        machine.Cpu.Registers.PC.Should().Be((ushort)snapshot.Cpu.Registers["PC"]);
        machine.CycleCount.Should().Be(snapshot.Cpu.CycleCount);
    }

    [Test]
    public void SnapshotCodec_RoundTripsSnapshot()
    {
        var machine = new Cpc6128Machine(new byte[Cpc6128MemoryBus.RomSize]);
        var encoded = Cpc6128SnapshotCodec.Encode(machine.CaptureState());
        var restored = Cpc6128SnapshotCodec.Decode(encoded);
        restored.Version.Should().Be(1);
        restored.PhysicalRam.Should().HaveCount(Cpc6128MemoryBus.PhysicalRamSize);
        restored.Cpu.Registers["SP"].Should().Be(0xFFFF);
    }

    private static byte[] CreateDsk(byte fill)
    {
        var dsk = new byte[0x400];
        Encoding.ASCII.GetBytes("MV - CPCEMU Disk-File\r\nDisk-Info\r\n").CopyTo(dsk, 0);
        dsk[0x30] = 1;
        dsk[0x31] = 1;
        dsk[0x32] = 0;
        dsk[0x33] = 3;
        Encoding.ASCII.GetBytes("Track-Info\r\n").CopyTo(dsk, 0x100);
        dsk[0x115] = 1;
        dsk[0x11A] = 1;
        dsk[0x11B] = 2;
        dsk.AsSpan(0x200).Fill(fill);
        return dsk;
    }
}
