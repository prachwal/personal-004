using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Chips.Tests;

/// <summary>Covers READ ADDRESS's real CRC-16 and the previously-unimplemented READ TRACK (0xE0)
/// / WRITE TRACK (0xF0) commands - see docs/trs80/model1-status.md for why these were added.</summary>
public sealed class FD1791TrackCommandsTests
{
    [Test]
    public void ReadAddress_ReturnsRealCrcInsteadOfThePlaceholderZeroBytes()
    {
        var disk = new TrackDisk();
        disk.Seed(3, 0, Enumerable.Repeat((byte)0xAA, 16).ToArray());
        var fdc = new FD1791(disk, seekTStates: 0) { Track = 3 };

        fdc.Write(FD1791.CommandStatusRegister, 0xC0);
        var bytes = ReadTransfer(fdc, 6);

        bytes[0].Should().Be(3); // track
        bytes[1].Should().Be(0); // side
        bytes[2].Should().Be(0); // sector (FirstSectorId)
        bytes[3].Should().Be(0); // length code: 16 doesn't match any real 128/256/512/1024 size, falls to the 0 default
        (bytes[4], bytes[5]).Should().NotBe((0, 0));
    }

    [Test]
    public void ReadAddress_CrcIsDeterministicForTheSameIdField()
    {
        var disk = new TrackDisk();
        disk.Seed(5, 0, new byte[16]);

        var first = new FD1791(disk, seekTStates: 0) { Track = 5 };
        first.Write(FD1791.CommandStatusRegister, 0xC0);
        var firstBytes = ReadTransfer(first, 6);

        var second = new FD1791(disk, seekTStates: 0) { Track = 5 };
        second.Write(FD1791.CommandStatusRegister, 0xC0);
        var secondBytes = ReadTransfer(second, 6);

        secondBytes.Should().Equal(firstBytes);
    }

    [Test]
    public void ReadTrack_ReconstructsEveryIdAndDataFieldWithMarksAndCrc()
    {
        var disk = new TrackDisk { SectorsPerTrackValue = 2 };
        disk.Seed(1, 0, Enumerable.Repeat((byte)0x11, 16).ToArray());
        disk.Seed(1, 1, Enumerable.Repeat((byte)0x22, 16).ToArray());
        var fdc = new FD1791(disk, seekTStates: 0) { Track = 1 };

        fdc.Write(FD1791.CommandStatusRegister, 0xE0);
        var track = ReadTransfer(fdc, ExpectedFmTrackLength(sectors: 2, sectorSize: 16));

        // Both ID address marks (0xFE) appear, immediately followed by track/side/sector/lengthcode.
        var idamPositions = IndexesOf(track, 0xFE);
        idamPositions.Should().HaveCount(2);
        track[idamPositions[0] + 1].Should().Be(1); // track
        track[idamPositions[0] + 3].Should().Be(0); // sector 0
        track[idamPositions[1] + 3].Should().Be(1); // sector 1

        // Both data address marks (0xFB) appear, immediately followed by the seeded payload.
        var damPositions = IndexesOf(track, 0xFB);
        damPositions.Should().HaveCount(2);
        track.Skip(damPositions[0] + 1).Take(16).Should().AllBeEquivalentTo((byte)0x11);
        track.Skip(damPositions[1] + 1).Take(16).Should().AllBeEquivalentTo((byte)0x22);
    }

    [Test]
    public void WriteTrack_RoundTripsThroughReadTrackBackIntoNormalSectorReads()
    {
        var source = new TrackDisk { SectorsPerTrackValue = 2 };
        source.Seed(0, 0, Enumerable.Repeat((byte)0xAB, 16).ToArray());
        source.Seed(0, 1, Enumerable.Repeat((byte)0xCD, 16).ToArray());
        var reader = new FD1791(source, seekTStates: 0);
        reader.Write(FD1791.CommandStatusRegister, 0xE0);
        var rawTrack = ReadTransfer(reader, ExpectedFmTrackLength(sectors: 2, sectorSize: 16));

        var target = new TrackDisk { SectorsPerTrackValue = 2 };
        var writer = new FD1791(target, seekTStates: 0);
        writer.Write(FD1791.CommandStatusRegister, 0xF0);
        foreach (var b in rawTrack)
            writer.Write(FD1791.DataRegister, b);

        writer.IntrqAsserted.Should().BeTrue();
        writer.Status.Should().Be(0);
        var recovered = new byte[16];
        target.TryReadSector(0, 0, recovered).Should().BeTrue();
        recovered.Should().AllBeEquivalentTo((byte)0xAB);
        target.TryReadSector(0, 1, recovered).Should().BeTrue();
        recovered.Should().AllBeEquivalentTo((byte)0xCD);
    }

    [Test]
    public void WriteTrack_OnAWriteProtectedDiskCompletesWithoutWritingAnything()
    {
        var disk = new TrackDisk { SectorsPerTrackValue = 1, WriteProtected = true };
        var fdc = new FD1791(disk, seekTStates: 0);

        fdc.Write(FD1791.CommandStatusRegister, 0xF0);

        fdc.Status.Should().Be(FD1791.WriteProtectFlag);
        fdc.IntrqAsserted.Should().BeTrue();
    }

    private static int ExpectedFmTrackLength(int sectors, int sectorSize) =>
        40 /* gap1 */ + sectors * (6 /* id sync */ + 5 /* idam+track+side+sector+len */ + 2 /* id crc */
            + 11 /* gap2 */ + 6 /* data sync */ + 1 /* dam */ + sectorSize + 2 /* data crc */ + 27 /* gap3 */);

    private static byte[] ReadTransfer(FD1791 fdc, int length)
    {
        var result = new byte[length];
        for (var i = 0; i < length; i++)
            result[i] = fdc.Read(FD1791.DataRegister);
        return result;
    }

    private static List<int> IndexesOf(byte[] data, byte value)
    {
        var result = new List<int>();
        for (var i = 0; i < data.Length; i++)
            if (data[i] == value)
                result.Add(i);
        return result;
    }

    private sealed class TrackDisk : IFD1791DiskImage
    {
        private readonly Dictionary<(int Track, int Sector), byte[]> _sectors = [];
        public int SectorsPerTrackValue { get; init; } = 1;
        public bool WriteProtected { get; init; }
        public int SectorSize => 16;
        public int FirstSectorId => 0;
        public int SectorsOnTrack(int track) => SectorsPerTrackValue;

        public void Seed(int track, int sector, byte[] data) => _sectors[(track, sector)] = data;

        public bool TryReadSector(int track, int sector, Span<byte> destination)
        {
            if (!_sectors.TryGetValue((track, sector), out var data))
                return false;
            data.CopyTo(destination);
            return true;
        }

        public bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source)
        {
            _sectors[(track, sector)] = source.ToArray();
            return true;
        }
    }
}
