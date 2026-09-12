using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;

namespace PetEmulator.Desktop.Views.Controls;

public partial class HexTextPreviewControl : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<byte>> BytesProperty =
        AvaloniaProperty.Register<HexTextPreviewControl, IReadOnlyList<byte>>(
            nameof(Bytes), Array.Empty<byte>());

    public static readonly StyledProperty<ushort> StartAddressProperty =
        AvaloniaProperty.Register<HexTextPreviewControl, ushort>(nameof(StartAddress));

    public static readonly StyledProperty<int> BytesPerLineProperty =
        AvaloniaProperty.Register<HexTextPreviewControl, int>(nameof(BytesPerLine), 40);

    public static readonly StyledProperty<int> MaxLinesProperty =
        AvaloniaProperty.Register<HexTextPreviewControl, int>(nameof(MaxLines), 16);

    public IReadOnlyList<byte> Bytes
    {
        get => GetValue(BytesProperty);
        set => SetValue(BytesProperty, value);
    }

    public ushort StartAddress
    {
        get => GetValue(StartAddressProperty);
        set => SetValue(StartAddressProperty, value);
    }

    public int BytesPerLine
    {
        get => GetValue(BytesPerLineProperty);
        set => SetValue(BytesPerLineProperty, value);
    }

    public int MaxLines
    {
        get => GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    public ObservableCollection<HexTextLine> Lines { get; } = [];

    public HexTextPreviewControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BytesProperty ||
            change.Property == StartAddressProperty ||
            change.Property == BytesPerLineProperty ||
            change.Property == MaxLinesProperty)
            RebuildLines();
    }

    private void RebuildLines()
    {
        Lines.Clear();
        var bytesPerLine = Math.Max(1, BytesPerLine);
        var maxLines = Math.Max(0, MaxLines);
        var lineCount = Math.Min(maxLines, (Bytes.Count + bytesPerLine - 1) / bytesPerLine);

        for (var line = 0; line < lineCount; line++)
        {
            var offset = line * bytesPerLine;
            var count = Math.Min(bytesPerLine, Bytes.Count - offset);
            var hex = string.Join(' ', Bytes.Skip(offset).Take(count).Select(value => value.ToString("X2")));
            hex = hex.PadRight(bytesPerLine * 3 - 1);
            var text = new string(Bytes.Skip(offset).Take(count).Select(ToPetText).ToArray());
            Lines.Add(new HexTextLine((StartAddress + offset).ToString("X4"), hex, text));
        }
    }

    private static char ToPetText(byte value) => value switch
    {
        >= 0x20 and <= 0x5F => (char)value,
        >= 0xA0 and <= 0xBF => (char)(value - 0x80),
        _ => '.'
    };
}

public sealed record HexTextLine(string Address, string Hex, string Text)
{
    public string Display => $"${Address}: {Hex} | {Text}";
}
