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
