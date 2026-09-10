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
    public string StatusText => $"{SelectedMachine}  row 0 read: 0x{ReadColumns(0):X2}";
    public ObservableCollection<KeyboardCell> Cells { get; }
    public string ReadColumnsText => $"0x{ReadColumns(0):X2}";

    partial void OnSelectedMachineChanged(string value)
    {
        Rebuild();
        OnPropertyChanged(nameof(StatusText));
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
        OnPropertyChanged(nameof(StatusText));
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
        var rows = SelectedMachine == "PET" ? PetKeyboardMatrix.RowCount : Vic20KeyboardMatrix.RowCount;
        for (var row = 0; row < rows; row++)
            for (var column = 0; column < 8; column++)
                Cells.Add(new KeyboardCell(row, column));
    }

    public void Dispose() { }
}

public sealed partial class KeyboardCell : ObservableObject
{
    public KeyboardCell(int row, int column) { Row = row; Column = column; }
    public int Row { get; }
    public int Column { get; }
    public string Label => $"{Row},{Column}";

    [ObservableProperty]
    private bool _isPressed;
}
