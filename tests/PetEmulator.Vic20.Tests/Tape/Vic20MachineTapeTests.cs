using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.Tape;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Vic20.Tests.Tape;

/// <summary>Real-KERNAL-level proof that <see cref="Vic20Datasette"/>'s wiring is correct - see
/// docs/vic20/tape.md for the investigation (an earlier, wrong VIA1-only wiring guess is why an
/// earlier version of this file only proved the machine reaches "SEARCHING", never a full
/// decode). <see cref="LoadDecodesARealTapeFileByteForByte"/> is the real proof: a genuine header
/// (filename, load address) plus payload bytes, round-tripped through the real KERNAL's own tape
/// decoder into RAM.</summary>
public sealed class Vic20MachineTapeTests
{
    [Test]
    [CancelAfter(60_000)]
    public void LoadDecodesARealTapeFileByteForByte()
    {
        var tapDirectory = RomLocatorDirectory("test-tapes", "hello-vic.tap");
        var tap = PetTapFile.Parse(File.ReadAllBytes(Path.Combine(tapDirectory, "hello-vic.tap")));

        var machine = new Vic20Machine(RomsRoot());
        machine.RunUntil(_ => machine.Vic.Columns > 0 && machine.Vic.Rows > 0, 2_000_000).Should().BeTrue("must boot first");
        machine.Run(200_000); // let the KERNAL fully settle its keyboard IRQ before typing

        machine.Datasette.LoadTape(tap.PulseCycles, "hello-vic.tap");
        // Real hardware order: PLAY is pressed before LOAD is typed (or the real KERNAL blocks on
        // "PRESS PLAY ON TAPE" - see CSTEL in docs/vic20/tape.md) - both work, this just avoids
        // needing to also assert the prompt appears and gets dismissed.
        machine.Datasette.PressPlay();

        Vic20TextTyper.Type(machine, "LOAD\n");
        machine.Run(15_000_000);

        ScreenContains(machine, FoundBytes).Should().BeTrue("LOAD should find the real header (filename HELLOVIC)");

        // The tape's first 3 payload bytes are a documented sacrificial pad (see
        // roms/vic20/test-tapes/README.md) - only the real payload from there on needs to match.
        byte[] expected = "HELLO VIC"u8.ToArray();
        var actual = new byte[expected.Length];
        for (var i = 0; i < actual.Length; i++)
            actual[i] = machine.Memory.Read((ushort)(0x1000 + 3 + i));

        actual.Should().Equal(expected, "the real KERNAL should have decoded the exact payload bytes into RAM at the header's load address");
    }

    [Test]
    [CancelAfter(60_000)]
    public void SaveThenLoadRoundTripsTheRealProgramBytes()
    {
        // Real proof, not a false positive: corrupts the target RAM with garbage between SAVE and
        // LOAD, so a match can only mean LOAD genuinely rewrote it - a program byte that happened
        // to survive untouched would look identical whether or not LOAD ever ran (this file's own
        // history already hit exactly that trap once - see docs/vic20/tape.md).
        var machine = new Vic20Machine(RomsRoot());
        machine.RunUntil(_ => machine.Vic.Columns > 0 && machine.Vic.Rows > 0, 2_000_000).Should().BeTrue("must boot first");
        machine.Run(200_000);

        machine.Datasette.NewBlankTape("MYPROG");
        machine.Datasette.PressPlay();
        Vic20TextTyper.Type(machine, "10 A=5\n", holdInstructions: 8000, gapInstructions: 8000);
        machine.Run(200_000);
        machine.Datasette.WriteRecorder.Begin();
        Vic20TextTyper.Type(machine, "SAVE\n", holdInstructions: 8000, gapInstructions: 8000);
        machine.Run(3_000_000);
        var recordedWritePulses = machine.Datasette.WriteRecorder.End();

        recordedWritePulses.Should().NotBeEmpty("the real SAVE routine must drive VIA2 PB3");
        recordedWritePulses.Should().OnlyContain(width => width > 0);

        const ushort ProgramStart = 0x1000;
        var saved = new byte[8];
        for (var i = 0; i < saved.Length; i++) saved[i] = machine.Memory.Read((ushort)(ProgramStart + i));
        saved.Should().NotBeEquivalentTo(new byte[8], "SAVE should have written a real, non-zero tokenized program");

        for (var i = 0; i < saved.Length; i++) machine.Memory.Write((ushort)(ProgramStart + i), 0xFF);

        // A real power-cycle with the tape still in the deck - Vic20Machine.Reset() rewinds the
        // datasette but doesn't touch its content (see Vic20Datasette.Reset).
        machine.Reset();
        machine.RunUntil(_ => machine.Vic.Columns > 0 && machine.Vic.Rows > 0, 2_000_000).Should().BeTrue("must reboot");
        machine.Run(200_000);
        machine.Datasette.PressPlay();
        Vic20TextTyper.Type(machine, "LOAD\n");
        machine.Run(8_000_000);

        var loaded = new byte[8];
        for (var i = 0; i < loaded.Length; i++) loaded[i] = machine.Memory.Read((ushort)(ProgramStart + i));

        loaded.Should().Equal(saved, "LOAD should have decoded the exact program SAVE wrote, overwriting the garbage");
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

    // "FOUND" - what LOAD prints once it locates a matching header (screen-code letters: A=1..Z=26).
    private static readonly byte[] FoundBytes = [6, 15, 21, 14, 4];

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

    private static string RomsRoot() => RomLocatorDirectory(null, null);

    private static string RomLocatorDirectory(string? subDir, string? file)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var romsVic20 = Path.Combine(dir.FullName, "roms", "vic20");
            if (!Directory.Exists(romsVic20))
                continue;
            return subDir is null ? romsVic20 : Path.Combine(romsVic20, subDir);
        }

        throw new DirectoryNotFoundException("Could not locate roms/vic20/ walking up from the test binary directory.");
    }
}
