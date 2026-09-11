using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public abstract partial class ChipDebugSessionBase : ObservableObject, IChipDebugSessionViewModel
{
    private readonly Action _tick;
    private readonly Action _reset;
    private readonly Func<string, byte> _readRegister;
    private readonly Action<string, byte> _writeRegister;
    private readonly IReadOnlyList<(string Name, Func<byte> Read)> _registerDefinitions;
    private readonly IReadOnlyList<(string Name, Func<bool> Read, Action<bool>? Write)> _pinDefinitions;
    private readonly DispatcherTimer _timer;
    private readonly List<WaveformSample> _samples = [];
    private readonly WaveformTimeline _timeline;
    private IReadOnlyList<ChipStimulus> _stimulus = [];
    private int _nextStimulus;
    private long _step;

    [ObservableProperty]
    private bool _isRunning;

    protected ChipDebugSessionBase(string windowTitle, string description, ChipDebugDefinition definition)
    {
        WindowTitle = windowTitle;
        Description = description;
        _tick = definition.Tick;
        _reset = definition.Reset;
        _readRegister = definition.ReadRegister;
        _writeRegister = definition.WriteRegister;
        _registerDefinitions = definition.Registers;
        _pinDefinitions = definition.Pins;
        Registers = [];
        Pins = [];
        _timeline = new WaveformTimeline(_samples);
        Timeline = _timeline;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) => RunCycles(TimerCyclesPerTick);
        Refresh();
    }

    public ObservableCollection<RegisterRow> Registers { get; }
    public ObservableCollection<PinRow> Pins { get; }
    public IWaveformSource Timeline { get; }
    public string WindowTitle { get; }
    public string Description { get; }
    public virtual object? Visual => null;
    protected virtual int TimerCyclesPerTick => 100;
    public string StatusText => IsRunning ? $"Running, step {_step}" : $"Paused, step {_step}";

    [RelayCommand]
    private void Run()
    {
        IsRunning = true;
        _timer.Start();
        OnPropertyChanged(nameof(StatusText));
    }

    [RelayCommand]
    private void Pause()
    {
        IsRunning = false;
        _timer.Stop();
        OnPropertyChanged(nameof(StatusText));
    }

    [RelayCommand]
    private void Step() => RunCycles(1);

    [RelayCommand]
    private void Reset()
    {
        _reset();
        _step = 0;
        _nextStimulus = 0;
        _samples.Clear();
        Refresh();
    }

    public void PokeRegister(string name, byte value)
    {
        _writeRegister(name.ToUpperInvariant(), value);
        Refresh();
    }

    public void SetPin(string name, bool level)
    {
        var pin = _pinDefinitions.FirstOrDefault(pin => pin.Name == name.ToUpperInvariant());
        if (pin.Write is null)
            throw new ArgumentException($"Unknown writable pin: {name}", nameof(name));
        pin.Write(level);
        Refresh();
    }

    public virtual void Dispose() => _timer.Stop();

    public void LoadStimulus(IReadOnlyList<ChipStimulus> stimulus)
    {
        _stimulus = stimulus.OrderBy(item => item.Cycle).ToArray();
        _nextStimulus = 0;
    }

    private void RunCycles(int cycles)
    {
        for (var i = 0; i < cycles; i++)
        {
            _tick();
            _step++;
            while (_nextStimulus < _stimulus.Count && _stimulus[_nextStimulus].Cycle <= _step)
                _stimulus[_nextStimulus++].Apply(this);
            foreach (var pin in _pinDefinitions)
                AddSample(pin.Name, pin.Read());
        }
        Refresh();
    }

    private void AddSample(string signal, bool level)
    {
        _samples.Add(new WaveformSample(_step, signal, level));
        if (_samples.Count > 4_096)
            _samples.RemoveRange(0, _samples.Count - 4_096);
        _timeline.NotifyChanged();
    }

    private void Refresh()
    {
        Registers.Clear();
        foreach (var register in _registerDefinitions)
            Registers.Add(new RegisterRow(register.Name, $"0x{register.Read():X2}"));
        Pins.Clear();
        foreach (var pin in _pinDefinitions)
            Pins.Add(new PinRow(pin.Name, pin.Read(), pin.Write is not null));
        OnPropertyChanged(nameof(StatusText));
        AfterRefresh();
    }

    protected virtual void AfterRefresh() { }

    private sealed class WaveformTimeline(IReadOnlyList<WaveformSample> samples) : IWaveformSource
    {
        public IReadOnlyList<WaveformSample> Samples => samples;
        public event EventHandler? Changed;
        public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }
}

public sealed record ChipDebugDefinition(
    Action Tick,
    Action Reset,
    Func<string, byte> ReadRegister,
    Action<string, byte> WriteRegister,
    IReadOnlyList<(string Name, Func<byte> Read)> Registers,
    IReadOnlyList<(string Name, Func<bool> Read, Action<bool>? Write)> Pins);
