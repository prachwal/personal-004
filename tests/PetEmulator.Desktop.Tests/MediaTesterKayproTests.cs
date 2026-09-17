using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.Services.DiskImages;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Tests;

public sealed class MediaTesterKayproTests
{
    [Test]
    public void MediaTesterOpensKayproDskAsCpmInsteadOfD64()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "kaypro", "cpm22-rom149.dsk");
        var viewModel = new MediaTesterViewModel(new StubFilePickerService());

        viewModel.LoadDisk(path);

        viewModel.StatusFields.Should().ContainSingle().Which.Value.Should().Contain("CP/M");
        viewModel.Directory.Should().NotBeEmpty();
        viewModel.PreviewBytes.Should().NotBeEmpty();
        viewModel.DiskTitle.Should().Contain("Kaypro II");
    }

    [Test]
    public void RegistrySelectsD64ProviderForPetImage()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "pet", "test-disks", "games-1.d64");
        var document = new DiskImageProviderRegistry().Open(path);

        document.Title.Should().StartWith("Disk:");
        document.Files.Should().NotBeEmpty();
        document.Sectors.Should().NotBeEmpty();
    }

    [Test]
    public void MediaTesterOpensNewDos80Jv1ImageThroughTrs80Provider()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "trs80", "newdos80-sssd-system.jv1");
        var viewModel = new MediaTesterViewModel(new StubFilePickerService());

        viewModel.LoadDisk(path);

        viewModel.DiskTitle.Should().Contain("NEWDOS80");
        viewModel.Directory.Should().HaveCount(37);
        viewModel.Directory.Should().Contain(entry => entry.Name == "BOOT.SYS");
        viewModel.IsReadOnly.Should().BeTrue();
    }

    [Test]
    public void MediaTesterOpensLsDos6DmkImageThroughTrs80Provider()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "trs80", "lsdos63-model4-system.dmk");
        var viewModel = new MediaTesterViewModel(new StubFilePickerService());

        viewModel.LoadDisk(path);

        viewModel.DiskTitle.Should().Contain("L631NEW");
        viewModel.Directory.Should().HaveCount(42);
        viewModel.Directory.Should().Contain(entry => entry.Name == "BOOT.SYS");
        viewModel.IsReadOnly.Should().BeTrue();
    }

    [Test]
    public void FormatIsRecognizedByContentNotByFileExtension()
    {
        // The .dmk fixture is deliberately renamed to .dsk (Kaypro's own raw extension) to prove
        // the registry picks the provider whose Open() actually succeeds, not the one that merely
        // matched the extension.
        var repository = FindRepositoryRoot();
        var renamed = Path.Combine(Path.GetTempPath(), $"disguised-{Guid.NewGuid():N}.dsk");
        File.Copy(Path.Combine(repository, "roms", "trs80", "lsdos63-model4-system.dmk"), renamed);
        try
        {
            var document = new DiskImageProviderRegistry().Open(renamed);

            document.Title.Should().Contain("LS-DOS 6");
        }
        finally
        {
            File.Delete(renamed);
        }
    }

    [Test]
    public void PreviewSectorShowsRawSectorBytesRegardlessOfFileOwnership()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "kaypro", "cpm22-rom149.dsk");
        var viewModel = new MediaTesterViewModel(new StubFilePickerService());
        viewModel.LoadDisk(path);
        var sector = viewModel.SectorMap.First(s => s.IsAllocated);

        viewModel.PreviewSector(sector);

        viewModel.PreviewTitle.Should().Be($"Sector {sector.Track}/{sector.Sector}");
        viewModel.PreviewBytes.Should().NotBeEmpty();
        viewModel.StartAddress.Should().Contain($"Track {sector.Track}").And.Contain($"sector {sector.Sector}");
    }

    [Test]
    public void PreviewSectorWorksForTrs80FormatsToo()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "trs80", "newdos80-sssd-system.jv1");
        var viewModel = new MediaTesterViewModel(new StubFilePickerService());
        viewModel.LoadDisk(path);
        var sector = viewModel.SectorMap.First(s => s.IsAllocated);

        viewModel.PreviewSector(sector);

        viewModel.PreviewBytes.Should().HaveCount(256);
        viewModel.OperationStatus.Should().NotContain("read-only");
    }

    [Test]
    public void DirectoryCanBeFilteredAndSortedWithDiskSummary()
    {
        var path = Path.Combine(FindRepositoryRoot(), "roms", "kaypro", "cpm22-rom149.dsk");
        var viewModel = new MediaTesterViewModel(new StubFilePickerService());

        viewModel.LoadDisk(path);

        viewModel.FilteredDirectory.Should().HaveSameCount(viewModel.Directory);
        viewModel.TotalSectors.Should().BeGreaterThan(0);
        viewModel.DiskSummary.Should().Contain("blocks free").And.Contain("directory");

        var filter = viewModel.Directory.First().Name[..Math.Min(3, viewModel.Directory.First().Name.Length)];
        viewModel.FilterText = filter;
        viewModel.FilteredDirectory.Should().OnlyContain(entry =>
            entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

        viewModel.FilterText = "";
        viewModel.SortByCommand.Execute("Size");
        viewModel.FilteredDirectory.Should().BeInAscendingOrder(entry => entry.SizeInSectors);
    }

    [Test]
    public void ImportAndExportUseHostFileContent()
    {
        var repository = FindRepositoryRoot();
        var imagePath = Path.Combine(repository, "roms", "kaypro", "cpm22-rom149.dsk");
        var importPath = Path.Combine(Path.GetTempPath(), $"kaypro-import-{Guid.NewGuid():N}.bin");
        var exportPath = Path.Combine(Path.GetTempPath(), $"kaypro-export-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(importPath, [0x41, 0x42, 0x43]);
        try
        {
            var viewModel = new MediaTesterViewModel(new StubFilePickerService(exportPath));
            viewModel.LoadDisk(imagePath);
            viewModel.ImportFile(importPath);

            viewModel.FileName.Should().Be(Path.GetFileName(importPath).ToUpperInvariant());
            viewModel.ContentBytes.Should().Equal(0x41, 0x42, 0x43);
            viewModel.ContentDump.Should().Contain("41 42 43");
            viewModel.IsEditPanelVisible.Should().BeTrue();

            viewModel.SelectedEntry = viewModel.Directory.First();
            viewModel.ExportFileCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            File.Exists(exportPath).Should().BeTrue();
        }
        finally
        {
            File.Delete(importPath);
            File.Delete(exportPath);
        }
    }

    [Test]
    public void HexDumpValidationRejectsMalformedByteWithoutLosingLastValidBytes()
    {
        var viewModel = new MediaTesterViewModel(new StubFilePickerService());
        viewModel.ContentDump = "0000  41 42 43  |ABC             |";

        viewModel.ContentBytes.Should().Equal(0x41, 0x42, 0x43);
        viewModel.ValidationError.Should().BeEmpty();

        viewModel.ContentDump = "0000  41 GG 43  |A.              |";

        viewModel.ValidationError.Should().Contain("invalid byte");
        viewModel.ContentBytes.Should().Equal(0x41, 0x42, 0x43);
    }

    [Test]
    public async Task OpenDiskReportsUnrecognizedImageInsteadOfThrowingThroughUi()
    {
        // 17 bytes matches no provider's content signature (not a valid Kaypro/D64/TRS-80 image
        // regardless of its .dsk extension) - content-based detection should reject it cleanly
        // rather than crash the UI.
        var invalidPath = Path.Combine(Path.GetTempPath(), $"invalid-kaypro-{Guid.NewGuid():N}.dsk");
        File.WriteAllBytes(invalidPath, new byte[17]);
        try
        {
            var viewModel = new MediaTesterViewModel(new StubFilePickerService(openPath: invalidPath));

            await viewModel.OpenDiskCommand.ExecuteAsync(null);

            viewModel.StatusFields.Should().ContainSingle().Which.Value.Should().Contain("Unable to open");
            viewModel.OperationStatus.Should().Contain("No disk image provider recognized");
            viewModel.HasImage.Should().BeFalse();
        }
        finally
        {
            File.Delete(invalidPath);
        }
    }

    [Test]
    public async Task ViewModelExecutesCpmCrudAndSaveCommands()
    {
        var repository = FindRepositoryRoot();
        var imagePath = Path.Combine(repository, "roms", "kaypro", "cpm22-rom149.dsk");
        var savePath = Path.Combine(Path.GetTempPath(), $"kaypro-crud-{Guid.NewGuid():N}.dsk");
        var picker = new StubFilePickerService(savePath);
        try
        {
            var viewModel = new MediaTesterViewModel(picker);
            viewModel.LoadDisk(imagePath);
            viewModel.FileName = "CRUD.TST";
            viewModel.ContentDump = MediaTesterViewModel.FormatHexDump(Enumerable.Repeat((byte)0xAA, 128).ToArray());
            viewModel.CreateFileCommand.Execute(null);
            viewModel.Directory.Should().Contain(entry => entry.Name == "CRUD.TST");

            viewModel.ContentDump = MediaTesterViewModel.FormatHexDump(Enumerable.Repeat((byte)0xBB, 128).ToArray());
            viewModel.UpdateFileCommand.Execute(null);
            viewModel.NewFileName = "DONE.TST";
            viewModel.RenameFileCommand.Execute(null);
            viewModel.OperationStatus.Should().Be("Renamed CRUD.TST to DONE.TST");
            viewModel.Directory.Should().Contain(entry => entry.Name == "DONE.TST");

            viewModel.FileName = "DONE.TST";
            viewModel.DeleteFileCommand.Execute(null);
            viewModel.Directory.Should().NotContain(entry => entry.Name == "DONE.TST");

            await viewModel.SaveDiskCommand.ExecuteAsync(null);
            File.Exists(savePath).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(savePath))
                File.Delete(savePath);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed class StubFilePickerService : IFilePickerService
    {
        private readonly string? _savePath;
        private readonly string? _openPath;

        public StubFilePickerService(string? savePath = null, string? openPath = null)
        {
            _savePath = savePath;
            _openPath = openPath;
        }

        public Task<string?> PickTapeToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickDiskToOpenAsync() => Task.FromResult(_openPath);
        public Task<string?> PickDiskToSaveAsync() => Task.FromResult(_savePath);
        public Task<string?> PickHostFileToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickHostFileToSaveAsync(string suggestedFileName) => Task.FromResult(_savePath);
        public Task<string?> PickCartridgeToOpenAsync() => Task.FromResult<string?>(null);
        public Task<string?> PickCartridgePluginToOpenAsync() => Task.FromResult<string?>(null);
    }
}
