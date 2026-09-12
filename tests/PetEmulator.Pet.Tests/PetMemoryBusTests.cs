using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Pet.Roms;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests;

/// <summary>Exercises <see cref="PetMemoryBus"/>'s address decoder in isolation (no CPU).</summary>
[TestFixture]
public sealed class PetMemoryBusTests
{
    [Test]
    public void Ram_ReadWrite_RoundTrips()
    {
        var bus = CreateBus(PetProfileCatalog.Pet2001_8, out _, out _, out _, out _);

        bus.Write(0x0100, 0x42);

        bus.Read(0x0100).Should().Be(0x42);
    }

    [Test]
    public void VideoRam_IsReachableAsRamSubrangeEvenBeyondMainRam()
    {
        // Pet2001_8: RamSize=0x2000, VideoRamStart=0x8000 - video RAM sits well beyond main RAM,
        // but per PetMemoryBus contract it is still a plain RAM read/write, not a separate device.
        var profile = PetProfileCatalog.Pet2001_8;
        var bus = CreateBus(profile, out _, out _, out _, out _);

        bus.Write((ushort)profile.VideoRamStart, 0x99);

        bus.Read((ushort)profile.VideoRamStart).Should().Be(0x99);
    }

    [Test]
    public void UnmappedGapBetweenRamAndVideoRam_ReadsAsOpenBus()
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var bus = CreateBus(profile, out _, out _, out _, out _);

