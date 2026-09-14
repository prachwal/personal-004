using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class FD1791ActivityTests
{
    [Test]
    public void ActivityIsSeparateFromDiagnosticObserver()
    {
        var fdc = new FD1791(new ActivityDisk(), sectorTStates: 0);
        var activity = new List<FD1791ActivityEventArgs>();
        var diagnostics = new List<FD1791DiagnosticEvent>();
        fdc.ActivityChanged += (_, value) => activity.Add(value);
        fdc.DiagnosticObserver = diagnostics.Add;
        fdc.Sector = 1;

        fdc.Write(FD1791.CommandStatusRegister, 0x80);

        activity.Select(value => value.Kind).Should().Contain(FD1791ActivityKind.CommandStarted);
        activity.Select(value => value.Kind).Should().Contain(FD1791ActivityKind.DataRequested);
        diagnostics.Should().NotBeEmpty();
        activity.Should().NotBeEmpty();
    }

    [Test]
    public void ActivitySnapshotExposesLedRelevantLevels()
    {
        var fdc = new FD1791(new ActivityDisk(), sectorTStates: 0);
        FD1791ActivityEventArgs? requested = null;
        fdc.ActivityChanged += (_, value) =>
        {
            if (value.Kind == FD1791ActivityKind.DataRequested)
                requested = value;
        };

        fdc.Sector = 1;
        fdc.Write(FD1791.CommandStatusRegister, 0x80);

        requested.Should().NotBeNull();
        requested!.Busy.Should().BeTrue();
        requested.DrqAsserted.Should().BeTrue();
        requested.IntrqAsserted.Should().BeFalse();
    }

    private sealed class ActivityDisk : IFD1791DiskImage
    {
        public bool WriteProtected => false;
        public int SectorSize => 1;
        public bool IsDoubleDensity => false;
        public int FirstSectorId => 1;
        public int SectorsOnTrack(int track) => track == 0 ? 1 : 0;
        public bool TryReadSector(int track, int sector, Span<byte> destination)
        {
            if (track != 0 || sector != 1 || destination.Length != 1)
                return false;
            destination[0] = 0xA5;
            return true;
        }
        public bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source) => false;
    }
}
