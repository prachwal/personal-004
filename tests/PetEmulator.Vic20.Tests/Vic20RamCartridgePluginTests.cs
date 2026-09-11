using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20RamCartridgePluginTests
{
    [TestCase("3K", "vic20-ram-3k", 0x0400, 0x0C00)]
    [TestCase("8K", "vic20-ram-8k", 0x2000, 0x2000)]
    [TestCase("16K", "vic20-ram-16k", 0x2000, 0x4000)]
    [TestCase("24K", "vic20-ram-24k", 0x2000, 0x6000)]
    [TestCase("35K", "vic20-ram-35k", 0x0400, 0x8C00)]
    public void Plugin_DeclaresExpectedRamResources(string label, string id, int firstAddress, int totalLength)
    {
        var plugin = Vic20CartridgePluginLoader.Load(PluginPath(id));

        plugin.Descriptor.Id.Should().Be(id);
        plugin.Descriptor.Resources.Should().OnlyContain(resource => resource.Kind == PetEmulator.Vic20.Cartridge.Abstractions.Vic20CartridgeResourceKind.Ram);
        plugin.Descriptor.Resources.Sum(resource => resource.Length).Should().Be(totalLength);
        plugin.Descriptor.Resources[0].StartAddress.Should().Be((ushort)firstAddress);
    }

    [Test]
    public void EightKPlugin_MountsWritableRamAndResetClearsIt()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        machine.MountCartridgePlugin(PluginPath("vic20-ram-8k"), Fixture("vic20-ram-8k.bin"));

        machine.Memory.Write(0x2000, 0x5A);
        machine.Memory.Read(0x2000).Should().Be(0x5A);
        machine.Reset();
        machine.Memory.Read(0x2000).Should().Be(0);
    }

    [Test]
    public void OverlappingRamPlugins_AreRejectedAtomically()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        machine.MountCartridgePlugin(PluginPath("vic20-ram-8k"), Fixture("vic20-ram-8k.bin"));

        FluentActions.Invoking(() => machine.MountCartridgePlugin(
                PluginPath("vic20-ram-16k"), Fixture("vic20-ram-16k.bin")))
            .Should().Throw<InvalidOperationException>().WithMessage("*overlap*");
        machine.MountedCartridges.Should().ContainSingle();
    }

    private static string Fixture(string name) =>
        Path.Combine(RomLocator.Directory("kernal.bin"), "cartridges", name);

    private static string PluginPath(string id)
    {
        var (directoryName, assemblyName) = id switch
        {
            "vic20-ram-3k" => ("PetEmulator.Vic20.Cartridge.Ram", "PetEmulator.Vic20.Cartridge.Ram.3K"),
            "vic20-ram-8k" => ("PetEmulator.Vic20.Cartridge.Ram.8K", "PetEmulator.Vic20.Cartridge.Ram.8K"),
            "vic20-ram-16k" => ("PetEmulator.Vic20.Cartridge.Ram.16K", "PetEmulator.Vic20.Cartridge.Ram.16K"),
            "vic20-ram-24k" => ("PetEmulator.Vic20.Cartridge.Ram.24K", "PetEmulator.Vic20.Cartridge.Ram.24K"),
            "vic20-ram-35k" => ("PetEmulator.Vic20.Cartridge.Ram.35K", "PetEmulator.Vic20.Cartridge.Ram.35K"),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };

        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "plugins", directoryName,
                "bin", "Debug", "net10.0", $"{assemblyName}.dll");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Could not locate {assemblyName}.dll.");
    }
}
