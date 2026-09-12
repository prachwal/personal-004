using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Vic20.Cartridge.Sample;
using PetEmulator.Vic20;

namespace PetEmulator.Desktop.Tests;

public sealed class CartridgeMountViewModelTests
{
    [Test]
    public void LoadPlugin_MountsImageAndRefreshesRows()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vic20-desktop-plugin-test-");
        var imagePath = Path.Combine(temporaryDirectory.FullName, "sample.bin");

        try
        {
            File.WriteAllBytes(imagePath, [0x42]);
            var machine = new Vic20MachineViewModel(RomsRoot());
            var viewModel = new CartridgeMountViewModel(machine, new StubFilePickerService());
            viewModel.SelectedPluginPath = typeof(SampleCartridgePlugin).Assembly.Location;
            viewModel.SelectedImagePath = imagePath;

            viewModel.LoadPluginCommand.Execute(null);

            viewModel.ErrorMessage.Should().BeNull();
            viewModel.LoadedCartridges.Should().ContainSingle(row => row.Name == "sample.bin");
        }
        finally
        {
            temporaryDirectory.Delete(true);
        }
    }

    [Test]
    public void LoadPlugin_ShowsConflictAndDoesNotAddRow()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vic20-desktop-plugin-test-");
        var imagePath = Path.Combine(temporaryDirectory.FullName, "sample.bin");

        try
        {
            File.WriteAllBytes(imagePath, [0x42]);
            var machine = new Vic20MachineViewModel(RomsRoot());
            machine.LoadCartridgePlugin(
                Path.Combine(AppContext.BaseDirectory, "PetEmulator.Vic20.Cartridge.Ram.35K.dll"),
                Path.Combine(RomsRoot(), "cartridges", "vic20-ram-35k.bin"));
            var viewModel = new CartridgeMountViewModel(machine, new StubFilePickerService());
            viewModel.SelectedPluginPath = typeof(SampleCartridgePlugin).Assembly.Location;
            viewModel.SelectedImagePath = imagePath;

            viewModel.LoadPluginCommand.Execute(null);

            viewModel.ErrorMessage.Should().Contain("overlap");
            viewModel.LoadedCartridges.Should().ContainSingle(row => row.Name == "vic20-ram-35k.bin");
        }
        finally
        {
            temporaryDirectory.Delete(true);
        }
    }

    private static string RomsRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "roms", "vic20");
            if (File.Exists(Path.Combine(candidate, "kernal.bin")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate roms/vic20/.");
    }

    private sealed class StubFilePickerService : IFilePickerService
    {
        public Task<string?> PickTapeToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickDiskToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickDiskToSaveAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickCartridgeToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickCartridgePluginToOpenAsync() => Task.FromResult<string?>(null);
    }
}
