using CommunityToolkit.Mvvm.ComponentModel;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Selection state for the ready-to-run VIC-20 program profiles.</summary>
public sealed class Vic20ProgramProfileSelectorViewModel : ObservableObject
{
    private Vic20ProgramProfile _selectedProfile;
    private string? _errorMessage;

    public Vic20ProgramProfileSelectorViewModel()
    {
        Profiles = Vic20ProgramProfileCatalog.All;
        _selectedProfile = Profiles[0];
    }

    public IReadOnlyList<Vic20ProgramProfile> Profiles { get; }

    public Vic20ProgramProfile SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (SetProperty(ref _selectedProfile, value))
                ProfileSelected?.Invoke(this, value);
        }
    }

    public void SetSelectedProfile(Vic20ProgramProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        SetProperty(ref _selectedProfile, profile);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public event EventHandler<Vic20ProgramProfile>? ProfileSelected;
}
