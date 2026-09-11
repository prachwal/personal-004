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

    [Test]
    public void Cbm4000Profiles_DeclareTheExactCrtcEditorVariant()
    {
        PetProfileCatalog.Cbm4008Crtc40N60.VideoHardware.Should().Be(PetVideoHardware.Crtc);
        PetProfileCatalog.Cbm4016Crtc40N60.VideoHardware.Should().Be(PetVideoHardware.Crtc);
        PetProfileCatalog.Cbm4008Crtc40N60.Name.Should().Contain("40n60");
        PetProfileCatalog.Cbm4016Crtc40N60.Name.Should().Contain("40n60");
        PetProfileCatalog.Cbm4008Crtc40N60.RomDirectory.Should().Be(PetProfileCatalog.Cbm4032.RomDirectory);
        PetProfileCatalog.Cbm4016Crtc40N60.RomDirectory.Should().Be(PetProfileCatalog.Cbm4032.RomDirectory);
    }

    [Test]
    public void Cbm8016ConvertedProfile_IsExplicitlyMarkedAsNonstandard()
    {
        var profile = PetProfileCatalog.Cbm8016Converted80N50;

        profile.Id.Should().Contain("converted");
        profile.Name.Should().Contain("converted");
        profile.Columns.Should().Be(80);
        profile.RamSize.Should().Be(0x4000);
        profile.VideoHardware.Should().Be(PetVideoHardware.Crtc);
        profile.KeyboardLayout.Should().Be(PetKeyboardLayout.Cbm4032);
        profile.RomDirectory.Should().Be(PetProfileCatalog.Cbm8032.RomDirectory);
        profile.RomManifest.Should().ContainSingle(requirement => requirement.Path == "edit-4-80-n-50Hz.4016_to_8016.bin");
    }

    [Test]
    public void UnknownConverted80NProfile_IsExplicitlyMarkedAsUnknown()
    {
        var profile = PetProfileCatalog.Converted80NUnknown;

        profile.Id.Should().Contain("unknown");
        profile.Name.Should().Contain("unknown");
        profile.Columns.Should().Be(80);
        profile.VideoHardware.Should().Be(PetVideoHardware.Crtc);
        profile.KeyboardLayout.Should().Be(PetKeyboardLayout.Cbm4032);
        profile.RomManifest.Should().ContainSingle(requirement => requirement.Path == "edit-4-80-n_unk.bin");
        profile.RomManifest.Single(requirement => requirement.Path == "edit-4-80-n_unk.bin").Length.Should().Be(0x1000);
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
