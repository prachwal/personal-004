using NUnit.Framework;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Pet.Tests.Keyboard;

[TestFixture]
public sealed class Cbm8032KeyboardMapTests
{
    [Test]
    public void Ordinary_key_press_and_release_produce_a_single_matrix_action()
    {
        var map = new Cbm8032KeyboardMap();

        Assert.That(map.Translate("KeyA", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(3, 0, true) }));
        Assert.That(map.Translate("KeyA", HostKeyEventKind.Release), Is.EqualTo(new[] { new MatrixAction(3, 0, false) }));
        Assert.That(map.Translate("ShiftLeft", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(6, 0, true) }));
        Assert.That(map.Translate("Minus", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(0, 3, true) }));
        Assert.That(map.Translate("Backspace", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(7, 6, true) }));
    }

    [Test]
    public void Plus_family_press_asserts_shift_instead_of_clearing_it()
    {
        var map = new Cbm8032KeyboardMap();

        var actions = map.Translate("Equal", HostKeyEventKind.Press);

        Assert.That(actions, Is.EqualTo(new[]
        {
            new MatrixAction(2, 6, true),
            new MatrixAction(6, 6, true)
        }));
    }

    // The centerpiece stateful test: ShiftRight held through a '+' press/release must NOT lose
    // its own (6,6) signal - only releasing the real ShiftRight clears it.
    [Test]
    public void Releasing_plus_while_shift_right_still_held_does_not_clear_shift_signal()
    {
        var map = new Cbm8032KeyboardMap();

        var shiftPress = map.Translate("ShiftRight", HostKeyEventKind.Press);
        Assert.That(shiftPress, Is.EqualTo(new[] { new MatrixAction(6, 6, true) }));

        var plusPress = map.Translate("Equal", HostKeyEventKind.Press);
        Assert.That(plusPress, Is.EqualTo(new[] { new MatrixAction(2, 6, true), new MatrixAction(6, 6, true) }));

        // ShiftRight is still physically held: releasing '+' must only release (2,6).
        var plusRelease = map.Translate("Equal", HostKeyEventKind.Release);
        Assert.That(plusRelease, Is.EqualTo(new[] { new MatrixAction(2, 6, false) }));

        // Now release the real ShiftRight: (6,6) must be released here.
        var shiftRelease = map.Translate("ShiftRight", HostKeyEventKind.Release);
        Assert.That(shiftRelease, Is.EqualTo(new[] { new MatrixAction(6, 6, false) }));
    }

    [Test]
    public void Releasing_plus_without_shift_right_held_clears_shift_signal_itself()
    {
        var map = new Cbm8032KeyboardMap();

        map.Translate("Equal", HostKeyEventKind.Press);
        var plusRelease = map.Translate("Equal", HostKeyEventKind.Release);

        Assert.That(plusRelease, Is.EqualTo(new[]
        {
            new MatrixAction(2, 6, false),
            new MatrixAction(6, 6, false)
        }));
    }

    [Test]
    public void Cursor_keys_use_the_business_keyboard_shift_cell()
    {
        var map = new Cbm8032KeyboardMap();

        Assert.That(map.Translate("Down", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(1, 6, true) }));
        Assert.That(map.Translate("Right", HostKeyEventKind.Press), Is.EqualTo(new[]
        {
            new MatrixAction(6, 0, true), new MatrixAction(0, 7, true)
        }));
        Assert.That(map.Translate("Right", HostKeyEventKind.Release), Is.EqualTo(new[]
        {
            new MatrixAction(0, 7, false), new MatrixAction(6, 0, false)
        }));
    }

    // Every plain-cell entry in Cbm8032KeyboardMap's own Table dictionary, transcribed
    // independently here so a future accidental edit to that table (a typo'd row/column, a
    // deleted entry) fails a test instead of silently shipping - see docs/pet/superpet-6809-boot-hang.md
    // for why this table's Enter/Backspace cells specifically were "confirmed behaviorally, not by
    // a text sweep" and turned out wrong for the SuperPET's own keyboard-scan/decode ROM (a
    // separate, ROM-side lookup this C# map has no visibility into - this test only pins what the
    // map itself claims, not whether the real 6809 ROM agrees byte-for-byte).
    private static readonly (string HostKey, int Row, int Column)[] PlainCells =
    [
        ("KeyA", 3, 0), ("KeyB", 6, 2), ("KeyC", 6, 1), ("KeyD", 3, 1), ("KeyE", 5, 1), ("KeyF", 2, 2),
        ("KeyG", 3, 2), ("KeyH", 2, 3), ("KeyI", 4, 5), ("KeyJ", 3, 3), ("KeyK", 2, 5), ("KeyL", 3, 5),
        ("KeyM", 8, 3), ("KeyN", 7, 2), ("KeyO", 5, 5), ("KeyP", 4, 6), ("KeyQ", 5, 0), ("KeyR", 4, 2),
        ("KeyS", 2, 1), ("KeyT", 5, 2), ("KeyU", 5, 3), ("KeyV", 7, 1), ("KeyW", 4, 1), ("KeyX", 8, 1),
        ("KeyY", 4, 3), ("KeyZ", 7, 0),
        ("Digit0", 7, 4), ("Digit1", 1, 0), ("Digit2", 0, 0), ("Digit3", 6, 7), ("Digit4", 1, 1),
        ("Digit5", 0, 1), ("Digit6", 9, 2), ("Digit7", 1, 2), ("Digit8", 0, 2), ("Digit9", 1, 7),
        ("Space", 0, 5),
        ("Enter", 3, 4), ("Backspace", 7, 6),
        ("Minus", 0, 3),
        ("ShiftLeft", 6, 0),
    ];

    [TestCaseSource(nameof(PlainCells))]
    public void Every_plain_cell_in_the_table_presses_and_releases_its_documented_matrix_position(
        (string HostKey, int Row, int Column) cell)
    {
        var map = new Cbm8032KeyboardMap();

        var press = map.Translate(cell.HostKey, HostKeyEventKind.Press);
        var release = map.Translate(cell.HostKey, HostKeyEventKind.Release);

        Assert.That(press, Is.EqualTo(new[] { new MatrixAction(cell.Row, cell.Column, true) }), cell.HostKey);
        Assert.That(release, Is.EqualTo(new[] { new MatrixAction(cell.Row, cell.Column, false) }), cell.HostKey);
    }

    [Test]
    public void Every_plain_cell_maps_to_a_distinct_matrix_position()
    {
        // A duplicate (row, column) pair here would mean two different keys are indistinguishable
        // to the emulated matrix - not necessarily wrong (real keyboards do legitimately share
        // cells - see ShiftRight/'+' above), but every entry in this specific plain-cell table is
        // supposed to be its own unique physical key, so a collision here is a copy-paste bug.
        var positions = PlainCells.Select(c => (c.Row, c.Column)).ToList();

        Assert.That(positions, Is.Unique);
    }

    [Test]
    public void Up_and_Left_cursor_keys_also_use_the_business_keyboard_shift_cell()
    {
        var map = new Cbm8032KeyboardMap();

        Assert.That(map.Translate("Left", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(0, 7, true) }));
        Assert.That(map.Translate("Left", HostKeyEventKind.Release), Is.EqualTo(new[] { new MatrixAction(0, 7, false) }));
        Assert.That(map.Translate("Up", HostKeyEventKind.Press), Is.EqualTo(new[]
        {
            new MatrixAction(6, 0, true), new MatrixAction(1, 6, true)
        }));
        Assert.That(map.Translate("Up", HostKeyEventKind.Release), Is.EqualTo(new[]
        {
            new MatrixAction(1, 6, false), new MatrixAction(6, 0, false)
        }));
    }

    [Test]
    public void Unrecognized_host_key_produces_no_matrix_action()
    {
        var map = new Cbm8032KeyboardMap();

        Assert.That(map.Translate("F13", HostKeyEventKind.Press), Is.Empty);
    }

    [Test]
    public void OemPlus_and_NumPadAdd_are_also_recognized_as_the_plus_family()
    {
        var map = new Cbm8032KeyboardMap();

        foreach (var hostKey in new[] { "OemPlus", "NumPadAdd" })
        {
            var press = map.Translate(hostKey, HostKeyEventKind.Press);
            Assert.That(press, Is.EqualTo(new[]
            {
                new MatrixAction(2, 6, true), new MatrixAction(6, 6, true)
            }), hostKey);
        }
    }

    [Test]
    public void Plus_sequence_drives_a_real_matrix_correctly_through_the_shift_right_interlock()
    {
        var map = new Cbm8032KeyboardMap();
        var matrix = new PetKeyboardMatrix();

        Apply(matrix, map.Translate("ShiftRight", HostKeyEventKind.Press));
        Apply(matrix, map.Translate("Equal", HostKeyEventKind.Press));
        Apply(matrix, map.Translate("Equal", HostKeyEventKind.Release));

        // (6,6) bit 6 must still be pressed (active-low: bit clear) - ShiftRight is still down.
        Assert.That(matrix.ReadColumns(6), Is.EqualTo(unchecked((byte)~(1 << 6))));

        Apply(matrix, map.Translate("ShiftRight", HostKeyEventKind.Release));

        // Now fully released.
        Assert.That(matrix.ReadColumns(6), Is.EqualTo(0xFF));
    }

    private static void Apply(PetKeyboardMatrix matrix, IReadOnlyList<MatrixAction> actions)
    {
        foreach (var action in actions)
        {
            if (action.Pressed) matrix.Press(action.Row, action.Column);
            else matrix.Release(action.Row, action.Column);
        }
    }
}
