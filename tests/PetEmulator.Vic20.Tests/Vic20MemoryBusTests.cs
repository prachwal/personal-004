using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Chips;
using PetEmulator.Vic20.Chips;
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
    public void VicRegisters_RouteThroughToTheChip()
    {
        var vic = new Vic6560("VIC", 0x9000);
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

    private static Vic20MemoryBus CreateBus(Vic6560? vic = null)
    {
        var romsRoot = RomLocator.Directory("kernal.bin");
        var roms = Vic20RomLoader.Load(romsRoot, Vic20RomManifest.Ntsc);
        vic ??= new Vic6560("VIC", 0x9000);
        var via1 = new Via6522("VIA1", 0x9110);
        var via2 = new Via6522("VIA2", 0x9120);
        var colorRam = new Vic20ColorRam("Color RAM", 0x9400, 0x0400);
        return new Vic20MemoryBus(roms, vic, via1, via2, colorRam);
    }
}
