using NUnit.Framework;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Pet.Tests.Keyboard;

[TestFixture]
public sealed class Pet2001GraphicsKeyboardMapTests
{
    [Test]
    public void Ordinary_key_press_and_release_produce_a_single_matrix_action()
    {
        var map = new Pet2001GraphicsKeyboardMap();

        Assert.That(map.Translate("KeyA", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(4, 0, true) }));
        Assert.That(map.Translate("KeyA", HostKeyEventKind.Release), Is.EqualTo(new[] { new MatrixAction(4, 0, false) }));
        Assert.That(map.Translate("ShiftLeft", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(8, 0, true) }));
        Assert.That(map.Translate("ShiftRight", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(8, 5, true) }));
        Assert.That(map.Translate("Digit7", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(2, 6, true) }));
    }

    [Test]
    public void Unknown_host_key_produces_no_actions()
    {
        var map = new Pet2001GraphicsKeyboardMap();

        Assert.That(map.Translate("F13", HostKeyEventKind.Press), Is.Empty);
    }

    [Test]
    public void Cursor_keys_use_the_real_horizontal_and_vertical_cursor_keys()
    {
        var map = new Pet2001GraphicsKeyboardMap();

        Assert.That(map.Translate("Left", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(0, 7, true) }));
        Assert.That(map.Translate("Down", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(1, 6, true) }));
        Assert.That(map.Translate("Right", HostKeyEventKind.Press), Is.EqualTo(new[]
        {
            new MatrixAction(8, 0, true), new MatrixAction(0, 7, true)
        }));
        Assert.That(map.Translate("Right", HostKeyEventKind.Release), Is.EqualTo(new[]
        {
            new MatrixAction(0, 7, false), new MatrixAction(8, 0, false)
        }));
    }

    [Test]
    public void Cursor_shift_is_not_released_while_host_shift_is_still_held()
    {
        var map = new Pet2001GraphicsKeyboardMap();

        map.Translate("ShiftLeft", HostKeyEventKind.Press);
        Assert.That(map.Translate("Up", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(1, 6, true) }));
        Assert.That(map.Translate("Up", HostKeyEventKind.Release), Is.EqualTo(new[] { new MatrixAction(1, 6, false) }));
        Assert.That(map.Translate("ShiftLeft", HostKeyEventKind.Release), Is.EqualTo(new[] { new MatrixAction(8, 0, false) }));
    }

    // The centerpiece of the task: pressing '+' doesn't just press one cell, it also
    // force-releases both Shift matrix cells - proving one host key event yields a SEQUENCE.
    [TestCase("Equal")]
    [TestCase("OemPlus")]
    [TestCase("NumPadAdd")]
    public void Plus_family_press_presses_target_and_force_releases_both_shift_cells(string hostKey)
    {
        var map = new Pet2001GraphicsKeyboardMap();

        var actions = map.Translate(hostKey, HostKeyEventKind.Press);

        Assert.That(actions, Is.EqualTo(new[]
        {
            new MatrixAction(7, 7, true),
            new MatrixAction(8, 0, false),
            new MatrixAction(8, 5, false)
        }));
    }

    [Test]
    public void Plus_family_release_also_force_releases_both_shift_cells()
    {
        var map = new Pet2001GraphicsKeyboardMap();

        var actions = map.Translate("Equal", HostKeyEventKind.Release);

        Assert.That(actions, Is.EqualTo(new[]
        {
            new MatrixAction(7, 7, false),
            new MatrixAction(8, 0, false),
            new MatrixAction(8, 5, false)
        }));
    }

    [Test]
    public void Plus_key_sequence_drives_a_real_matrix_correctly()
    {
        var map = new Pet2001GraphicsKeyboardMap();
        var matrix = new PetKeyboardMatrix();

        // Simulate holding host Shift (asserts (8,0)) then pressing '+'.
        Apply(matrix, map.Translate("ShiftLeft", HostKeyEventKind.Press));
        Apply(matrix, map.Translate("Equal", HostKeyEventKind.Press));

        // (8,0) must have been force-released despite ShiftLeft still being physically held.
        Assert.That(matrix.ReadColumns(8), Is.EqualTo(0xFF));
        // (7,7) must be pressed (active-low: bit 7 clear, all other bits high).
        Assert.That(matrix.ReadColumns(7), Is.EqualTo(0x7F));
    }

    private static void Apply(PetKeyboardMatrix matrix, IReadOnlyList<MatrixAction> actions)
    {
        foreach (var action in actions)
        {
            if (action.Pressed) matrix.Press(action.Row, action.Column);
            else matrix.Release(action.Row, action.Column);
        }
    }

    // Same fix as '+': a host Shift held to type '"' (Shift+Quote on a modern keyboard) must not
    // also drive the PET's own Shift row, since (1,0) already means '"' unshifted - live bug
    // report: pressing Shift+' on the desktop GUI echoed a graphics glyph instead of '"'.
    [Test]
    public void Quote_force_releases_both_shift_cells()
    {
        var map = new Pet2001GraphicsKeyboardMap();

        var actions = map.Translate("Quote", HostKeyEventKind.Press);

        Assert.That(actions, Is.EqualTo(new[]
        {
            new MatrixAction(1, 0, true),
            new MatrixAction(8, 0, false),
            new MatrixAction(8, 5, false)
        }));
    }

    // CellLabels is derived from Table (single source of truth) for the Keyboard Matrix demo -
    // spot-checks that the inversion lands on the same cells Translate itself produces.
    [TestCase(4, 0, "A")]
    [TestCase(2, 6, "7")]
    [TestCase(9, 2, "SPACE")]
    [TestCase(6, 5, "RETURN")]
    [TestCase(1, 7, "←")]
    [TestCase(8, 0, "SHIFT")]
    [TestCase(8, 5, "SHIFT")]
    [TestCase(7, 3, ",")]
    [TestCase(7, 7, "+")]
    public void CellLabels_MatchesWhatTranslateProducesForTheSameCell(int row, int column, string expectedLabel)
    {
        Assert.That(Pet2001GraphicsKeyboardMap.CellLabels[(row, column)], Is.EqualTo(expectedLabel));
    }
}
