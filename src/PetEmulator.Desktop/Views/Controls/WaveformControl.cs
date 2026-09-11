using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views.Controls;

public sealed class WaveformControl : Control
{
    public static readonly StyledProperty<IWaveformSource?> SourceProperty =
        AvaloniaProperty.Register<WaveformControl, IWaveformSource?>(nameof(Source));

    private IWaveformSource? _source;

    public IWaveformSource? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty)
        {
            if (_source is not null)
                _source.Changed -= OnSourceChanged;
            _source = change.NewValue as IWaveformSource;
            if (_source is not null)
                _source.Changed += OnSourceChanged;
            InvalidateVisual();
        }
    }

    private void OnSourceChanged(object? sender, EventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        var samples = Source?.Samples;
        if (samples is null || samples.Count == 0 || Bounds.Width < 2 || Bounds.Height < 2)
            return;

        var signals = samples.Select(sample => sample.Signal).Distinct().ToArray();
        var rowHeight = Bounds.Height / signals.Length;
        var minStep = samples[0].Step;
        var maxStep = Math.Max(minStep + 1, samples[^1].Step);
        var pen = new Pen(Brushes.LimeGreen, 1);

        foreach (var (signal, row) in signals.Select((signal, row) => (signal, row)))
        {
            var points = samples.Where(sample => sample.Signal == signal).ToArray();
            var previous = points[0];
            foreach (var current in points.Skip(1))
            {
                var previousX = (previous.Step - minStep) * Bounds.Width / (maxStep - minStep);
                var currentX = (current.Step - minStep) * Bounds.Width / (maxStep - minStep);
                var high = row * rowHeight + rowHeight * 0.25;
                var low = row * rowHeight + rowHeight * 0.75;
                var previousY = previous.Level ? high : low;
                var currentY = current.Level ? high : low;
                context.DrawLine(pen, new Point(previousX, previousY), new Point(currentX, previousY));
                context.DrawLine(pen, new Point(currentX, previousY), new Point(currentX, currentY));
                previous = current;
            }
        }
    }
}
