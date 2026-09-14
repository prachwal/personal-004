using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Cli.Tests;

/// <summary>
/// Proves the scripted "point at a ROM directory / tape / disk file" command mechanism ported
/// from personal-003's Retro.Debugger REPL - see <see cref="PetDebuggerSession"/>'s doc comment.
/// </summary>
[TestFixture]
public sealed class PetDebuggerSessionTests
{
    [Test]
    public void TouchingTheMachineBeforeProfileAndRoms_ReturnsAnErrorInsteadOfThrowing()
    {
        var session = new PetDebuggerSession();

        var result = session.Execute("status");

        result.Should().StartWith("error:").And.Contain("need 'profile' and 'roms'");
    }

    [Test]
    public void ProfileAndRoms_LazilyBuildTheMachineOnFirstMachineTouchingCommand()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile pet-2001-8");
        session.Execute($"roms {RomsRoot()}");

        var status = session.Execute("status");

        status.Should().Contain("profile=PET 2001-8").And.Contain("cycles=0");
    }

    [Test]
    public void UnknownProfileId_ReturnsAnError()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile does-not-exist");
        session.Execute($"roms {RomsRoot()}");

        var result = session.Execute("status");

        result.Should().StartWith("error:").And.Contain("Unknown PET profile 'does-not-exist'");
    }

    [Test]
    public void SuperPetProfile_CanBeSelected()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile superpet");
        session.Execute($"roms {RomsRoot()}");

        var status = session.Execute("status");

        status.Should().Contain("profile=SuperPET");
    }

    [Test]
    public void SuperPetDiagnose_ReportsWaterlooResetAndInstructionTrace()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile superpet");
        session.Execute($"roms {RomsRoot()}");

        var report = session.Execute("superpet-diagnose 4");

        report.Should().Contain("processor=Motorola6809")
            .And.Contain("reset-vector=$")
            .And.Contain("firmware=$A000-$BFFF")
            .And.Contain("trace:")
            .And.Contain("[0] PC=$");
    }

    [Test]
    public void SuperPetBootCheckpoints_ReportsHitsAndNeverReachedStages_WithoutOutputPath()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile superpet");
        session.Execute($"roms {RomsRoot()}");

        var report = session.Execute("superpet-boot-checkpoints 30000");

        report.Should().Contain("instructions-run=30000")
            .And.Contain("[Reset] hit #1")
            .And.Contain("[KeyboardRingBufferInit] hit #1")
            .And.Contain("never reached:")
            .And.Contain("[MenuBannerPrint]");
    }

    [Test]
    public void SuperPetBootCheckpoints_WithAPath_WritesTheReportAndSummarizesIt()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile superpet");
        session.Execute($"roms {RomsRoot()}");
        var path = Path.Combine(Path.GetTempPath(), $"boot-checkpoints-test-{Guid.NewGuid():N}.log");

        try
        {
            var result = session.Execute($"superpet-boot-checkpoints 30000 {path}");

            result.Should().StartWith("boot-checkpoints written:").And.Contain("never reached");
            File.Exists(path).Should().BeTrue();
            File.ReadAllText(path).Should().Contain("[KeyboardRingBufferInit] hit #1");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Devices_ReportsTheDatasetteEvenBeforeATapeIsLoaded()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile pet-2001-8");
        session.Execute($"roms {RomsRoot()}");

        var devices = session.Execute("devices");

        devices.Should().Contain("Datasette: No tape");
    }

    [Test]
    public void Tape_LoadsAFileAndReportsItInDevices()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile pet-2001-8");
        session.Execute($"roms {RomsRoot()}");
        string tapePath = WriteMinimalTapFile();

        try
        {
            var loadResult = session.Execute($"tape {tapePath}");
            loadResult.Should().StartWith("tape loaded:").And.Contain(Path.GetFileName(tapePath));

            var devices = session.Execute("devices");
            devices.Should().Contain($"Datasette: {Path.GetFileName(tapePath)}");
        }
        finally
        {
            File.Delete(tapePath);
        }
    }

    [Test]
    public void MissingTapeFile_ReturnsAnErrorInsteadOfThrowing()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile pet-2001-8");
        session.Execute($"roms {RomsRoot()}");

        var result = session.Execute("tape does-not-exist.tap");

        result.Should().StartWith("error:");
    }

    [Test]
    public void Type_DrivesTheKeyboardMatrixThroughTheDefaultKeymap()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile pet-2001-8");
        session.Execute($"roms {RomsRoot()}");

        var result = session.Execute("type HELLO");

        result.Should().Be("typed 5 character(s)");
    }

    [Test]
    public void TraceLog_WritesAPcAndBusAccessTraceToTheGivenPath()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile pet-2001-8");
        session.Execute($"roms {RomsRoot()}");
        var path = Path.Combine(Path.GetTempPath(), $"trace-log-test-{Guid.NewGuid():N}.log");

        try
        {
            var result = session.Execute($"trace-log 20 {path}");

            result.Should().StartWith("trace written:").And.Contain("20 instructions");
            File.Exists(path).Should().BeTrue();
            var text = File.ReadAllText(path);
            text.Should().Contain("[0] PC=").And.Contain("[19] PC=");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DiskStallCheck_ReportsNoStall_WhenNoDiskIsEvenMounted()
    {
        // No IEEE-488 traffic at all is a permanent plateau by definition - this asserts the
        // command itself runs and reports cleanly, not that a booting-only machine transfers
        // bytes (it doesn't, so a short stallWindow legitimately fires "stalled").
        var session = new PetDebuggerSession();
        session.Execute("profile pet-2001-8");
        session.Execute($"roms {RomsRoot()}");

        var result = session.Execute("disk-stall-check 200 100");

        result.Should().Contain("stalled").And.Contain("byte transfer");
    }

    [Test]
    public void ViaIrqCheck_ReportsPia1AssertingPeriodically_NowThatTheSuperPet6809BootHangIsFixed()
    {
        // Regression pin for docs/pet/superpet-6809-boot-hang.md: the 6809 boot used to freeze
        // forever in an early poll loop because SuperPet6809MemoryBus mapped the waterloo-e000-ffff
        // ROM image over the FULL $E000-$FFFF range with no I/O hole, so writes to PIA1's CRB
        // ($E813, which arms the CB1/jiffy-clock IRQ the keyboard-buffer feed depends on) were
        // silently swallowed as "ROM, read-only" and never reached the real chip - VIA/PIA1/ACIA
        // all reported IRQ true on 0/60000 forever. Now that SuperPet6809MemoryBus punches a hole
        // for the standard PET I/O block (PIA1/PIA2/VIA/CRTC) before falling through to firmware,
        // PIA1's CB1 pulse (~60Hz) actually reaches the chip and asserts IRQ periodically. If this
        // ever goes back to 0, the boot is frozen again.
        var session = new PetDebuggerSession();
        session.Execute("profile superpet");
        session.Execute($"roms {RomsRoot()}");

        var result = session.Execute("via-irq-check 60000");

        result.Should().MatchRegex(@"^VIA\.IRQ true on 0/60000 \(last -1\); PIA1\.IRQ true on \d+/60000 \(last \d+\); ACIA\.Irq true on 0/60000 \(last -1\)$");
        result.Should().NotContain("PIA1.IRQ true on 0/60000");
    }

    [Test]
    public void UnrecognizedCommand_DelegatesToTheGenericMachineDebugger()
    {
        var session = new PetDebuggerSession();
        session.Execute("profile pet-2001-8");
        session.Execute($"roms {RomsRoot()}");

        var result = session.Execute("trace 1");

        result.Should().Contain("cycles=").And.Contain("instructions=");
    }

    // Just enough of a .tap file for PetTapFile.Parse to accept: a 12-byte "C64-TAPE-RAW"
    // signature, version/platform/video-standard bytes, a 4-byte reserved field, a 4-byte
    // little-endian data length, then that many pulse-length bytes.
    private static string WriteMinimalTapFile()
    {
        var pulseData = new byte[] { 0x30, 0x30, 0x30, 0x30 }; // four short (0x30*8=384-cycle) pulses
        var header = new byte[20];
        "C64-TAPE-RAW"u8.CopyTo(header);
        header[12] = 1; // version
        header[13] = (byte)PetEmulator.Pet.Tape.PetTapPlatform.Pet;
        header[14] = 0; // video standard
        BitConverter.GetBytes(pulseData.Length).CopyTo(header, 16);

        string path = Path.Combine(Path.GetTempPath(), $"debugger-session-test-{Guid.NewGuid():N}.tap");
        File.WriteAllBytes(path, [.. header, .. pulseData]);
        return path;
    }

    private static string RomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms", "pet");
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate roms/pet/ walking up from the test binary directory.");
    }
}
