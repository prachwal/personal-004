using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80PioSignalAdapterTests
{
    [Test]
    public void ActiveLowAdapterExposesLogicalLevelsAndEncodesWrites()
    {
        var physical = new TestPort { Ready = false };
        var adapter = new Z80PioSignalAdapter(physical, Z80PioSignalPolarity.ActiveLow);

        adapter.Ready.Should().BeTrue();
        adapter.SetReady(false);
        physical.Ready.Should().BeTrue();
        adapter.SetStrobe(true);
        physical.StrobeAsserted.Should().BeFalse();
    }

    [Test]
    public void StrobeAcceptsOnlyTheAssertionEdge()
    {
        var pio = new Z80PioDevice();
        var port = new TestPort();
        pio.AttachPort(Z80PioDevice.PortB, port);
        pio.WritePort(3, 0x4F); // input mode
        port.DriveInput(0x12);
        pio.SetStrobe(Z80PioDevice.PortB, true);
        pio.SetStrobe(Z80PioDevice.PortB, true);
        pio.CaptureState().PortB.InputOverrun.Should().BeFalse();
        pio.ReadPort(1).Should().Be(0x12);
    }

    private sealed class TestPort : IZ80PioPort
    {
        public byte InputValue { get; private set; }
        public bool StrobeAsserted { get; private set; }
        public bool Ready { get; set; } = true;
        public event Action<byte>? InputChanged;
        public event Action<bool>? StrobeChanged;
        public event Action<byte>? OutputChanged;
        public event Action<bool>? ReadyChanged;

        public void DriveInput(byte value) { InputValue = value; InputChanged?.Invoke(value); }
        public void SetStrobe(bool asserted) { StrobeAsserted = asserted; StrobeChanged?.Invoke(asserted); }
        public void WriteOutput(byte value) { OutputChanged?.Invoke(value); }
        public void SetReady(bool asserted) { Ready = asserted; ReadyChanged?.Invoke(asserted); }
    }
}
