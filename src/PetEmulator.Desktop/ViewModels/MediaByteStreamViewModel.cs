using System.Collections.ObjectModel;
using System.Text;
using PetEmulator.Pet.Ieee488;
using PetEmulator.Vic20.Serial;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Desktop.ViewModels;

public sealed class MediaByteStreamViewModel
{
    private readonly List<WaveformSample> _samples = [];
    private long _step;

    public ObservableCollection<string> Events { get; } = [];
    public IWaveformSource Timeline { get; }

    public MediaByteStreamViewModel() => Timeline = new TimelineSource(_samples);

    public void AddIeeeActivity(IeeeBusActivity activity) => Add(activity.Kind, activity.Detail);
    public void AddVicActivity(Vic20SerialActivity activity) => Add(activity.Kind, activity.Detail);
    public void AddTapeActivity(DatasetteActivity activity) => Add(activity.Kind, activity.Detail);

    public void AddBytes(ReadOnlySpan<byte> bytes, string source)
    {
        foreach (var value in bytes)
            Add("byte", $"0x{value:X2} read ({source})");
    }

    private void Add(string kind, string detail)
    {
        _step++;
        Events.Insert(0, $"{_step,6}: {detail}");
        if (Events.Count > 512)
            Events.RemoveAt(Events.Count - 1);

        if (kind == "byte")
        {
            var value = ParseByte(detail);
            _samples.Add(new WaveformSample(_step, "DIO", value is not null && (value & 1) != 0));
        }
        else if (kind == "line-change")
        {
            foreach (var signal in detail.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = signal.IndexOf('=');
                if (separator <= 0 || !bool.TryParse(signal[(separator + 1)..].TrimEnd(')'), out var level))
                    continue;
                _samples.Add(new WaveformSample(_step, signal[..separator], level));
            }
        }

        while (_samples.Count > 4_096)
            _samples.RemoveAt(0);
        ((TimelineSource)Timeline).NotifyChanged();
    }

    private static byte? ParseByte(string detail)
    {
        var marker = detail.IndexOf("0x", StringComparison.OrdinalIgnoreCase);
        return marker >= 0 && marker + 4 <= detail.Length &&
            byte.TryParse(detail.AsSpan(marker + 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var value)
            ? value : null;
    }

    private sealed class TimelineSource(IReadOnlyList<WaveformSample> samples) : IWaveformSource
    {
        public IReadOnlyList<WaveformSample> Samples => samples;
        public event EventHandler? Changed;
        public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
