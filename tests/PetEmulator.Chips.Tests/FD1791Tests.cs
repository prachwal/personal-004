using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class FD1791Tests
{
    [Test]
    public void BaseController_ProvidesTheCommonRegisterContract()
    {
        var fdc = new FD1791();

        fdc.Write(FD1791.TrackRegister, 0x12);
        fdc.Write(FD1791.SectorRegister, 0x03);
        fdc.Write(FD1791.DataRegister, 0xA5);

        fdc.Read(FD1791.TrackRegister).Should().Be(0x12);
        fdc.Read(FD1791.SectorRegister).Should().Be(0x03);
        fdc.Read(FD1791.DataRegister).Should().Be(0xA5);
        fdc.DataBusMode.Should().Be(Fd179xDataBusMode.Inverted);
    }

    [Test]
    public void FD1793_IsAnFD1791WithTrueDataBus()
    {
        FD1791 fdc = new FD1793();

        fdc.Should().BeOfType<FD1793>();
        fdc.DataBusMode.Should().Be(Fd179xDataBusMode.True);
    }

    [Test]
    public void DataBusConversion_IsReversibleForEveryByte()
    {
        var fd1791 = new FD1791();
        var fd1793 = new FD1793();

        for (var value = 0; value <= byte.MaxValue; value++)
        {
            var logical = (byte)value;

            fd1791.EncodeDataBus(logical).Should().Be((byte)~logical);
            fd1791.DecodeDataBus(fd1791.EncodeDataBus(logical)).Should().Be(logical);
            fd1793.EncodeDataBus(logical).Should().Be(logical);
            fd1793.DecodeDataBus(fd1793.EncodeDataBus(logical)).Should().Be(logical);
        }
    }

    [Test]
    public void CommonDiskImage_IsAcceptedByFD1791()
    {
        IFD1791DiskImage disk = new TestDisk();
        var fdc = new FD1791(disk, seekTStates: 0);

        fdc.Write(FD1791.CommandStatusRegister, 0x00);

        fdc.Track.Should().Be(0);
        fdc.IntrqAsserted.Should().BeTrue();
    }

    private sealed class TestDisk : IFD1791DiskImage
    {
        public bool WriteProtected => false;
        public int SectorSize => 128;

        public bool TryReadSector(int track, int sector, Span<byte> destination) => true;

        public bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source) => true;
    }
}
