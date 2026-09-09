using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Vic20.Tests.Keyboard;

/// <summary>Layer 0 (see docs/vic20-migration-plan.md step 12) for the matrix itself - covers the
/// real bug docs/vic20-rendering-fixes.md's keyboard investigation found: the KERNAL's own
/// "is anything pressed at all" fast path asserts ALL EIGHT rows simultaneously (real
/// open-collector wire-OR hardware behavior), not one row at a time.</summary>
public sealed class Vic20KeyboardMatrixTests
{
    [Test]
    public void ReadColumns_WithASingleRowAsserted_ReturnsOnlyThatRowsState()
    {
        var matrix = new Vic20KeyboardMatrix();
        matrix.Press(2, 0);

        matrix.SetRowSelect(unchecked((byte)~(1 << 2))); // only row 2 asserted

        matrix.ReadColumns().Should().Be(unchecked((byte)~1), "row 2 col 0 is pressed, no other row is asserted");
    }

    [Test]
    public void ReadColumns_WithAllRowsAsserted_OrsColumnStateAcrossEveryAssertedRow()
    {
        var matrix = new Vic20KeyboardMatrix();
        matrix.Press(2, 0); // row 2, col 0

        matrix.SetRowSelect(0x00); // all 8 rows asserted at once - the real KERNAL "any key?" check

        matrix.ReadColumns().Should().Be(unchecked((byte)~1),
            "real open-collector hardware pulls column 0 low the moment ANY asserted row has it pressed, " +
            "not just row 0 - a single-row model would wrongly report nothing pressed here");
    }

    [Test]
    public void ReadColumns_WithNoRowAsserted_ReturnsAllOnes()
    {
        var matrix = new Vic20KeyboardMatrix();
        matrix.Press(2, 0);

        matrix.SetRowSelect(0xFF);

        matrix.ReadColumns().Should().Be(0xFF);
    }

    [Test]
    public void ReadColumns_ReflectsRelease()
    {
        var matrix = new Vic20KeyboardMatrix();
        matrix.Press(2, 0);
        matrix.Release(2, 0);

        matrix.SetRowSelect(0x00);

        matrix.ReadColumns().Should().Be(0xFF);
    }

    [TestCase('P', 1, 5)]
    [TestCase('A', 2, 1)]
    [TestCase('Q', 6, 0)]
    [TestCase('5', 0, 2)]
    [TestCase('0', 7, 4)]
    public void Vic20HostKeyMap_MatchesTheEmpiricallyVerifiedRealMatrix(char ch, int expectedRow, int expectedCol)
    {
        // Spot-checks a few entries against the full 64-cell scan recorded in
        // docs/vic20-rendering-fixes.md - the table this repo shipped before that investigation
        // (ported from a reference project) had every one of these wrong.
        Vic20HostKeyMap.Find(ch).Should().Be((expectedRow, expectedCol));
    }

    [Test]
    public void Vic20HostKeyMap_Space_IsRowFourColZero()
    {
        Vic20HostKeyMap.Find(' ').Should().Be((4, 0));
    }

    [Test]
    public void Vic20HostKeyMap_Enter_IsRowThreeColSeven()
    {
        // Confirmed via the real KERNAL's screen line-pointer advancing by exactly one row
        // (22 = the profile's column count) after this cell alone - see the investigation doc.
        Vic20HostKeyMap.Find('\n').Should().Be((3, 7));
    }
}
