using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Vic20.Roms;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

/// <summary>Layer 1 (see docs/vic20-migration-plan.md step 12): register-level bus integration -
/// pokes chip registers directly through the decoded bus, no CPU/KERNAL involved.</summary>
public sealed class Vic20MemoryBusTests
{
    [Test]
    public void RealRoms_LoadAtTheirRealAddresses()
    {
        var bus = CreateBus();

        // KERNAL reset vector lives at $FFFC/$FFFD - a real, meaningful byte only present if the
        // real kernal.bin actually loaded at $E000.
        var lo = bus.Read(0xFFFC);
        var hi = bus.Read(0xFFFD);
        var resetVector = (ushort)(lo | (hi << 8));

        resetVector.Should().BeInRange(0xE000, 0xFFFF, "the RESET vector must point into KERNAL ROM");
    }

    [Test]
    public void Ram_IsReadWrite_AtBothRegions()
    {
        var bus = CreateBus();

        bus.Write(0x0080, 0x42);
        bus.Write(0x1500, 0x99);

        bus.Read(0x0080).Should().Be(0x42);
        bus.Read(0x1500).Should().Be(0x99);
    }

    [Test]
    public void Rom_WritesAreSilentlyDropped()
    {
        var bus = CreateBus();
        var before = bus.Read(0xE000);

        bus.Write(0xE000, (byte)(before + 1));

        bus.Read(0xE000).Should().Be(before);
    }

    [Test]
    public void UnmappedAddress_ReadsAsOpenBus()
    {
        var bus = CreateBus();

        bus.Read(0xA000).Should().Be(0xFF, "the cartridge window is unmapped in v1 - see plan step 5");
    }

    [Test]
    public void Io2AndIo3_ReadAsOpenBus_AndIgnoreWrites()
    {
        var bus = CreateBus();

        foreach (var address in new ushort[] { 0x9800, 0x9FFF })
        {
            bus.Read(address).Should().Be(0xFF);

            bus.Write(address, 0x42);
            bus.Read(address).Should().Be(0xFF, $"I/O2/I/O3 address ${address:X4} is not mapped");
        }
    }

    [TestCase(Vic20ExpansionPreset.ThreeK, 0x0400, 0x2000)]
    [TestCase(Vic20ExpansionPreset.EightK, 0x2000, 0x0400)]
    [TestCase(Vic20ExpansionPreset.SixteenK, 0x4000, 0x6000)]
    [TestCase(Vic20ExpansionPreset.TwentyFourK, 0x6000, 0xA000)]
    public void ExpansionPreset_EnablesExpectedRamAndLeavesNextBlockUnmapped(
        Vic20ExpansionPreset preset, int enabledAddress, int unmappedAddress)
    {
        var bus = CreateBus(expansionPreset: preset);

        bus.Write((ushort)enabledAddress, 0x42);
        bus.Read((ushort)enabledAddress).Should().Be(0x42);
        bus.Read((ushort)unmappedAddress).Should().Be(0xFF);
    }

    [Test]
    public void AllExpansionPreset_EnablesEveryRamBlock()
    {
        var bus = CreateBus(expansionPreset: Vic20ExpansionPreset.All);
        ushort[] addresses = [0x0400, 0x2000, 0x4000, 0x6000, 0xA000];

        foreach (var address in addresses)
        {
            bus.Write(address, 0x42);
            bus.Read(address).Should().Be(0x42);
        }
    }

    [Test]
    public void VicRegisters_RouteThroughToTheChip()
    {
        var vic = new MOS6560("VIC", 0x9000);
        var bus = CreateBus(vic: vic);

        bus.Write(0x9002, 0x16);

        vic.Columns.Should().Be(22);
        bus.Read(0x9002).Should().Be(0x16);
    }

    [Test]
    public void ColorRam_RoutesThroughToTheChip_MaskedToLowNibble()
    {
        var bus = CreateBus();

        bus.Write(0x9400, 0xFF);

        bus.Read(0x9400).Should().Be(0x0F);
    }

    [Test]
    public void Observer_FiresForEveryRealReadAndWrite()
    {
        var bus = CreateBus();
        var events = new List<PetEmulator.Core.BusAccess>();
        bus.Observer = events.Add;

        bus.Write(0x0080, 0x42);
        bus.Read(0x0080);

        events.Should().ContainSingle(e => e.IsWrite && e.Address == 0x0080 && e.Value == 0x42);
        events.Should().ContainSingle(e => !e.IsWrite && e.Address == 0x0080 && e.Value == 0x42);
    }

    private static Vic20MemoryBus CreateBus(
        MOS6560? vic = null,
        Vic20ExpansionPreset expansionPreset = Vic20ExpansionPreset.Unexpanded)
    {
        var romsRoot = RomLocator.Directory("kernal.bin");
        var roms = Vic20RomLoader.Load(romsRoot, Vic20RomManifest.Ntsc);
        vic ??= new MOS6560("VIC", 0x9000);
        var via1 = new MOS6522("VIA1", 0x9110);
        var via2 = new MOS6522("VIA2", 0x9120);
        var colorRam = new MOS2114("Color RAM", 0x9400, 0x0400);
        return new Vic20MemoryBus(roms, vic, via1, via2, colorRam, expansionPreset);
    }
}
