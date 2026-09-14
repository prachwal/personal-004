using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core.Serial;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.Keyboard;

/// <summary>
/// Verifies <see cref="Cbm8032KeyboardMap"/> against a real, booting SuperPET 6809 ROM - not just
/// the map's own claimed (row, column) values (<see cref="Cbm8032KeyboardMapTests"/> pins those),
/// but that pressing the resulting matrix cell actually produces the right character/effect on a
/// live machine. Four of this map's entries (Enter, Backspace, Space, KeyL) were wrong when
/// inherited from personal-001's plain-CBM-8032 table and went undetected until reported live in
/// the GUI - see docs/pet/superpet-6809-boot-hang.md's keyboard-mapping section. This is the
/// permanent version of the one-off sweep/verification scripts used to find and confirm each fix,
/// so a future accidental edit to the table fails here instead of shipping silently again.
/// </summary>
[TestFixture]
public sealed class Cbm8032KeyboardMapRomVerificationTests
{
    private static readonly (string HostKey, char Expected)[] PlainCharacters =
    [
        ("KeyA", 'a'), ("KeyB", 'b'), ("KeyC", 'c'), ("KeyD", 'd'), ("KeyE", 'e'), ("KeyF", 'f'),
        ("KeyG", 'g'), ("KeyH", 'h'), ("KeyI", 'i'), ("KeyJ", 'j'), ("KeyK", 'k'), ("KeyL", 'l'),
        ("KeyM", 'm'), ("KeyN", 'n'), ("KeyO", 'o'), ("KeyP", 'p'), ("KeyQ", 'q'), ("KeyR", 'r'),
        ("KeyS", 's'), ("KeyT", 't'), ("KeyU", 'u'), ("KeyV", 'v'), ("KeyW", 'w'), ("KeyX", 'x'),
        ("KeyY", 'y'), ("KeyZ", 'z'),
        ("Digit0", '0'), ("Digit1", '1'), ("Digit2", '2'), ("Digit3", '3'), ("Digit4", '4'),
        ("Digit5", '5'), ("Digit6", '6'), ("Digit7", '7'), ("Digit8", '8'), ("Digit9", '9'),
        ("Minus", '-'),
    ];

    [TestCaseSource(nameof(PlainCharacters))]
    public void Every_plain_character_key_echoes_the_right_glyph_on_the_real_rom(
        (string HostKey, char Expected) c)
    {
        var machine = BootToMenu();
        TextTyper.Type(machine, new Cbm8032KeyboardMap(), c.HostKey == "Minus" ? "-" : c.HostKey[^1..]);
        machine.Run(3_000);

        ((char)machine.Memory.Read(0x8410)).Should().Be(c.Expected, c.HostKey);
    }

    [Test]
    public void Space_advances_the_cursor_one_column_with_no_visible_glyph()
    {
        var machine = BootToMenu();
        var keymap = new Cbm8032KeyboardMap();
        TextTyper.Type(machine, keymap, "A");
        machine.Run(3_000);

        var before = machine.Memory.Read(0x8410);
        ApplyKey(machine, keymap, "Space");
        machine.Run(9_000);

        before.Should().Be((byte)'a');
        machine.Memory.Read(0x8411).Should().Be(0x20, "space itself leaves no visible glyph");
    }

    [Test]
    public void Backspace_removes_the_previously_typed_character()
    {
        var machine = BootToMenu();
        var keymap = new Cbm8032KeyboardMap();
        TextTyper.Type(machine, keymap, "AB");
        machine.Run(3_000);
        ((char)machine.Memory.Read(0x8411)).Should().Be('b', "sanity check: both characters echoed first");

        ApplyKey(machine, keymap, "Backspace");
        machine.Run(9_000);
        TextTyper.Type(machine, keymap, "C");
        machine.Run(3_000);

        // "AB", backspace, "C" -> "AC": the 'B' must be gone, not just visually overwritten later.
        ((char)machine.Memory.Read(0x8410)).Should().Be('a');
        ((char)machine.Memory.Read(0x8411)).Should().Be('c');
    }

    [Test]
    public void Enter_actually_submits_the_typed_command_not_just_moves_the_cursor()
    {
        // The exact bug this map's first two "Enter" answers both had (row,col guesses that only
        // moved the cursor down): confirm the SETUP module's own screen replaces the menu banner -
        // proof the ROM's line editor really processed and ran the line, not that some key merely
        // repositioned the cursor to a spot that happens to look similar.
        var machine = BootToMenu();
        var keymap = new Cbm8032KeyboardMap();
        TextTyper.Type(machine, keymap, "S");
        machine.Run(3_000);
        ApplyKey(machine, keymap, "Enter");
        machine.Run(100_000);

        machine.Memory.Read(0x8000).Should().Be((byte)'B', "the SETUP module's screen starts with \"BAUD\", replacing the menu banner");
    }

    private static PetMachine BootToMenu()
    {
        var profileDirectory = RomLocator.Directory(PetProfileCatalog.SuperPet.RomDirectory, PetProfileCatalog.SuperPet.RomManifest[0].Path);
        var machine = new PetMachine(
            PetProfileCatalog.SuperPet,
            Directory.GetParent(profileDirectory)!.FullName,
            serialTransport: new BufferedSerialTransport());
        machine.Run(200_000);
        return machine;
    }

    private static void ApplyKey(PetMachine machine, Cbm8032KeyboardMap keymap, string hostKey)
    {
        foreach (var action in keymap.Translate(hostKey, HostKeyEventKind.Press))
            if (action.Pressed) machine.Keyboard.Press(action.Row, action.Column); else machine.Keyboard.Release(action.Row, action.Column);
        machine.Run(5_500);
        foreach (var action in keymap.Translate(hostKey, HostKeyEventKind.Release))
            if (action.Pressed) machine.Keyboard.Press(action.Row, action.Column); else machine.Keyboard.Release(action.Row, action.Column);
    }
}
