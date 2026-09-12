using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Cartridge.Sample;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20CartridgePluginTests
{
    [Test]
    public void Loader_LoadsOnePluginFromDll()
    {
        var plugin = Vic20CartridgePluginLoader.Load(typeof(SampleCartridgePlugin).Assembly.Location);

        plugin.Descriptor.Id.Should().Be("sample-8k-io");
        plugin.Descriptor.Resources.Should().Contain(resource => resource.StartAddress == 0xA000);
        plugin.Descriptor.Resources.Should().Contain(resource => resource.StartAddress == 0x9800);
    }

    [Test]
    public void Loader_RejectsAssemblyWithoutPlugin()
    {
        var action = () => Vic20CartridgePluginLoader.Load(typeof(Vic20Machine).Assembly.Location);

        action.Should().Throw<InvalidDataException>()
            .WithMessage("*exports no plugin*");
    }

    [Test]
    public void Machine_MountsPluginImageAndRoutesRomAndIo()
    {
        var romsRoot = RomLocator.Directory("kernal.bin");
        var pluginPath = typeof(SampleCartridgePlugin).Assembly.Location;
        var temporaryDirectory = Directory.CreateTempSubdirectory("vic20-plugin-test-");
        var imagePath = Path.Combine(temporaryDirectory.FullName, "sample.bin");

        try
        {
            File.WriteAllBytes(imagePath, [0x42, 0x43]);
            var machine = new Vic20Machine(romsRoot);

            machine.MountCartridgePlugin(pluginPath, imagePath);
            machine.Memory.Read(0xA000).Should().Be(0x42);
            machine.Memory.Write(0x9800, 0x5A);
            machine.Memory.Read(0x9800).Should().Be(0x5A);
        }
        finally
        {
            temporaryDirectory.Delete(true);
        }
    }

    [Test]
    public void Machine_ResetAndEjectOperateOnTheMountedPlugin()
    {
        var romsRoot = RomLocator.Directory("kernal.bin");
        var pluginPath = typeof(SampleCartridgePlugin).Assembly.Location;
        var temporaryDirectory = Directory.CreateTempSubdirectory("vic20-plugin-test-");
        var imagePath = Path.Combine(temporaryDirectory.FullName, "sample.bin");

        try
        {
            File.WriteAllBytes(imagePath, [0x42]);
            var machine = new Vic20Machine(romsRoot);
            machine.MountCartridgePlugin(pluginPath, imagePath);

            machine.Memory.Write(0x9800, 0x5A);
            machine.Reset();
            machine.Memory.Read(0x9800).Should().Be(0);
            machine.StepInstruction();

            machine.EjectCartridge(imagePath);
            machine.Memory.Read(0xA000).Should().Be(0xFF);
            machine.MountedCartridges.Should().BeEmpty();
        }
        finally
        {
            temporaryDirectory.Delete(true);
        }
    }

    [Test]
    public void Machine_DoesNotMountPluginWhenImageIsInvalid()
    {
        var romsRoot = RomLocator.Directory("kernal.bin");
        var pluginPath = typeof(SampleCartridgePlugin).Assembly.Location;
        var temporaryDirectory = Directory.CreateTempSubdirectory("vic20-plugin-test-");
        var imagePath = Path.Combine(temporaryDirectory.FullName, "empty.bin");

        try
        {
            File.WriteAllBytes(imagePath, []);
            var machine = new Vic20Machine(romsRoot);

            Action action = () => machine.MountCartridgePlugin(pluginPath, imagePath);

            action.Should().Throw<ArgumentOutOfRangeException>();
            machine.MountedCartridges.Should().BeEmpty();
        }
        finally
        {
            temporaryDirectory.Delete(true);
        }
    }

    [Test]
    public void Machine_DoesNotMountPluginWhenResourcesConflictWithMountedRamCartridge()
    {
        var romsRoot = RomLocator.Directory("kernal.bin");
        var pluginPath = typeof(SampleCartridgePlugin).Assembly.Location;
        var temporaryDirectory = Directory.CreateTempSubdirectory("vic20-plugin-test-");
        var imagePath = Path.Combine(temporaryDirectory.FullName, "sample.bin");

        try
        {
            File.WriteAllBytes(imagePath, [0x42]);
            var machine = new Vic20Machine(romsRoot);
            machine.MountCartridgePlugin(
                FindRamPlugin("vic20-ram-35k"),
                Path.Combine(romsRoot, "cartridges", "vic20-ram-35k.bin"));

            Action action = () => machine.MountCartridgePlugin(pluginPath, imagePath);

            action.Should().Throw<InvalidOperationException>().WithMessage("*overlap*$A000*");
            machine.MountedCartridges.Should().ContainSingle();
        }
        finally
        {
            temporaryDirectory.Delete(true);
        }
    }

    private static string FindRamPlugin(string id)
    {
        var assemblyName = id switch
        {
            "vic20-ram-35k" => "PetEmulator.Vic20.Cartridge.Ram.35K.dll",
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };

        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "plugins", "PetEmulator.Vic20.Cartridge.Ram.35K",
                "bin", "Debug", "net10.0", assemblyName);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Could not locate {assemblyName}.");
    }
}
