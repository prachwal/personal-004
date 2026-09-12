using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Roms;
using PetEmulator.Pet.Profiles;

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
    public void Profiles_DeclareKeyboardCassetteAndConnectorRevision()
    {
        PetProfileCatalog.Pet2001_8.CassetteConfiguration.Should().Be(PetCassetteConfiguration.InternalAndExternal);
        PetProfileCatalog.Cbm3008.KeyboardRevision.Should().Be(PetKeyboardRevision.Cbm3000Graphics);
        PetProfileCatalog.Cbm4032.ConnectorConfiguration.Should().Be(PetConnectorConfiguration.Cbm4000);
        PetProfileCatalog.Cbm8032.KeyboardRevision.Should().Be(PetKeyboardRevision.Cbm8000Business);
        PetProfileCatalog.Cbm8016Converted80N50.ConnectorConfiguration.Should().Be(PetConnectorConfiguration.Unverified);
    }

    [Test]
    public void BankedPetProfiles_DeclareThe8296CompatibleControlRegisterAndCapacity()
    {
        PetProfileCatalog.Cbm8096French.MemoryExpansion.Should().Be(new PetMemoryExpansion(0xFFF0, 0x8000));
        PetProfileCatalog.Cbm8296.MemoryExpansion.Should().Be(new PetMemoryExpansion(0xFFF0, 0x10000));
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

    [Test]
    public void ExpandedPetProfiles_ArePlaceholdersUntilTheirAdditionalHardwareIsImplemented()
    {
        PetProfileCatalog.Planned.Should().HaveCount(4);
        PetProfileCatalog.Planned.Should().OnlyContain(profile => profile.Status == PetProfileStatus.Placeholder);
        PetProfileCatalog.Cbm8096French.RomManifest.Should().ContainSingle(requirement => requirement.Path == "edit-french.bin");
        PetProfileCatalog.Cbm8296.RomManifest.Should().ContainSingle(requirement => requirement.Path == "edit-50hz-324243-02b.bin");
        PetProfileCatalog.SuperPet6502.ExpansionRomManifest.Should().HaveCount(3);
        PetProfileCatalog.SuperPet6809.ExpansionRomManifest.Should().HaveCount(3);
    }

    [Test]
    public void AvailableProfiles_IncludeOnlyProfilesValidatedForHostSelection()
    {
        PetProfileCatalog.Available.Should().Contain(PetProfileCatalog.SuperPet6502);
        PetProfileCatalog.Available.Should().Contain(PetProfileCatalog.SuperPet6809);
        PetProfileCatalog.Available.Should().Contain(PetProfileCatalog.Cbm8032);
        PetProfileCatalog.Available.Should().NotContain(PetProfileCatalog.Cbm8096French);
        PetProfileCatalog.Available.Should().NotContain(PetProfileCatalog.Cbm8296);
    }

    [Test]
    public void SuperPetProfilesRepresentTheTwoPhysicalProcessorModes()
    {
        PetProfileCatalog.SuperPet6502.InitialProcessor.Should().Be(SuperPetProcessor.Mos6502);
        PetProfileCatalog.SuperPet6809.InitialProcessor.Should().Be(SuperPetProcessor.Motorola6809);
        PetProfileCatalog.SuperPet6502.Id.Should().NotBe(PetProfileCatalog.SuperPet6809.Id);
        PetProfileCatalog.SuperPet.Should().BeSameAs(PetProfileCatalog.SuperPet6809);
    }

    [Test]
    public void EveryProfileDefinition_IsAnnotatedAndRegisteredInTheRuntimeCatalog()
    {
        var runtimeProfiles = PetProfileCatalog.All.Concat(PetProfileCatalog.Planned).ToDictionary(profile => profile.Id);
        var definitions = typeof(PetProfileDefinition).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(PetProfileDefinition).IsAssignableFrom(type))
            .Select(type =>
            {
                var definition = (PetProfileDefinition)Activator.CreateInstance(type)!;
                return (Definition: definition, Attribute: type.GetCustomAttributes(typeof(PetProfileAttribute), false).SingleOrDefault() as PetProfileAttribute);
        })
            .ToArray();

        definitions.Should().HaveCount(19);
        definitions.Select(item => item.Attribute).Should().NotContainNulls();
        definitions.Select(item => item.Attribute!.Id).Should().OnlyHaveUniqueItems();

        foreach (var item in definitions)
        {
            var metadata = item.Attribute!;
            runtimeProfiles.Should().ContainKey(metadata.Id);
            item.Definition.Profile.Id.Should().Be(metadata.Id);
            item.Definition.Profile.Name.Should().Be(metadata.Name);
            item.Definition.Profile.Status.Should().Be(metadata.Status);
        }
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

    [TestCaseSource(nameof(PlannedProfiles))]
    public void PlannedProfile_RomManifest_LoadsFromSharedFirmwareFolder(PetProfile profile)
    {
        var directory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);

        var images = PetRomLoader.Load(directory, profile.RomManifest);

        images.Should().HaveCount(profile.RomManifest.Count);
    }

    [Test]
    public void SuperPet_WaterlooFirmwareManifest_LoadsFromSharedFirmwareFolder()
    {
        var profile = PetProfileCatalog.SuperPet;
        var directory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);

        var images = PetRomLoader.Load(directory, profile.ExpansionRomManifest!);

        images.Should().HaveCount(3);
        images.Select(image => image.Requirement.Address).Should().Equal(0xA000, 0xC000, 0xE000);
    }

    private static IEnumerable<PetProfile> Profiles() => PetProfileCatalog.All;

    private static IEnumerable<PetProfile> PlannedProfiles() => PetProfileCatalog.Planned;
}
