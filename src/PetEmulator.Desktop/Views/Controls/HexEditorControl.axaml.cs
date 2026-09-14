using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views.Controls;

public partial class HexEditorControl : UserControl
{
    public static readonly StyledProperty<byte[]> BytesProperty =
        AvaloniaProperty.Register<HexEditorControl, byte[]>(nameof(Bytes), [], defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int> BytesPerLineProperty =
        AvaloniaProperty.Register<HexEditorControl, int>(nameof(BytesPerLine), 16);

    public static readonly StyledProperty<int> MaxLinesProperty =
        AvaloniaProperty.Register<HexEditorControl, int>(nameof(MaxLines), 0);

    public static readonly StyledProperty<int> SelectedOffsetProperty =
        AvaloniaProperty.Register<HexEditorControl, int>(nameof(SelectedOffset), -1, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HexEditorControl, bool>(nameof(IsReadOnly), false);

    private readonly Dictionary<int, TextBox> _hexEditors = [];
    private bool _syncing;

    public HexEditorViewModel Editor { get; } = new();

    public byte[] Bytes
    {
        get => GetValue(BytesProperty);
        set => SetValue(BytesProperty, value);
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

    public int SelectedOffset
    {
        get => GetValue(SelectedOffsetProperty);
        set => SetValue(SelectedOffsetProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public HexEditorControl()
    {
        InitializeComponent();
        Editor.PropertyChanged += OnEditorPropertyChanged;
        Editor.SetBytes(Bytes);
        Editor.BytesPerLine = BytesPerLine;
        Editor.MaxLines = MaxLines;
        Editor.IsReadOnly = IsReadOnly;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_syncing)
            return;
        if (change.Property == BytesProperty)
            Editor.SetBytes(Bytes ?? []);
        else if (change.Property == BytesPerLineProperty)
            Editor.BytesPerLine = BytesPerLine;
        else if (change.Property == MaxLinesProperty)
            Editor.MaxLines = MaxLines;
        else if (change.Property == SelectedOffsetProperty && SelectedOffset >= 0)
            Editor.MoveTo(SelectedOffset, false);
        else if (change.Property == IsReadOnlyProperty)
            Editor.IsReadOnly = IsReadOnly;
    }

    private void OnEditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HexEditorViewModel.Bytes))
            SetFromEditor(BytesProperty, Editor.Bytes);
        else if (e.PropertyName == nameof(HexEditorViewModel.SelectedOffset))
            SetFromEditor(SelectedOffsetProperty, Editor.SelectedOffset);
    }

    private void SetFromEditor<T>(AvaloniaProperty<T> property, T value)
    {
        _syncing = true;
        try
        {
            SetCurrentValue(property, value);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnHexCellLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { Tag: int offset } editor)
            _hexEditors[offset] = editor;
    }

    private void OnHexCellUnloaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { Tag: int offset })
            _hexEditors.Remove(offset);
    }

    private void OnHexTextInput(object? sender, TextInputEventArgs e)
    {
        if (Editor.IsReadOnly)
        {
            e.Handled = true;
            return;
        }
        e.Handled = string.IsNullOrEmpty(e.Text) || e.Text.Any(character => !IsHex(character));
    }

    private void OnHexTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Editor.IsReadOnly)
            return;
        if (sender is not TextBox { Tag: int offset } editor || FindCell(offset) is not { } cell)
            return;
        var text = editor.Text?.ToUpperInvariant() ?? "";
        if (!Editor.EditCell(offset, text))
        {
            editor.Text = cell.HexText;
            editor.CaretIndex = editor.Text.Length;
            return;
        }
        if (text.Length == 2)
            MoveTo(offset + 1, false);
    }

    private void OnHexKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox { Tag: int offset })
            return;
        if (Editor.IsReadOnly)
        {
            e.Handled = e.Key is Key.Delete or Key.Insert ||
                        e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Tab or Key.Enter;
            return;
        }
        var shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        var target = e.Key switch
        {
            Key.Left => offset - 1,
            Key.Right or Key.Tab or Key.Enter => offset + 1,
            Key.Up => offset - Math.Max(1, Editor.BytesPerLine),
            Key.Down => offset + Math.Max(1, Editor.BytesPerLine),
            _ => -1
        };
        if (e.Key is Key.Delete)
        {
            Editor.DeleteSelectionOrByte();
            e.Handled = true;
        }
        else if (e.Key is Key.Insert)
        {
            Editor.InsertByte();
            e.Handled = true;
        }
        else if (target >= 0)
        {
            MoveTo(target, shift);
            e.Handled = true;
        }
    }

    private void OnHexPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBox { Tag: int offset })
            MoveTo(offset, (e.KeyModifiers & KeyModifiers.Shift) != 0);
    }

    private void OnAsciiPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock { DataContext: HexEditorCellViewModel cell })
        {
            MoveTo(cell.Offset, (e.KeyModifiers & KeyModifiers.Shift) != 0);
            e.Handled = true;
        }
    }

    private void MoveTo(int offset, bool extendSelection)
    {
        Editor.MoveTo(offset, extendSelection);
        if (_hexEditors.TryGetValue(Editor.SelectedOffset, out var editor))
        {
            editor.Focus();
            editor.CaretIndex = editor.Text?.Length ?? 0;
        }
    }

    private HexEditorCellViewModel? FindCell(int offset) =>
        Editor.Rows.SelectMany(row => row.Cells).FirstOrDefault(cell => cell.Offset == offset);

    private static bool IsHex(char value) => value is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';
}
