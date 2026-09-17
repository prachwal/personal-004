using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Views.Controls;

namespace PetEmulator.Desktop.Tests;

public sealed class CpcKeyboardLayoutFactoryTests
{
    [Test]
    public void Build_ProducesAValidLayout()
    {
        var act = () => CpcKeyboardLayoutFactory.Build();

        act.Should().NotThrow();
    }

    [Test]
    public void Build_CoversTheFullCsvKeySet()
    {
        var layout = CpcKeyboardLayoutFactory.Build();

        layout.Keys.Should().HaveCount(89);
        layout.Keys.Select(key => key.Id).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Build_EverySignalParsesToAMatrixCellInRange()
    {
        var layout = CpcKeyboardLayoutFactory.Build();

        foreach (var key in layout.Keys)
        {
            if (key.SignalIds[0] == CpcKeyboardLayoutFactory.UnknownSignal)
            {
                key.Id.Should().Match(id => id.StartsWith("f:", StringComparison.Ordinal) || id == "r2:BLANK");
                continue;
            }

            CpcKeyboardLayoutFactory.TryParseSignal(key.SignalIds[0], out var row, out var col, out _)
                .Should().BeTrue($"signal '{key.SignalIds[0]}' should parse");
            row.Should().BeLessThanOrEqualTo(9);
            col.Should().BeLessThanOrEqualTo(7);
        }
    }

    [TestCase("r1:ESC", 8, 2)]
    [TestCase("r5:SPACE", 5, 7)]
    [TestCase("r1:CLR", 2, 0)]
    [TestCase("r5:COPY", 1, 1)]
    [TestCase("r2:RETURN", 2, 2)]
    [TestCase("pad:ENTER", 0, 6)]
    [TestCase("pad:0", 1, 7)]
    public void Build_KeyCellsMatchTheVerifiedMatrix(string idPrefix, byte row, byte col)
    {
        var layout = CpcKeyboardLayoutFactory.Build();
        var key = layout.Keys.Single(k => k.Id.StartsWith(idPrefix, StringComparison.Ordinal));

        CpcKeyboardLayoutFactory.TryParseSignal(key.SignalIds[0], out var actualRow, out var actualCol, out _)
            .Should().BeTrue();
        actualRow.Should().Be(row);
        actualCol.Should().Be(col);
    }

    [TestCase("kaypro:0D")]
    [TestCase("vic20:1,2")]
    public void TryParseSignal_RejectsForeignPrefixesCleanly(string signal)
    {
        CpcKeyboardLayoutFactory.TryParseSignal(signal, out _, out _, out _).Should().BeFalse();
    }
}
