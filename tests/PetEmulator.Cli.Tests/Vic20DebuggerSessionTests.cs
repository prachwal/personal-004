using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Cli.Tests;

public sealed class Vic20DebuggerSessionTests
{
    [Test]
    public void TouchingTheMachineBeforeRoms_ReturnsAnErrorInsteadOfThrowing()
    {
        var session = new Vic20DebuggerSession();

        var result = session.Execute("status");

        result.Should().StartWith("error:").And.Contain("need 'roms'");
    }

    [Test]
    public void Roms_LazilyBuildsTheMachineOnFirstMachineTouchingCommand()
    {
        var session = new Vic20DebuggerSession();
        session.Execute($"roms {RomsRoot()}");

        var status = session.Execute("status");

        status.Should().Contain("VIC-20").And.Contain("cycles=0");
    }

    [Test]
    public void Type_DrivesTheKeyboardMatrixThroughTheDefaultHostMap()
    {
        var session = new Vic20DebuggerSession();
        session.Execute($"roms {RomsRoot()}");

        var result = session.Execute("type HELLO");

        result.Should().Be("typed 5 character(s)");
    }

    [Test]
    public void UnrecognizedCommand_DelegatesToTheGenericMachineDebugger()
    {
        var session = new Vic20DebuggerSession();
        session.Execute($"roms {RomsRoot()}");

        var result = session.Execute("trace 1");

        result.Should().Contain("cycles=").And.Contain("PC=");
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
