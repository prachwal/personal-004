using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Kaypro;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproPioWiringTests
{
    [Test]
    public void MapsIndependentPioGAndPioSWindows()
    {
        var wiring = new KayproPioWiring();
        wiring.Write(KayproPioWiring.PioGBasePort + 2, 0x0F);
        wiring.Write(KayproPioWiring.PioGBasePort, 0x55);
        wiring.Write(KayproPioWiring.PioSBasePort, 0x00);

        Assert.That(wiring.Read(KayproPioWiring.PioGBasePort), Is.EqualTo(0x55));
        Assert.That(wiring.Read(KayproPioWiring.PioSBasePort), Is.EqualTo(0x00));
        Assert.That(wiring.Read(0x20), Is.EqualTo(0xFF));
    }

    [Test]
    public void SystemPioOutputUpdatesMachineBankAndDriveSignalsInOrder()
    {
        var bus = new KayproBus();

        // Bit pattern 0x01 selects drive A per the real 81-149C ROM's board-latch convention
        // (see KayproFdcWiring's own class doc comment: "01=A and 02=B") - 0x00 selects no drive
        // at all (matches KayproFdcWiring.WriteSystemPort's else branch), so it could never make
        // Fdc.DriveSelect become 1 the way this test originally (and incorrectly) wrote it.
        bus.WritePort(KayproBus.SystemPort, 0x01);

        Assert.That(bus.SystemPortValue, Is.EqualTo(0x01));
        Assert.That(bus.RomEnabled, Is.False);
        Assert.That(bus.Fdc.DriveSelect, Is.EqualTo(1));
        Assert.That(bus.ReadPort(KayproBus.SystemPort), Is.EqualTo(0x01));
    }

    [Test]
    public void PioGCanUseAnExternalPortWithoutKayproSemanticsInTheChip()
    {
        var wiring = new KayproPioWiring();
        var printer = new TestPort();
        wiring.ConnectPrinterPort(printer);
        wiring.Write(KayproPioWiring.PioGBasePort + 2, 0x0F);
        wiring.Write(KayproPioWiring.PioGBasePort, 0xA6);

        Assert.That(printer.Output, Is.EqualTo(new byte[] { 0xA6 }));
    }

    private sealed class TestPort : IZ80PioPort
    {
        public byte InputValue { get; private set; }
        public bool StrobeAsserted { get; private set; }
        public bool Ready { get; private set; } = true;
        public List<byte> Output { get; } = [];
        public event Action<byte>? InputChanged;
        public event Action<bool>? StrobeChanged;
        public event Action<byte>? OutputChanged;
        public event Action<bool>? ReadyChanged;

        public void DriveInput(byte value) { InputValue = value; InputChanged?.Invoke(value); }
        public void SetStrobe(bool asserted) { StrobeAsserted = asserted; StrobeChanged?.Invoke(asserted); }
        public void WriteOutput(byte value) { Output.Add(value); OutputChanged?.Invoke(value); }
        public void SetReady(bool asserted) { Ready = asserted; ReadyChanged?.Invoke(asserted); }
    }
}
