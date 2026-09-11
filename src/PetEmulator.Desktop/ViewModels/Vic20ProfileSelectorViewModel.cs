using CommunityToolkit.Mvvm.ComponentModel;
using PetEmulator.Vic20;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Selectable VIC-20 hardware profiles shown next to the cartridge widget.</summary>
public sealed class Vic20ProfileSelectorViewModel : ObservableObject
{
    private Vic20ExpansionProfile _selectedProfile;

    public Vic20ProfileSelectorViewModel(Vic20ExpansionPreset selectedPreset)
    {
        Profiles = Vic20ExpansionPresetCatalog.All;
        _selectedProfile = Profiles.First(profile => profile.Preset == selectedPreset);
    }

    public IReadOnlyList<Vic20ExpansionProfile> Profiles { get; }

    public Vic20ExpansionProfile SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!SetProperty(ref _selectedProfile, value))
                return;

            ProfileSelected?.Invoke(this, value);
        }
    }

    public event EventHandler<Vic20ExpansionProfile>? ProfileSelected;
}
