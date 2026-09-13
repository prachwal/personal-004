using System.Security.Cryptography;
using NUnit.Framework;
using PetEmulator.Kaypro;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproTd0ReaderTests
{
    [Test]
    public void ReadsTheImportedKayproSystemDisk()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "kaypro", "kpii-149.td0");
        var bytes = File.ReadAllBytes(path);

        Assert.That(Convert.ToHexString(SHA256.HashData(bytes)), Is.EqualTo(
            "6CBA0285FD22126970FF46EE19A35EC20576980627496BECC5F69ACA5B7D926F"));

        var image = KayproTd0Reader.Read(new MemoryStream(bytes));

        Assert.That(image.ToArray(), Has.Length.EqualTo(KayproDiskImage.ImageSize));
        Assert.That(image.TryReadSector(0, 1, new byte[KayproDiskImage.SectorSizeBytes]), Is.True);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
