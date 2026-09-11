using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Tests;

public sealed class Vic20ProgramProfileTests
{
    [Test]
    public void CatalogContainsTheThreeDownloadedProgramsWithNonOverlappingRam()
    {
        Vic20ProgramProfileCatalog.All.Should().HaveCount(11);
        Vic20ProgramProfileCatalog.All.Where(profile => profile.Id is "alien-blitz-ntsc" or "alien-blitz-pal")
            .Should().OnlyContain(profile =>
            profile.Cartridges.Count == 2
            && profile.Cartridges[0] == new Vic20ProgramCartridge("vic20-ram-24k.bin", "vic20-ram-24k"));
        Vic20ProgramProfileCatalog.All.Single(profile => profile.Id == "alphoids").Cartridges.Should().ContainSingle();
        Vic20ProgramProfileCatalog.All.Single(profile => profile.Id == "sound-test").Cartridges.Should().ContainSingle();
        Vic20ProgramProfileCatalog.All.Where(profile => profile.Id.StartsWith("ram-"))
            .SelectMany(profile => profile.Cartridges)
            .Should().OnlyContain(cartridge => cartridge.PluginId != null);
    }

    [Test]
    public void CatalogContainsTheRtcCartridgePluginProfile()
    {
        var profile = Vic20ProgramProfileCatalog.All.Single(profile => profile.Id == "rtc");

        profile.Name.Should().Be("MC146818 RTC");
        profile.Cartridges.Should().ContainSingle()
            .Which.Should().Be(new Vic20ProgramCartridge("vic20-mc146818-rtc.bin", "vic20-mc146818-rtc"));
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

        selector.SelectedProfile = Vic20ProgramProfileCatalog.All.Single(profile => profile.Id == "alien-blitz-ntsc");

        selected.Should().NotBeNull();
        selected!.Id.Should().Be("alien-blitz-ntsc");
    }
}
