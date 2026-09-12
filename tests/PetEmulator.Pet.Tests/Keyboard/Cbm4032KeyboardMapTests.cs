using NUnit.Framework;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Pet.Tests.Keyboard;

[TestFixture]
public sealed class Cbm4032KeyboardMapTests
{
    // Table computed from personal-001's Lookup byte array + Basic4KeyCodes() + the source's
    // own "index = Array.IndexOf(Lookup, code); row = 9 - index/8; column = 7 - index%8;"
    // formula, via a throwaway Python script that reproduces that exact algorithm (see task
    // report for the script). These are the resulting literals, spot-checked below.

    [Test]
    public void Ordinary_key_press_produces_a_single_matrix_action()
    {
        var map = new Cbm4032KeyboardMap();

        Assert.That(map.Translate("KeyA", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(4, 0, true) }));
        Assert.That(map.Translate("KeyZ", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(6, 0, true) }));
        Assert.That(map.Translate("Digit0", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(8, 6, true) }));
        Assert.That(map.Translate("Space", HostKeyEventKind.Release), Is.EqualTo(new[] { new MatrixAction(9, 2, false) }));
        Assert.That(map.Translate("Minus", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(8, 7, true) }));
    }

    [Test]
    public void Plus_family_still_force_releases_both_shift_cells()
    {
        var map = new Cbm4032KeyboardMap();

        var actions = map.Translate("Equal", HostKeyEventKind.Press);

        Assert.That(actions, Is.EqualTo(new[]
        {
            new MatrixAction(7, 7, true),
            new MatrixAction(8, 0, false),
            new MatrixAction(8, 5, false)
        }));
    }

    // Same fix as '+': a host Shift held to type '"' (Shift+Quote on a modern keyboard) must not
    // also drive the PET's own Shift row, since (1,0) already means '"' unshifted.
    [Test]
    public void Quote_force_releases_both_shift_cells()
    {
        var map = new Cbm4032KeyboardMap();

        var actions = map.Translate("Quote", HostKeyEventKind.Press);

        Assert.That(actions, Is.EqualTo(new[]
        {
            new MatrixAction(1, 0, true),
            new MatrixAction(8, 0, false),
            new MatrixAction(8, 5, false)
        }));
    }

    // Mandatory verification step: sanity-check the computed table instead of eyeballing hex.
    [Test]
    public void Computed_table_places_distinct_in_range_letter_positions_with_no_duplicates()
    {
        var map = new Cbm4032KeyboardMap();

        string[] letters =
        [
            "KeyA", "KeyB", "KeyC", "KeyD", "KeyE", "KeyF", "KeyG", "KeyH", "KeyI", "KeyJ", "KeyK", "KeyL", "KeyM",
            "KeyN", "KeyO", "KeyP", "KeyQ", "KeyR", "KeyS", "KeyT", "KeyU", "KeyV", "KeyW", "KeyX", "KeyY", "KeyZ"
        ];

        var seen = new HashSet<(int Row, int Column)>();
        foreach (var key in letters)
        {
            var actions = map.Translate(key, HostKeyEventKind.Press);
            Assert.That(actions, Has.Count.EqualTo(1), $"{key} should be a plain 1:1 key");

            var action = actions[0];
            Assert.That(action.Row, Is.InRange(0, 9), $"{key} row out of range");
            Assert.That(action.Column, Is.InRange(0, 7), $"{key} column out of range");
            Assert.That(seen.Add((action.Row, action.Column)), Is.True, $"{key} duplicates a matrix cell already assigned to another letter");
        }

        // KeyA and KeyZ must land at genuinely different cells.
        Assert.That(map.Translate("KeyA", HostKeyEventKind.Press)[0], Is.Not.EqualTo(map.Translate("KeyZ", HostKeyEventKind.Press)[0]));
    }

    [Test]
    public void Unknown_host_key_produces_no_actions()
    {
        var map = new Cbm4032KeyboardMap();

        Assert.That(map.Translate("F13", HostKeyEventKind.Press), Is.Empty);
    }

    [Test]
    public void Cursor_keys_use_the_real_cursor_pair_and_keyboard_shift()
    {
        var map = new Cbm4032KeyboardMap();

        Assert.That(map.Translate("Left", HostKeyEventKind.Press), Is.EqualTo(new[] { new MatrixAction(0, 7, true) }));
        Assert.That(map.Translate("Up", HostKeyEventKind.Press), Is.EqualTo(new[]
        {
            new MatrixAction(8, 5, true), new MatrixAction(1, 6, true)
        }));
        Assert.That(map.Translate("Up", HostKeyEventKind.Release), Is.EqualTo(new[]
        {
            new MatrixAction(1, 6, false), new MatrixAction(8, 5, false)
        }));
    }
}
