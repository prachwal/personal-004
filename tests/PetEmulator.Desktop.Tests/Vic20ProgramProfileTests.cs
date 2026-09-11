using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Tests;

public sealed class Vic20ProgramProfileTests
{
    [Test]
    public void CatalogContainsTheThreeDownloadedProgramsWithNonOverlappingRam()
    {
        Vic20ProgramProfileCatalog.All.Should().HaveCount(5);
        Vic20ProgramProfileCatalog.All.Skip(2).Take(2).Should().OnlyContain(profile =>
            profile.RamImageFileName == "vic20-ram-24k.bin"
            && profile.RamRange == "$2000-$7FFF");
        Vic20ProgramProfileCatalog.All[1].RamImageFileName.Should().BeNull();
        Vic20ProgramProfileCatalog.All[1].RamRange.Should().BeNull();
        Vic20ProgramProfileCatalog.All[4].RamImageFileName.Should().BeNull();
        Vic20ProgramProfileCatalog.All[4].RamRange.Should().BeNull();
    }

    [Test]
    public void CatalogStartsWithAnEmptyCartridgeOption()
    {
        Vic20ProgramProfileCatalog.All[0].IsEmpty.Should().BeTrue();
        Vic20ProgramProfileCatalog.All[0].Name.Should().Be("-- BRAK --");
    }

    [Test]
    public void SelectorRaisesEventWhenAProgramIsSelected()
    {
        var selector = new Vic20ProgramProfileSelectorViewModel();
        Vic20ProgramProfile? selected = null;
        selector.ProfileSelected += (_, profile) => selected = profile;

        selector.SelectedProfile = Vic20ProgramProfileCatalog.All[2];

        selected.Should().NotBeNull();
        selected!.Id.Should().Be("alien-blitz-ntsc");
    }
}
