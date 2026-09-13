using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80PioMode23Tests
{
    [Test]
    public void Mode2PortAChangesDirectionWithoutLosingLatchedData()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort { Ready = true };
        pio.AttachPort(Z80PioDevice.PortA, port);
        SelectMode(pio, Z80PioDevice.PortA, Z80PioMode.Bidirectional);

        pio.SetBidirectionalDirection(Z80PioDevice.PortA, input: true);
        port.DriveInput(0x3C);
        port.SetStrobe(true);
        pio.ReadPort(0x00).Should().Be(0x3C);

        pio.SetBidirectionalDirection(Z80PioDevice.PortA, input: false);
        pio.WritePort(0x00, 0xA5);
        port.Output.Should().ContainSingle().Which.Should().Be(0xA5);
        pio.ReadPort(0x00).Should().Be(0xA5);

        pio.SetBidirectionalDirection(Z80PioDevice.PortA, input: true);
        port.DriveInput(0x5A);
        port.SetStrobe(true);
        pio.ReadPort(0x00).Should().Be(0x5A);
    }

    [Test]
    public void Mode2IsRestrictedToPortAAndRejectsBirectionalPortBProgramming()
    {
        var pio = new Z80PioDevice();

        SelectMode(pio, Z80PioDevice.PortB, Z80PioMode.Bidirectional);

        pio.CaptureState().PortB.Mode.Should().Be(Z80PioMode.Input);
        pio.SetBidirectionalDirection(Z80PioDevice.PortB, input: false);
        pio.CaptureState().PortB.BidirectionalInput.Should().BeTrue();
    }

    [Test]
    public void Mode2DoesNotDriveOutputWhilePortAIsInInputDirection()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort { Ready = true };
        pio.AttachPort(Z80PioDevice.PortA, port);
        SelectMode(pio, Z80PioDevice.PortA, Z80PioMode.Bidirectional);

        pio.SetBidirectionalDirection(Z80PioDevice.PortA, input: true);
        pio.WritePort(0x00, 0x44);
        port.Output.Should().BeEmpty();

        port.DriveInput(0x11);
        port.SetStrobe(true);
        port.DriveInput(0x22);
        port.SetStrobe(true);
        pio.ReadPort(0x00).Should().Be(0x11);
        pio.CaptureState().PortA.InputOverrun.Should().BeTrue();
    }

    [Test]
    public void Mode3ReadsInputBitsAndOutputBitsAsOneMixedValue()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort();
        pio.AttachPort(Z80PioDevice.PortA, port);
        SelectMode(pio, Z80PioDevice.PortA, Z80PioMode.BitControl, directionMask: 0x0F);

        port.DriveInput(0x0B);
        pio.WritePort(0x00, 0xA0);

        pio.ReadPort(0x00).Should().Be(0xAB);
        port.Output.Should().ContainSingle().Which.Should().Be(0xAB);
    }

    [Test]
    public void Mode3WritesOnlyOutputBitsAndPreservesInputBits()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort();
        pio.AttachPort(Z80PioDevice.PortB, port);
        SelectMode(pio, Z80PioDevice.PortB, Z80PioMode.BitControl, directionMask: 0x0F);

        port.DriveInput(0x05);
        pio.WritePort(0x01, 0xF0);
        pio.WritePort(0x01, 0x00);

        pio.ReadPort(0x01).Should().Be(0x05);
        pio.CaptureState().PortB.OutputLatch.Should().Be(0x00);
    }

    [Test]
    public void Mode3SupportsOrHighAndAndLowInterruptConditions()
    {
        var pio = new Z80PioDevice();
        var port = new TestPioPort();
        pio.AttachPort(Z80PioDevice.PortA, port);
        SelectMode(pio, Z80PioDevice.PortA, Z80PioMode.BitControl, directionMask: 0xFF);

        WriteControl(pio, Z80PioDevice.PortA, 0xB7); // enable, OR, high, mask follows
        WriteControl(pio, Z80PioDevice.PortA, 0x0C);
        port.DriveInput(0x08);
        pio.InterruptRequested.Should().BeTrue();
        pio.TryAcknowledgeInterrupt(out _).Should().BeTrue();
        pio.NotifyReti();

        pio.Reset();
        SelectMode(pio, Z80PioDevice.PortA, Z80PioMode.BitControl, directionMask: 0xFF);
        WriteControl(pio, Z80PioDevice.PortA, 0xD7); // enable, AND, low, mask follows
        WriteControl(pio, Z80PioDevice.PortA, 0x03);
        port.DriveInput(0x00);
        pio.InterruptRequested.Should().BeTrue();
    }

    private static void SelectMode(
        Z80PioDevice pio,
        int port,
        Z80PioMode mode,
        byte? directionMask = null)
    {
        var control = ControlPort(port);
        pio.WritePort(control, (byte)(((byte)mode << 6) | 0x0F));
        if (mode == Z80PioMode.BitControl)
            pio.WritePort(control, directionMask ?? 0xFF);
    }

    private static void WriteControl(Z80PioDevice pio, int port, byte value) =>
        pio.WritePort(ControlPort(port), value);

    private static ushort ControlPort(int port) =>
        (ushort)(port == Z80PioDevice.PortA ? 0x02 : 0x03);

    private sealed class TestPioPort : IZ80PioPort
    {
        private byte _inputValue;
        private bool _strobeAsserted;
        private bool _ready;

        public byte InputValue => _inputValue;
        public bool StrobeAsserted => _strobeAsserted;
        public bool Ready
        {
            get => _ready;
            set => _ready = value;
        }

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
