using PetEmulator.Kaypro;
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
    public void PortsUseKayproMapAndKeyboardTravelsThroughSio()
    {
        var machine = new KayproMachine();
        machine.FeedKeyboardByte((byte)'K');

        Assert.That(machine.Bus.ReadPort(0x07) & 0x01, Is.EqualTo(1));
        Assert.That(machine.Bus.ReadPort(0x05), Is.EqualTo((byte)'K'));
        Assert.That(machine.Bus.ReadPort(0x07) & 0x01, Is.EqualTo(0));
        Assert.That(machine.Bus.ReadPort(0x10), Is.EqualTo(0x80));

        machine.Bus.WritePort(0x08, 0x55);
        Assert.That(machine.Bus.ReadPort(0x08), Is.EqualTo(0x55));
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
}
