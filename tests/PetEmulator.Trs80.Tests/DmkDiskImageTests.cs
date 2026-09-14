using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Trs80.Tests;

public sealed class DmkDiskImageTests
{
    private static string FixturePath => Path.Combine(FindRepositoryRoot(), "roms", "trs80", "lsdos63-model4-system.dmk");

    [Test]
    public void ParsesHeaderGeometry()
    {
        var disk = DmkDiskImage.Load(FixturePath);

        disk.Tracks.Should().Be(40);
        disk.Sides.Should().Be(2);
        disk.SectorsPerSide.Should().Be(18);
    }

    [Test]
    public void ReadsRawSectorViaIdamAndDataAddressMark()
    {
        var disk = DmkDiskImage.Load(FixturePath);

        var sector = disk.ReadSector(track: 0, side: 0, sector: 0);

        sector.Should().HaveCount(256);
        sector[0].Should().Be(0x00);
        sector[1].Should().Be(0xFE);
    }

    [Test]
    public void SecondSideOfEveryTrackIsUnformattedOnThisSingleSidedDump()
    {
        var disk = DmkDiskImage.Load(FixturePath);

        Action act = () => disk.ReadSector(track: 0, side: 1, sector: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void LooksLikeDmkAcceptsRealFixtureAndRejectsGarbage()
    {
        DmkDiskImage.LooksLikeDmk(File.ReadAllBytes(FixturePath), out _).Should().BeTrue();
        DmkDiskImage.LooksLikeDmk(new byte[300], out var reason).Should().BeFalse();
        reason.Should().NotBeEmpty();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
