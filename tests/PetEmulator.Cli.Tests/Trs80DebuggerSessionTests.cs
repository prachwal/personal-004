using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Cli.Tests;

public sealed class Trs80DebuggerSessionTests
{
    [Test]
    public void TouchingTheMachineBeforeRoms_ReturnsAnErrorInsteadOfThrowing()
    {
        var session = new Trs80DebuggerSession();

        var result = session.Execute("status");

        result.Should().StartWith("error:").And.Contain("need 'roms'");
    }

    [Test]
    public void Roms_LazilyBuildsTheMachineOnFirstMachineTouchingCommand()
    {
        var session = new Trs80DebuggerSession();
        session.Execute($"roms {RomsRoot()}");

        var status = session.Execute("status");

        status.Should().Contain("TRS-80").And.Contain("cycles=0");
    }

    [Test]
    public void Disk_HotSwapsIntoTheRunningMachineInsteadOfRebuildingIt()
    {
        var session = new Trs80DebuggerSession();
        session.Execute($"roms {RomsRoot()}");
        session.Execute("trace 5"); // advance the machine so a rebuild would be observable
        var cyclesBefore = ExtractCycles(session.Execute("status"));

        session.Execute($"disk {DiskPath()}").Should().Contain("disk mounted");

        var cyclesAfter = ExtractCycles(session.Execute("status"));
        cyclesAfter.Should().Be(cyclesBefore, "mounting a disk must not reset the already-running machine");
    }

    [Test]
    public void Tape_HotSwapsIntoTheRunningMachineInsteadOfRebuildingIt()
    {
        var session = new Trs80DebuggerSession();
        session.Execute($"roms {RomsRoot()}");
        session.Execute("trace 5");
        var cyclesBefore = ExtractCycles(session.Execute("status"));

        session.Execute($"tape {TapePath()}").Should().Contain("tape loaded");

        var cyclesAfter = ExtractCycles(session.Execute("status"));
        cyclesAfter.Should().Be(cyclesBefore, "loading a tape must not reset the already-running machine");
    }

    private static long ExtractCycles(string status)
    {
        var marker = "cycles=";
        var start = status.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = status.IndexOf(' ', start);
        return long.Parse(status[start..end]);
    }

    private static string RomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms");
            if (Directory.Exists(Path.Combine(candidate, "trs80")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate roms/trs80/ walking up from the test binary directory.");
    }

    private static string DiskPath() => Path.Combine(RomsRoot(), "trs80", "newdos80-sssd-system.jv1");
    private static string TapePath() => Path.Combine(RomsRoot(), "trs80", "test-tapes", "swamp.cas");
}
