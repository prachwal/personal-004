using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Trs80.Tests;

/// <summary>Verified against a real NEWDOS/80 v2.0 system disk (roms/trs80/newdos80-sssd-system.jv1):
/// a decoded reference dump of this fixture's directory was cross-checked file-by-file (name, size,
/// extent/granule fields) before the reader was written.</summary>
public sealed class NewDos80FileSystemTests
{
    private static string FixturePath => Path.Combine(FindRepositoryRoot(), "roms", "trs80", "newdos80-sssd-system.jv1");

    [Test]
    public void ReadsDiskNameAndDirectoryTrack()
    {
        var fs = new NewDos80FileSystem(Jv1DiskImage.Load(FixturePath));

        fs.DiskName.Should().Be("NEWDOS80");
        fs.DirectoryTrack.Should().Be(17);
    }

    [Test]
    public void ReadsFullDirectoryWithKnownSystemFiles()
    {
        var fs = new NewDos80FileSystem(Jv1DiskImage.Load(FixturePath));

        var entries = fs.ReadDirectory();

        entries.Should().HaveCount(37);
        entries.Should().ContainSingle(e => e.FileName == "BOOT.SYS" && e.SizeInSectors == 5);
        entries.Should().ContainSingle(e => e.FileName == "SUPERZAP.CMD" && e.SizeInSectors == 31);
        entries.Should().ContainSingle(e => e.FileName == "DIRCHECK.CMD" && e.SizeInSectors == 15);
        entries.Where(e => e.Extension == "SYS" && e.Name.StartsWith("SYS")).Should().HaveCount(22);
    }

    [Test]
    public void ReadsFileContentMatchingDeclaredSize()
    {
        var fs = new NewDos80FileSystem(Jv1DiskImage.Load(FixturePath));
        var entry = fs.ReadDirectory().Single(e => e.FileName == "BOOT.SYS");

        var content = fs.ReadFile(entry);

        content.Should().HaveCount(entry.SizeInBytes);
        // BOOT/SYS is the disk's Z80 boot loader; every NEWDOS/80-formatted disk starts with 00H.
        content[0].Should().Be(0x00);
    }

    [Test]
    public void ReadsFileSectorsWithinDiskBounds()
    {
        var disk = Jv1DiskImage.Load(FixturePath);
        var fs = new NewDos80FileSystem(disk);
        var entry = fs.ReadDirectory().Single(e => e.FileName == "SUPERZAP.CMD");

        var sectors = fs.ReadFileSectors(entry);

        sectors.Should().HaveCount(entry.SizeInSectors);
        sectors.Should().OnlyContain(s => s.Track >= 0 && s.Track < disk.Tracks && s.Sector >= 0 && s.Sector < Jv1DiskImage.SectorsPerTrack);
        sectors.Should().NotContain(s => s.Track == fs.DirectoryTrack);
    }

    [Test]
    public void SectorMapCoversEveryTrackAndMarksDirectoryTrackAllocated()
    {
        var disk = Jv1DiskImage.Load(FixturePath);
        var fs = new NewDos80FileSystem(disk);

        var map = fs.ReadSectorMap();

        map.Should().HaveCount(disk.Tracks * Jv1DiskImage.SectorsPerTrack);
        map.Where(s => s.Track == fs.DirectoryTrack).Should().OnlyContain(s => s.IsAllocated);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
