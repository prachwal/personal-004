using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Desktop.ViewModels.ChipTests;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class ChipTesterViewModel : ObservableObject, IShellModule
{
    private readonly string _romsRoot;
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("ChipTester");

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

    public IReadOnlyList<StatusField> StatusFields =>
        SelectedScenario?.StatusFields ?? [new("Status", "Select a scenario")];

    partial void OnSelectedItemChanged(object? value)
    {
        if (value is ChipScenarioEntry scenario)
        {
            Log.LogInformation("Opening scenario '{Scenario}'.", scenario.Name);
            try
            {
                UnsubscribeScenario();
                SelectedScenario?.Dispose();
                SelectedScenario = scenario.Create();
                if (SelectedScenario is System.ComponentModel.INotifyPropertyChanged notify)
                    notify.PropertyChanged += OnScenarioPropertyChanged;
                SelectedScenario.LoadStimulus(scenario.Stimulus ?? []);
                OnPropertyChanged(nameof(SelectedScenario));
                OnPropertyChanged(nameof(StatusFields));
                Log.LogInformation("Scenario '{Scenario}' ready.", scenario.Name);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Failed to open scenario '{Scenario}'.", scenario.Name);
                throw;
            }
        }
    }

    /// <summary>Forwards the scenario's own live status notifications (it notifies
    /// <c>StatusText</c> for its tab) so the side panel's <see cref="StatusFields"/> - read live
    /// off the scenario - refresh mid-step instead of only on scenario switch.</summary>
    private void OnScenarioPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "StatusText")
            OnPropertyChanged(nameof(StatusFields));
    }

    public void Dispose()
    {
        UnsubscribeScenario();
        SelectedScenario?.Dispose();
    }

    private void UnsubscribeScenario()
    {
        if (SelectedScenario is System.ComponentModel.INotifyPropertyChanged notify)
            notify.PropertyChanged -= OnScenarioPropertyChanged;
    }
}

public sealed record ChipTreeNode(string Name, IReadOnlyList<ChipScenarioEntry> Scenarios);

public sealed record ChipScenarioEntry(
    string Name,
    Func<IChipDebugSessionViewModel> Create,
    IReadOnlyList<ChipStimulus>? Stimulus = null);
