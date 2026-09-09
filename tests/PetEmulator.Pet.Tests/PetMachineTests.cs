using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests;

/// <summary>
/// First smoke tests of real ROM execution through <see cref="PetMachine"/>'s brand-new bus
/// decoder. Deliberately bounded (hundreds of instructions, not a full BASIC boot) with a hard
/// [CancelAfter] backstop: the CPU's per-instruction cycle cap throws instead of hanging on a
/// handler bug, but a real bug there is still a legitimate failure here, not something to swallow.
/// </summary>
[TestFixture]
public sealed class PetMachineTests
{
    private static readonly byte[] ReadyBytes = [0x12, 0x05, 0x01, 0x04, 0x19, 0x2E]; // "READY."

    [Test]
    public void Name_And_IsReady_ReflectProfile()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Name.Should().Be(PetProfileCatalog.Pet2001_8.Name);
        machine.IsReady.Should().BeTrue();
    }

    [Test]
    [CancelAfter(30_000)]
    public void Pet2001_8_BootsWithoutThrowing_ForBoundedSteps()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Run(500);

        machine.Processor.Halted.Should().BeFalse();
        machine.Processor.InstructionCount.Should().BeGreaterThan(0);
    }

    [Test]
    [CancelAfter(30_000)]
    public void Cbm8032_BootsWithoutThrowing_ForBoundedSteps()
    {
        var machine = CreateMachine(PetProfileCatalog.Cbm8032);

        machine.Run(500);

        machine.Processor.Halted.Should().BeFalse();
        machine.Processor.InstructionCount.Should().BeGreaterThan(0);
    }

    [Test]
    public void Reset_ZeroesRamAndRestartsCpuAtResetVector()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        machine.Memory.Write(0x0100, 0x77);

        machine.Reset();

        machine.Memory.Read(0x0100).Should().Be(0);
        machine.Processor.InstructionCount.Should().Be(0);
    }

    [Test]
    public void Keyboard_PressedKey_ReadableThroughPia1OverTheBus()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        const ushort pia1Base = 0xE810;

        // Select the data register (not the direction register) on PIA1's port A and B.
        machine.Memory.Write((ushort)(pia1Base + 1), 0x04);
        machine.Memory.Write((ushort)(pia1Base + 3), 0x04);

        // KeyA on the PET 2001 graphics keyboard sits at matrix row 4, column 0.
        machine.Keyboard.Press(4, 0);

        // Software selects row 4 by writing it to port A (PA0-3); port B then echoes that row's columns.
        machine.Memory.Write(pia1Base, 0x04);
        var columns = machine.Memory.Read((ushort)(pia1Base + 2));

        columns.Should().Be(machine.Keyboard.ReadColumns(4), "PIA1 port B should echo the selected row's live matrix state");
        (columns & 0x01).Should().Be(0, "column 0 is held down (active-low) once KeyA is pressed");

        machine.Keyboard.Release(4, 0);
        var afterRelease = machine.Memory.Read((ushort)(pia1Base + 2));
        (afterRelease & 0x01).Should().Be(0x01, "releasing the key should clear its active-low bit");
    }

    [Test]
    [CancelAfter(30_000)]
    public void RunUntil_StopsAsSoonAsConditionIsTrue()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        var met = machine.RunUntil(mem => ContainsReady(mem, PetProfileCatalog.Pet2001_8), 1_000_000);

        met.Should().BeTrue("the real ROM should print READY. well within a million instructions");
        machine.Processor.InstructionCount.Should().BeLessThan(1_000_000);
    }

    [TestCase("HELLO", new[] { "KeyH", "KeyE", "KeyL", "KeyL", "KeyO" })]
    [TestCase("A1 B", new[] { "KeyA", "Digit1", "Space", "KeyB" })]
    [TestCase("X+Y", new[] { "KeyX", "Equal", "KeyY" })]
    public void TextTyper_ToHostKey_MapsEveryCharacter(string text, string[] expectedHostKeys)
    {
        var actual = text.Select(TextTyper.ToHostKey).ToArray();
        actual.Should().Equal(expectedHostKeys);
    }

    [Test]
    public void TextTyper_UnmappableCharacter_IsSkippedNotThrown()
    {
        var act = () => TextTyper.ToHostKey('$');
        act.Should().NotThrow();
        TextTyper.ToHostKey('$').Should().BeNull();
    }

    /// <summary>
    /// Root cause was PetMachine driving the wrong chip's interrupt line entirely: the ~60Hz
    /// jiffy-clock pulse that KERNAL's keyboard scan depends on belongs on PIA1's CB1 (confirmed
    /// by a register-level comparison against personal-001's PetMachine.ClockPia1Cb1), not VIA's
    /// CA1 (an earlier, incorrect fix). Boots to a genuinely stable READY. via RunUntil (a fixed
    /// instruction-count guess previously produced a false pass from the boot banner still
    /// printing mid-test), holds KeyA for personal-001's own proven-sufficient budget (5_500
    /// instructions), and checks the screen actually changed.
    /// </summary>
    [Test]
    public void Keyboard_HeldKeyPress_ProducesVisibleScreenChange()
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();

        machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();
        var before = SnapshotScreen(machine, profile);

        foreach (var action in map.Translate("KeyA", HostKeyEventKind.Press))
        {
            if (action.Pressed) machine.Keyboard.Press(action.Row, action.Column);
            else machine.Keyboard.Release(action.Row, action.Column);
        }
        machine.Run(5_500); // personal-001's own proven-sufficient per-key budget
        foreach (var action in map.Translate("KeyA", HostKeyEventKind.Release))
        {
            if (action.Pressed) machine.Keyboard.Press(action.Row, action.Column);
            else machine.Keyboard.Release(action.Row, action.Column);
        }
        machine.Run(5_500);

        var after = SnapshotScreen(machine, profile);
        after.Should().NotEqual(before, "holding a real key through IPetKeyboardMap should change what BASIC has drawn on screen");
    }

    /// <summary>Same PIA1 CB1 fix as <see cref="Keyboard_HeldKeyPress_ProducesVisibleScreenChange"/>,
    /// exercised through the higher-level <see cref="TextTyper"/> the GUI's "type text" feature uses.</summary>
    [Test]
    public void TextTyper_TypedLine_ExecutesInBasic()
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();

        machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();
        var before = SnapshotScreen(machine, profile);

        TextTyper.Type(machine, map, "PRINT2+2\n");
        machine.Run(50_000);

        var after = SnapshotScreen(machine, profile);
        after.Should().Contain((byte)0x34, "PRINT2+2 should evaluate and print the digit '4' (PET screen code 0x34) somewhere on screen");
        after.Should().NotEqual(before, "typing PRINT2+2<Enter> should produce visible output");
    }

    private static bool ContainsReady(IMemoryBus memory, PetProfile profile)
    {
        for (var start = profile.VideoRamStart; start + ReadyBytes.Length <= profile.VideoRamStart + profile.VideoRamLength; start++)
        {
            var match = true;
            for (var j = 0; j < ReadyBytes.Length; j++)
            {
                if (memory.Read((ushort)(start + j)) != ReadyBytes[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    private static byte[] SnapshotScreen(PetMachine machine, PetProfile profile)
    {
        var bytes = new byte[profile.Columns * profile.Rows];
        for (ushort i = 0; i < bytes.Length; i++)
            bytes[i] = machine.Memory.Read((ushort)(profile.VideoRamStart + i));
        return bytes;
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }
}
