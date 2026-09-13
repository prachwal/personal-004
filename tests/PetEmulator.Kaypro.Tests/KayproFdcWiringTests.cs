using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Kaypro;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproFdcWiringTests
{
    [Test]
    public void SystemLatchSelectsRomDriveValuesAndSideOutsideTheGenericFdc()
    {
        var wiring = new KayproFdcWiring(new FD1793());

        wiring.WriteSystemPort(0x80);
        Assert.That(wiring.SelectedDrive, Is.Null);
        Assert.That(wiring.Controller.DriveSelect, Is.EqualTo(0));
        Assert.That(wiring.Controller.Side, Is.EqualTo(0));

        wiring.WriteSystemPort(0x01);
        Assert.That(wiring.SelectedDrive, Is.EqualTo(0));
        Assert.That(wiring.Controller.DriveSelect, Is.EqualTo(1));
        Assert.That(wiring.Controller.Side, Is.EqualTo(0));

        wiring.WriteSystemPort(0x02);
        Assert.That(wiring.SelectedDrive, Is.EqualTo(1));
        Assert.That(wiring.Controller.DriveSelect, Is.EqualTo(2));

        wiring.WriteSystemPort(0x06);
        Assert.That(wiring.Controller.Side, Is.EqualTo(1));
    }

    [Test]
    public void BitSixSelectsCharacterSetAndDoesNotAssertFdcWait()
    {
        var wiring = new KayproFdcWiring(new FD1793());

        wiring.WriteSystemPort(0x40);
        Assert.That(wiring.NormalCharacterSetSelected, Is.False);
        Assert.That(wiring.WaitAsserted, Is.False);

        wiring.WriteSystemPort(0x00);
        Assert.That(wiring.NormalCharacterSetSelected, Is.True);
    }

    [Test]
    public void WaitIsOwnedByAdapterAndReleasesOnControllerActivityOrWatchdog()
    {
        var wiring = new KayproFdcWiring(new FD1793());

        wiring.BeginWait();
        Assert.That(wiring.WaitAsserted, Is.True);
        wiring.Tick(1_817);
        Assert.That(wiring.WaitAsserted, Is.False);

        wiring.BeginWait();
        wiring.Controller.Write(FD1793.CommandStatusRegister, 0xD0);
        wiring.Tick(0);
        Assert.That(wiring.WaitAsserted, Is.False);
    }

    [Test]
    public void InterruptRequestedAggregatesIntrqAndDrqForKayproNmiWiring()
    {
        var disk = new TestDisk();
        var wiring = new KayproFdcWiring(new FD1793(disk, dataByteTStates: 1, sectorTStates: 1));
        wiring.WriteSystemPort(0x02);
        wiring.Controller.Sector = 1;
        wiring.Controller.Write(FD1793.CommandStatusRegister, 0x80);

        wiring.Tick(1);

        Assert.That(wiring.InterruptRequested, Is.True);
    }

    [Test]
    public void KayproProfileUsesGenericMultipleRecordTransfer()
    {
        var wiring = new KayproFdcWiring(new FD1793());

        Assert.That(wiring.Controller.MultipleRecordEnabled, Is.True);
    }

    [Test]
    public void FdcRaisesAUniqueInterruptSequenceForEachDataRequest()
    {
        var disk = new TestDisk();
        var fdc = new FD1793(disk, dataByteTStates: 1, sectorTStates: 1);
        fdc.DriveSelect = 1;
        fdc.Sector = 0;
        fdc.Write(FD1793.CommandStatusRegister, 0x80);
        fdc.Tick(1);
        var first = fdc.InterruptSequence;

        _ = fdc.Read(FD1793.DataRegister);

        Assert.That(fdc.InterruptSequence, Is.GreaterThan(first));
        Assert.That(fdc.DrqAsserted, Is.True);
    }

    [Test]
    public void FdcDiagnosticsExposeCommandDataRequestReadAndCompletionSequence()
    {
        var events = new List<FD1791DiagnosticEvent>();
        var fdc = new FD1793(new TestDisk(), dataByteTStates: 1, sectorTStates: 1)
        {
            DiagnosticObserver = events.Add
        };
        fdc.DriveSelect = 1;
        fdc.Sector = 0;

        fdc.Write(FD1793.CommandStatusRegister, 0x80);
        fdc.Tick(1);
        var data = fdc.Read(FD1793.DataRegister);
        while (fdc.DrqAsserted)
            _ = fdc.Read(FD1793.DataRegister);

        Assert.That(data, Is.EqualTo(0));
        Assert.That(events.Select(item => item.Kind), Does.Contain(FD1791DiagnosticKind.CommandAccepted));
        Assert.That(events.Select(item => item.Kind), Does.Contain(FD1791DiagnosticKind.DataRequested));
        Assert.That(events.Select(item => item.Kind), Does.Contain(FD1791DiagnosticKind.DataRead));
        Assert.That(events.Select(item => item.Kind), Does.Contain(FD1791DiagnosticKind.CommandCompleted));
        Assert.That(events.First(item => item.Kind == FD1791DiagnosticKind.DataRequested).DrqAsserted, Is.True);
        Assert.That(events.Last(item => item.Kind == FD1791DiagnosticKind.CommandCompleted).IntrqAsserted, Is.True);
    }

    [Test]
    public void FdcDiagnosticsIdentifyLostDataWithDeadlineAndTransferState()
    {
        var events = new List<FD1791DiagnosticEvent>();
        var fdc = new FD1793(new TestDisk(), dataByteTStates: 3, sectorTStates: 1)
        {
            DiagnosticObserver = events.Add
        };
        fdc.DriveSelect = 1;
        fdc.Sector = 0;
        fdc.Write(FD1793.CommandStatusRegister, 0x80);
        fdc.Tick(1);
        fdc.Tick(3);

        var lost = events.Single(item => item.Kind == FD1791DiagnosticKind.LostData);
        Assert.That(lost.Command, Is.EqualTo(0x80));
        Assert.That(lost.Status, Is.EqualTo(FD1793.BusyFlag | FD1793.DataRequestFlag));
        Assert.That(lost.DataDeadlineTStates, Is.EqualTo(0));
        Assert.That(events.Last().Kind, Is.EqualTo(FD1791DiagnosticKind.CommandCompleted));
        Assert.That(events.Last().Value, Is.EqualTo(FD1793.LostDataFlag));
    }

    private sealed class TestDisk : IFD1791DiskImage
    {
        public bool WriteProtected => false;
        public int SectorSize => 128;
        public bool TryReadSector(int track, int sector, Span<byte> destination)
        {
            destination.Clear();
            return true;
        }

        public bool TryWriteSector(int track, int sector, ReadOnlySpan<byte> source) => true;
    }
}
