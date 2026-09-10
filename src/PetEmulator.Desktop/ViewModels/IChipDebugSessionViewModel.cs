using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace PetEmulator.Desktop.ViewModels;

public interface IChipDebugSessionViewModel : IShellModule
{
    object? Visual { get; }

    /// <summary>Plain-text description of the real chip this session models - shown in Chip
    /// Tester's "Opis" tab, sourced from
    /// <see cref="PetEmulator.Desktop.Resources.ChipDescriptions"/>.</summary>
    string Description { get; }

    ObservableCollection<RegisterRow> Registers { get; }
    ObservableCollection<PinRow> Pins { get; }
    IWaveformSource Timeline { get; }
    bool IsRunning { get; }
    IRelayCommand RunCommand { get; }
    IRelayCommand PauseCommand { get; }
    IRelayCommand StepCommand { get; }
    IRelayCommand ResetCommand { get; }
    void PokeRegister(string name, byte value);
    void SetPin(string name, bool level);

    void LoadStimulus(IReadOnlyList<ChipStimulus> stimulus);
}

public sealed record RegisterRow(string Name, string Value);
public sealed record PinRow(string Name, bool Level, bool IsWritable);
public sealed record WaveformSample(long Step, string Signal, bool Level);
public sealed record ChipStimulus(long Cycle, Action<IChipDebugSessionViewModel> Apply);

public interface IWaveformSource
{
    IReadOnlyList<WaveformSample> Samples { get; }

    event EventHandler? Changed;
}