        // 0x3000 falls after RamSize (0x2000) and before VideoRamStart (0x8000): unmapped.
        bus.Read(0x3000).Should().Be(0xFF);
    }

    [TestCase((ushort)0xE800)]
    [TestCase((ushort)0xE830)]
    [TestCase((ushort)0xE850)]
    [TestCase((ushort)0xE890)]
    public void UnmappedPeripheralAddress_ReadsAsOpenBus(ushort address)
    {
        var bus = CreateBus(PetProfileCatalog.Pet2001_8, out _, out _, out _, out _);

        bus.Read(address).Should().Be(0xFF);

        bus.Write(address, 0x42);
        bus.Read(address).Should().Be(0xFF, "unmapped writes must have no effect");
    }

    [Test]
    public void Rom_ReadsLoadedBytes_AndIsReadOnly()
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var directory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var roms = PetRomLoader.Load(directory, profile.RomManifest);
        var bus = CreateBus(profile, out _, out _, out _, out _, roms);

        var firstRom = roms[0];
        var address = (ushort)firstRom.Requirement.Address;
        var originalByte = firstRom.Data[0];

        bus.Read(address).Should().Be(originalByte);

        bus.Write(address, (byte)~originalByte);

        bus.Read(address).Should().Be(originalByte, "ROM writes must be silently dropped");
    }

    [Test]
    public void Pia1BaseAddress_RoutesToPia1()
    {
        var bus = CreateBus(PetProfileCatalog.Pet2001_8, out var pia1, out _, out _, out _);

        bus.Write(PetMemoryBus.Pia1Base, 0xFF); // select all-output DDRA (data-select bit clear)
        bus.Write((ushort)(PetMemoryBus.Pia1Base + 1), 0x04); // CRA: select data register
        bus.Write(PetMemoryBus.Pia1Base, 0x5A); // ORA

        pia1.ORA.Should().Be(0x5A);
        bus.Read(PetMemoryBus.Pia1Base).Should().Be(0x5A);
    }

    [Test]
    public void Pia2BaseAddress_RoutesToPia2()
    {
        var bus = CreateBus(PetProfileCatalog.Pet2001_8, out _, out var pia2, out _, out _);

        bus.Write(PetMemoryBus.Pia2Base, 0xFF);
        bus.Write((ushort)(PetMemoryBus.Pia2Base + 1), 0x04);
        bus.Write(PetMemoryBus.Pia2Base, 0xA5);

        pia2.ORA.Should().Be(0xA5);
        bus.Read(PetMemoryBus.Pia2Base).Should().Be(0xA5);
    }

    [Test]
    public void ViaBaseAddress_RoutesToVia()
    {
        var bus = CreateBus(PetProfileCatalog.Pet2001_8, out _, out _, out var via, out _);
        var acrAddress = (ushort)(PetMemoryBus.ViaBase + MOS6522.AuxiliaryControl);

        bus.Write(acrAddress, 0x42);

        via.ACR.Should().Be(0x42);
        bus.Read(acrAddress).Should().Be(0x42);
    }

    [Test]
    public void CrtcBaseAddress_RoutesToCrtc_WhenProfileRequiresIt()
    {
        var bus = CreateBus(PetProfileCatalog.Cbm8032, out _, out _, out _, out var crtc);
        crtc.Should().NotBeNull();

        bus.Write(PetMemoryBus.CrtcBase, 0); // select register 0
        bus.Write((ushort)(PetMemoryBus.CrtcBase + 1), 0x50);

        bus.Read((ushort)(PetMemoryBus.CrtcBase + 1)).Should().Be(0x50);
    }

    [Test]
    public void CrtcRange_ReadsAsOpenBus_WhenProfileHasNoCrtc()
    {
        var bus = CreateBus(PetProfileCatalog.Pet2001_8, out _, out _, out _, out var crtc);
        crtc.Should().BeNull();

        bus.Read(PetMemoryBus.CrtcBase).Should().Be(0xFF);
    }

    [Test]
    public void Cbm8296_ExpansionSwitchesTheSelected16KiBBank()
    {
        var bus = CreateBus(PetProfileCatalog.Cbm8296, out _, out _, out _, out _);

        bus.Write(0xFFF0, PetMemoryExpansion.Enabled);
        bus.Write(0x8000, 0x11);
        bus.Write(0xFFF0, (byte)(PetMemoryExpansion.Enabled | 0x04));
        bus.Write(0x8000, 0x22);

        bus.Write(0xFFF0, PetMemoryExpansion.Enabled);
        bus.Read(0x8000).Should().Be(0x11);
        bus.Write(0xFFF0, (byte)(PetMemoryExpansion.Enabled | 0x04));
        bus.Read(0x8000).Should().Be(0x22);
    }

    [Test]
    public void Cbm8296_ExpansionWriteProtectsBothWindows_AndControlRegisterIsWriteOnly()
    {
        var bus = CreateBus(PetProfileCatalog.Cbm8296, out _, out _, out _, out _);

        bus.Write(0xFFF0, (byte)(PetMemoryExpansion.Enabled | PetMemoryExpansion.LowerWriteProtect));
        bus.Write(0x8000, 0x55);

        bus.Read(0x8000).Should().Be(0x00);
        bus.Read(0xFFF0).Should().Be(0xFF);
        bus.ExpansionControl.Should().Be((byte)(PetMemoryExpansion.Enabled | PetMemoryExpansion.LowerWriteProtect));
    }

    [Test]
    public void Cbm8296_PeekThroughRestoresMainScreenAndIoInsteadOfExpansionRam()
    {
        var bus = CreateBus(PetProfileCatalog.Cbm8296, out var pia1, out _, out _, out _);

        bus.Write(0xFFF0, (byte)(PetMemoryExpansion.Enabled | PetMemoryExpansion.IoPeekThrough));
        bus.Write(0xE810, 0xFF);
        bus.Write(0xE811, 0x04);
        bus.Write(0xE810, 0x22);
        bus.Write(0xFFF0, PetMemoryExpansion.Enabled);
        bus.Write(0x8000, 0x11);

        bus.Write(0xFFF0, (byte)(PetMemoryExpansion.Enabled | PetMemoryExpansion.ScreenPeekThrough | PetMemoryExpansion.IoPeekThrough));
        bus.Write(0x8000, 0x33);
        bus.Read(0x8000).Should().Be(0x33);
        pia1.ORA.Should().Be(0x22);
        bus.Read(0xE810).Should().Be(0x22);
    }

    [Test]
    public void Cbm8296_ExpansionWindowCanExposeAddressThatIsRomWhenDisabled()
    {
        var requirement = new PetRomRequirement("test.bin", 0xF000, 1);
        var bus = CreateBus(PetProfileCatalog.Cbm8296, out _, out _, out _, out _, [new PetRomImage(requirement, [0x42])]);

        bus.Read(0xF000).Should().Be(0x42);
        bus.Write(0xFFF0, (byte)(PetMemoryExpansion.Enabled | PetMemoryExpansion.UpperWriteProtect));
        bus.Write(0xF000, 0x99);
        bus.Read(0xF000).Should().Be(0x00, "the protected expansion bank shadows the ROM and rejects writes");

        bus.Write(0xFFF0, PetMemoryExpansion.Enabled);
        bus.Write(0xF000, 0x99);
        bus.Read(0xF000).Should().Be(0x99);
    }

    private static PetMemoryBus CreateBus(
        PetProfile profile,
        out MT6520 pia1,
        out MT6520 pia2,
        out MOS6522 via,
        out MT6545? crtc,
        IReadOnlyList<PetRomImage>? roms = null)
    {
        pia1 = new MT6520("PIA1", PetMemoryBus.Pia1Base);
        pia2 = new MT6520("PIA2", PetMemoryBus.Pia2Base);
        via = new MOS6522("VIA", PetMemoryBus.ViaBase);
        crtc = profile.RequiresCrtc ? new MT6545("CRTC", PetMemoryBus.CrtcBase) : null;
        return new PetMemoryBus(profile, roms ?? [], pia1, pia2, via, crtc);
    }
}
