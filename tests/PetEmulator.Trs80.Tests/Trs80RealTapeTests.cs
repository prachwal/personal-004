using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Trs80.Tests;

/// <summary>Exercises Trs80CassettePlayer against the real captured tape and the blank tape under
/// roms/trs80/test-tapes/ - see that directory's README.md for provenance.</summary>
public sealed class Trs80RealTapeTests
{
    [Test]
    public void RealSwampCasTicksThroughWithoutExceptionAndProducesPulses()
    {
        var cassette = new Trs80CassettePlayer(File.ReadAllBytes(FixturePath("swamp.cas")));
        cassette.Write(Trs80MemoryMap.CassettePort, 0x04); // motor on

        var sawPulse = false;
        var tStates = 0;
        const int budget = 500_000_000;
        while (!cassette.AtEndOfTape && tStates < budget)
        {
            cassette.Tick(100_000);
            tStates += 100_000;
            if ((cassette.Read(Trs80MemoryMap.CassettePort) & 0x80) != 0)
                sawPulse = true;
        }

        cassette.AtEndOfTape.Should().BeTrue("a 500,000,000 T-state budget is generous for a 5.4KB tape at 500bps");
        sawPulse.Should().BeTrue("the encoding should have produced at least one EAR rising edge");
    }

    [Test]
    public void BlankCasIsAtEndOfTapeImmediately()
    {
        var cassette = new Trs80CassettePlayer(File.ReadAllBytes(FixturePath("blank.cas")));

        cassette.AtEndOfTape.Should().BeTrue();

        cassette.Write(Trs80MemoryMap.CassettePort, 0x04); // motor on - must not throw on an empty tape
        cassette.Tick(1_000);
        cassette.AtEndOfTape.Should().BeTrue();
    }

    private static string FixturePath(string name) =>
        Path.Combine(FindRepositoryRoot(), "roms", "trs80", "test-tapes", name);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
