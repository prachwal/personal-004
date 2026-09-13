using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class FD1793Tests
{
    [Test]
    public void Constructor_RejectsInvalidTiming()
    {
        FluentActions.Invoking(() => new FD1793(dataByteTStates: 0))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new FD1793(indexPeriodTStates: 10, indexPulseWidthTStates: 11))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Registers_ExposeTrackSectorDataAndIgnoreUnknownOffsets()
    {
        var fdc = new FD1793();
        fdc.Write(FD1793.TrackRegister, 7);
        fdc.Write(FD1793.SectorRegister, 3);
        fdc.Write(FD1793.DataRegister, 0xA5);

        fdc.Read(FD1793.TrackRegister).Should().Be(7);
        fdc.Read(FD1793.SectorRegister).Should().Be(3);
        fdc.Read(FD1793.DataRegister).Should().Be(0xA5);
        fdc.Read(0x04).Should().Be(0xFF);
    }

    [Test]
    public void Restore_WithDisk_SelectsTrackZeroAndRaisesInterrupt()
    {
        var disk = new TestDisk(4);
        var fdc = new FD1793(disk, seekTStates: 0);
        fdc.Track = 9;

        fdc.Write(FD1793.CommandStatusRegister, 0x00);

        fdc.Track.Should().Be(0);
        fdc.Status.Should().Be(FD1793.TrackZeroFlag);
        fdc.IntrqAsserted.Should().BeTrue();
    }

    [Test]
    public void Seek_UsesDataRegisterAsTarget()
    {
        var fdc = new FD1793(new TestDisk(4), seekTStates: 0);
        fdc.Write(FD1793.DataRegister, 12);
        fdc.Write(FD1793.CommandStatusRegister, 0x10);

        fdc.Track.Should().Be(12);
        fdc.Status.Should().Be(0);
    }

    [Test]
    public void Step_RepeatsLastStepDirection()
    {
        var fdc = new FD1793(new TestDisk(2), seekTStates: 0);
        fdc.Track = 2;

        fdc.Write(FD1793.CommandStatusRegister, 0x30); // STEP IN
        fdc.Track.Should().Be(3);
        fdc.Write(FD1793.CommandStatusRegister, 0x20); // STEP, repeats IN
        fdc.Track.Should().Be(4);
        fdc.Write(FD1793.CommandStatusRegister, 0x50); // STEP OUT
        fdc.Track.Should().Be(3);
        fdc.Write(FD1793.CommandStatusRegister, 0x20); // STEP, repeats OUT
        fdc.Track.Should().Be(2);
    }

    [Test]
    public void ReadSector_RequestsEveryByteAndCompletesWithInterrupt()
    {
        var disk = new TestDisk(4);
        disk.Set(2, 0, 1, 0x10, 0x20, 0x30, 0x40);
        var fdc = new FD1793(disk, seekTStates: 0, sectorTStates: 0);
        fdc.Track = 2;
        fdc.Sector = 1;

        fdc.Write(FD1793.CommandStatusRegister, 0x80);
        fdc.Status.Should().Be((byte)(FD1793.BusyFlag | FD1793.DataRequestFlag));
        fdc.Read(FD1793.DataRegister).Should().Be(0x10);
        fdc.Read(FD1793.DataRegister).Should().Be(0x20);
        fdc.Read(FD1793.DataRegister).Should().Be(0x30);
        fdc.Read(FD1793.DataRegister).Should().Be(0x40);

        fdc.Status.Should().Be(0);
        fdc.IntrqAsserted.Should().BeTrue();
        fdc.Read(FD1793.CommandStatusRegister).Should().Be(0);
        fdc.IntrqAsserted.Should().BeFalse();
    }

    [Test]
    public void ReadSector_ReportsRecordNotFound()
    {
        var fdc = new FD1793(new TestDisk(4), sectorTStates: 0);
        fdc.Write(FD1793.CommandStatusRegister, 0x80);

        fdc.Status.Should().Be(FD1793.RecordNotFoundFlag);
        fdc.IntrqAsserted.Should().BeTrue();
    }

    [Test]
    public void ReadSector_RejectsDensityMismatch()
    {
        var disk = new TestDisk(2) { IsDoubleDensity = true };
        disk.Set(0, 0, 0, 1, 2);
        var fdc = new FD1793(disk, sectorTStates: 0) { DoubleDensityEnabled = false };

        fdc.Write(FD1793.CommandStatusRegister, 0x80);

        fdc.Status.Should().Be(FD1793.RecordNotFoundFlag);
    }

    [Test]
    public void ReadSector_ReportsDeletedDataMark()
    {
        var disk = new TestDisk(2);
        disk.Set(0, 0, 1, 0xAA, 0xBB);
        disk.Deleted.Add((0, 1));
        var fdc = new FD1793(disk, seekTStates: 0, sectorTStates: 0);
        fdc.Sector = 1;

        fdc.Write(FD1793.CommandStatusRegister, 0x80);

        fdc.Status.Should().Be((byte)(FD1793.BusyFlag | FD1793.DataRequestFlag | FD1793.RecordTypeFlag));
    }

    [Test]
    public void MultiRecordRead_ContinuesUntilTheNextSectorIsMissing()
    {
        var disk = new TestDisk(2);
        disk.Set(0, 0, 1, 1, 2);
        disk.Set(0, 0, 2, 3, 4);
        var fdc = new FD1793(disk, sectorTStates: 1);
        fdc.Sector = 1;
        fdc.Write(FD1793.CommandStatusRegister, 0x90);
        fdc.Tick(1);

        fdc.Read(FD1793.DataRegister).Should().Be(1);
        fdc.Read(FD1793.DataRegister).Should().Be(2);
        fdc.Read(FD1793.DataRegister).Should().Be(3);
        fdc.Read(FD1793.DataRegister).Should().Be(4);
        fdc.Status.Should().Be(FD1793.BusyFlag);

        fdc.Tick(1);

        fdc.Status.Should().Be(FD1793.RecordNotFoundFlag);
    }

    [Test]
    public void WriteSector_WritesAllBytesAndCompletes()
    {
        var disk = new TestDisk(4);
        var fdc = new FD1793(disk, seekTStates: 0, sectorTStates: 0);
        fdc.Track = 1;
        fdc.Sector = 2;
        fdc.Write(FD1793.CommandStatusRegister, 0xA0);

        fdc.Write(FD1793.DataRegister, 1);
        fdc.Write(FD1793.DataRegister, 2);
        fdc.Write(FD1793.DataRegister, 3);
        fdc.Write(FD1793.DataRegister, 4);

        disk.Get(1, 2).Should().Equal(1, 2, 3, 4);
        fdc.Status.Should().Be(0);
        fdc.IntrqAsserted.Should().BeTrue();
    }

    [Test]
    public void WriteSector_ReportsWriteProtect()
    {
        var disk = new TestDisk(2) { WriteProtected = true };
        var fdc = new FD1793(disk, seekTStates: 0, sectorTStates: 0);
        fdc.Write(FD1793.CommandStatusRegister, 0xA0);

        fdc.Status.Should().Be(FD1793.WriteProtectFlag);
    }

    [Test]
    public void DataRequestTimeout_ReportsLostData()
    {
        var disk = new TestDisk(2);
        disk.Set(0, 0, 0, 1, 2);
        var fdc = new FD1793(disk, dataByteTStates: 3, sectorTStates: 0);
        fdc.Write(FD1793.CommandStatusRegister, 0x80);

        fdc.Tick(3);

        fdc.Status.Should().Be(FD1793.LostDataFlag);
        fdc.IntrqAsserted.Should().BeTrue();
    }

    [Test]
    public void ReadAddress_ReturnsTrackSideSectorAndLengthCode()
    {
        var disk = new TestDisk(512) { FirstSectorId = 1, TrackSectorCount = 10 };
        var fdc = new FD1793(disk, seekTStates: 0, sectorTStates: 0);
        fdc.Track = 4;
        fdc.Side = 1;
        fdc.Write(FD1793.CommandStatusRegister, 0xC0);

        fdc.Read(FD1793.DataRegister).Should().Be(4);
        fdc.Read(FD1793.DataRegister).Should().Be(1);
        fdc.Read(FD1793.DataRegister).Should().Be(1);
        fdc.Read(FD1793.DataRegister).Should().Be(2);
    }

    [Test]
    public void DriveSelect_UsesLowestSetDriveWithoutBoardWaitSideEffects()
    {
        var fdc = new FD1793(seekTStates: 0);
        fdc.DriveSelect = 0x43;
        fdc.DriveSelect.Should().Be(1);

        var disk = new TestDisk(2);
        fdc.InsertDisk(0, disk);
        fdc.Write(FD1793.CommandStatusRegister, 0x00);
        fdc.Tick(0);
    }

    [Test]
    public void ForceInterrupt_CancelsPendingCommand()
    {
        var fdc = new FD1793(new TestDisk(2), seekTStates: 100);
        fdc.Write(FD1793.CommandStatusRegister, 0x00);
        fdc.Write(FD1793.CommandStatusRegister, 0xD0);

        fdc.Busy.Should().BeFalse();
        fdc.IntrqAsserted.Should().BeTrue();
        fdc.PendingTStatesRemaining.Should().Be(0);
    }

    [Test]
    public void Reset_ClearsCommandStateButKeepsInsertedDisk()
    {
        var disk = new TestDisk(2);
        var fdc = new FD1793(disk, seekTStates: 0, sectorTStates: 0);
        fdc.Write(FD1793.CommandStatusRegister, 0x00);
        fdc.Reset();
        fdc.Write(FD1793.CommandStatusRegister, 0x00);

        fdc.Status.Should().Be(FD1793.TrackZeroFlag);
        fdc.IntrqAsserted.Should().BeTrue();
    }

    private sealed class TestDisk(int sectorSize) : IFD1793DiskImage
    {
        private readonly Dictionary<(int Track, int Sector), byte[]> _sectors = new();

        public bool WriteProtected { get; set; }
        public int SectorSize { get; } = sectorSize;
        public bool IsDoubleDensity { get; set; }
        public int FirstSectorId { get; set; }
        public int TrackSectorCount { get; set; }
        public HashSet<(int Track, int Sector)> Deleted { get; } = new();

        public int SectorsOnTrack(int track) => TrackSectorCount;

        public bool TryReadSector(int track, int sector, Span<byte> destination)
        {
            if (!_sectors.TryGetValue((track, sector), out var data) || destination.Length < data.Length)
                return false;
            data.CopyTo(destination);
            return true;
        }

        public bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source)
        {
            if (source.Length != SectorSize)
                return false;
            _sectors[(track, sector)] = source.ToArray();
            return true;
        }

        public bool IsDeletedDataMark(int track, int sector) => Deleted.Contains((track, sector));

        public void Set(int track, int side, int sector, params byte[] data)
        {
            side.Should().Be(0);
            data.Length.Should().Be(SectorSize);
            _sectors[(track, sector)] = data;
        }

        public byte[] Get(int track, int sector) => _sectors[(track, sector)];
    }
}
