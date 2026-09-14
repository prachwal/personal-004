using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PetEmulator.Desktop.ViewModels;

public sealed class HexEditorViewModel : ObservableObject
{
    private byte[] _bytes = [];
    private int _bytesPerLine = 16;
    private int _maxLines;
    private int _selectedOffset = -1;
    private int _anchorOffset = -1;
    private bool _isReadOnly;
    private readonly Dictionary<int, HexEditorCellViewModel> _cellsByOffset = [];

    public byte[] Bytes
    {
        get => _bytes;
        private set => SetProperty(ref _bytes, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    public int BytesPerLine
    {
        get => _bytesPerLine;
        set
        {
            if (SetProperty(ref _bytesPerLine, Math.Max(1, value)))
                RebuildRows();
        }
    }

    public int MaxLines
    {
        get => _maxLines;
        set
        {
            if (SetProperty(ref _maxLines, Math.Max(0, value)))
                RebuildRows();
        }
    }

    public int SelectedOffset
    {
        get => _selectedOffset;
        private set
        {
            if (SetProperty(ref _selectedOffset, value))
                RefreshSelection();
        }
    }

    public ObservableCollection<HexEditorRowViewModel> Rows { get; } = [];

    public void SetBytes(byte[] bytes)
    {
        Bytes = bytes.ToArray();
        if (Bytes.Length == 0)
            _selectedOffset = -1;
        else if (SelectedOffset >= Bytes.Length)
            _selectedOffset = Bytes.Length - 1;
        if (_anchorOffset >= Bytes.Length)
            _anchorOffset = Bytes.Length - 1;
        RebuildRows();
    }

    public void MoveTo(int offset, bool extendSelection)
    {
        if (Bytes.Length == 0)
            return;
        offset = Math.Clamp(offset, 0, Bytes.Length - 1);
        if (!extendSelection)
            _anchorOffset = offset;
        else if (_anchorOffset < 0)
            _anchorOffset = SelectedOffset < 0 ? offset : SelectedOffset;
        SelectedOffset = offset;
    }

    public bool EditCell(int offset, string text)
    {
        if (IsReadOnly || offset < 0 || offset >= Bytes.Length || text.Length > 2 || text.Any(character => !IsHex(character)))
            return false;
        if (!_cellsByOffset.TryGetValue(offset, out var cell))
            return false;
        cell.HexText = text.ToUpperInvariant();
        if (text.Length != 2 || !byte.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out var value))
            return true;
        var bytes = Bytes.ToArray();
        bytes[offset] = value;
        Bytes = bytes;
        cell.SetValue(value);
        return true;
    }

    public void DeleteSelectionOrByte()
    {
        if (IsReadOnly || Bytes.Length == 0)
            return;
        var (start, end) = SelectionRange();
        Bytes = Bytes.Where((_, index) => index < start || index > end).ToArray();
        SelectedOffset = Math.Min(start, Math.Max(0, Bytes.Length - 1));
        _anchorOffset = SelectedOffset;
        RebuildRows();
    }

    public void InsertByte()
    {
        if (IsReadOnly)
            return;
        var offset = SelectedOffset < 0 ? Bytes.Length : SelectedOffset;
        var bytes = new byte[Bytes.Length + 1];
        Array.Copy(Bytes, 0, bytes, 0, offset);
        Array.Copy(Bytes, offset, bytes, offset + 1, Bytes.Length - offset);
        Bytes = bytes;
        SelectedOffset = offset;
        _anchorOffset = offset;
        RebuildRows();
    }

    private void RebuildRows()
    {
        Rows.Clear();
        _cellsByOffset.Clear();
        var lineCount = (Bytes.Length + BytesPerLine - 1) / BytesPerLine;
        if (MaxLines > 0)
            lineCount = Math.Min(lineCount, MaxLines);
        for (var line = 0; line < lineCount; line++)
        {
            var offset = line * BytesPerLine;
            var row = new HexEditorRowViewModel(offset.ToString("X4"));
            var count = Math.Min(BytesPerLine, Bytes.Length - offset);
            for (var index = 0; index < count; index++)
            {
                var cell = new HexEditorCellViewModel(offset + index, Bytes[offset + index]);
                row.Cells.Add(cell);
                _cellsByOffset[cell.Offset] = cell;
            }
            Rows.Add(row);
        }
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        var (start, end) = SelectionRange();
        foreach (var cell in _cellsByOffset.Values)
            cell.IsSelected = SelectedOffset >= 0 && cell.Offset >= start && cell.Offset <= end;
    }

    private (int Start, int End) SelectionRange()
    {
        var current = Math.Max(0, SelectedOffset);
        var anchor = _anchorOffset < 0 ? current : _anchorOffset;
        return (Math.Min(anchor, current), Math.Max(anchor, current));
    }

    private static bool IsHex(char value) => value is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';
}

public sealed class HexEditorRowViewModel(string address)
{
    public string Address { get; } = address;
    public ObservableCollection<HexEditorCellViewModel> Cells { get; } = [];
}

public sealed class HexEditorCellViewModel : ObservableObject
{
    private string _hexText;
    private string _asciiText;
    private bool _isSelected;

    public HexEditorCellViewModel(int offset, byte value)
    {
        Offset = offset;
        _hexText = value.ToString("X2");
        _asciiText = ToAscii(value).ToString();
    }

    public int Offset { get; }

    public string HexText
    {
        get => _hexText;
        set => SetProperty(ref _hexText, value);
    }

    public string AsciiText
    {
        get => _asciiText;
        private set => SetProperty(ref _asciiText, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                OnPropertyChanged(nameof(CellBackground));
        }
    }

    public IBrush CellBackground => IsSelected ? Brushes.DodgerBlue : Brushes.Transparent;

    public void SetValue(byte value)
    {
        HexText = value.ToString("X2");
        AsciiText = ToAscii(value).ToString();
    }

    private static char ToAscii(byte value) => value is >= 0x20 and <= 0x7E ? (char)value : '.';
}
