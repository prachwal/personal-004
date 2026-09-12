using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20PrgParserTests
{
    [Test]
    public void Parse_ReadsLittleEndianLoadAddressAndPayload()
    {
        Vic20PrgImage image = Vic20PrgParser.Parse(Fixture("vic20-test-6000.prg"));

        image.LoadAddress.Should().Be(0x6000);
        image.Data.Should().Equal((byte)'P');
    }

    [Test]
    public void Machine_MountsPrgAtItsDeclaredAddress()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));

        machine.MountCartridge(Fixture("vic20-test-6000.prg"));

        machine.Memory.Read(0x6000).Should().Be((byte)'P');
    }

    [Test]
    public void Machine_MountsPrgAtA000WithoutChangingBinDefaults()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));

        machine.MountCartridge(Fixture("vic20-test-a000.prg"));

        machine.Memory.Read(0xA000).Should().Be((byte)'Q');
    }

    [Test]
    public void Parse_RejectsAddressOnlyPrg()
    {
        FluentActions.Invoking(() => Vic20PrgParser.Parse([0x00, 0x60]))
            .Should().Throw<InvalidDataException>().WithMessage("*at least one byte*");
    }

    [Test]
    public void Machine_MountsDownloadedIfrCartridgeImage()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        string path = Fixture("IFR-Flight-Simulator.prg", "cartridges");
        byte[] file = File.ReadAllBytes(path);

        machine.MountCartridge(path);

        machine.Memory.Read(0xA000).Should().Be(file[2]);
        machine.Memory.Read(0xBFFF).Should().Be(file[^1]);
    }

    [TestCase("Alphoids.prg")]
    [TestCase("Alien-Blitz.NTSC.prg")]
    [TestCase("Alien-Blitz.PAL.prg")]
    public void Parse_RecognizesDownloadedRamProgram(string name)
    {
        Vic20PrgImage image = Vic20PrgParser.Parse(Fixture(name, "test-programs"));

        image.LoadAddress.Should().Be(0x1001);
        image.Data.Should().NotBeEmpty();
    }

    private static string Fixture(string name, string directory = "cartridges") =>
        Path.Combine(RomLocator.Directory("kernal.bin"), directory, name);
}
