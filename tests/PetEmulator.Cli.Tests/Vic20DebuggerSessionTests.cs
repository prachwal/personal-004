using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Cartridge.Sample;
using PetEmulator.Pet.CbmDos;

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

    [Test]
    public void Disk_MountsAnExistingD64()
    {
        var path = TemporaryDiskPath();
        File.WriteAllBytes(path, D64Image.CreateFormatted("VICDISK", "00"));
        try
        {
            var session = new Vic20DebuggerSession();
            session.Execute($"roms {RomsRoot()}");

            session.Execute($"disk {path}").Should().Contain("device 8");
            session.Execute("devices").Should().Contain(Path.GetFileName(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void NewDisk_CreatesAndMountsAFormattedD64()
    {
        var path = TemporaryDiskPath();
        try
        {
            var session = new Vic20DebuggerSession();
            session.Execute($"roms {RomsRoot()}");

            session.Execute($"new-disk {path} VICNEW 9").Should().Contain("device 9");
            File.Exists(path).Should().BeTrue();
            D64Image.Load(path).DiskName.Trim().Should().Be("VICNEW");
            session.Execute("devices").Should().Contain("Drive 9");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Cartridge_MountsARawBinary()
    {
        var path = Path.Combine(Path.GetTempPath(), $"vic20-cartridge-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(path, [0x42, 0x43]);
        try
        {
            var session = new Vic20DebuggerSession();
            session.Execute($"roms {RomsRoot()}");

            session.Execute($"cartridge {path}").Should().Contain("cartridge mounted");
            session.Execute("eject-cartridge").Should().Be("cartridge ejected");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void CartridgePlugin_MountsPluginAndImage()
    {
        var imagePath = Path.Combine(Path.GetTempPath(), $"vic20-plugin-image-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(imagePath, [0x42]);
        var pluginPath = typeof(SampleCartridgePlugin).Assembly.Location;
        try
        {
            var session = new Vic20DebuggerSession();
            session.Execute($"roms {RomsRoot()}");

            session.Execute($"cartridge-plugin {pluginPath} {imagePath}")
                .Should().Contain("cartridge plugin mounted");
            session.Execute("eject-cartridge").Should().Be("cartridge ejected");
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    [Test]
    public void Disk_BeforeRoms_ReturnsAnErrorInsteadOfThrowing()
    {
        var session = new Vic20DebuggerSession();

        session.Execute("disk missing.d64").Should().StartWith("error:").And.Contain("need 'roms'");
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

    private static string TemporaryDiskPath() => Path.Combine(Path.GetTempPath(), $"vic20-cli-disk-{Guid.NewGuid():N}.d64");
}
