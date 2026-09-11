using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet.Tests.Roms;

/// <summary>
/// Confirms each profile's ROM subfolder actually holds the ROMs its manifest and character-ROM
/// path point at, and that the loader accepts them (right filenames, right lengths).
/// </summary>
public sealed class PetProfileCatalogTests
{
    [Test]
    public void Profiles_DeclareTheirRomVerifiedKeyboardLayout()
    {
        PetProfileCatalog.Pet2001_8.KeyboardLayout.Should().Be(PetKeyboardLayout.Pet2001Graphics);
        PetProfileCatalog.Pet2001_32.KeyboardLayout.Should().Be(PetKeyboardLayout.Pet2001Graphics);
        PetProfileCatalog.Cbm4032.KeyboardLayout.Should().Be(PetKeyboardLayout.Cbm4032);
        PetProfileCatalog.Cbm8032.KeyboardLayout.Should().Be(PetKeyboardLayout.Cbm8032);
    }

    [Test]
    public void Profiles_DeclareTheVideoHardwareUsedByTheirRevision()
    {
        PetProfileCatalog.Pet2001_8.VideoHardware.Should().Be(PetVideoHardware.Discrete);
        PetProfileCatalog.Pet2001_32.VideoHardware.Should().Be(PetVideoHardware.Discrete);
        PetProfileCatalog.Cbm4032.VideoHardware.Should().Be(PetVideoHardware.Crtc);
        PetProfileCatalog.Cbm8032.VideoHardware.Should().Be(PetVideoHardware.Crtc);
    }

    [Test]
    public void Cbm3000Profiles_ReuseOnlyTheVerifiedBasic2RomSet_AndVaryRamCapacity()
    {
        PetProfileCatalog.Cbm3008.RomDirectory.Should().Be(PetProfileCatalog.Pet2001_32.RomDirectory);
        PetProfileCatalog.Cbm3016.RomDirectory.Should().Be(PetProfileCatalog.Pet2001_32.RomDirectory);
        PetProfileCatalog.Cbm3032.RomDirectory.Should().Be(PetProfileCatalog.Pet2001_32.RomDirectory);
        PetProfileCatalog.Cbm3008.RamSize.Should().Be(0x2000);
        PetProfileCatalog.Cbm3016.RamSize.Should().Be(0x4000);
        PetProfileCatalog.Cbm3032.RamSize.Should().Be(0x8000);
    }

    [TestCaseSource(nameof(Profiles))]
    public void RomManifest_LoadsFromProfileSubfolder(PetProfile profile)
    {
        var directory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);

        var images = PetRomLoader.Load(directory, profile.RomManifest);

        images.Should().HaveCount(profile.RomManifest.Count);
        images.Should().OnlyContain(i => i.Data.Length == i.Requirement.Length);
    }

    [TestCaseSource(nameof(Profiles))]
    public void CharacterRom_ExistsInProfileSubfolder(PetProfile profile)
    {
        var directory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);

        File.Exists(Path.Combine(directory, profile.CharacterRomPath)).Should().BeTrue(
            $"{profile.Id}'s character ROM should sit alongside its main ROM set");
    }

    private static IEnumerable<PetProfile> Profiles() => PetProfileCatalog.All;
}
