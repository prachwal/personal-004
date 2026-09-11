using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20AutostartCartridgeTests
{
    [TestCase("Alphoids-autostart.crt", "Alphoids.prg")]
    [TestCase("Alien-Blitz-NTSC-autostart.crt", "Alien-Blitz.NTSC.prg")]
    [TestCase("Alien-Blitz-PAL-autostart.crt", "Alien-Blitz.PAL.prg")]
    [TestCase("vic20-sound-test-autostart.crt", "vic20-sound-test.prg")]
    public void GeneratedCartridge_HasVic20AutostartHeaderAndOriginalBasicPayload(
        string cartridgeName, string sourceName)
    {
        Vic20CrtImage image = Vic20CrtParser.Parse(Cartridge(cartridgeName));
        Vic20CrtChip chip = image.Chips.Should().ContainSingle().Subject;
        byte[] source = File.ReadAllBytes(Program(sourceName));

        chip.LoadAddress.Should().Be(0xA000);
        chip.Data.Should().HaveCount(0x2000);
        chip.Data.Take(2).Should().Equal(0x09, 0xA0);
        chip.Data.Skip(2).Take(2).Should().Equal(0x56, 0xFF);
        chip.Data.Skip(4).Take(5).Should().Equal(0x41, 0x30, 0xC3, 0xC2, 0xCD);
        chip.Data.Skip(0x43).Take(source.Length - 2).Should().Equal(source.Skip(2));
    }

    [TestCase("Alphoids-autostart.crt", "Alphoids.prg")]
    [TestCase("Alien-Blitz-NTSC-autostart.crt", "Alien-Blitz.NTSC.prg")]
    [TestCase("Alien-Blitz-PAL-autostart.crt", "Alien-Blitz.PAL.prg")]
    [CancelAfter(30_000)]
    public void MachineLanguageAutostartCartridge_CopiesPrgAndJumpsToItsSysTarget(
        string cartridgeName, string sourceName)
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        machine.MountCartridge(Cartridge(cartridgeName));

        machine.RunUntil(_ => machine.Processor is IDebuggableProcessor debug
            && (ushort)debug.GetRegisters()["PC"] == 0x100E, 250_000)
            .Should().BeTrue("the autostart loader must copy the PRG and enter its SYS target at $100E");

        byte[] source = File.ReadAllBytes(Program(sourceName));
        machine.Memory.Read(0x1001).Should().Be(source[2]);
        machine.Memory.Read(0x100E).Should().Be(source[2 + 13]);
        machine.Run(250_000);

        machine.Vic.Columns.Should().Be(22);
        machine.Vic.Rows.Should().Be(23);
        machine.Vic.ScreenAddr.Should().Be(0x1E00);
        machine.Memory.Read(0x00C6).Should().Be(0, "the bootstrap SYS command must be consumed");
        Enumerable.Range(0, machine.Vic.Columns * machine.Vic.Rows)
            .Select(i => machine.Memory.Read((ushort)(machine.Vic.ScreenAddr + i)))
            .Count(code => code != 0x20)
            .Should().BeGreaterThan(0);
    }

    [Test]
    [CancelAfter(10_000)]
    public void SoundTestCartridge_EnablesVicOscillatorAndVolume()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        machine.MountCartridge(Cartridge("vic20-sound-test-autostart.crt"));

        machine.RunUntil(_ => machine.Processor is IDebuggableProcessor debug
            && (ushort)debug.GetRegisters()["PC"] == 0x100E, 250_000)
            .Should().BeTrue();

        machine.Run(30_000);

        machine.Vic.Volume.Should().Be(0x0F);
        machine.Vic.Oscillator1Enabled.Should().BeTrue();
    }

    private static string Cartridge(string name) =>
        Path.Combine(RomLocator.Directory("kernal.bin"), "cartridges", name);

    private static string Program(string name) =>
        Path.Combine(RomLocator.Directory("kernal.bin"), "test-programs", name);
}
