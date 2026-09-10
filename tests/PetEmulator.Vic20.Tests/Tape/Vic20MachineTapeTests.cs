using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Vic20.Tests.Tape;

/// <summary>Real-KERNAL-level proof that <see cref="Vic20Datasette"/>'s wiring is correct - mirrors
/// PET's own <c>PetMachineTests.PressPlay_LetsLoadProceedPastThePressPlayPrompt</c> in scope:
/// proves the machine reacts correctly to a tape being attached and LOAD being typed (motor
/// engages, KERNAL prints "SEARCHING"), the same level PET's own equivalent test settles for.
///
/// Deliberately does NOT assert a full byte-for-byte KERNAL decode of a synthetic tape into RAM -
/// unlike PET's tape stack (verified against a real captured Datasette recording,
/// roms/pet/test-tapes/tower-and-dragon-town.tap), this repo has no real captured VIC-20 tape to
/// test against, and this session's own investigation into the real KERNAL's byte-level pulse
/// timing (docs/vic20-tape.md) did not converge on a synthetic pulse stream the real ROM fully
/// decodes. See that doc for what's confirmed (the hardware wiring) versus open (exact
/// pulse-width/leader-length the real KERNAL's decoder expects).</summary>
public sealed class Vic20MachineTapeTests
{
    [Test]
    [CancelAfter(30_000)]
    public void LoadWithATapeAttached_TurnsTheMotorOnAndReachesSearching()
    {
        var machine = new Vic20Machine(RomsRoot());
        machine.RunUntil(_ => machine.Vic.Columns > 0 && machine.Vic.Rows > 0, 2_000_000).Should().BeTrue("must boot first");
        machine.Run(200_000); // let the KERNAL fully settle its keyboard IRQ before typing - see docs/vic20-tape.md

        // A real tape's exact pulse content doesn't matter for this test - only that motor/sense
        // wiring reacts correctly to LOAD once *something* is attached and playing.
        machine.Datasette.LoadTape([500, 500, 500, 500], "smoke-test.tap");
        machine.Datasette.HasTape.Should().BeTrue();

        Vic20TextTyper.Type(machine, "LOAD\n");
        machine.Run(300_000);

        machine.Datasette.MotorOn.Should().BeTrue("LOAD should turn the cassette motor on immediately, before it even needs PLAY");
        ScreenContains(machine, SearchingBytes).Should().BeTrue("LOAD should print SEARCHING once it starts hunting for a tape header");
    }

    [Test]
    public void Devices_ReportsTheDatasetteEvenWithNoTapeLoaded()
    {
        var machine = new Vic20Machine(RomsRoot());

        machine.Devices.Should().ContainSingle(d => d.Id == "datasette")
            .Which.StatusText.Should().Be("No tape");
    }

    [Test]
    public void Devices_ReflectsALoadedTapesName()
    {
        var machine = new Vic20Machine(RomsRoot());

        machine.Datasette.LoadTape([100, 200], "starwars.tap");

        machine.Devices.Single(d => d.Id == "datasette").StatusText.Should().Be("starwars.tap - press play");
    }

    // "SEARCHING" - the KERNAL screen codes for the word it prints while hunting for a header
    // (screen-code letters: A=1..Z=26).
    private static readonly byte[] SearchingBytes = [19, 5, 1, 18, 3, 8, 9, 14, 7];

    private static bool ScreenContains(Vic20Machine machine, byte[] pattern)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        var screenAddr = machine.Vic.ScreenAddr;
        for (var start = 0; start + pattern.Length <= cols * rows; start++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (machine.Memory.Read((ushort)(screenAddr + start + j)) != pattern[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    private static string RomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms", "vic20");
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate roms/vic20/ walking up from the test binary directory.");
    }
}
