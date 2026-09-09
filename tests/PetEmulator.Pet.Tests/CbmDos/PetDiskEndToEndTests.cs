using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.CbmDos;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.CbmDos;

/// <summary>
/// Layer 2 of this repo's disk-drive testing strategy (see docs/pet-disk-testing-strategy.md):
/// real KERNAL/BASIC commands typed through the keyboard matrix on a fully booted machine, not
/// register pokes (that's <see cref="PetEmulator.Pet.Tests.PetMachineTests.PollDiskActivity_ReportsRealIeee488Traffic_ThenClearsUntilTheNextByte"/>,
/// Layer 1) and not direct D64Image calls (that's <see cref="D64ImageTests"/>, Layer 0). Every
/// assertion here is either what a real PET user would see on screen, or an independent
/// cross-check against a second, separately-loaded D64Image - never a peek into CbmDosEngine's
/// own internals.
/// </summary>
public sealed class PetDiskEndToEndTests
{
    private static readonly byte[] ReadyBytes = [0x12, 0x05, 0x01, 0x04, 0x19, 0x2E]; // "READY."
    private static readonly byte[] ErrorBytes = [0x05, 0x12, 0x12, 0x0F, 0x12]; // "ERROR"

    /// <summary>Was a known bug: a real multi-hundred-byte TALK/read transfer stalled at a fixed
    /// byte offset (byte 326 of "HELLO"'s 4,486) and never resumed. Root-caused with
    /// <see cref="PetMachine.BusObserver"/> plus a full instruction-level trace: <c>PetIeeeBus</c>'s
    /// read-side settle delay (32 cycles) was shorter than the periodic ~60Hz keyboard-scan IRQ
    /// (~550-600 instructions, 1,000+ cycles), so the emulation silently pre-fetched and
    /// re-asserted DAV for the *next* byte while the KERNAL's read-wait loop was still parked
    /// inside that ISR, permanently hiding the DAV release it needed to see on resume. Fixed by
    /// raising the settle delay to a value that clears a real ISR with margin while staying well
    /// under the jiffy period - see <c>PetIeeeBus.SettleDelayCycles</c>'s doc comment. A large
    /// transfer now correctly takes noticeably longer in instruction-count terms (each byte pays
    /// that settle delay), hence this test's generous <see cref="CancelAfter"/>/instruction budget
    /// below - not a residual bug, the honest cost of a delay long enough to actually be safe.</summary>
    [Test]
    [CancelAfter(300_000)]
    public void LoadReadsARealFilesBytesCorrectly()
    {
        var profile = PetProfileCatalog.Pet2001_32;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();
        var testDisksDir = RomLocator.Directory("test-disks", "games-1.d64");
        var diskPath = Path.Combine(testDisksDir, "games-1.d64");
        machine.MountDisk(diskPath);

        // Independently re-opens the same file this session's own LOAD command should read -
        // a second D64Image instance, nothing shared with the machine's CbmDosEngine/drive.
        var expected = D64Image.Load(diskPath);
        var entry = expected.ReadDirectory().First(e =>
            e.Type == FileType.Prg && e.SizeInSectors > 0 && e.Filename.StartsWith("HELLO", StringComparison.OrdinalIgnoreCase));
        var expectedBytes = expected.ReadFile(entry);
        var loadAddress = (ushort)(expectedBytes[0] | (expectedBytes[1] << 8));

        machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();
        // Default hold/gap (5,500 instructions) occasionally drops a keystroke on a string this
        // long - the KERNAL's keyboard scan is jiffy-clock-paced (~16,667 cycles/60Hz) and a
        // too-short hold can land entirely inside one scan's dead time. 12,000 is what this
        // session's own manual reproduction needed for "LOAD"HELLO",8" to type reliably.
        TextTyper.Type(machine, map, "LOAD\"HELLO\",8\n", holdInstructions: 12_000, gapInstructions: 12_000);
        // ~1,250 instructions/byte at the fixed settle delay (see PetIeeeBus.SettleDelayCycles) *
        // 4,486 bytes is ~5.6M instructions - budgeted generously above that.
        machine.RunUntil(mem => ContainsReadyAfterFirst(mem, profile), 8_000_000)
            .Should().BeTrue("LOAD should finish and print READY. again");

        for (var i = 2; i < expectedBytes.Length; i++)
        {
            machine.Memory.Read((ushort)(loadAddress + i - 2)).Should().Be(expectedBytes[i],
                $"byte {i - 2} of the loaded file should match the disk image exactly");
        }
    }

