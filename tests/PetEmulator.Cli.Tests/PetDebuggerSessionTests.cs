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
