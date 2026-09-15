using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Cli.Tests;

public sealed class Cpc464DebuggerSessionTests
{
    [Test]
    public void TouchingTheMachineBeforeRoms_ReturnsAnErrorInsteadOfThrowing()
    {
        var session = new Cpc464DebuggerSession();

        var result = session.Execute("status");

        result.Should().StartWith("error:").And.Contain("need 'roms'");
    }

    [Test]
    public void Roms_LazilyBuildsTheMachineOnFirstMachineTouchingCommand()
    {
        var session = new Cpc464DebuggerSession();
        session.Execute($"roms {RomsRoot()}");

        var status = session.Execute("status");

        status.Should().Contain("Amstrad CPC464").And.Contain("cycles=0");
    }

    [Test]
    public void Key_DrivesTheRawMatrixPositionOnTheRunningMachineInsteadOfRebuildingIt()
    {
        var session = new Cpc464DebuggerSession();
        session.Execute($"roms {RomsRoot()}");
        session.Execute("trace 5"); // advance the machine so a rebuild would be observable
        var cyclesBefore = ExtractCycles(session.Execute("status"));

        session.Execute("key 3 3 down").Should().Contain("key 3,3 down");

        var cyclesAfter = ExtractCycles(session.Execute("status"));
        cyclesAfter.Should().Be(cyclesBefore, "pressing a key must not reset the already-running machine");
    }

    [Test]
    public void Tape_HotSwapsIntoTheRunningMachineInsteadOfRebuildingIt()
    {
        var session = new Cpc464DebuggerSession();
        session.Execute($"roms {RomsRoot()}");
        session.Execute("trace 5");
        var cyclesBefore = ExtractCycles(session.Execute("status"));
        var tapePath = WriteSyntheticCdt();

        try
        {
            session.Execute($"tape {tapePath}").Should().Contain("tape loaded");

            var cyclesAfter = ExtractCycles(session.Execute("status"));
            cyclesAfter.Should().Be(cyclesBefore, "loading a tape must not reset the already-running machine");
        }
        finally
        {
            File.Delete(tapePath);
        }
    }

    private static long ExtractCycles(string status)
    {
        var marker = "cycles=";
        var start = status.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = status.IndexOf(' ', start);
        return long.Parse(status[start..end]);
    }

    /// <summary>A minimal CDT/TZX file: the "ZXTape!" magic, one version byte pair, and a single
    /// standard-speed (0x10) block with a handful of bytes - enough for Cpc464CdtImage.Parse to
    /// succeed without needing a real (licensed) game tape asset.</summary>
    private static string WriteSyntheticCdt()
    {
        byte[] data = [0xA5, 0x3C, 0x7E];
        var bytes = new List<byte>("ZXTape!"u8.ToArray()) { 0x1A, 1, 20, 0x10, 0, 0 };
        bytes.Add((byte)data.Length);
        bytes.Add((byte)(data.Length >> 8));
        bytes.AddRange(data);

        var path = Path.Combine(Path.GetTempPath(), $"cpc464-debugger-test-{Guid.NewGuid():N}.cdt");
        File.WriteAllBytes(path, bytes.ToArray());
        return path;
    }

    private static string RomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms");
            if (Directory.Exists(Path.Combine(candidate, "cpc464")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate roms/cpc464/ walking up from the test binary directory.");
    }
}
