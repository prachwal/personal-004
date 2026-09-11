using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Vic20;

namespace PetEmulator.Desktop.Tests;

public sealed class Vic20ProfileSelectorViewModelTests
{
    [Test]
    public void StartsWithTheRequestedProfile()
    {
        var selector = new Vic20ProfileSelectorViewModel(Vic20ExpansionPreset.SixteenK);

        selector.SelectedProfile.Preset.Should().Be(Vic20ExpansionPreset.SixteenK);
        selector.Profiles.Should().BeEquivalentTo(Vic20ExpansionPresetCatalog.All);
    }

    [Test]
    public void ChangingProfileRaisesSelectionRequest()
    {
        var selector = new Vic20ProfileSelectorViewModel(Vic20ExpansionPreset.Unexpanded);
        Vic20ExpansionProfile? selected = null;
        selector.ProfileSelected += (_, profile) => selected = profile;

        selector.SelectedProfile = Vic20ExpansionPresetCatalog.Get(Vic20ExpansionPreset.All);

        selected.Should().NotBeNull();
        selected!.Preset.Should().Be(Vic20ExpansionPreset.All);
    }
}
