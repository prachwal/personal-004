using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core.Keyboard;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Vic20.Tests.Keyboard;

/// <summary>Layer 0 (see docs/vic20-migration-plan.md step 12) for the matrix itself - covers the
/// real bug docs/vic20-rendering-fixes.md's keyboard investigation found: the KERNAL's own
/// "is anything pressed at all" fast path asserts ALL EIGHT rows simultaneously (real
/// open-collector wire-OR hardware behavior), not one row at a time.</summary>
public sealed class Vic20KeyboardMatrixTests
{
    [Test]
    public void AtKeyboardCatalog_ContainsEveryKeyAndVicMapCanClassifyEachOne()
    {
        var map = new Vic20KeyboardMap();

        AtKeyboardMapping.AllKeys.Should().NotBeEmpty();
        AtKeyboardMapping.AllKeys.Should().OnlyHaveUniqueItems();
        foreach (var key in AtKeyboardMapping.AllKeys)
            _ = map.Translate(key);
    }

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
    public void Vic20HostKeyMap_Enter_IsRowOneColSeven()
    {
        // A screen line-pointer advancing by one row after this cell alone is NOT sufficient
        // proof of Enter - CRSR-DOWN (a real, different key: (3,7), this table's first, wrong
        // guess) produces the exact same pointer movement without ever executing anything typed.
        // See Vic20MachineTests.Enter_ActuallyExecutesTheTypedLine_NotJustCrsrDown for the real
        // test that distinguishes them.
        Vic20HostKeyMap.Find('\n').Should().Be((1, 7));
    }

    // Vic20KeyboardMap's five control-key cells (Shift/Ctrl/Escape/Home/Backspace) weren't
    // covered by the 64-cell empirical sweep above (letters/digits/punctuation only) and were
    // simply wrong - re-derived from the real KERNAL disassembly's own "VIC 20 keyboard matrix
    // layout" table (vic-20-rom.asm, under VIA2PA1 $9121) and cross-checked by transposing every
    // already-verified letter/digit cell first (all landed exactly where Vic20HostKeyMap says)
    // before trusting the same transpose for these previously-unverified cells. See
    // Vic20KeyboardMap's own class doc comment for the full derivation and the old, wrong values.
    [TestCase(AtKeyboardKey.LeftShift, 3, 1)]
    [TestCase(AtKeyboardKey.RightShift, 4, 6)]
    [TestCase(AtKeyboardKey.LeftCtrl, 2, 0)]
    [TestCase(AtKeyboardKey.RightCtrl, 2, 0)]
    [TestCase(AtKeyboardKey.Escape, 3, 0)] // RUN/STOP
    [TestCase(AtKeyboardKey.Home, 7, 6)]
    [TestCase(AtKeyboardKey.Backspace, 0, 7)] // DEL/INST
    [TestCase(AtKeyboardKey.Delete, 0, 7)]
    [TestCase(AtKeyboardKey.LeftAlt, 5, 0)] // C=
    [TestCase(AtKeyboardKey.F1, 4, 7)]
    [TestCase(AtKeyboardKey.F3, 5, 7)]
    [TestCase(AtKeyboardKey.F5, 6, 7)]
    [TestCase(AtKeyboardKey.F7, 7, 7)]
    public void Vic20KeyboardMap_ControlKeys_MatchTheRealKernalMatrixTable(AtKeyboardKey key, int expectedRow, int expectedCol)
    {
        var map = new Vic20KeyboardMap();

        map.Translate(key).Should().Be(new KeyboardMatrixPosition(expectedRow, expectedCol));
    }

    [Test]
    public void Vic20KeyboardMap_LeftAndRightShift_AreDifferentCells()
    {
        // Real hardware has two separate physical Shift keys on two separate matrix cells - they
        // were wrongly combined onto one cell ((4,7), which is really F1) before this fix.
        var map = new Vic20KeyboardMap();

        map.Translate(AtKeyboardKey.LeftShift).Should().NotBe(map.Translate(AtKeyboardKey.RightShift));
    }

    [Test]
    public void Vic20KeyboardMap_Quote_UsesShiftAndTwo()
    {
        var map = new Vic20KeyboardMap();

        map.Translate("Quote", HostKeyEventKind.Press).Should().Equal(
            new MatrixAction(4, 6, true), new MatrixAction(7, 0, true));
    }

    [Test]
    public void Vic20KeyboardMap_Quote_ReleasesTwoBeforeSyntheticShift()
    {
        var map = new Vic20KeyboardMap();
        map.Translate("Quote", HostKeyEventKind.Press);

        map.Translate("Quote", HostKeyEventKind.Release).Should().Equal(
            new MatrixAction(7, 0, false), new MatrixAction(4, 6, false));
    }

    [Test]
    public void Vic20KeyboardMap_Quote_DoesNotReleasePhysicallyHeldShift()
    {
        var map = new Vic20KeyboardMap();
        map.Translate("ShiftRight", HostKeyEventKind.Press);
        map.Translate("Quote", HostKeyEventKind.Press);

        map.Translate("Quote", HostKeyEventKind.Release).Should().Equal(
            new MatrixAction(7, 0, false));
    }

    [Test]
    public void Vic20KeyboardMap_Quote_ReleasesSyntheticShiftWhenHostUsesLeftShift()
    {
        var map = new Vic20KeyboardMap();
        map.Translate("ShiftLeft", HostKeyEventKind.Press);
        map.Translate("Quote", HostKeyEventKind.Press);

        map.Translate("Quote", HostKeyEventKind.Release).Should().Equal(
            new MatrixAction(7, 0, false), new MatrixAction(4, 6, false));
    }

    [Test]
    public void Vic20KeyboardMap_Two_DoesNotGenerateShift()
    {
        var map = new Vic20KeyboardMap();

        map.Translate("Digit2", HostKeyEventKind.Press).Should().Equal(
            new MatrixAction(7, 0, true));
        map.Translate("Digit2", HostKeyEventKind.Release).Should().Equal(
            new MatrixAction(7, 0, false));
    }
}
