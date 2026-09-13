using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80PioMode01Tests
{
    [Test]
    public void Mode0HoldsOutputUntilPeripheralIsReadyThenRaisesOutputInterrupt()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort();
        pio.AttachPort(Z80PioDevice.PortA, port);
        SelectMode(pio, Z80PioDevice.PortA, Z80PioMode.Output);
        EnableInterrupts(pio, Z80PioDevice.PortA);

        port.SetReady(false);
        WriteData(pio, Z80PioDevice.PortA, 0x55);

        port.Output.Should().BeEmpty();
        pio.CaptureState().PortA.OutputPending.Should().BeTrue();
        pio.InterruptRequested.Should().BeFalse();

        port.SetReady(true);

        port.Output.Should().ContainSingle().Which.Should().Be(0x55);
        pio.CaptureState().PortA.OutputPending.Should().BeFalse();
        pio.InterruptRequested.Should().BeTrue();
        pio.TryAcknowledgeInterrupt(out _).Should().BeTrue();
    }

    [Test]
    public void Mode0UsesTheLatestLatchedValueWhenSeveralWritesWaitForReady()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort();
        pio.AttachPort(Z80PioDevice.PortA, port);
        SelectMode(pio, Z80PioDevice.PortA, Z80PioMode.Output);

        WriteData(pio, Z80PioDevice.PortA, 0x11);
        WriteData(pio, Z80PioDevice.PortA, 0x22);
        port.SetReady(true);

        port.Output.Should().ContainSingle().Which.Should().Be(0x22);
    }

    [Test]
    public void Mode1AcceptsInputOnlyOnStrobeAndReadRearmsTheLatch()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort();
        pio.AttachPort(Z80PioDevice.PortB, port);
        SelectMode(pio, Z80PioDevice.PortB, Z80PioMode.Input);
        EnableInterrupts(pio, Z80PioDevice.PortB);

        port.DriveInput(0x00);
        pio.CaptureState().PortB.InputAvailable.Should().BeFalse();
        port.SetStrobe(true);

        pio.CaptureState().PortB.InputAvailable.Should().BeTrue();
        pio.InterruptRequested.Should().BeTrue();
        pio.ReadPort(0x01).Should().Be(0x00);
        pio.CaptureState().PortB.InputAvailable.Should().BeFalse();
    }

    [Test]
    public void Mode1DoesNotOverwriteFullInputLatchAndReportsOverrun()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort();
        pio.AttachPort(Z80PioDevice.PortB, port);
        SelectMode(pio, Z80PioDevice.PortB, Z80PioMode.Input);

        port.DriveInput(0x12);
        port.SetStrobe(true);
        port.SetStrobe(false);
        port.DriveInput(0x34);
        port.SetStrobe(true);

        pio.ReadPort(0x01).Should().Be(0x12);
        pio.CaptureState().PortB.InputOverrun.Should().BeTrue();
    }

    private static void SelectMode(Z80PioDevice pio, int port, Z80PioMode mode)
    {
        var control = (ushort)(port == Z80PioDevice.PortA ? 0x02 : 0x03);
        pio.WritePort(control, (byte)(((byte)mode << 6) | 0x0F));
        if (mode == Z80PioMode.BitControl)
            pio.WritePort(control, 0xFF);
    }

    private static void EnableInterrupts(Z80PioDevice pio, int port)
    {
        var control = (ushort)(port == Z80PioDevice.PortA ? 0x02 : 0x03);
        pio.WritePort(control, 0x87);
    }

    private static void WriteData(Z80PioDevice pio, int port, byte value)
    {
        pio.WritePort((ushort)port, value);
    }

    private sealed class TestPioPort : IZ80PioPort
    {
        private byte _inputValue;
        private bool _strobeAsserted;
        private bool _ready;

        public byte InputValue => _inputValue;
        public bool StrobeAsserted => _strobeAsserted;
        public bool Ready => _ready;
        public List<byte> Output { get; } = [];
        public event Action<byte>? InputChanged;
        public event Action<bool>? StrobeChanged;
        public event Action<byte>? OutputChanged;
        public event Action<bool>? ReadyChanged;

        public void DriveInput(byte value)
        {
            _inputValue = value;
            InputChanged?.Invoke(value);
        }

        public void SetStrobe(bool asserted)
        {
            _strobeAsserted = asserted;
            StrobeChanged?.Invoke(asserted);
        }

        public void WriteOutput(byte value)
        {
            Output.Add(value);
            OutputChanged?.Invoke(value);
        }

        public void SetReady(bool asserted)
        {
            _ready = asserted;
            ReadyChanged?.Invoke(asserted);
        }
    }
}
