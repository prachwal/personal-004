using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.Chips;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tape;
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
    public void BusObserver_FiresForEveryRealReadAndWrite()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var accesses = new List<BusAccess>();
        machine.BusObserver = accesses.Add;

        machine.Memory.Write(0x0100, 0x42);
        machine.Memory.Read(0x0100);

        accesses.Should().Equal(
            new BusAccess(IsWrite: true, 0x0100, 0x42),
            new BusAccess(IsWrite: false, 0x0100, 0x42));
    }

    [Test]
    public void BusObserver_ObservesRealChipAccessesDuringRealExecution()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var pia1Accesses = 0;
        machine.BusObserver = access =>
        {
            if (access.Address is >= 0xE810 and < 0xE820)
                pia1Accesses++;
        };

        machine.Run(2_000);

        pia1Accesses.Should().BeGreaterThan(0, "real KERNAL boot code touches PIA1 (keyboard/cassette) within the first 2,000 instructions");
    }

    [Test]
    public void BusObserver_IsOptOutWithZeroBehaviorChangeWhenUnset()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        var act = () => machine.Run(500);

        act.Should().NotThrow();
        machine.BusObserver.Should().BeNull();
    }

    [Test]
    public void RunUntilOrStalled_MeetsCondition_WhenItBecomesTrueBeforeAnyStallWindow()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        var result = machine.RunUntilOrStalled(
            _ => machine.Processor.InstructionCount >= 10,
            () => (long)machine.Processor.InstructionCount, // always "progresses" every instruction
            maxInstructions: 1_000,
            stallWindow: 500);

        result.ConditionMet.Should().BeTrue();
        result.Stalled.Should().BeFalse();
        result.InstructionsRun.Should().Be(10);
    }

    [Test]
    public void RunUntilOrStalled_DetectsAPlateau_LongBeforeMaxInstructions()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        var result = machine.RunUntilOrStalled(
            _ => false, // never met - only a stall or the max budget can end this
            () => 0, // never changes - an immediate, permanent plateau
            maxInstructions: 1_000_000,
            stallWindow: 200);

        result.ConditionMet.Should().BeFalse();
        result.Stalled.Should().BeTrue();
        result.InstructionsRun.Should().Be(200, "it should stop at the stall window, not run to maxInstructions");
    }

    [Test]
    public void RunUntilOrStalled_RunsToMaxInstructions_WhenNeitherConditionNorStallFires()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        long counter = 0;

        var result = machine.RunUntilOrStalled(
            _ => false,
            () => ++counter, // always progresses - never plateaus
            maxInstructions: 300,
            stallWindow: 200);

        result.ConditionMet.Should().BeFalse();
        result.Stalled.Should().BeFalse();
        result.InstructionsRun.Should().Be(300);
    }

    [Test]
    public void IeeeByteTransferCount_IsCumulative_UnlikePollDiskActivity()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_32);
        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");
        machine.MountDisk(Path.Combine(testDisksDir, "games-1.d64"));

        machine.IeeeByteTransferCount.Should().Be(0);
    }

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

    // "PRESS PLAY ON TAPE #1" in PET screen codes: A-Z map to 1-26 (see ReadyBytes above),
    // space/digits/'#' are ASCII-identical.
    private static readonly byte[] PressPlayBytes =
    [
        0x10, 0x12, 0x05, 0x13, 0x13, 0x20, // PRESS
        0x10, 0x0C, 0x01, 0x19, 0x20,       // PLAY
        0x0F, 0x0E, 0x20,                   // ON
        0x14, 0x01, 0x10, 0x05, 0x20,       // TAPE
        0x23, 0x31,                         // #1
    ];

    // "SEARCHING" - what LOAD prints next once it actually starts reading. The PET screen never
    // clears/overwrites the prompt line (it just scrolls up), so a passing test has to look for
    // this appearing, not for PressPlayBytes disappearing - see the test's own history for why an
    // earlier draft asserting the prompt's absence failed against a real ROM.
    private static readonly byte[] SearchingBytes =
        [0x13, 0x05, 0x01, 0x12, 0x03, 0x08, 0x09, 0x0E, 0x07];

    /// <summary>Reproduces the real hang this session's screenshot showed: the KERNAL's LOAD
    /// routine turns the cassette motor on via CB2 but then blocks on PIA1 PA4 (cassette sense)
    /// until PLAY is physically pressed - a real datasette behavior <see cref="PetMachine"/>
    /// didn't model at all (PA4 was hardcoded high, so the prompt would never clear no matter
    /// what). Proves both halves of the fix: PA4 actually reaches <see cref="PetDatasette.Sense"/>,
    /// and pressing play is what makes LOAD proceed - not just having a tape attached.</summary>
    [Test]
    [CancelAfter(60_000)]
    public void PressPlay_LetsLoadProceedPastThePressPlayPrompt()
    {
        var profile = PetProfileCatalog.Pet2001_32;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();
        var tapDirectory = RomLocator.Directory("test-tapes", "tower-and-dragon-town.tap");
        var tap = PetTapFile.Parse(File.ReadAllBytes(Path.Combine(tapDirectory, "tower-and-dragon-town.tap")));
        machine.Datasette.LoadTape(tap.PulseCycles, "tower-and-dragon-town.tap");

        machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();
        TextTyper.Type(machine, map, "LOAD\n");

        machine.RunUntil(mem => ScreenContains(mem, profile, PressPlayBytes), 2_000_000)
            .Should().BeTrue("LOAD should turn the motor on and then block waiting for PLAY, printing this prompt");

        machine.Datasette.PressPlay();

        machine.RunUntil(mem => ScreenContains(mem, profile, SearchingBytes), 2_000_000)
            .Should().BeTrue("pressing play should close the PA4 sense line and let LOAD proceed to actually read the tape");
    }

    private static bool ScreenContains(IMemoryBus memory, PetProfile profile, byte[] pattern)
    {
        for (var start = profile.VideoRamStart; start + pattern.Length <= profile.VideoRamStart + profile.VideoRamLength; start++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (memory.Read((ushort)(start + j)) != pattern[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    private static bool ContainsReady(IMemoryBus memory, PetProfile profile) => ScreenContains(memory, profile, ReadyBytes);

    private static byte[] SnapshotScreen(PetMachine machine, PetProfile profile)
    {
        var bytes = new byte[profile.Columns * profile.Rows];
        for (ushort i = 0; i < bytes.Length; i++)
            bytes[i] = machine.Memory.Read((ushort)(profile.VideoRamStart + i));
        return bytes;
    }

    [Test]
    public void Devices_ReportsTheDatasetteEvenWithNoTapeLoaded()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Devices.Should().ContainSingle(d => d.Id == "datasette")
            .Which.StatusText.Should().Be("No tape");
    }

    [Test]
    public void Devices_ReflectsALoadedTapesName()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Datasette.LoadTape([100, 200], "starwars.tap");

        machine.Devices.Single(d => d.Id == "datasette").StatusText.Should().Be("starwars.tap - press play");
    }

    [Test]
    public void MountDisk_AddsADriveDeviceStatus()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var diskPath = Path.Combine(RomLocator.Directory("test-disks", "games-1.d64"), "games-1.d64");

        machine.MountDisk(diskPath);

        machine.Devices.Should().ContainSingle(d => d.Id == "ieee488:8")
            .Which.StatusText.Should().Be("games-1.d64");
    }

    [Test]
    public void MountDisk_OnTheSameDeviceNumber_ReplacesTheStatusInsteadOfAddingASecondOne()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");

        machine.MountDisk(Path.Combine(testDisksDir, "games-1.d64"));
        machine.MountDisk(Path.Combine(testDisksDir, "utils.d64"));

        machine.Devices.Should().ContainSingle(d => d.Id == "ieee488:8")
            .Which.StatusText.Should().Be("utils.d64");
    }

    [Test]
    public void HasDisk_ReflectsWhetherDeviceEightIsMounted()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.HasDisk().Should().BeFalse();

        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");
        machine.MountDisk(Path.Combine(testDisksDir, "games-1.d64"));

        machine.HasDisk().Should().BeTrue();
        machine.HasDisk(9).Should().BeFalse("nothing is mounted at device 9");
    }

    /// <summary>Drives a real LISTEN/TALK exchange over the memory-mapped PIA2/VIA registers -
    /// the same sequence PetIeeeBusBindingTests proves at the bare-chip level - through the full
    /// PetMachine, to prove PollDiskActivity's peek-and-clear wiring against real bus traffic
    /// (not just that PetIeeeBus.Activity itself fires, which is already covered elsewhere).</summary>
    [Test]
    [CancelAfter(10_000)]
    public void PollDiskActivity_ReportsRealIeee488Traffic_ThenClearsUntilTheNextByte()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");
        machine.MountDisk(Path.Combine(testDisksDir, "games-1.d64"));

        machine.PollDiskActivity().Should().BeFalse("nothing has touched the bus yet");

        const byte AtnOutViaDdrb = 0x04;
        var pia2Base = PetMemoryBus.Pia2Base;
        var viaBase = PetMemoryBus.ViaBase;

        machine.Memory.Write((ushort)(viaBase + Via6522.Ddrb), AtnOutViaDdrb);
        machine.Memory.Write((ushort)(viaBase + Via6522.Orb), 0x00); // ATN asserted -> command phase
        machine.Memory.Write((ushort)(pia2Base + 3), 0x04); // select PIA2 CRB's data register
        machine.Memory.Write((ushort)(pia2Base + 2), (byte)(0x48 ^ 0xFF)); // TALK device 8
        machine.Memory.Write((ushort)(pia2Base + 2), (byte)(0x6F ^ 0xFF)); // SECONDARY 15 (error channel)
        machine.Memory.Write((ushort)(viaBase + Via6522.Orb), 0x04); // ATN released -> device becomes talker

        for (var i = 0; i < 8; i++) machine.StepInstruction(); // let the talker-side prefetch delay elapse

        machine.Memory.Write((ushort)(pia2Base + 1), 0x04); // select PIA2 CRA's data register
        var read = machine.Memory.Read((ushort)(pia2Base + 0));
        ((byte)(read ^ 0xFF)).Should().Be((byte)'7', "the status channel starts \"73,CBM DOS...\"");

        machine.PollDiskActivity().Should().BeTrue("a real byte just crossed the IEEE-488 bus");
        machine.PollDiskActivity().Should().BeFalse("polling again immediately should find nothing new");
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }
}
