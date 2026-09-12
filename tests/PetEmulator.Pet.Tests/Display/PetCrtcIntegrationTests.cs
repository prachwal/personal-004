using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.Display;
using PetEmulator.Pet.Fonts;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.Display;

public sealed class PetCrtcIntegrationTests
{
    [TestCase("cbm-4032")]
    [TestCase("cbm-8032")]
    [CancelAfter(30_000)]
    [Explicit("Long-running real ROM/CRTC integration test; run explicitly.")]
    public void RealRomBoot_InitializesCrtcAndRendersScreen(string profileId)
    {
        var profile = PetProfileCatalog.Find(profileId);
        var machine = CreateMachine(profile);

        profile.RequiresCrtc.Should().BeTrue();
        machine.Crtc.Should().NotBeNull();

        machine.RunUntil(_ => machine.Crtc!.DisplayStartAddress != 0, 1_000_000)
            .Should().BeTrue("the profile ROM should configure the CRTC display start address");
        machine.RunUntil(_ => machine.Crtc!.DisplayEnable, 1_000_000)
            .Should().BeTrue("the CRTC should eventually enter its visible display interval");

        var crtcDisplayStart = machine.Crtc!.DisplayStartAddress;
        crtcDisplayStart.Should().NotBe(0, "the ROM should select a visible CRTC memory origin");

        machine.Memory.Write(PetMemoryBus.CrtcBase, 12);
        machine.Memory.Read((ushort)(PetMemoryBus.CrtcBase + 1))
            .Should().Be((byte)(crtcDisplayStart >> 8));

        var fontPath = Path.Combine(
            RomLocator.Directory(profile.RomDirectory, profile.CharacterRomPath),
            profile.CharacterRomPath);
        var font = new PetCharacterRomLoader().Load(fontPath);
        var display = new PetRasterDisplay(profile, machine.Memory, font);
        var frame = new uint[display.PixelWidth * display.PixelHeight];

        display.Render(frame);

        frame.Should().Contain(0xFF8DFF72u, "the boot screen should contain rendered PET glyph pixels");
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }
}
