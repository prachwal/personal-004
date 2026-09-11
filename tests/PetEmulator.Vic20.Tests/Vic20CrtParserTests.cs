using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20CrtParserTests
{
    [Test]
    public void Parse_ReadsHeaderAndSingleChip()
    {
        Vic20CrtImage image = Vic20CrtParser.Parse(Fixture("vic20-test-single.crt"));

        image.Name.Should().Be("VIC-20 test single");
        image.HardwareType.Should().Be(2);
        image.Chips.Should().ContainSingle();
        image.Chips[0].LoadAddress.Should().Be(0xA000);
        image.Chips[0].Data.Should().Equal((byte)'S');
    }

    [Test]
    public void Parse_RejectsInvalidSignature()
    {
        byte[] crt = CreateCrt([0x42], 0xA000);
        crt[0] = (byte)'X';

        FluentActions.Invoking(() => Vic20CrtParser.Parse(crt))
            .Should().Throw<InvalidDataException>().WithMessage("*invalid signature*");
    }

    [Test]
    public void Parse_RejectsTruncatedChipPacket()
    {
        byte[] crt = CreateCrt([0x42], 0xA000);
        Array.Resize(ref crt, crt.Length - 1);

        FluentActions.Invoking(() => Vic20CrtParser.Parse(crt))
            .Should().Throw<InvalidDataException>().WithMessage("*packet length*");
    }

    [Test]
    public void Machine_MountsSingleChipCrtAtItsLoadAddress()
    {
        var directory = Directory.CreateTempSubdirectory("vic20-crt-test-");
        try
        {
            string path = Path.Combine(directory.FullName, "test.crt");
            File.Copy(Fixture("vic20-test-single.crt"), path);
            var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));

            machine.MountCartridge(path);

            machine.Memory.Read(0xA000).Should().Be((byte)'S');
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Test]
    public void Machine_MountsMultiChipCrtAndSelectsBanks()
    {
        var directory = Directory.CreateTempSubdirectory("vic20-crt-test-");
        try
        {
            string path = Path.Combine(directory.FullName, "multi.crt");
            File.Copy(Fixture("vic20-test-banked.crt"), path);
            var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));

            machine.MountCartridge(path);

            machine.Memory.Read(0xA000).Should().Be((byte)'A');
            machine.Memory.Write(0x9800, 1);
            machine.Memory.Read(0xA000).Should().Be((byte)'B');
            machine.Reset();
            machine.Memory.Read(0xA000).Should().Be((byte)'A');
            machine.Memory.Read(0x9800).Should().Be(0);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Test]
    public void Machine_RejectsSecondBankedCrtWithoutPartialMount()
    {
        var directory = Directory.CreateTempSubdirectory("vic20-crt-test-");
        try
        {
            string path = Path.Combine(directory.FullName, "multi.crt");
            File.Copy(Fixture("vic20-test-banked.crt"), path);
            var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
            machine.MountCartridge(path);

            FluentActions.Invoking(() => machine.MountCartridge(path))
                .Should().Throw<InvalidOperationException>().WithMessage("*overlap*");
            machine.MountedCartridges.Should().ContainSingle();
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Test]
    public void Machine_RejectsBankedCrtWithDifferentBankSize()
    {
        FluentActions.Invoking(() => Vic20Cartridge.Load(Fixture("vic20-test-invalid-layout.crt")))
            .Should().Throw<InvalidDataException>().WithMessage("*same load address and size*");
    }

    private static string Fixture(string name) =>
        Path.Combine(RomLocator.Directory("kernal.bin"), "cartridges", name);

    private static byte[] CreateCrt(byte[] data, ushort loadAddress, ushort bank = 0)
    {
        byte[] header = new byte[0x40];
        Encoding.ASCII.GetBytes("C64 CARTRIDGE   ").CopyTo(header, 0);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0x10), 0x40);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x14), 0x0100);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x16), 2);
        Encoding.ASCII.GetBytes("Test cartridge").CopyTo(header, 0x20);
        return header.Concat(CreateChip(data, loadAddress, bank)).ToArray();
    }

    private static byte[] CreateChip(byte[] data, ushort loadAddress, ushort bank)
    {
        byte[] chip = new byte[0x10 + data.Length];
        Encoding.ASCII.GetBytes("CHIP").CopyTo(chip, 0);
        BinaryPrimitives.WriteUInt32BigEndian(chip.AsSpan(4), (uint)chip.Length);
        BinaryPrimitives.WriteUInt16BigEndian(chip.AsSpan(8), 2);
        BinaryPrimitives.WriteUInt16BigEndian(chip.AsSpan(0xA), bank);
        BinaryPrimitives.WriteUInt16BigEndian(chip.AsSpan(0xC), loadAddress);
        BinaryPrimitives.WriteUInt16BigEndian(chip.AsSpan(0xE), (ushort)data.Length);
        data.CopyTo(chip, 0x10);
        return chip;
    }
}
