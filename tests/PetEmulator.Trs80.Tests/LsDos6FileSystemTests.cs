using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Trs80.Tests;

/// <summary>Verified against a real LS-DOS 6.3 Model 4 disk (roms/trs80/lsdos63-model4-system.dmk):
/// the directory-cylinder pointer, GAT DOS-version byte, and a decoded directory dump were
/// cross-checked against the official "Programmer's Guide to TRSDOS Version 6" before this
/// reader was written.</summary>
public sealed class LsDos6FileSystemTests
{
    private static string FixturePath => Path.Combine(FindRepositoryRoot(), "roms", "trs80", "lsdos63-model4-system.dmk");

    [Test]
    public void ReadsDiskNameAndDirectoryTrack()
    {
        var fs = new LsDos6FileSystem(DmkDiskImage.Load(FixturePath));

        fs.DiskName.Should().Be("L631NEW");
        fs.DirectoryTrack.Should().Be(20);
        fs.SectorsPerTrack.Should().Be(18);
    }

    [Test]
    public void ReadsFullDirectoryWithKnownSystemFiles()
    {
        var fs = new LsDos6FileSystem(DmkDiskImage.Load(FixturePath));

        var entries = fs.ReadDirectory();

        entries.Should().HaveCount(42);
        entries.Should().ContainSingle(e => e.FileName == "BOOT.SYS" && e.SizeInSectors == 14);
        entries.Should().ContainSingle(e => e.FileName == "BASIC.CMD" && e.SizeInSectors == 91);
        entries.Should().ContainSingle(e => e.FileName == "MODELA.III");
        entries.Where(e => e.Extension == "SYS" && e.Name.StartsWith("SYS")).Should().HaveCount(14);
    }

    [Test]
    public void ReadsFileContentMatchingDeclaredSize()
    {
        var fs = new LsDos6FileSystem(DmkDiskImage.Load(FixturePath));
        var entry = fs.ReadDirectory().Single(e => e.FileName == "BOOT.SYS");

        var content = fs.ReadFile(entry);

        content.Should().HaveCount(entry.SizeInBytes);
        content[0].Should().Be(0x00);
        content[1].Should().Be(0xFE);
    }

    [Test]
    public void ReadsFileSectorsWithinDiskBounds()
    {
        var disk = DmkDiskImage.Load(FixturePath);
        var fs = new LsDos6FileSystem(disk);
        var entry = fs.ReadDirectory().Single(e => e.FileName == "BASIC.CMD");

        var sectors = fs.ReadFileSectors(entry);

        sectors.Should().HaveCount(entry.SizeInSectors);
        sectors.Should().OnlyContain(s => s.Track >= 0 && s.Track < disk.Tracks && s.Sector >= 0 && s.Sector < fs.SectorsPerTrack);
    }

    [Test]
    public void SectorMapCoversEveryTrack()
    {
        var disk = DmkDiskImage.Load(FixturePath);
        var fs = new LsDos6FileSystem(disk);

        var map = fs.ReadSectorMap();

        map.Should().HaveCount(disk.Tracks * fs.SectorsPerTrack);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
