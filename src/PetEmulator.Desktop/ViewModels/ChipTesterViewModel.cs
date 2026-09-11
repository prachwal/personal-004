using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PetEmulator.Desktop.ViewModels.ChipTests;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class ChipTesterViewModel : ObservableObject, IShellModule
{
    private readonly string _romsRoot;

    public ChipTesterViewModel(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        _romsRoot = romsRoot;
        Chips = CreateChips(romsRoot);
    }

    [ObservableProperty]
    private object? _selectedItem;

    public ObservableCollection<ChipTreeNode> Chips { get; }

    private static ObservableCollection<ChipTreeNode> CreateChips(string romsRoot) =>
    [
        new("MOS 2114", [new ChipScenarioEntry("Color RAM", () => new Mos2114DebugSession())]),
        new("MOS 6522 VIA", [new ChipScenarioEntry("Register and pin state", () => new Mos6522DebugSession())]),
        new("MOS 6560 VIC", [new ChipScenarioEntry("Video and audio state", () => new Mos6560DebugSession(romsRoot))]),
        new("MT 6520 PIA", [new ChipScenarioEntry("Register and pin state", () => new Mt6520DebugSession())]),
        new("MT 6545 CRTC", [new ChipScenarioEntry("Timing state", () => new Mt6545DebugSession(romsRoot))]),
    ];

    public IChipDebugSessionViewModel? SelectedScenario { get; private set; }

    public string WindowTitle => "Chip Tester";

    public string StatusText => SelectedScenario?.StatusText ?? "Select a scenario";

    partial void OnSelectedItemChanged(object? value)
    {
        if (value is ChipScenarioEntry scenario)
        {
            SelectedScenario?.Dispose();
            SelectedScenario = scenario.Create();
            SelectedScenario.LoadStimulus(scenario.Stimulus ?? []);
            OnPropertyChanged(nameof(SelectedScenario));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public void Dispose() => SelectedScenario?.Dispose();
}

public sealed record ChipTreeNode(string Name, IReadOnlyList<ChipScenarioEntry> Scenarios);

public sealed record ChipScenarioEntry(
    string Name,
    Func<IChipDebugSessionViewModel> Create,
    IReadOnlyList<ChipStimulus>? Stimulus = null);
