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
        Assert.That(map.Translate("Backspace", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(6, 5, true) }));
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
