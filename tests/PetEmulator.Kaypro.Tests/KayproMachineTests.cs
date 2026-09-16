using PetEmulator.Kaypro;
using PetEmulator.Chips;
using NUnit.Framework;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproMachineTests
{
    [Test]
    public void MachineUsesSharedZ80AndCoreContracts()
    {
        var machine = new KayproMachine();

        Assert.That(machine.Name, Is.EqualTo("Kaypro II"));
        Assert.That(machine.Processor.GetType().Name, Is.EqualTo("Z80Cpu"));
        Assert.That(machine.Memory, Is.SameAs(machine.Bus));
        Assert.That(machine.IsReady, Is.False);
    }

    [Test]
    public void SystemPortBanksMonitorRomAndMapsVideo()
    {
        var machine = new KayproMachine();
        machine.LoadMonitorRom(Enumerable.Repeat((byte)0xA5, 0x800).ToArray());

        Assert.That(machine.Memory.Read(0), Is.EqualTo(0xA5));
        machine.Memory.Write(0, 0x31);
        machine.Bus.WritePort(0x1C, 0x00);
        Assert.That(machine.Memory.Read(0), Is.EqualTo(0x31));

        machine.Memory.Write(0x3000, (byte)'A');
        Assert.That(machine.Video.GetText()[0], Is.EqualTo('A'));
    }

    [Test]
    public void VideoUsesKayproVirtualRowStride()
    {
        var machine = new KayproMachine();

        machine.Memory.Write(0x3000, (byte)'A');
        machine.Memory.Write(0x3080, (byte)'B');
        machine.Memory.Write(0x30A0, (byte)'C');

        var text = machine.Video.GetText();
        Assert.That(text[0], Is.EqualTo('A'));
        Assert.That(text[80], Is.EqualTo('B'));
        Assert.That(text[80 + 32], Is.EqualTo('C'));
    }

    [Test]
    public void PortsUseKayproMapAndKeyboardTravelsThroughSio()
    {
        var machine = new KayproMachine();
        machine.Bus.WritePort(0x07, 0x03); // select SIO channel B WR3
        machine.Bus.WritePort(0x07, 0xC1); // receiver enabled, 8 data bits
        machine.FeedKeyboardByte((byte)'K');

        machine.Bus.Tick(100_000);
        Assert.That(machine.Bus.ReadPort(0x07) & 0x01, Is.EqualTo(1));
        Assert.That(machine.Bus.ReadPort(0x05), Is.EqualTo((byte)'K'));
        Assert.That(machine.Bus.ReadPort(0x07) & 0x01, Is.EqualTo(0));
        Assert.That(machine.Bus.ReadPort(0x10), Is.EqualTo(0x80));

        machine.Bus.WritePort(0x0A, 0x0F); // PIO-G Port A control: Mode 0 output
        machine.Bus.WritePort(0x08, 0x55);
        Assert.That(machine.Bus.ReadPort(0x08), Is.EqualTo(0x55));
    }

    [Test]
    public void KayproSioWiringOwnsOnlyKayproPortMapAndChannelBKeyboardPolicy()
    {
        var wiring = new KayproSioWiring();
        wiring.Write(KayproSioWiring.ChannelBControl, 0x03);
        wiring.Write(KayproSioWiring.ChannelBControl, 0xC1);
        wiring.EnqueueKeyboardByte((byte)'Q');
        wiring.Tick(100_000);

        Assert.That(wiring.Read(KayproSioWiring.ChannelBControl) & 0x01, Is.EqualTo(1));
        Assert.That(wiring.Read(KayproSioWiring.ChannelBData), Is.EqualTo((byte)'Q'));
    }

    [Test]
    public void KayproRoutesSioInterruptVectorAndReleasesItOnReti()
    {
        var machine = new KayproMachine();
        machine.Bus.WritePort(0x07, 0x02); // select SIO channel B WR2
        machine.Bus.WritePort(0x07, 0xA0);
        machine.Bus.WritePort(0x07, 0x03); // select SIO channel B WR3
        machine.Bus.WritePort(0x07, 0xC1);
        machine.Bus.WritePort(0x07, 0x01); // select SIO channel B WR1
        machine.Bus.WritePort(0x07, 0x18); // receiver interrupt on all characters
        machine.FeedKeyboardByte((byte)'I');
        machine.Bus.Tick(100_000);

        Assert.That(machine.Bus.InterruptLines.IntAsserted, Is.True);
        Assert.That(machine.Bus.AcknowledgeInterrupt(), Is.EqualTo(0xA0));
        Assert.That(machine.Bus.InterruptLines.IntAsserted, Is.False);
        Assert.That(machine.Bus.Sio.InterruptInService, Is.True);

        machine.Cpu.Registers.SP = 0x4000;
        machine.Memory.Write(0x4000, 0x34);
        machine.Memory.Write(0x4001, 0x12);
        machine.Memory.Write(0x0100, 0xED);
        machine.Memory.Write(0x0101, 0x4D); // RETI
        machine.Bus.WritePort(KayproBus.SystemPort, 0x00); // execute the RAM test stub
        machine.Cpu.Registers.PC = 0x0100;
        machine.StepInstruction();
        Assert.That(machine.Bus.Sio.InterruptInService, Is.False);
    }

    [Test]
    public void KayproPioInterruptUsesBusAcknowledgeAndCpuReti()
    {
        var machine = new KayproMachine();
        machine.Bus.Pio.PioG.WritePort(2, 0x20);
        machine.Bus.Pio.PioG.WritePort(2, 0x4F);
        machine.Bus.Pio.PioG.WritePort(2, 0x87);
        machine.Bus.Pio.PioG.DriveInput(Z80PioDevice.PortA, 0xAA);
        machine.Bus.Tick(0);

        Assert.That(machine.Bus.InterruptLines.IntAsserted, Is.True);
        Assert.That(machine.Bus.AcknowledgeInterrupt(), Is.EqualTo(0x20));
        Assert.That(machine.Bus.Pio.PioG.InterruptInService, Is.True);

        machine.Memory.Write(0x1000, 0xED);
        machine.Memory.Write(0x1001, 0x4D);
        machine.Cpu.Registers.PC = 0x1000;
        machine.StepInstruction();

        Assert.That(machine.Bus.Pio.PioG.InterruptInService, Is.False);
    }

    [Test]
    public void RawDiskImageUsesKayproGeometry()
    {
        var data = new byte[KayproDiskImage.ImageSize];
        data[(39 * 10 + 10 - 1) * 512] = 0x5A;
        var image = new KayproDiskImage(data);
        var sector = new byte[512];

        Assert.That(image.TryReadSector(39, 10, sector), Is.True);
        Assert.That(sector[0], Is.EqualTo(0x5A));
        Assert.That(image.TryReadSector(40, 1, sector), Is.False);
    }

    [Test]
    public void Snapshot_RoundTripsOnANewMachineWithVideoPioSioAndFdcState()
    {
        var rom = Enumerable.Repeat((byte)0xA5, 0x800).ToArray();
        var diskBytes = new byte[KayproDiskImage.ImageSize];
        var source = new KayproMachine();
        source.LoadMonitorRom(rom);
        source.InsertDisk(0, new KayproDiskImage(diskBytes));
        source.Memory.Write(0x4000, 0xD6);
        source.Memory.Write(KayproBus.VideoBase, (byte)'K');
        source.Bus.WritePort(KayproBus.SystemPort, 0x45);
        source.Bus.Fdc.Track = 7;
        source.Bus.Fdc.Sector = 3;
        source.Bus.FdcWiring.BeginWait();
        source.FeedKeyboardByte((byte)'Q');
        source.Bus.Tick(100);
        var snapshot = source.CaptureState();

        var restored = new KayproMachine();
        restored.LoadMonitorRom(rom);
        restored.InsertDisk(0, new KayproDiskImage(diskBytes));
        restored.Memory.Write(0x4000, 0x11);
        restored.Memory.Write(KayproBus.VideoBase, (byte)'X');
        restored.RestoreState(snapshot);

        Assert.That(restored.Memory.Read(0x4000), Is.EqualTo(0xD6));
        Assert.That(restored.Video.GetText()[0], Is.EqualTo('K'));
        Assert.That(restored.Bus.Fdc.Track, Is.EqualTo(7));
        Assert.That(restored.Bus.Fdc.Sector, Is.EqualTo(3));
        Assert.That(restored.Bus.FdcWiring.SystemPortValue, Is.EqualTo(0x45));
        Assert.That(restored.Bus.FdcWiring.SelectedDrive, Is.EqualTo(0));
        Assert.That(restored.Bus.FdcWiring.WaitAsserted, Is.True);
    }

    [Test]
    public void Snapshot_RoundTripsWithTheSameMountedDiskImage()
    {
        var diskBytes = Enumerable.Repeat((byte)0x3C, KayproDiskImage.ImageSize).ToArray();
        var source = new KayproMachine();
        source.InsertDisk(1, new KayproDiskImage(diskBytes));
        source.Bus.Fdc.Track = 12;
        var snapshot = source.CaptureState();

        var restored = new KayproMachine();
        restored.InsertDisk(1, new KayproDiskImage(diskBytes));
        restored.RestoreState(snapshot);

        Assert.That(restored.Bus.Fdc.Track, Is.EqualTo(12));
        Assert.That(restored.Bus.FdcNmiPulseCount, Is.EqualTo(0));
    }
}
