using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Tests;

public sealed class HexEditorViewModelTests
{
    [Test]
    public void BuildsRowsAndEditsAByteWithoutViewLogic()
    {
        var editor = new HexEditorViewModel();
        editor.SetBytes([0x10, 0x20, 0x30]);

        editor.Rows.Should().ContainSingle();
        editor.Rows[0].Cells.Select(cell => cell.HexText).Should().Equal("10", "20", "30");

        editor.EditCell(1, "AF").Should().BeTrue();
        editor.Bytes.Should().Equal(0x10, 0xAF, 0x30);
        editor.Rows[0].Cells[1].AsciiText.Should().Be(".");
    }

    [Test]
    public void RejectsInvalidCellTextWithoutChangingBytes()
    {
        var editor = new HexEditorViewModel();
        editor.SetBytes([0x10]);

        editor.EditCell(0, "GG").Should().BeFalse();
        editor.EditCell(0, "123").Should().BeFalse();
        editor.Bytes.Should().Equal(0x10);
    }

    [Test]
    public void ShiftMovementSelectsRangeAndDeleteRemovesIt()
    {
        var editor = new HexEditorViewModel();
        editor.SetBytes([1, 2, 3, 4, 5]);

        editor.MoveTo(1, false);
        editor.MoveTo(3, true);
        editor.Rows.SelectMany(row => row.Cells).Where(cell => cell.IsSelected)
            .Select(cell => cell.Offset).Should().Equal(1, 2, 3);

        editor.DeleteSelectionOrByte();
        editor.Bytes.Should().Equal(1, 5);
    }

    [Test]
    public void InsertAddsZeroAtSelectedOffset()
    {
        var editor = new HexEditorViewModel();
        editor.SetBytes([1, 2]);
        editor.MoveTo(1, false);

        editor.InsertByte();

        editor.Bytes.Should().Equal(1, 0, 2);
        editor.SelectedOffset.Should().Be(1);
    }

    [Test]
    public void ReadOnlyEditorRejectsAllMutations()
    {
        var editor = new HexEditorViewModel { IsReadOnly = true };
        editor.SetBytes([1, 2]);
        editor.MoveTo(0, false);

        editor.EditCell(0, "FF").Should().BeFalse();
        editor.InsertByte();
        editor.DeleteSelectionOrByte();

        editor.Bytes.Should().Equal(1, 2);
    }
}
