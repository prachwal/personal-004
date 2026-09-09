using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests;

/// <summary>
/// First smoke tests of real ROM execution through <see cref="PetMachine"/>'s brand-new bus
/// decoder. Deliberately bounded (hundreds of instructions, not a full BASIC boot) with a hard
/// [CancelAfter] backstop: the CPU's per-instruction cycle cap throws instead of hanging on a
/// handler bug, but a real bug there is still a legitimate failure here, not something to swallow.
/// </summary>
[TestFixture]
public sealed class PetMachineTests
{
    [Test]
    public void Name_And_IsReady_ReflectProfile()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Name.Should().Be(PetProfileCatalog.Pet2001_8.Name);
        machine.IsReady.Should().BeTrue();
    }

    [Test]
    [CancelAfter(30_000)]
    public void Pet2001_8_BootsWithoutThrowing_ForBoundedSteps()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);

        machine.Run(500);

        machine.Processor.Halted.Should().BeFalse();
        machine.Processor.InstructionCount.Should().BeGreaterThan(0);
    }

    [Test]
    [CancelAfter(30_000)]
    public void Cbm8032_BootsWithoutThrowing_ForBoundedSteps()
    {
        var machine = CreateMachine(PetProfileCatalog.Cbm8032);

        machine.Run(500);

        machine.Processor.Halted.Should().BeFalse();
        machine.Processor.InstructionCount.Should().BeGreaterThan(0);
    }

    [Test]
    public void Reset_ZeroesRamAndRestartsCpuAtResetVector()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        machine.Memory.Write(0x0100, 0x77);

        machine.Reset();

        machine.Memory.Read(0x0100).Should().Be(0);
        machine.Processor.InstructionCount.Should().Be(0);
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }
}
