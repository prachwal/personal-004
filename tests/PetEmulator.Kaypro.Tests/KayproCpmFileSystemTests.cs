using NUnit.Framework;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproCpmFileSystemTests
{
    [Test]
    public void ReadsDirectoryAndFileFromImportedCpmDisk()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "kaypro", "cpm22-rom149.dsk");
        var image = new KayproDiskImage(File.ReadAllBytes(path), firstSectorId: 0);
        var fileSystem = new KayproCpmFileSystem(image);

        var entries = fileSystem.ReadDirectory();

        Assert.That(entries, Is.Not.Empty);
        var entry = entries.First(value => value.SizeBytes > 0);
        Assert.That(fileSystem.ReadFile(entry.Name), Has.Length.EqualTo(entry.SizeBytes));
        Assert.That(fileSystem.ReadFileSectors(entry.Name), Is.Not.Empty);
        Assert.That(fileSystem.ReadSectorMap(), Has.Count.EqualTo(400));
    }

    [Test]
    public void CreateUpdateRenameDeleteAndSaveFile()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "kaypro", "cpm22-rom149.dsk");
        var image = new KayproDiskImage(File.ReadAllBytes(path), firstSectorId: 0);
        var fileSystem = new KayproCpmFileSystem(image);
        var original = Enumerable.Range(0, 256).Select(index => (byte)index).ToArray();

        fileSystem.CreateFile("CRUD.TST", original);
        Assert.That(fileSystem.ReadFile("CRUD.TST"), Is.EqualTo(original));

        var updated = Enumerable.Repeat((byte)0xA5, 128).ToArray();
        fileSystem.UpdateFile("CRUD.TST", updated);
        fileSystem.RenameFile("CRUD.TST", "DONE.TST");
        Assert.That(fileSystem.ReadFile("DONE.TST"), Is.EqualTo(updated));

        fileSystem.DeleteFile("DONE.TST");
        Assert.That(fileSystem.ReadDirectory().Any(entry => entry.Name == "DONE.TST"), Is.False);
        var reloaded = new KayproCpmFileSystem(new KayproDiskImage(image.ToArray(), firstSectorId: 0));
        Assert.That(reloaded.ReadDirectory().Any(entry => entry.Name == "DONE.TST"), Is.False);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
