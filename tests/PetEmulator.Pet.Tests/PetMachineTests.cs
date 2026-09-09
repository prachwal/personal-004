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

    [Test]
    public void Keyboard_PressedKey_ReadableThroughPia1OverTheBus()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        const ushort pia1Base = 0xE810;

        // Select the data register (not the direction register) on PIA1's port A and B.
        machine.Memory.Write((ushort)(pia1Base + 1), 0x04);
        machine.Memory.Write((ushort)(pia1Base + 3), 0x04);

        // KeyA on the PET 2001 graphics keyboard sits at matrix row 4, column 0.
        machine.Keyboard.Press(4, 0);

        // Software selects row 4 by writing it to port A (PA0-3); port B then echoes that row's columns.
        machine.Memory.Write(pia1Base, 0x04);
        var columns = machine.Memory.Read((ushort)(pia1Base + 2));

        columns.Should().Be(machine.Keyboard.ReadColumns(4), "PIA1 port B should echo the selected row's live matrix state");
        (columns & 0x01).Should().Be(0, "column 0 is held down (active-low) once KeyA is pressed");

        machine.Keyboard.Release(4, 0);
        var afterRelease = machine.Memory.Read((ushort)(pia1Base + 2));
        (afterRelease & 0x01).Should().Be(0x01, "releasing the key should clear its active-low bit");
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }
}
