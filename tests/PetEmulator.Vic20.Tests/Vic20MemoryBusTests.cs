using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Vic20.Roms;
using PetEmulator.Vic20.Tests.Roms;
using PetEmulator.Vic20.Cartridge.Abstractions;

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

    [Test]
    public void Cartridge_RomIsMappedAtA000_AndRomWritesAreIgnored()
    {
        var cartridge = new Vic20Cartridge([0x42, 0x43]);
        var bus = CreateBus(cartridge: cartridge);

        bus.Read(0xA000).Should().Be(0x42);
        bus.Read(0xA001).Should().Be(0x43);
        bus.Write(0xA000, 0x99);
        bus.Read(0xA000).Should().Be(0x42);
    }

    [Test]
    public void Cartridge_RegisterDevice_IsMappedToIo2AndIo3()
    {
        var device = new Vic20RegisterIoDevice(0x9800, 2);
        var bus = CreateBus(cartridge: new Vic20Cartridge([0x42], ioDevices: [device]));

        bus.Write(0x9801, 0x77);

        bus.Read(0x9801).Should().Be(0x77);
        bus.Read(0xA000).Should().Be(0x42);
    }

    [Test]
    public void CartridgeResources_RejectOverlappingRanges()
    {
        var first = new Vic20Cartridge([0x42]);
        var second = new Vic20Cartridge([0x43]);

        Action action = () => Vic20CartridgeResourceValidator.ThrowIfConflicting(
            first.Resources.Concat(second.Resources));

        action.Should().Throw<InvalidOperationException>().WithMessage("*ROM*overlap*$A000*");
    }

    [Test]
    public void MultipleCartridges_AllowIndependentRanges_AndKeepPreviousOnConflict()
    {
        var bus = CreateBus();
        var first = new Vic20Cartridge([0x42], 0xA000);
        var second = new Vic20Cartridge([0x43], 0xB000);
        var conflicting = new Vic20Cartridge([0x99], 0xA000);

        bus.InsertCartridge(first);
        bus.InsertCartridge(second);
        Action action = () => bus.InsertCartridge(conflicting);

        action.Should().Throw<InvalidOperationException>();
        bus.Cartridges.Should().HaveCount(2);
        bus.Read(0xA000).Should().Be(0x42);
        bus.Read(0xB000).Should().Be(0x43);
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
        Vic20Cartridge? cartridge = null)
    {
        var romsRoot = RomLocator.Directory("kernal.bin");
        var roms = Vic20RomLoader.Load(romsRoot, Vic20RomManifest.Ntsc);
        vic ??= new MOS6560("VIC", 0x9000);
        var via1 = new MOS6522("VIA1", 0x9110);
        var via2 = new MOS6522("VIA2", 0x9120);
        var colorRam = new MOS2114("Color RAM", 0x9400, 0x0400);
        return new Vic20MemoryBus(roms, vic, via1, via2, colorRam, cartridge);
    }
}
