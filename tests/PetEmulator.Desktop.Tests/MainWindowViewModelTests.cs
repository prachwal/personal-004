using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Tests;

public sealed class MainWindowViewModelTests
{
    [Test]
    public void ModuleChoices_ExposeOneVic20Entry()
    {
        using var viewModel = new MainWindowViewModel(new StubFilePickerService());

        viewModel.ModuleChoices
            .Select(choice => choice.Label)
            .Where(label => label.StartsWith("VIC-20", StringComparison.Ordinal))
            .Should().Equal("VIC-20");
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