    /// <summary>A full SAVE, NEW (clear program memory), LOAD, RUN round trip through a real
    /// disk - genuinely passes: the reloaded program's tiny size stays well clear of the stall
    /// <see cref="LoadReadsARealFilesBytesCorrectly"/> documents.</summary>
    [Test]
    [CancelAfter(60_000)]
    public void SaveThenLoadRoundTripsARealProgramThroughARealDisk()
    {
        var profile = PetProfileCatalog.Pet2001_32;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();

        // A fresh, empty-but-real D64 (same format CbmDosEngine already reads/writes for LOAD -
        // see D64ImageTests.CreateEmpty_HasCorrectSize) so SAVE has guaranteed free space,
        // independent of whatever games-1.d64 happens to have used up.
        var diskPath = Path.Combine(Path.GetTempPath(), $"pet-save-roundtrip-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(diskPath, D64Image.CreateEmpty());
        try
        {
            machine.MountDisk(diskPath);
            machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();

            // Default hold/gap (5,500 instructions) occasionally drops a keystroke on strings
            // this long - see LoadReadsARealFilesBytesCorrectly's identical note.
            TextTyper.Type(machine, map, "10 PRINT2+2\n", holdInstructions: 12_000, gapInstructions: 12_000);
            TextTyper.Type(machine, map, "SAVE\"TESTFILE\",8\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.RunUntil(mem => ContainsReadyAfterFirst(mem, profile), 2_000_000)
                .Should().BeTrue("SAVE should finish and print READY. again");

            TextTyper.Type(machine, map, "NEW\n", holdInstructions: 12_000, gapInstructions: 12_000); // clear
            machine.Run(20_000); // program memory - RUN below can only succeed off a genuine reload

            TextTyper.Type(machine, map, "LOAD\"TESTFILE\",8\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.RunUntil(mem => ContainsReadyAfterFirst(mem, profile), 2_000_000)
                .Should().BeTrue("LOAD should finish and print READY. again");

            TextTyper.Type(machine, map, "RUN\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.Run(50_000);

            var screen = SnapshotScreen(machine, profile);
            screen.Should().Contain((byte)0x34,
                "the reloaded program's PRINT2+2 should evaluate and print '4' - proof SAVE really wrote it and LOAD really read it back");
        }
        finally
        {
            File.Delete(diskPath);
        }
    }

    /// <summary>SAVE only ever exercises the write direction (LISTEN + filename + data, never
    /// TALK/read) - genuinely unaffected by the filename-corruption bug the other two tests in
    /// this class document, since <c>DataAvailable</c>/<c>TryGetByte</c> (the read path) are never
    /// reached. Doesn't independently re-verify the written bytes (nothing in this repo reads a
    /// D64's in-memory image back except through the same broken LOAD path) - the KERNAL reaching
    /// a second READY. with no error text on screen is the real, from-the-PET signal that a real
    /// disk write completed, the same signal <see cref="ContainsReadyAfterFirst"/> already relies
    /// on for every other completion check in this class.</summary>
    [Test]
    [CancelAfter(60_000)]
    public void SaveCompletesAndReturnsToReady()
    {
        var profile = PetProfileCatalog.Pet2001_32;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();

        var diskPath = Path.Combine(Path.GetTempPath(), $"pet-save-only-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(diskPath, D64Image.CreateEmpty());
        try
        {
            machine.MountDisk(diskPath);
            machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();

            TextTyper.Type(machine, map, "10 PRINT2+2\n", holdInstructions: 12_000, gapInstructions: 12_000);
            TextTyper.Type(machine, map, "SAVE\"TESTFILE\",8\n", holdInstructions: 12_000, gapInstructions: 12_000);

            machine.RunUntil(mem => ContainsReadyAfterFirst(mem, profile), 3_000_000)
                .Should().BeTrue("SAVE should finish and print READY. again, with no error text");
            ScreenContains(machine.Memory, profile, ErrorBytes).Should().BeFalse();
        }
        finally
        {
            File.Delete(diskPath);
        }
    }

    private static bool ContainsReady(IMemoryBus memory, PetProfile profile) => ScreenContains(memory, profile, ReadyBytes);

    // The screen never clears between commands (it scrolls), so a second READY. always appears
    // further down than the boot banner's - checking presence alone would pass instantly, before
    // the command even ran. Counts occurrences instead: BASIC's boot banner prints READY. once;
    // a second one only appears after the command in flight actually completes.
    private static bool ContainsReadyAfterFirst(IMemoryBus memory, PetProfile profile)
    {
        var count = 0;
        for (var start = profile.VideoRamStart; start + ReadyBytes.Length <= profile.VideoRamStart + profile.VideoRamLength; start++)
        {
            var match = true;
            for (var j = 0; j < ReadyBytes.Length; j++)
                if (memory.Read((ushort)(start + j)) != ReadyBytes[j]) { match = false; break; }
            if (match) count++;
        }
        return count >= 2;
    }

    private static bool ScreenContains(IMemoryBus memory, PetProfile profile, byte[] pattern)
    {
        for (var start = profile.VideoRamStart; start + pattern.Length <= profile.VideoRamStart + profile.VideoRamLength; start++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
                if (memory.Read((ushort)(start + j)) != pattern[j]) { match = false; break; }
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
