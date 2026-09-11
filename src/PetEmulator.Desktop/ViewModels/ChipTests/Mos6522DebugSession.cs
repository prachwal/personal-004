using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Chips;
using PetEmulator.Desktop.Resources;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed partial class Mos6522DebugSession : ObservableObject, IChipDebugSessionViewModel
{
    public object? Visual => null;
    public string Description => ChipDescriptions.Get("Mos6522");
    private readonly MOS6522 _chip = new("Chip Tester VIA");
    private readonly DispatcherTimer _timer;
    private readonly List<WaveformSample> _samples = [];
    private readonly WaveformTimeline _timeline;
    private long _step;

    [ObservableProperty]
    private bool _isRunning;

    public Mos6522DebugSession()
    {
        Registers = [];
        Pins = [];
        _timeline = new WaveformTimeline(_samples);
        Timeline = _timeline;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) => RunCycles(100);
        Refresh();
    }

    public ObservableCollection<RegisterRow> Registers { get; }
    public ObservableCollection<PinRow> Pins { get; }
    public IWaveformSource Timeline { get; }
    public string WindowTitle => "MOS 6522 VIA";
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
        _chip.Reset();
        _step = 0;
        _samples.Clear();
        Refresh();
    }

    public void PokeRegister(string name, byte value)
    {
        var address = name.ToUpperInvariant() switch
        {
            "ORB" => MOS6522.Orb,
            "ORA" => MOS6522.Ora,
            "DDRB" => MOS6522.Ddrb,
            "DDRA" => MOS6522.Ddra,
            "ACR" => MOS6522.AuxiliaryControl,
            "PCR" => MOS6522.PeripheralControl,
            "IER" => MOS6522.InterruptEnable,
            _ => throw new ArgumentException($"Unknown writable register: {name}", nameof(name))
        };
        _chip.Write(address, value);
        Refresh();
    }

    public void SetPin(string name, bool level)
    {
        switch (name.ToUpperInvariant())
        {
            case "CA1": _chip.CA1 = level; break;
            case "CA2": _chip.CA2 = level; break;
            case "CB1": _chip.CB1 = level; break;
            case "CB2": _chip.CB2 = level; break;
            default: throw new ArgumentException($"Unknown writable pin: {name}", nameof(name));
        }
        Refresh();
    }

    public void Dispose() => _timer.Stop();

    public void LoadStimulus(IReadOnlyList<ChipStimulus> stimulus) { }

    private void RunCycles(int cycles)
    {
        for (var i = 0; i < cycles; i++)
        {
            _chip.Update();
            _step++;
            AddSample("CA1", _chip.CA1);
            AddSample("CA2", _chip.CA2);
            AddSample("CB1", _chip.CB1);
            AddSample("CB2", _chip.CB2);
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
        AddRegister("ORA", _chip.ORA); AddRegister("ORB", _chip.ORB);
        AddRegister("DDRA", _chip.DDRA); AddRegister("DDRB", _chip.DDRB);
        AddRegister("ACR", _chip.ACR); AddRegister("PCR", _chip.PCR);
        AddRegister("IFR", _chip.IFR); AddRegister("IER", _chip.IER);
        Pins.Clear();
        AddPin("CA1", _chip.CA1, true); AddPin("CA2", _chip.CA2, true);
        AddPin("CB1", _chip.CB1, true); AddPin("CB2", _chip.CB2, true);
        AddPin("IRQ", _chip.IRQ, false);
        OnPropertyChanged(nameof(StatusText));
    }

    private void AddRegister(string name, byte value) => Registers.Add(new RegisterRow(name, $"0x{value:X2}"));
    private void AddPin(string name, bool level, bool writable) => Pins.Add(new PinRow(name, level, writable));

    private sealed class WaveformTimeline(IReadOnlyList<WaveformSample> samples) : IWaveformSource
    {
        public IReadOnlyList<WaveformSample> Samples => samples;

        public event EventHandler? Changed;

        public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
