using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class KeyboardMatrixViewModel : ObservableObject, IShellModule
{
    private readonly PetKeyboardMatrix _pet = new();
    private readonly Vic20KeyboardMatrix _vic = new();

    [ObservableProperty]
    private string _selectedMachine = "PET";

    public static IReadOnlyList<string> Machines { get; } = ["PET", "VIC-20"];

    public KeyboardMatrixViewModel()
    {
        Cells = [];
        Rebuild();
    }

    public string WindowTitle => "Keyboard Matrix";
    public IReadOnlyList<StatusField> StatusFields =>
        [new("Machine", SelectedMachine), new("Row 0", $"0x{ReadColumns(0):X2}")];
    public ObservableCollection<KeyboardCell> Cells { get; }
    public string ReadColumnsText => $"0x{ReadColumns(0):X2}";

    /// <summary>Drives the PET matrix directly from an on-screen keyboard's signal callback
    /// (see PetGraphicsKeyboardLayoutFactory), bypassing the raw-cell ToggleButton grid.</summary>
    public void SetPetMatrixCell(int row, int column, bool pressed)
    {
        if (pressed) _pet.Press(row, column); else _pet.Release(row, column);
        OnPropertyChanged(nameof(StatusFields));
        OnPropertyChanged(nameof(ReadColumnsText));
    }

    /// <summary>Drives the VIC-20 matrix from an on-screen keyboard's signal callback (see
    /// Vic20KeyboardLayoutFactory). Also asserts just that cell's row, same as
    /// <see cref="ToggleCell"/> does - nothing else scans this standalone matrix.</summary>
    public void SetVicMatrixCell(int row, int column, bool pressed)
    {
        if (pressed) _vic.Press(row, column); else _vic.Release(row, column);
        _vic.SetRowSelect((byte)~(1 << row));
        OnPropertyChanged(nameof(StatusFields));
        OnPropertyChanged(nameof(ReadColumnsText));
    }

    partial void OnSelectedMachineChanged(string value)
    {
        Rebuild();
        OnPropertyChanged(nameof(StatusFields));
    }

    public void ToggleCell(KeyboardCell cell)
    {
        if (SelectedMachine == "PET")
        {
            if (cell.IsPressed) _pet.Press(cell.Row, cell.Column); else _pet.Release(cell.Row, cell.Column);
        }
        else
        {
            if (cell.IsPressed) _vic.Press(cell.Row, cell.Column); else _vic.Release(cell.Row, cell.Column);
            _vic.SetRowSelect((byte)~(1 << cell.Row));
        }
        OnPropertyChanged(nameof(StatusFields));
        OnPropertyChanged(nameof(ReadColumnsText));
    }

    private byte ReadColumns(int row)
    {
        if (SelectedMachine == "PET")
            return _pet.ReadColumns(row);
        _vic.SetRowSelect((byte)~(1 << row));
        return _vic.ReadColumns();
    }

    private void Rebuild()
    {
        _pet.Reset();
        _vic.Reset();
        Cells.Clear();
        var isPet = SelectedMachine == "PET";
        var rows = isPet ? PetKeyboardMatrix.RowCount : Vic20KeyboardMatrix.RowCount;
        var labels = isPet ? Pet2001GraphicsKeyboardMap.CellLabels : Vic20KeyboardMap.CellLabels;
        for (var row = 0; row < rows; row++)
            for (var column = 0; column < 8; column++)
                Cells.Add(new KeyboardCell(row, column, labels.GetValueOrDefault((row, column), "")));
    }

    public void Dispose() { }
}

public sealed partial class KeyboardCell : ObservableObject
{
    public KeyboardCell(int row, int column, string label) { Row = row; Column = column; Label = label; }
    public int Row { get; }
    public int Column { get; }

    /// <summary>The character or key name printed at this matrix cell (e.g. "A", "RETURN",
    /// "SHIFT") - empty for a real matrix cell no key uses, per
    /// <see cref="Pet2001GraphicsKeyboardMap.CellLabels"/>/<see cref="Vic20KeyboardMap.CellLabels"/>,
    /// not the raw (row, column) numbers.</summary>
    public string Label { get; }

    [ObservableProperty]
    private bool _isPressed;
}
