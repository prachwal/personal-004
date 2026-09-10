using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class ChipTesterViewModel : ObservableObject, IShellModule
{
    [ObservableProperty]
    private object? _selectedItem;

    public ObservableCollection<ChipTreeNode> Chips { get; } =
    [
        new("MOS 2114", []),
        new("MOS 6522 VIA", []),
        new("MOS 6560 VIC", []),
        new("MT 6520 PIA", []),
        new("MT 6545 CRTC", []),
    ];

    public IShellModule? SelectedScenario { get; private set; }

    public string WindowTitle => "Chip Tester";

    public string StatusText => SelectedScenario?.StatusText ?? "Select a scenario";

    partial void OnSelectedItemChanged(object? value)
    {
        if (value is ChipScenarioEntry scenario)
        {
            SelectedScenario?.Dispose();
            SelectedScenario = scenario.Create();
            OnPropertyChanged(nameof(SelectedScenario));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public void Dispose() => SelectedScenario?.Dispose();
}

public sealed record ChipTreeNode(string Name, IReadOnlyList<ChipScenarioEntry> Scenarios);

public sealed record ChipScenarioEntry(string Name, Func<IShellModule> Create);
