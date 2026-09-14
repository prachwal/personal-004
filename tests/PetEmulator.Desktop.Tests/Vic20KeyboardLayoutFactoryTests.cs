using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Views.Controls;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Desktop.Tests;

public sealed class Vic20KeyboardLayoutFactoryTests
{
    [Test]
    public void Build_ProducesAValidLayoutCoveringEveryRealMatrixCell()
    {
        var layout = Vic20KeyboardLayoutFactory.Build();

        var wiredCells = layout.Keys
            .Select(key => Vic20KeyboardLayoutFactory.TryParseSignal(key.SignalIds[0], out var row, out var column)
                ? (row, column) : (-1, -1))
            .ToHashSet();

        foreach (var cell in Vic20KeyboardMap.CellLabels.Keys)
            wiredCells.Should().Contain(cell, $"cell {cell} ({Vic20KeyboardMap.CellLabels[cell]}) should be reachable from the on-screen keyboard");
    }

    [Test]
    public void Build_EveryKeyIsWithinTheRealMatrixBounds()
    {
        var layout = Vic20KeyboardLayoutFactory.Build();

        foreach (var key in layout.Keys)
        {
            Vic20KeyboardLayoutFactory.TryParseSignal(key.SignalIds[0], out var row, out var column).Should().BeTrue();
            row.Should().BeInRange(0, Vic20KeyboardMatrix.RowCount - 1);
            column.Should().BeInRange(0, Vic20KeyboardMatrix.ColumnCount - 1);
        }
    }

    [Test]
    public void PressingAnOnScreenKeySetsTheExactMatrixCellItClaims()
    {
        var layout = Vic20KeyboardLayoutFactory.Build();
        var matrix = new Vic20KeyboardMatrix();
        var spaceKey = layout.Keys.Single(k => k.AutomationName == "Space");
        Vic20KeyboardLayoutFactory.TryParseSignal(spaceKey.SignalIds[0], out var row, out var column);

        matrix.Press(row, column);
        matrix.SetRowSelect((byte)~(1 << row));

        matrix.ReadColumns().Should().Be((byte)~(1 << column));
    }

    [TestCase("vic20:3,1", 3, 1)]
    [TestCase("vic20:0,0", 0, 0)]
    [TestCase("pet:4,0", 0, 0)]
    public void TryParseSignal_RoundTripsOrRejectsCleanly(string signal, int expectedRow, int expectedColumn)
    {
        var ok = Vic20KeyboardLayoutFactory.TryParseSignal(signal, out var row, out var column);

        if (signal.StartsWith("vic20:"))
        {
            ok.Should().BeTrue();
            row.Should().Be(expectedRow);
            column.Should().Be(expectedColumn);
        }
        else
        {
            ok.Should().BeFalse();
        }
    }
}
