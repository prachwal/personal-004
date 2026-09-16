using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Cli.Tests;

public sealed class Cpc6128DebuggerSessionTests
{
    [Test]
    public void TouchingTheMachineBeforeRoms_ReturnsAnErrorInsteadOfThrowing()
    {
        new Cpc6128DebuggerSession().Execute("status").Should().StartWith("error:").And.Contain("need 'roms'");
    }

    [Test]
    public void Roms_LazilyBuildsTheCpc6128Machine()
    {
        var session = new Cpc6128DebuggerSession();
        session.Execute($"roms {RomsRoot()}");

        session.Execute("status").Should().Contain("Amstrad CPC6128").And.Contain("cycles=0");
    }

    [Test]
    public void Key_DrivesTheRunningMachineWithoutRebuildingIt()
    {
        var session = new Cpc6128DebuggerSession();
        session.Execute($"roms {RomsRoot()}");
        session.Execute("trace 5");
        var cycles = ExtractCycles(session.Execute("status"));

        session.Execute("key 3 3 down").Should().Contain("key 3,3 down");
        ExtractCycles(session.Execute("status")).Should().Be(cycles);
    }

    [Test]
    public void Tape_HotSwapsIntoTheRunningMachine()
    {
        var session = new Cpc6128DebuggerSession();
        session.Execute($"roms {RomsRoot()}");
        session.Execute("trace 5");
        var cycles = ExtractCycles(session.Execute("status"));
        var path = WriteSyntheticCdt();
        try
        {
            session.Execute($"tape {path}").Should().Contain("tape loaded");
            ExtractCycles(session.Execute("status")).Should().Be(cycles);
        }
        finally { File.Delete(path); }
    }

    private static long ExtractCycles(string status)
    {
        const string marker = "cycles=";
        var start = status.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        return long.Parse(status[start..status.IndexOf(' ', start)]);
    }

    private static string WriteSyntheticCdt()
    {
        byte[] data = [0xA5, 0x3C, 0x7E];
        var bytes = new List<byte>("ZXTape!"u8.ToArray()) { 0x1A, 1, 20, 0x10, 0, 0 };
        bytes.Add((byte)data.Length); bytes.Add((byte)(data.Length >> 8)); bytes.AddRange(data);
        var path = Path.Combine(Path.GetTempPath(), $"cpc6128-debugger-test-{Guid.NewGuid():N}.cdt");
        File.WriteAllBytes(path, bytes.ToArray());
        return path;
    }

    private static string RomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms");
            if (Directory.Exists(Path.Combine(candidate, "cpc6128"))) return candidate;
        }
        throw new DirectoryNotFoundException("Could not locate roms/cpc6128/ walking up from the test binary directory.");
    }
}
