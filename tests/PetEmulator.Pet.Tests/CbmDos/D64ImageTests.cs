using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.CbmDos;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.CbmDos;

/// <summary>Ported from personal-001's D64ImageTests. LoadGames1/LoadUtils/ReadFile cases were
/// skipped in the initial port for lack of roms/pet/test-disks/{games-1,utils}.d64 - those
/// fixtures are now imported, so they're ported here too.</summary>
public sealed class D64ImageTests
{
    [Test]
    public void LoadGames1_DiskName()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDirectory, "games-1.d64"));
        img.DiskName.Should().Be("GAMES-1");
        img.DiskId.Should().Be("AE");
        img.DosType.Should().Be("2A");
    }

    [Test]
    public void LoadGames1_DirectoryHasFiles()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDirectory, "games-1.d64"));
        var dir = img.ReadDirectory();
        dir.Should().HaveCountGreaterThan(30);
    }

    [Test]
    public void LoadGames1_ContainsAce()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDirectory, "games-1.d64"));
        var dir = img.ReadDirectory();
        dir.Should().Contain(e => e.Filename.Contains("ACE", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void LoadGames1_ContainsBattleship()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDirectory, "games-1.d64"));
        var dir = img.ReadDirectory();
        dir.Should().Contain(e => e.Filename.Contains("BATTLESHIP", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void LoadUtils_DiskName()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDirectory, "utils.d64"));
        img.DiskName.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void LoadUtils_DirectoryHasFiles()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDirectory, "utils.d64"));
        var dir = img.ReadDirectory();
        dir.Should().HaveCountGreaterThan(20);
    }

    [Test]
    public void ReadFile_ReturnsData()
    {
        var img = D64Image.Load(Path.Combine(TestDisksDirectory, "games-1.d64"));
        var dir = img.ReadDirectory();

        var prg = dir.FirstOrDefault(e => e.Type == FileType.Prg);
        prg.Should().NotBe(default, "at least one PRG should exist");

        byte[] data = img.ReadFile(prg);
        data.Length.Should().BeGreaterThan(2);
        data[0].Should().BeGreaterThan(0, "PRG load address low byte should be non-zero");
    }

    [Test]
    public void TrackSectorToOffset_StandardPositions()
    {
        D64Image.TrackSectorToOffset(18, 0).Should().Be(0x16500);
        D64Image.TrackSectorToOffset(1, 0).Should().Be(0);
        D64Image.TrackSectorToOffset(18, 1).Should().Be(0x16500 + 256);
        D64Image.TrackSectorToOffset(35, 16).Should().Be(174592);
        (D64Image.TrackSectorToOffset(35, 16) + 255).Should().Be(174847);
    }

    [Test]
    public void CreateEmpty_HasCorrectSize()
    {
        var data = D64Image.CreateEmpty();
        data.Length.Should().Be(174848);
    }

    private static string TestDisksDirectory => RomLocator.Directory("test-disks", "games-1.d64");
}
