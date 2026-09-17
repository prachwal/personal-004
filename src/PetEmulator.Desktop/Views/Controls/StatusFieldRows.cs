using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Shared renderer for <see cref="StatusField"/> rows (see
/// <see cref="ViewModels.IShellModule.StatusFields"/>): one row per value, name left and value
/// in phosphor green. Vertical (side panel, fixed name column so values align) or Horizontal
/// (tool headers, name/value pairs side by side). Replaces three copy-pasted
/// ItemsControl+DataTemplate blocks.</summary>
public sealed class StatusFieldRows : ItemsControl
{
    public static readonly StyledProperty<Orientation> DirectionProperty =
        AvaloniaProperty.Register<StatusFieldRows, Orientation>(nameof(Direction), Orientation.Vertical);

    public static readonly StyledProperty<double> FieldFontSizeProperty =
        AvaloniaProperty.Register<StatusFieldRows, double>(nameof(FieldFontSize), 12.0);

    public Orientation Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public double FieldFontSize
    {
        get => GetValue(FieldFontSizeProperty);
        set => SetValue(FieldFontSizeProperty, value);
    }

    public StatusFieldRows()
    {
        ItemTemplate = new FuncDataTemplate<StatusField>((field, _) => field is null ? null : BuildRow(field));
        UpdatePanel();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DirectionProperty)
            UpdatePanel();
    }

    private void UpdatePanel() => ItemsPanel = new FuncTemplate<Panel?>(() => Direction == Orientation.Horizontal
        ? new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 }
        : new StackPanel { Orientation = Orientation.Vertical });

    private Control BuildRow(StatusField field)
    {
        var valueBrush = this.FindResource("StatusTextBrush") as IBrush ?? Brushes.LimeGreen;
        var name = new TextBlock
        {
            Text = field.Name,
            Opacity = 0.6,
            FontFamily = new FontFamily("monospace"),
            FontSize = FieldFontSize,
        };
        var value = new TextBlock
        {
            Text = field.Value,
            FontFamily = new FontFamily("monospace"),
            Foreground = valueBrush,
            FontSize = FieldFontSize,
        };

        if (Direction == Orientation.Horizontal)
        {
            var pair = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            pair.Children.Add(name);
            pair.Children.Add(value);
            return pair;
        }

        name.Margin = new Thickness(0, 0, 8, 0);
        value.TextWrapping = TextWrapping.Wrap;
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("110,*"),
            Margin = new Thickness(0, 1),
        };
        row.Children.Add(name);
        Grid.SetColumn(value, 1);
        row.Children.Add(value);
        return row;
    }
}
