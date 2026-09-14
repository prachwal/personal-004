using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Views.Controls;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.Tests;

public sealed class PetGraphicsKeyboardLayoutFactoryTests
{
    [Test]
    public void Build_ProducesAValidLayoutCoveringEveryRealMatrixCell()
    {
        var layout = PetGraphicsKeyboardLayoutFactory.Build();

        // Every real matrix cell wired to a host key on the graphics keyboard (per
        // Pet2001GraphicsKeyboardMap.CellLabels, the ROM-verified single source of truth) must
        // have a key here - the on-screen keyboard should never be missing a real key.
        var wiredCells = layout.Keys
            .Select(key => PetGraphicsKeyboardLayoutFactory.TryParseSignal(key.SignalIds[0], out var row, out var column)
                ? (row, column) : (-1, -1))
            .ToHashSet();

        foreach (var cell in Pet2001GraphicsKeyboardMap.CellLabels.Keys)
            wiredCells.Should().Contain(cell, $"cell {cell} ({Pet2001GraphicsKeyboardMap.CellLabels[cell]}) should be reachable from the on-screen keyboard");
    }

    [Test]
    public void Build_EveryKeyIsWithinTheRealMatrixBounds()
    {
        var layout = PetGraphicsKeyboardLayoutFactory.Build();

        foreach (var key in layout.Keys)
        {
            PetGraphicsKeyboardLayoutFactory.TryParseSignal(key.SignalIds[0], out var row, out var column).Should().BeTrue();
            row.Should().BeInRange(0, PetKeyboardMatrix.RowCount - 1);
            column.Should().BeInRange(0, PetKeyboardMatrix.ColumnCount - 1);
        }
    }

    [Test]
    public void PressingAnOnScreenKeySetsTheExactMatrixCellItClaims()
    {
        var layout = PetGraphicsKeyboardLayoutFactory.Build();
        var matrix = new PetKeyboardMatrix();
        var spaceKey = layout.Keys.Single(k => k.Id == "SPACE");
        PetGraphicsKeyboardLayoutFactory.TryParseSignal(spaceKey.SignalIds[0], out var row, out var column);

        matrix.Press(row, column);

        matrix.ReadColumns(row).Should().Be((byte)~(1 << column));
    }

    [TestCase("pet:4,0", 4, 0)]
    [TestCase("pet:9,2", 9, 2)]
    [TestCase("not-a-signal", 0, 0)]
    public void TryParseSignal_RoundTripsOrRejectsCleanly(string signal, int expectedRow, int expectedColumn)
    {
        var ok = PetGraphicsKeyboardLayoutFactory.TryParseSignal(signal, out var row, out var column);

        if (signal.StartsWith("pet:"))
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
