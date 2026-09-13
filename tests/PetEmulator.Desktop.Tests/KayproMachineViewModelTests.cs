using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Tests;

public sealed class KayproMachineViewModelTests
{
    [Test]
    public void KayproProfileLoadsMonitorFontAndDefaultDisk()
    {
        using var viewModel = new KayproMachineViewModel(FindRomsRoot());

        viewModel.DiskLoaded.Should().BeTrue();
        viewModel.PixelWidth.Should().Be(640);
        viewModel.PixelHeight.Should().Be(240);
        viewModel.DiskIconBrush.Should().NotBeNull();
    }

    private static string FindRomsRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "roms");
            if (File.Exists(Path.Combine(candidate, "kaypro", "kaypro-81-146.bin")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Repository roms/kaypro assets were not found.");
    }
}
