using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Pet;

namespace PetEmulator.Desktop.Tests;

public sealed class MainWindowViewModelTests
{
    [Test]
    public void MachineViewModels_DeclareMediaAndAudioCapabilitiesExplicitly()
    {
        typeof(PetMachineViewModel).Should().Implement<ITapeViewModel>();
        typeof(Vic20MachineViewModel).Should().Implement<IDatasetteViewModel>();
        typeof(KayproMachineViewModel).Should().Implement<IDiskDriveViewModel>();
        typeof(Trs80MachineViewModel).Should().Implement<ITapeViewModel>();
        typeof(Cpc464MachineViewModel).Should().Implement<IDatasetteViewModel>();
        typeof(Cpc6128MachineViewModel).Should().Implement<IDatasetteViewModel>();
        typeof(Cpc6128MachineViewModel).Should().Implement<IDiskDriveViewModel>();

        typeof(PetMachineViewModel).Should().Implement<IMachineViewModel>();
        typeof(Vic20MachineViewModel).Should().Implement<IMachineViewModel>();
        typeof(KayproMachineViewModel).Should().Implement<IMachineViewModel>();
        typeof(Trs80MachineViewModel).Should().Implement<IMachineViewModel>();
        typeof(Cpc464MachineViewModel).Should().Implement<IMachineViewModel>();
        typeof(Cpc6128MachineViewModel).Should().Implement<IMachineViewModel>();
    }

    [Test]
    public void ModuleChoices_ExposeOneVic20Entry()
    {
        using var viewModel = new MainWindowViewModel(new StubFilePickerService());

        viewModel.ModuleChoices
            .Select(choice => choice.Label)
            .Where(label => label.StartsWith("VIC-20", StringComparison.Ordinal))
            .Should().Equal("VIC-20");
    }

    [Test]
    public void ModuleChoices_GroupPetAndCbmProfilesByFamily()
    {
        using var viewModel = new MainWindowViewModel(new StubFilePickerService());

        viewModel.ModuleChoices.Select(choice => choice.Label).Should().Equal(
            "PET 20xx", "CBM 30xx", "CBM 40xx", "CBM 80xx", "SuperPET", "VIC-20", "Kaypro II", "TRS-80 Model I", "Amstrad CPC464", "Amstrad CPC6128");
        viewModel.ModuleChoices[0].Children!.Select(choice => choice.Label).Should().Equal(
            PetProfileCatalog.Pet2001_8.Name,
            PetProfileCatalog.Pet2001_32.Name);
        viewModel.ModuleChoices[3].Children!.Select(choice => choice.Label).Should().Equal(
            PetProfileCatalog.Cbm8032.Name,
            PetProfileCatalog.Cbm8032Crtc80B50.Name,
            PetProfileCatalog.Cbm8016Converted80N50.Name,
            PetProfileCatalog.Converted80NUnknown.Name);
    }

    [Test]
    public void SuperPetMenu_ExposesTwoProcessorModes()
    {
        using var viewModel = new MainWindowViewModel(new StubFilePickerService());

        viewModel.SuperPetChoices.Select(choice => choice.Label).Should().Equal("6502", "6809");
        viewModel.ModuleChoices.Should().Contain(choice =>
            choice.Label == "SuperPET" && choice.Children == viewModel.SuperPetChoices);
        viewModel.ModuleChoices.Select(choice => choice.Label)
            .Should().NotContain(PetProfileCatalog.SuperPet6502.Name);
        viewModel.ModuleChoices.Select(choice => choice.Label)
            .Should().NotContain(PetProfileCatalog.SuperPet6809.Name);
    }

    [Test]
    public void ToolChoices_ExposeDeveloperAndMediaToolsSeparatelyFromMachines()
    {
        using var viewModel = new MainWindowViewModel(new StubFilePickerService());

        viewModel.ToolChoices.Select(choice => choice.Label).Should().Equal(
            "Chip Tester",
            "Media Tester",
            "Font / Glyph Viewer",
            "CPU Opcode Stepper",
            "Keyboard Matrix");
        viewModel.ModuleChoices.Select(choice => choice.Label)
            .Should().NotContain("Chip Tester");
    }

    private sealed class StubFilePickerService : IFilePickerService
    {
        public Task<string?> PickTapeToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickDiskToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickDiskToSaveAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickHostFileToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickHostFileToSaveAsync(string suggestedFileName) => Task.FromResult<string?>(null);
        public Task<string?> PickCartridgeToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickCartridgePluginToOpenAsync() => Task.FromResult<string?>(null);
    }
}
