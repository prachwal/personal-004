using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Kaypro;

namespace PetEmulator.Kaypro.Tests;

public sealed class KayproPioEndToEndTests
{
    [Test]
    public void Z80ProgramTransfersAByteThroughKayproPioGToPeripheral()
    {
        var machine = new KayproMachine();
        var printer = new TestPort();
        machine.Bus.Pio.ConnectPrinterPort(printer);

        var program = new byte[] { 0x3E, 0x0F, 0xD3, 0x0A, 0x3E, 0xA5, 0xD3, 0x08 };
        for (var index = 0; index < program.Length; index++)
            machine.Memory.Write((ushort)(0x1000 + index), program[index]);
        machine.Cpu.Registers.PC = 0x1000;

        for (var index = 0; index < 4; index++)
            machine.StepInstruction();

        Assert.That(printer.Output, Is.EqualTo(new byte[] { 0xA5 }));
        Assert.That(machine.Bus.ReadPort(KayproPioWiring.PioGBasePort), Is.EqualTo(0xA5));
    }

    private sealed class TestPort : IZ80PioPort
    {
        public byte InputValue => 0;
        public bool StrobeAsserted => false;
        public bool Ready => true;
        public List<byte> Output { get; } = [];
        public event Action<byte>? InputChanged;
        public event Action<bool>? StrobeChanged;
        public event Action<byte>? OutputChanged;
        public event Action<bool>? ReadyChanged;

        public void DriveInput(byte value) => InputChanged?.Invoke(value);
        public void SetStrobe(bool asserted) => StrobeChanged?.Invoke(asserted);
        public void WriteOutput(byte value) { Output.Add(value); OutputChanged?.Invoke(value); }
        public void SetReady(bool asserted) => ReadyChanged?.Invoke(asserted);
    }
}
